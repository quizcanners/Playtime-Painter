using System.Collections.Generic;
using System.Linq;
using PlayerAndEditorGUI;
using PlaytimePainter.ComponentModules;
using QuizCannersUtilities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter.Examples
{
#pragma warning disable IDE0018 // Inline variable declaration

    public class PaintWithoutComponent : MonoBehaviour, IPEGI
    {

        public Brush brush = new Brush();
        private Stroke continiousStroke = new Stroke(); // For continious
        private PaintingReceiver previousTargetForContinious;
        public int shoots = 1;
        public bool continious;
        public float spread;

        private void Update()
        {
            if (Input.GetMouseButton(0))
                Paint();
            else if (continious)
                continiousStroke.OnMouseUnPressed();
        }

        private static readonly List<TextureMeta> _texturesNeedUpdate = new List<TextureMeta>();

        private void Paint()
        {

            RaycastHit hit;

            for (var i = 0; i < (continious ? 1 : shoots); i++)
                if (Physics.Raycast(new Ray(transform.position, transform.forward +
                                (continious ? Vector3.zero :
                                (transform.right * Random.Range(-spread, spread)
                                + transform.up * Random.Range(-spread, spread)))
                                )
                    , out hit))
                {


                    var receivers = hit.transform.GetComponentsInParent<PaintingReceiver>();

                    if (receivers.Length == 0) continue;

                    int subMesh;
                    var receiver = receivers[0];

                    #region Multiple Submeshes
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

                    #endregion



                    if (!receiver) continue;

                    var tex = receiver.GetTexture();

                    if (!tex) continue;

                    var rendTex = receiver.TryGetRenderTexture(); //(receiver.texture.GetType() == typeof(RenderTexture)) ? (RenderTexture)receiver.texture : null;

                    #region  WORLD SPACE BRUSH

                    if (continious)
                    {
                        if (previousTargetForContinious && (receiver != previousTargetForContinious))
                            continiousStroke.OnMouseUnPressed();

                        previousTargetForContinious = receiver;
                    }

                    if (rendTex)
                    {


                        var st = continious ? continiousStroke :
                            new Stroke(hit, receiver.useTexcoord2);

                        st.unRepeatedUv = hit.collider.GetType() == typeof(MeshCollider)
                            ? (receiver.useTexcoord2 ? hit.textureCoord2 : hit.textureCoord).Floor()
                            : receiver.meshUvOffset;

                        if (continious)
                            st.OnMousePressed(hit, receiver.useTexcoord2);



                        if (receiver.type == PaintingReceiver.RendererType.Skinned && receiver.skinnedMeshRenderer)
                            BrushTypes.Sphere.Paint(
                                receiver.TryMakePaintCommand(st, brush, subMesh));
                               // new PaintCommand.WorldSpace(st, rendTex.GetTextureMeta(), brush, receiver.skinnedMeshRenderer,
                                  //  subMesh, receiver.gameObject)
                       
                        else if (receiver.type == PaintingReceiver.RendererType.Regular && receiver.meshFilter)
                        {
                            if (brush.GetBrushType(false) == BrushTypes.Sphere.Inst)
                            {
                                var mat = receiver.Material;
                                if (mat && mat.IsAtlased())
                                    BrushTypes.Sphere.PaintAtlased(receiver.TryMakePaintCommand(st, brush, subMesh),

                                       /* rendTex, receiver.gameObject,
                                        receiver.originalMesh
                                            ? receiver.originalMesh
                                            : receiver.meshFilter.sharedMesh, brush, st, new List<int> { subMesh },*/
                                        (int)mat.GetFloat(PainterShaderVariables.ATLASED_TEXTURES)
                                            );
                                else
                                    BrushTypes.Sphere.Paint(
                                        receiver.TryMakePaintCommand(st, brush, subMesh));

                                        /*new PaintCommand.WorldSpace(st, rendTex.GetTextureMeta(), brush, receiver.originalMesh
                                            ? receiver.originalMesh
                                            : receiver.meshFilter.sharedMesh,
                                            subMesh,
                                            receiver.gameObject
                                            )*/
                                     /*   rendTex, receiver.gameObject,
                                        receiver.originalMesh
                                            ? receiver.originalMesh
                                            : receiver.meshFilter.sharedMesh, brush, st,
                                        new List<int> { subMesh });*/
                            }
                            else
                                BrushTypes.Normal.Paint(rendTex, brush, st);

                            break;
                        }

                    }
                    #endregion
                    #region TEXTURE SPACE BRUSH
                    else if (receiver.texture is Texture2D)
                    {

                        if (hit.collider.GetType() != typeof(MeshCollider))
                            Debug.Log("Can't get UV coordinates from a Non-Mesh Collider");

                        BlitFunctions.Paint(receiver.useTexcoord2 ? hit.textureCoord2 : hit.textureCoord, 1, (Texture2D)receiver.texture, Vector2.zero, Vector2.one, brush);
                        var id = receiver.texture.GetTextureMeta();
                        _texturesNeedUpdate.AddIfNew(id);

                    }
                    #endregion
                    else Debug.Log(receiver.gameObject.name + " doesn't have any combination of paintable things setup on his PainterReciver.");

                }

        }



        private void LateUpdate()
        {
            foreach (var t in _texturesNeedUpdate)
                t.SetAndApply(); // True for Mipmaps. But best to disable mipmaps on textures or set this to false 

            _texturesNeedUpdate.Clear();
        }

        #region Inspector
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {

            var tf = transform;

            var pos = tf.position;

            var f = tf.forward;

            var ray = new Ray(pos, f);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {

                var painter = hit.transform.GetComponentInParent<PlaytimePainter>();

                Gizmos.color = !painter ? Color.red : Color.green;
                Gizmos.DrawLine(pos, hit.point);

            }
            else
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pos, pos + f);
            }
        }
#endif

        bool Documentation()
        {
            "I can paint on objects with PaintingReceiver script and:".nl();
            "Mesh Collider + any Texture".nl();
            "Skinned Mesh + any Collider + Render Texture".nl();
            "Also its better to use textures without mipMaps".nl();
            "Render Texture Painting may have artifacts if material has tiling or offset".nl();
            "Editing will be symmetrical if mesh is symmetrical".nl();
            "Brush type should be Sphere".nl();

            return false;
        }

        public bool Inspect()
        {
            var changed = false;

            pegi.toggleDefaultInspector(this);

            pegi.FullWindowService.DocumentationClickOpen(Documentation);

            "Continious".toggleIcon(ref continious).nl();

            if (!continious)
            {
                "Bullets:".edit(50, ref shoots, 1, 50).nl(ref changed);
                "Spread:".edit(50, ref spread, 0f, 1f).nl(ref changed);
            }

            if ("Fire!".Click().nl())
                Paint();

            brush.Targets_PEGI().nl(ref changed);
            brush.Mode_Type_PEGI().nl(ref changed);
            brush.ColorSliders().nl(ref changed);

            if (brush.targetIsTex2D)
            {
                "Script expects Render Texture terget".writeWarning();
                pegi.nl();

                if ("Switch to Render Texture".Click())
                    brush.targetIsTex2D = false;
            }
            else if (brush.GetBrushType(false).GetType() != typeof(BrushTypes.Sphere))
            {
                "This component works best with Sphere Brush? also supports Normal Brush.".writeHint();
                //if ("Switch to Sphere Brush".Click())
                //  brush.SetBrushType(false, BrushTypes.Sphere.Inst);
            }


            if (!brush.PaintingRGB)
                "Enable RGB, disable A to use faster Brush Shader (if painting to RenderTexture).".writeHint();

            return changed;
        }
        #endregion
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PaintWithoutComponent))]
    public class PaintWithoutComponentEditor : PEGI_Inspector_Mono<PaintWithoutComponent> { }
#endif
}