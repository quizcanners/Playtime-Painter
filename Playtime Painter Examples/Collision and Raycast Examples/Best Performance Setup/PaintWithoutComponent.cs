using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace PlaytimePainter.Examples
{

    public class PaintWithoutComponent : MonoBehaviour, IPEGI
    {

        public BrushConfig brush = new BrushConfig();
        public int shoots = 1;
        public float spread;


        private void Update()
        {
            if (Input.GetMouseButton(0))
                Paint();
        }

        readonly List<ImageMeta> _texturesNeedUpdate = new List<ImageMeta>();

        private void Paint() {

            RaycastHit hit;

            for (var i = 0; i < shoots; i++)
                if (Physics.Raycast(new Ray(transform.position, transform.forward + transform.right * Random.Range(-spread, spread) + transform.up * Random.Range(-spread, spread)), out hit)) {

                    var receivers = hit.transform.GetComponentsInParent<PaintingReceiver>();
                    
                    if (receivers.Length <= 0) continue;
                    
                    int subMesh;
                    var receiver = receivers[0];

                    // IF FEW SubMeshes
                    if (hit.collider.GetType() == typeof(MeshCollider))
                    {

                        subMesh = ((MeshCollider)hit.collider).sharedMesh.GetSubMeshNumber(hit.triangleIndex);

                        if (receivers.Length > 1)
                        {

                            var mats = receiver.Renderer.materials;

                            var material = mats[subMesh % mats.Length];

                            receiver = receivers.FirstOrDefault(r => r.Material == material);
                        }
                    }
                    else
                        subMesh = receiver.materialIndex;

                    // ACTUAL PAINTING

                    if (!receiver) continue;
                    
                    var tex = receiver.GetTexture();
                    
                    if (!tex) continue;
                    
                    var rendTex = (receiver.texture.GetType() == typeof(RenderTexture)) ? (RenderTexture)receiver.texture : null;

                    // WORLD SPACE BRUSH

                    if (rendTex)
                    {
                        var st = new StrokeVector(hit.point)
                        {
                            unRepeatedUv = hit.collider.GetType() == typeof(MeshCollider) ?
                                (receiver.useTexcoord2 ? hit.textureCoord2 : hit.textureCoord).Floor() : receiver.meshUvOffset,
                                        

                        };

                        switch (receiver.type)
                        {
                            case PaintingReceiver.RendererType.Skinned when receiver.skinnedMeshRenderer:
                                BrushTypeSphere.Paint(rendTex, receiver.gameObject, receiver.skinnedMeshRenderer, brush, st, subMesh);
                                break;
                            case PaintingReceiver.RendererType.Regular when receiver.meshFilter:
                            {
                                var mat = receiver.Material;
                                if (mat && mat.IsAtlased())
                                    BrushTypeSphere.PaintAtlased (rendTex, receiver.gameObject,
                                        receiver.originalMesh ? receiver.originalMesh : receiver.meshFilter.sharedMesh, brush, st, new List<int> { subMesh }, (int)mat.GetFloat(PainterDataAndConfig.ATLASED_TEXTURES));
                                else
                                    BrushTypeSphere.Paint(rendTex, receiver.gameObject,
                                        receiver.originalMesh ? receiver.originalMesh : receiver.meshFilter.sharedMesh, brush, st, new List<int> { subMesh } );
                                break;
                            }
                        }
                    }
                    // TEXTURE SPACE BRUSH
                    else if (receiver.texture is Texture2D) {

                        if (hit.collider.GetType() != typeof(MeshCollider))
                            Debug.Log("Can't get UV coordinates from a Non-Mesh Collider");

                        BlitFunctions.Paint(receiver.useTexcoord2 ? hit.textureCoord2 : hit.textureCoord, 1, (Texture2D)receiver.texture, Vector2.zero, Vector2.one, brush, null);
                        var id = receiver.texture.GetImgData();
                        _texturesNeedUpdate.AddIfNew(id);

                    }
                    else Debug.Log(receiver.gameObject.name + " doesn't have any combination of paintable things setup on his PainterReciver.");
                }
    
        }


        private void LateUpdate()
        {
            foreach (var t in _texturesNeedUpdate)
                t.SetAndApply(); // True for Mipmaps. But best to disable mipmaps on textures or set this to false 

            _texturesNeedUpdate.Clear();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {

            var tf = transform;

            var pos = tf.position;

            var f = tf.forward;
            
            var ray = new Ray(pos, f);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit)) {

                var painter = hit.transform.GetComponentInParent<PlaytimePainter>();

                Gizmos.color = !painter ? Color.red : Color.green;
                Gizmos.DrawLine(pos, hit.point);

            }
            else   {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pos, pos + f);
            }
        }
#endif
#if PEGI

        bool Documentation()
        {
            "I can paint on objects with PaintingReceiver script and:".nl();
            "Mesh Collider + any Texture".nl();
            "Skinned Mesh + any Collider + Render Texture".nl();
            "Also its better to use textures without mipMaps".nl();
            "Render Texture Painting will fail if material has tiling or offset".nl();
            "Editing will be symmetrical if mesh is symmetrical".nl();
            "Brush type should be Sphere".nl();

            ("I was going to make only one component painting method at first, but later it became clear that it's best to have this set up on both: receiving and shooting ends." +
             "This doesn't need PlaytimePainter component but still needs Painting Receiver script").writeHint();

            return false;
        }

        
        public bool Inspect()
        {
            var changed = false;

            pegi.fullWindowDocumentationClickOpen(Documentation);

            "Bullets:".edit(50, ref shoots, 1, 50).nl(ref changed);
            "Spread:".edit(50, ref spread, 0f, 1f).nl(ref changed);

            if ("Fire!".Click().nl())
                Paint();

            brush.Targets_PEGI().nl(ref changed);
            brush.Mode_Type_PEGI().nl(ref changed);
            brush.ColorSliders().nl(ref changed);

            if (!brush.PaintingRGB)
                "Enable RGB, disable A to use faster Brush Shader (if painting to RenderTexture).".writeHint();
            
            return changed;
        }
#endif
    }
}