using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using PlaytimePainter.ComponentModules;
using QuizCanners.Utils;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter {

    [ExecuteInEditMode]
    [Serializable]
    public class VolumeTexture : MonoBehaviour, IGotName, IPEGI
    {
        public static List<VolumeTexture> all = new List<VolumeTexture>();

        private static int _tmpWidth = 1024;

        private int changePositionOnOffset = 32;

        public int hSlices = 4;
        public float size = 1;

        public Texture _texture;

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

                var posSize = pos.ToVector4(1f / size);

                PositionAndScaleProperty.SetGlobal(posSize);

                return posSize;
            }
        }

        public Vector4 Slices4Shader {
            get {
                float w = ((ImageMeta?.width ?? (TexturesPool.inst ? TexturesPool.inst.width : _tmpWidth)) //- hSlices * 2
                    ) / hSlices;
                return new Vector4(hSlices, w * 0.5f, 1f / w, 1f / hSlices);
            }
        }

        private void UpdateImageMeta()
        {
            if (ImageMeta == null)
                return;
            ImageMeta.isAVolumeTexture = true;
        }

        #region Inspect

        [SerializeField] private PlaytimePainter _painter;
        private bool _searchedForPainter;
        protected int inspectedElement = -1;
        protected int inspectedMaterial = -1;

        protected virtual bool VolumeDocumentation()
        {
            "In this context Volumes are Textures2D and RenderTextures that are used as".writeBig();
            " 3D Textures ".ClickLink("https://docs.unity3d.com/Manual/class-Texture3D.html").nl();
               ("3D Textures are not supported on most mobile devices. That is why this trick with Texture2D is used " +
                " The texture is sampled using World Space Position. Currently I implemented it to use only one volume per scene." +
                " It will use global shader parameters to set all the values. This makes it easier to manage." +
                " But there is no reason why many volumes can't be used in a scene with proper material manager.").writeBig();

            return false;
        }
        
        public virtual void Inspect()
        {
            var changed = pegi.ChangeTrackStart();

            pegi.nl();

            pegi.toggleDefaultInspector(this);

            pegi.FullWindow.DocumentationClickOpen(VolumeDocumentation);

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

          

            if (inspectedElement == -1)
            {
                "Position chunks".edit(ref changePositionOnOffset);

                pegi.FullWindow
                    .DocumentationClickOpen("For Baking optimisations, how often position is changed");

                pegi.nl();
            }

            var tex = ImageMeta.CurrentTexture();

            if ("Volume Texture ({0})".F(tex ? NameForPEGI : "NULL").isEntered(ref inspectedElement, 1).nl())
            {
                var n = NameForPEGI;
                if ("Name".editDelayed(50, ref n).nl())
                    NameForPEGI = n;
               
                PositionAndScaleProperty.NameForDisplayPEGI().write_ForCopy().nl();

                SlicesShadeProperty.NameForDisplayPEGI().write_ForCopy().nl();
                

                if (tex == null)
                    ImageMeta = null;

                if ("Texture".edit(60, ref tex).nl())
                    ImageMeta = tex ? tex.GetTextureMeta() : null;

                "Volume Scale".edit(70, ref size).nl();
                size = Mathf.Max(0.0001f, size);

                if (ImageMeta == null)
                {

                    if (!TexturesPool.inst)
                    {
                        pegi.nl();
                        "Texture Width".edit(90, ref _tmpWidth);

                        if ("Create Pool".Click().nl())
                        {
                            _tmpWidth = Mathf.ClosestPowerOfTwo(Mathf.Clamp(_tmpWidth, 128, 2048));
                            TexturesPool.ForceInstance.width = _tmpWidth;
                        }
                    }
                    else
                    {
                        if ("Get From Pool".Click().nl())
                            ImageMeta = TexturesPool.inst.GetTexture2D().GetTextureMeta();

                        TexturesPool.inst.Nested_Inspect().nl();
                    }
                

                }
                pegi.nl();

                "Slices:".edit("How texture will be sliced for height", 80, ref hSlices, 1, 8).nl();

                if (changed)
                    UpdateImageMeta();

                var w = Width;
                ("Will result in X:" + w + " Z:" + w + " Y:" + Height + "volume").nl();

                tex.draw(width: 256);
                pegi.nl();
            }

            if (changed || icon.Refresh.Click("Update Materials"))
                UpdateShaderVariables();

            pegi.nl();

        }
        
        #endregion

        public virtual void UpdateShaderVariables() {
            SlicesShadeProperty.SetGlobal(Slices4Shader);
            TextureInShaderProperty.SetGlobal(ImageMeta.CurrentTexture());
            PositionAndScaleProperty.SetGlobal(PosSize4Shader);
        }

        private Vector3 _previousWorldPosition = Vector3.zero;

        private Texture _previousTarget;

        public void Update()
        {
            bool needsUpdate = false;

            var currentTexture = ImageMeta.CurrentTexture();

            if (currentTexture != _previousTarget)
            {
                _previousTarget = currentTexture;
                needsUpdate = true;
            }

            if (_previousWorldPosition != transform.position)
            {
                _previousWorldPosition = transform.position;
                needsUpdate = true;
            }
            
            if (needsUpdate)
                UpdateShaderVariables();
        }

        public virtual void OnEnable()
        {
            all.Add(this);
            UpdateShaderVariables();
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
    [CustomEditor(typeof(VolumeTexture))] internal class VolumeTextureEditor : PEGI_Inspector { }
#endif

}
