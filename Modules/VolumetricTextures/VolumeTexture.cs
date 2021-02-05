using System;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using PlaytimePainter.ComponentModules;
using QuizCannersUtilities;
using UnityEngine;
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

        private int changePositionOnOffset = 32;

        public int hSlices = 1;
        public float size = 1;

        [SerializeField] public Texture _texture;

        public Texture Texture
        {
            get { return _texture; }
            set
            {
                _texture = value;
                TextureInShaderProperty.SetGlobal(ImageMeta.CurrentTexture());
            }
        }

        public TextureMeta ImageMeta
        {
            get { return _texture.GetTextureMeta(); }
            set { _texture = value?.ExclusiveTexture(); }
        }

        private ShaderProperty.TextureValue _textureInShaderr;
        private ShaderProperty.VectorValue _slicesInShader;
        private ShaderProperty.VectorValue _positionNsizeInShader;

        public ShaderProperty.TextureValue TextureInShaderProperty
        {
            get
            {
                if (_textureInShaderr != null)
                    return _textureInShaderr;

                _textureInShaderr = new ShaderProperty.TextureValue(name);

                return _textureInShaderr;
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
            get { return name; }
            set
            {
                name = value;
                _textureInShaderr = null;
            }
        }

        public int Height => hSlices * hSlices;

        public int Width => (ImageMeta?.width ?? (TexturesPool.inst ? TexturesPool.inst.width : _tmpWidth)) / hSlices;

        public Vector4 PosSize4Shader {
            get
            {
                changePositionOnOffset = Mathf.Max(1, changePositionOnOffset);

                var scaledChunks = changePositionOnOffset * size;

                var pos = transform.position / scaledChunks;
                pos = Vector3Int.FloorToInt(pos);
                pos *= scaledChunks;
                    
                return pos.ToVector4(1f / size);
            }
        }

        public Vector4 Slices4Shader {
            get {
                float w = ((ImageMeta?.width ?? (TexturesPool.inst ? TexturesPool.inst.width : _tmpWidth)) - hSlices * 2) / hSlices;
                return new Vector4(hSlices, w * 0.5f, 1f / w, 1f / hSlices);
            }
        }

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
            //ImageMeta.Rename(name + hSlices);
        }

        protected virtual void OnBecomeActive()
        {

        }

        #region Inspect

        private bool _searchedForPainter = false;
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
            
            pegi.toggleDefaultInspector(this);

            if (_textureInShaderr != null)
            {
                if (icon.Delete.Click())
                    _textureInShaderr = null;

                _textureInShaderr.GetNameForInspector().nl();
            }

            if (_searchedForPainter)
            {
                _searchedForPainter = true;
                _painter = GetComponent<PlaytimePainter>();
            }

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
                if (_painter.gameObject != gameObject)
                {
                    "Painter is on a different Game Object".writeWarning();
                    if ("Disconnect".Click())
                        _painter = null;
                }

                var mod = _painter.GetModule<VolumeTextureComponentModule>();
                if (!mod.volumeTexture && "Assign volume texture to painter".Click().nl())
                    mod.volumeTexture = this;

                if (mod.volumeTexture)
                {
                    if (mod.volumeTexture == this)
                        "This volume texture is attached".write();

                    if (icon.UnLinked.Click("Unlink"))
                        mod.volumeTexture = null;
                }

                pegi.nl();
            }

            pegi.FullWindow.DocumentationClickOpen(VolumeDocumentation);

            if (inspectedElement == -1)
            {
                "Also set for Global shader parameters".toggleIcon(ref setForGlobal).nl(ref changed);
                
                "Position chunks".edit(ref changePositionOnOffset).changes(ref changed);

                pegi.FullWindow
                    .DocumentationClickOpen("For Baking optimisations, how often position is changed");

                pegi.nl();
            }

            if ("Volume Texture".enter(ref inspectedElement, 1).nl())
            {

                var n = NameForPEGI;
                if ("Name".editDelayed(50, ref n).nl(ref changed))
                {
                    NameForPEGI = n;
                }

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
                            TexturesPool.ForceInstance.width = _tmpWidth;
                        }
                    }
                    else
                    {
                        if ("Get From Pool".Click().nl(ref changed))
                            ImageMeta = TexturesPool.inst.GetTexture2D().GetTextureMeta();

                        TexturesPool.inst.Nested_Inspect().nl();
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

            if (changed || icon.Refresh.Click("Update Materials"))
                UpdateMaterials();

            pegi.nl();

            return changed;
        }
        
        #endregion

        public virtual void UpdateMaterials() {
            
            materials.SetVolumeTexture(this);

            if (!setForGlobal) return;

            if (PositionAndScaleProperty.GlobalValue != PosSize4Shader)
            {
               // Debug.Log("Updating pos n shader during move " +Time.frameCount );
                PositionAndScaleProperty.SetGlobal(PosSize4Shader);
            }

            SlicesShadeProperty.SetGlobal(Slices4Shader);
            TextureInShaderProperty.SetGlobal(ImageMeta.CurrentTexture());
            
        }

        private Vector3 _previousWorldPosition = Vector3.zero;

        private Texture _previousTarget;

        public virtual void LateUpdate()
        {

            var currentTexture = ImageMeta.CurrentTexture();

            if (currentTexture != _previousTarget)
            {
                _previousTarget = currentTexture;

                UpdateMaterials();
            }

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
