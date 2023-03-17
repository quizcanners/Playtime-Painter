using System.Collections.Generic;
using QuizCanners.Inspect;
using PainterTool.ComponentModules;
using QuizCanners.Utils;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

namespace PainterTool {

    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class C_VolumeTexture : MonoBehaviour, IGotName, IPEGI
    {
        internal static List<C_VolumeTexture> all = new List<C_VolumeTexture>();

        public static C_VolumeTexture LatestInstance => all.TryGetLast();

        private static int _tmpWidth = 1024;

        private int changePositionOnOffset = 32;

        private readonly Gate.DirtyVersion _dirtyVersion = new Gate.DirtyVersion();

        public int hSlices = 4;
        public float size = 1;

        private readonly Gate.Vector3Value _locationForSortingGate = new();
        private int _locationForSortingVersion = 0;

        public int LocationVersion
        {
            get
            {
                if (_locationForSortingGate.TryChange(GetPositionAndSizeForShader().XYZ()))
                {
                    _locationForSortingVersion++;
                }
                return _locationForSortingVersion;
            }
        }

        public Texture _texture;

        public Texture Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                TextureInShaderProperty.SetGlobal(value);
            }
        }

        internal TextureMeta ImageMeta
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

                _textureInShaderr = new ShaderProperty.TextureValue("_RayMarchingVolume");

                return _textureInShaderr;
            }
        }

        private ShaderProperty.VectorValue SlicesShadeProperty
        {
            get
            {
                if (_slicesInShader != null)
                    return _slicesInShader;

                _slicesInShader = new ShaderProperty.VectorValue(name + "VOLUME_H_SLICES");

                return _slicesInShader;
            }
        }

        private ShaderProperty.VectorValue PositionAndScaleProperty
        {
            get
            {
                if (_positionNsizeInShader != null)
                    return _positionNsizeInShader;

                _positionNsizeInShader = new ShaderProperty.VectorValue(name + "VOLUME_POSITION_N_SIZE");

                return _positionNsizeInShader;
            }
        }

        public string NameForInspector
        {
            get { return name; }
            set
            {
                name = value;
                _textureInShaderr = null;
            }
        }

        public int Height => hSlices * hSlices;

        public int Width
        {
            get
            {
                int width;
                if (Texture)
                    width = Texture.width;
                else
                    width = Singleton.GetValue<Singleton_TexturesPool, int>(s => s.defaultWidth, defaultValue: _tmpWidth, logOnServiceMissing: false);

                return width / hSlices;
            }
        }

        private readonly Gate.Frame _recalculateFrame = new Gate.Frame();
        private Vector4 posNSizeCached;

        public Vector4 GetPositionAndSizeForShader() 
        {
            if (_recalculateFrame.TryEnter())
            {
                changePositionOnOffset = Mathf.Max(1, changePositionOnOffset);
                var scaledChunks = changePositionOnOffset * size;
                var pos = (transform.position + (0.5f * scaledChunks * Vector3.one)) / scaledChunks;
                pos = Vector3Int.FloorToInt(pos);
                pos *= scaledChunks;
                posNSizeCached = pos.ToVector4(1f / size);
            }

            return posNSizeCached;
        }

        public virtual Vector4 UpdateShaderVariables()
        {
            Vector4 res = GetPositionAndSizeForShader();
            SlicesShadeProperty.SetGlobal(Slices4Shader);
            TextureInShaderProperty.SetGlobal(Texture); //ImageMeta.CurrentTexture());
            PositionAndScaleProperty.SetGlobal(res);
            return res;
        }


        public Vector4 Slices4Shader {
            get {
                //var srv = Singleton.Get<TexturesPoolSingleton>();

                float w = Width; //((ImageMeta?.Width ?? (srv ? srv.width : _tmpWidth)) //- hSlices * 2
                    //) / hSlices;
                return new Vector4(hSlices, w * 0.5f, 1f / w, 1f / hSlices);
            }
        }

        private void UpdateImageMeta()
        {
            if (ImageMeta == null)
                return;
            ImageMeta.IsAVolumeTexture = true;
        }

        #region Inspect

        [SerializeField] private PainterComponent _painter;
        private bool _searchedForPainter;
        protected pegi.EnterExitContext context = new pegi.EnterExitContext();
        protected int inspectedMaterial = -1;

        protected virtual void VolumeDocumentation()
        {
            "In this context Volumes are Textures2D and RenderTextures that are used as".PegiLabel().WriteBig();
            " 3D Textures ".PegiLabel().ClickLink("https://docs.unity3d.com/Manual/class-Texture3D.html").Nl();
               ("3D Textures are not supported on most mobile devices. That is why this trick with Texture2D is used " +
                " The texture is sampled using World Space Position. Currently I implemented it to use only one volume per scene." +
                " It will use global shader parameters to set all the values. This makes it easier to manage." +
                " But there is no reason why many volumes can't be used in a scene with proper material manager.").PegiLabel().WriteBig();
        }
        
        public virtual void Inspect()
        {
            using (context.StartContext())
            {
                var changed = pegi.ChangeTrackStart();

                pegi.FullWindow.DocumentationClickOpen(VolumeDocumentation).Nl();

                if (_textureInShaderr != null)
                {
                    if (Icon.Delete.Click())
                        _textureInShaderr = null;

                    pegi.GetNameForInspector(_textureInShaderr).PegiLabel().Nl();
                }

                if (_searchedForPainter)
                {
                    _searchedForPainter = true;
                    _painter = GetComponent<PainterComponent>();
                }

                if (!_painter)
                {
                    "Painter [None]".PegiLabel().Write();
                    if ("Search".PegiLabel().Click())
                        _painter = GetComponent<PainterComponent>();

                    if (Icon.Add.Click())
                    {
                        _painter = GetComponent<PainterComponent>();
                        if (!_painter)
                            _painter = gameObject.AddComponent<PainterComponent>();
                    }

                    pegi.Nl();
                }
                else
                {
                    if (_painter.gameObject != gameObject)
                    {
                        "Painter is on a different Game Object".PegiLabel().WriteWarning();
                        if ("Disconnect".PegiLabel().Click())
                            _painter = null;
                    }

                    var mod = _painter.GetModule<VolumeTextureComponentModule>();
                    if (!mod.volumeTexture && "Assign volume texture to painter".PegiLabel().Click().Nl())
                        mod.volumeTexture = this;

                    if (mod.volumeTexture)
                    {
                        if (mod.volumeTexture == this)
                            "This volume texture is attached".PegiLabel().Write();

                        if (Icon.UnLinked.Click("Unlink"))
                            mod.volumeTexture = null;
                    }

                    pegi.Nl();
                }

                if (context.IsAnyEntered == false)
                {
                    "Position chunk size".PegiLabel().Edit(ref changePositionOnOffset);

                    pegi.FullWindow
                        .DocumentationClickOpen("For Baking optimisations, how often position is changed");

                    pegi.Nl();
                }


                if ("Volume Texture ({0})".F(Texture ? NameForInspector : "NULL").PegiLabel().IsEntered().Nl())
                {
                    var n = NameForInspector;
                    if ("Name".PegiLabel(50).Edit_Delayed(ref n).Nl())
                        NameForInspector = n;

                    PositionAndScaleProperty.ToString().PegiLabel().Write_ForCopy().Nl();

                    SlicesShadeProperty.ToString().PegiLabel().Write_ForCopy().Nl();

                    var tex = Texture;
                    if ("Texture".PegiLabel(60).Edit(ref tex).Nl())
                        Texture = tex;

                    if ("Volume Scale".PegiLabel(70).Edit_Delayed(ref size).Nl())
                        size = Mathf.Max(0.0001f, size);

                    if (Texture)
                    {
                        if (Texture is RenderTexture && "Save Texture As Texture 2D".PegiLabel().Click())
                        {
                            var asRt = Texture as RenderTexture;

                            var tex2D = new Texture2D(asRt.width, asRt.height, TextureFormat.RGBA32, mipChain: false);

                            RenderTexture.active = Texture as RenderTexture;
                            tex2D.ReadPixels(new Rect(0, 0, asRt.width, asRt.height), 0, 0);
                            tex2D.Apply();



                            string tname = (_textureInShaderr == null ? name : _textureInShaderr.ToString()) + " " + gameObject.scene.name + "_Volume Size {0} Slices {1}".F(size, hSlices);


#if UNITY_EDITOR

                            if (QcUnity.TryGetActiveScenePath(out string path)) 
                            {
                                path = QcUnity.RemoveAssetsFromPath(path);
                            } else
                            {
                                path = "Volume Textures";
                            }

                            tex2D = QcUnity.SaveTextureAsAsset(tex2D, path, ref tname, saveAsNew: false);
                            var imp = tex2D.GetTextureImporter_Editor();
                            var needsReimport = imp.WasWrongIsColor_Editor(isColor: false) | imp.WasWrongAlphaIsTransparency_Editor(isTransparency: false);
                            if (needsReimport)
                                imp.SaveAndReimport();

                           // _texture = tex2D;

#endif


                        }

                    } else 
                    { 
                        if (!Singleton.Get<Singleton_TexturesPool>())
                        {
                            pegi.Nl();
                            "Texture Width".PegiLabel(90).Edit(ref _tmpWidth);

                            if ("Create Pool".PegiLabel().Click().Nl())
                            {
                                _tmpWidth = Mathf.ClosestPowerOfTwo(Mathf.Clamp(_tmpWidth, 128, 2048));
                                Singleton_TexturesPool.ForcedInstance.defaultWidth = _tmpWidth;
                            }
                        }
                        else
                        {
                            Singleton.Get<Singleton_TexturesPool>().Nested_Inspect().Nl();
                        }
                    }
                    pegi.Nl();

                    "Slices:".PegiLabel("How texture will be sliced for height", 80).Edit(ref hSlices, 1, 8).Nl();

                    if (changed)
                        UpdateImageMeta();

                    var w = Width;
                    ("Will result in X:{0} Z:{0} Y:{1} volume".F(w,Height)).PegiLabel().Nl();

                    pegi.Draw(tex, width: 256);
                    pegi.Nl();
                }

                if (changed | Icon.Refresh.Click("Update Materials"))
                {
                    _dirtyVersion.IsDirty = true;
                }

                pegi.Nl();
            }
        }
        
        #endregion



        private readonly Gate.Vector3Value _worldPosValue = new Gate.Vector3Value();
        private Texture _previousTarget;

        public void Update()
        {
            var currentTexture = Texture; 

            if (currentTexture != _previousTarget)
            {
                _previousTarget = currentTexture;
                _dirtyVersion.IsDirty = true;
            }

            if (_worldPosValue.TryChange(transform.position))
                _dirtyVersion.IsDirty = true;

            if (_dirtyVersion.TryClear())
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
            {
                all.Remove(this);
                if (all.Count > 0)
                    all.Last().UpdateShaderVariables();
            }
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

        public virtual void DrawGizmosOnPainter(PainterComponent painter) { }

    }


    [PEGI_Inspector_Override(typeof(C_VolumeTexture))] internal class VolumeTextureEditor : PEGI_Inspector_Override { }


}
