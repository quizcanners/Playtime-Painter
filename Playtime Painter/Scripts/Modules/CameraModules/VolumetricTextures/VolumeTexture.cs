using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter {

    [ExecuteInEditMode]
    [Serializable]
    public class VolumeTexture : PainterSystemMono, IGotName
    {

        [SerializeField] public bool setForGlobal;

        public static List<VolumeTexture> all = new List<VolumeTexture>();

        private static int _tmpWidth = 1024;

        public int hSlices = 1;
        public float size = 1;

        [SerializeField] public Texture2D texture;

        public TextureMeta ImageMeta
        {
            get { return texture.GetTextureMeta(); }
            set { texture = value?.texture2D; }
        }

        private ShaderProperty.TextureValue _textureInShader;
        private ShaderProperty.VectorValue _slicesInShader;
        private ShaderProperty.VectorValue _positionNsizeInShader;

        public ShaderProperty.TextureValue TextureInShaderProperty
        {
            get
            {
                if (_textureInShader != null)
                    return _textureInShader;

                _textureInShader = new ShaderProperty.TextureValue(name);

                return _textureInShader;
            }
        }

        public ShaderProperty.VectorValue SlicesShadeProperty
        {
            get
            {
                if (_slicesInShader != null)
                    return _slicesInShader;

                _slicesInShader = new ShaderProperty.VectorValue(name + "VOLUME_H_SLICES");

                return _slicesInShader;
            }
        }

        public ShaderProperty.VectorValue PositionAndScaleProperty
        {
            get
            {
                if (_positionNsizeInShader != null)
                    return _positionNsizeInShader;

                _positionNsizeInShader = new ShaderProperty.VectorValue(name + "VOLUME_POSITION_N_SIZE");

                return _positionNsizeInShader;
            }
        }
    
        public List<Material> materials;

        public string NameForPEGI
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                _textureInShader = null;
            }
        }

        public int Height => hSlices * hSlices;

        public int Width => (ImageMeta?.width ?? (TexturesPool.inst ? TexturesPool.inst.width : _tmpWidth)) / hSlices;

        public Vector4 PosSize4Shader => transform.position.ToVector4(1f / size);

        public Vector4 Slices4Shader { get { float w = ((ImageMeta?.width ?? (TexturesPool.inst ? TexturesPool.inst.width : _tmpWidth)) - hSlices * 2) / hSlices;
            return new Vector4(hSlices, w * 0.5f, 1f / w, 1f / hSlices); } }

        public virtual bool NeedsToManageMaterials => true;

        public virtual void AddIfNew(PlaytimePainter p) => AddIfNew(p.Material);

        private bool AddIfNew(Material mat)
        {
            if (materials.Contains(mat)) return false;
            
            materials.Add(mat);
            
            if (NeedsToManageMaterials)
                UpdateMaterials();

            return true;
        }

        private void UpdateImageMeta()
        {
            if (ImageMeta == null)
                return;
            ImageMeta.isAVolumeTexture = true;
            ImageMeta.Rename(name + hSlices.ToString());
        }

        protected virtual void OnBecomeActive()
        {

        }

        #region Inspect

        private bool searchedForPainter = false;
        [SerializeField] private PlaytimePainter _painter;

        protected int inspectedElement = -1;

        protected int inspectedMaterial = -1;

        protected virtual bool VolumeDocumentation()
        {
            "Volumes are 2D Textures that are used as".writeBig();
            " 3D Textures ".ClickLink("https://docs.unity3d.com/Manual/class-Texture3D.html").nl();
               ("But 3D Textures are not supported on most mobile devices. That is why this trick with Texture2D is used " +
                " The texture is sampled using World Space Position. Currently I implemented it to use only one volume per scene." +
                " It will use global shader parameters to set all the values. This makes it easier to manage." +
                " But there is no reason why many volumes can't be used in a scene.").writeBig();



            return false;
        }
        
        public override bool Inspect()
        {
            var changed = false;

            if (searchedForPainter)
                _painter = GetComponent<PlaytimePainter>();

            if (!_painter)
            {
                "Painter [None]".write();
                if ("Search".Click())
                    _painter = GetComponent<PlaytimePainter>();

                if (icon.Add.Click())
                {
                    _painter = GetComponent<PlaytimePainter>();
                    if (!_painter)
                        _painter = gameObject.AddComponent<PlaytimePainter>();
                }

                pegi.nl();

            }
            else
            {
                var mod = _painter.GetModule<VolumeTextureManagement>();
                if (!mod.volumeTexture && "Assign volume texture to painter".Click())
                    mod.volumeTexture = this;
            }

            pegi.fullWindowDocumentationClickOpen(VolumeDocumentation);

            if (inspectedElement == -1)
            {
                "Also set for Global shader parameters".toggleIcon(ref setForGlobal, true).changes(ref changed);

                pegi.nl();
            }

            if ("Volume Texture".enter(ref inspectedElement, 1).nl())
            {

                var n = name;
                if ("Name".editDelayed(50, ref n).nl(ref changed))
                    name = n;

                if (setForGlobal)
                {
                    "FOR GLOBAL ONLY:".nl();

                    PositionAndScaleProperty.NameForDisplayPEGI().write_ForCopy().nl();

                    SlicesShadeProperty.NameForDisplayPEGI().write_ForCopy().nl();
                }

                var tex = ImageMeta.CurrentTexture();

                if (tex == null)
                    ImageMeta = null;

                if ("Texture".edit(60, ref tex).nl(ref changed))
                    ImageMeta = tex ? tex.GetTextureMeta() : null;

                "Volume Scale".edit(70, ref size).nl(ref changed);
                size = Mathf.Max(0.0001f, size);

                if (ImageMeta == null)
                {

                    if (!TexturesPool.inst)
                    {
                        pegi.nl();
                        "Texture Width".edit(90, ref _tmpWidth).changes(ref changed);

                        if ("Create Pool".Click().nl(ref changed))
                        {
                            _tmpWidth = Mathf.ClosestPowerOfTwo(Mathf.Clamp(_tmpWidth, 128, 2048));
                            TexturesPool.Inst.width = _tmpWidth;
                        }
                    }
                    else
                    {
                        if ("Get From Pool".Click().nl(ref changed))
                            ImageMeta = TexturesPool.inst.GetTexture2D().GetTextureMeta();

                        TexturesPool.Inst.Nested_Inspect().nl();
                    }
                

                }
                pegi.nl();

                "Slices:".edit("How texture will be sliced for height", 80, ref hSlices, 1, 8).nl(ref changed);

                if (changed)
                    UpdateImageMeta();

                var w = Width;
                ("Will result in X:" + w + " Z:" + w + " Y:" + Height + "volume").nl();
            }

            if ("Materials [{0}]".F(materials.Count).enter(ref inspectedElement, 2).nl_ifFoldedOut()) {

                "Materials".edit_List_UObj(ref materials, ref inspectedMaterial);

                if (inspectedMaterial == -1 && InspectedPainter)
                {
                    var pMat = InspectedPainter.Material;
                    if (pMat != null && materials.Contains(pMat) && "Remove This Material".Click().nl(ref changed))
                        materials.Remove(pMat);
                }
            }

            if (changed && icon.Refresh.Click("Update Materials"))
                UpdateMaterials();

            pegi.nl();

            return changed;
        }
        
        #endregion

        public virtual void UpdateMaterials() {
            
            materials.SetVolumeTexture(this);

            if (!setForGlobal) return;
            
            PositionAndScaleProperty.SetGlobal(PosSize4Shader);
            SlicesShadeProperty.SetGlobal(Slices4Shader);
            TextureInShaderProperty.SetGlobal(ImageMeta.CurrentTexture());
            
        }

        private Vector3 _previousWorldPosition = Vector3.zero;

        public virtual void Update()
        {
            if (_previousWorldPosition == transform.position)
                return;
            
            _previousWorldPosition = transform.position;
            
            UpdateMaterials();
        }

        public virtual void OnEnable()
        {
            if (materials == null)
                materials = new List<Material>();

            all.Add(this);

            UpdateMaterials();

            if (_painter)
                _painter.GetModule<VolumeTextureManagement>().volumeTexture = this;
        }

        public virtual void OnDisable()
        {
            if (all.Contains(this))
                all.Remove(this);
        }

        public virtual void OnDrawGizmosSelected()
        {
            if (ImageMeta == null) return;
            var center = transform.position;
            var w = Width;
            center.y += Height * 0.5f * size;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, new Vector3(w, Height, w) * size);
        }

        public virtual bool DrawGizmosOnPainter(PlaytimePainter painter) => false; 

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VolumeTexture))]
    public class VolumeTextureDrawer : PEGI_Inspector_Mono<VolumeTexture> { }
#endif

}
