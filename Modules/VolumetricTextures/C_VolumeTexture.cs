using System.Collections.Generic;
using QuizCanners.Inspect;
using PainterTool.ComponentModules;
using QuizCanners.Utils;
using UnityEngine;
using System.Linq;
using System;

namespace PainterTool {

    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Playtime Painter/Volume Texture")]
    public class C_VolumeTexture : MonoBehaviour, IGotName, IPEGI
    {
        internal static List<C_VolumeTexture> all = new();

        [NonSerialized] public bool ManagedExternally;

        public bool DiscretePosition;

        public static C_VolumeTexture LatestInstance => all.TryGetLast();

        private static int _tmpWidth = 1024;

        private int changePositionOnOffset = 32;

        private Texture _previousTarget;

        public int hSlices = 4;
        public float size = 1;
       // public bool _staticPosition;
        private Vector4 posNSizeCached;

        public DirtyState Dirty = new();

        public class DirtyState 
        {
            public readonly Gate.DirtyVersion ShaderData = new();
            public readonly Gate.DirtyVersion LocationForSorting_Version = new();
            public readonly Gate.Vector3Value LocationForSortingGate_Gate = new();

            public readonly Gate.Frame PositionUpdate = new();
            //public bool PosAndSize = false;
          
            public readonly Gate.Vector3Value PreviousPosition = new();

            public void SetDirty() 
            {
                ShaderData.IsDirty = true;
                LocationForSorting_Version.IsDirty = true;

                PositionUpdate.ValueIsDefined = false;
                LocationForSortingGate_Gate.ValueIsDefined = false;
                PreviousPosition.ValueIsDefined = false;
              //  PosAndSize = false;
            }

        }

        public int LocationVersion
        {
            get
            {
                if (Dirty.LocationForSortingGate_Gate.TryChange(GetPositionAndSizeForShader().XYZ()))
                {
                    Dirty.LocationForSorting_Version.IsDirty = true;
                }
                return Dirty.LocationForSorting_Version.Version;
            }
        }

        public Texture _texture;

        private bool _isRuntimeTexture;

        public Texture GetOrCreate() 
        {
            if (_texture)
                return _texture;

            _texture = new RenderTexture(1024, 1024, depth: 0, RenderTextureFormat.ARGBHalf, mipCount: 0)
            {
                wrapMode = TextureWrapMode.Clamp
            };

            _isRuntimeTexture = true;

            return _texture;
        }



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


        public const string NAME = "_RayMarchingVolume"; // Temporarily hardcoded

        

        public ShaderProperty.TextureValue TextureInShaderProperty
        {
            get
            {
                if (_textureInShaderr != null)
                    return _textureInShaderr;

                _textureInShaderr = new ShaderProperty.TextureValue(NAME);

                return _textureInShaderr;
            }
        }

        private ShaderProperty.VectorValue SlicesShadeProperty
        {
            get
            {
                if (_slicesInShader != null)
                    return _slicesInShader;

                _slicesInShader = new ShaderProperty.VectorValue(NAME + "VOLUME_H_SLICES");

                return _slicesInShader;
            }
        }
        private ShaderProperty.VectorValue PositionAndScaleProperty
        {
            get
            {
                if (_positionNsizeInShader != null)
                    return _positionNsizeInShader;

                _positionNsizeInShader = new ShaderProperty.VectorValue(NAME + "VOLUME_POSITION_N_SIZE");

                return _positionNsizeInShader;
            }
        }





        #region Cubemap

       [Header("Cube Map")]
        public CubeSide Texture_0_RIGHT = new();
        public CubeSide Texture_1_LEFT = new();
        public CubeSide Texture_2_UP = new();
        public CubeSide Texture_3_DOWN = new();
        public CubeSide Texture_4_FRONT = new();
        public CubeSide Texture_5_BACK = new();

        private ShaderProperty.TextureValue _cubeMapProperty;
        public RenderTexture cubeArray;

        public void Release() 
        {
            if (cubeArray) 
            {
                cubeArray.DestroyWhatever();
                cubeArray = null;
            }

            Texture_0_RIGHT.Clear();
            Texture_1_LEFT.Clear();
            Texture_2_UP.Clear();
            Texture_3_DOWN.Clear();
            Texture_4_FRONT.Clear();
            Texture_5_BACK.Clear();

            if (_isRuntimeTexture)
                _texture.DestroyWhatever();
        }

        public void ToTextureArray(int face, Texture tex) 
        {
            Graphics.Blit(tex, GetTextureArray(), sourceDepthSlice: 0, destDepthSlice: (int)face);
            SetTextureArray();
        }

        RenderTexture GetTextureArray() 
        {
            if (cubeArray == null)
            {
                cubeArray = new RenderTexture(width: 1024, 1024, depth: 0, RenderTextureFormat.ARGBHalf)
                {
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray,
                    useMipMap = false,
                    autoGenerateMips = false,
                    wrapMode = TextureWrapMode.Clamp,
                    
                    volumeDepth = 6,
                    name = "Specular Cubemap Array",
                };
            }

            return cubeArray;
        }

        private void SetTextureArray() 
        {
            _cubeMapProperty ??= new ShaderProperty.TextureValue(RAY_MARCH_VOLUME + "CUBE");
            _cubeMapProperty.SetGlobal(cubeArray);
        }

        public bool ToTextureArray() 
        {
            if (!SystemInfo.supports2DArrayTextures)
            {
                return false;
            }

            GetTextureArray();

            for (int i=0; i<6; i++) 
            {
                var dir = (CubemapFace)i;

                var tex = this[dir];

                Graphics.Blit(tex.GetTexture(), cubeArray, sourceDepthSlice: 0, destDepthSlice: i);
            }

            SetTextureArray();

            return true;
        }


        const string RAY_MARCH_VOLUME = "_RayMarchingVolume_";

        [Serializable]
        public class CubeSide : IPEGI
        {
            [NonSerialized] public LogicWrappers.Request IsDirty = new();
            [NonSerialized] private Texture _texture;
            private ShaderProperty.TextureValue _property;

            public Texture GetTexture()
            {
                if (_texture)
                    return _texture;

                _texture = new RenderTexture(1024, 1024, 0, RenderTextureFormat.ARGBHalf, mipCount: 0)
                {
                    wrapMode = TextureWrapMode.Clamp
                };

                return _texture;
            }

            public void Clear() 
            {
                if (_texture)
                {
                    _texture.DestroyWhatever();
                    _texture = null;
                }
            }

            public void SetGlobalTexture(C_VolumeTexture parent, VolumeCubeMapped.Direction dir, Texture tex) 
            {
                _texture = tex;
                GetProperty(parent, dir).SetGlobal(_texture);
            }

            void IPEGI.Inspect()
            {
                _property?.Nested_Inspect().Nl();

                if (_texture && _texture is RenderTexture && Icon.Clear.Click())
                        RenderTextureBuffersManager.Blit(Color.clear, _texture as RenderTexture);


                pegi.Draw(_texture, width: 256, alphaBlend: false).Nl();
            }

            public void SetGlobalTexture(C_VolumeTexture parent, VolumeCubeMapped.Direction dir) 
            {
                GetProperty(parent, dir).SetGlobal(_texture);
            }

            private ShaderProperty.TextureValue GetProperty(C_VolumeTexture parent, VolumeCubeMapped.Direction dir)
            {
                if (_property == null)
                {
                    var newName = RAY_MARCH_VOLUME + dir.ToString();
                   
                    _property = new ShaderProperty.TextureValue(newName);
                }

                return _property;
            }
        }

        public CubeSide this[VolumeCubeMapped.Direction dir]
        {
            get
            {
                switch (dir)
                {
                    case VolumeCubeMapped.Direction.RIGHT: return Texture_0_RIGHT;
                    case VolumeCubeMapped.Direction.LEFT: return Texture_1_LEFT;
                    case VolumeCubeMapped.Direction.UP: return Texture_2_UP;
                    case VolumeCubeMapped.Direction.DOWN: return Texture_3_DOWN;
                    case VolumeCubeMapped.Direction.FRONT: return Texture_4_FRONT;
                    case VolumeCubeMapped.Direction.BACK: return Texture_5_BACK;
                 
                    default:
                        Debug.LogError(QcLog.CaseNotImplemented(dir, nameof(C_VolumeTexture)));
                        return Texture_3_DOWN;
                }
            }
        }

        public CubeSide this[CubemapFace face] 
        {
            get 
            {
                switch (face) 
                {
                    case CubemapFace.PositiveX: return Texture_0_RIGHT;
                    case CubemapFace.NegativeX: return Texture_1_LEFT;
                    case CubemapFace.PositiveY: return Texture_2_UP;
                    case CubemapFace.NegativeY: return Texture_3_DOWN;
                    case CubemapFace.PositiveZ: return Texture_4_FRONT;
                    case CubemapFace.NegativeZ: return Texture_5_BACK;

                    default:
                        Debug.LogError(QcLog.CaseNotImplemented(face, nameof(C_VolumeTexture)));
                        return Texture_2_UP;
                }
            }
        }

        #endregion

        public string NameForInspector
        {
            get { return name; }
            set
            {
                name = value;
                _textureInShaderr = null;
            }
        }

        public int TextureHeight => hSlices * hSlices;

        public int TextureWidth
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

    //   public bool IsDifferent(int heightSlices, float size, bool staticPosition) 
      //      => hSlices != heightSlices || this.size != size || staticPosition != _staticPosition;
        

        public bool TryChange(int heightSlices, float size) 
        {
            if (hSlices == heightSlices && this.size == size)
                return false;

            hSlices = heightSlices;
            this.size = size;
           // _staticPosition = staticPosition;

            Dirty.SetDirty();

            return true;
        }

        public Vector4 GetSlices4Shader()
        {
           
                //var srv = Singleton.Get<TexturesPoolSingleton>();

                float w = TextureWidth; //((ImageMeta?.Width ?? (srv ? srv.width : _tmpWidth)) //- hSlices * 2
                                 //) / hSlices;
                return new Vector4(hSlices, w * 0.5f, 1f / w, 1f / hSlices);
            
        }


        public Vector4 GetPositionAndSizeForShader() 
        {
            if (!Dirty.PositionUpdate.TryEnter())
                return posNSizeCached;

            Vector3 pos;

            if (!DiscretePosition) 
            {
                pos = transform.position;
              //  Dirty.PosAndSize = false;
            } else 
            {
                pos = TracedVolume.GetDiscretePosition(transform.position, size, out float scaledChunks, changePositionOnOffset);// Vector3Int.FloorToInt(currentPosition / scaledChunks);

               // if ((!posNSizeCached.XYZ().Equals(pos) && Vector3.Distance(transform.position, posNSizeCached.XYZ()) > scaledChunks))
                  //  Dirty.PosAndSize = false;
            }


            posNSizeCached = pos.ToVector4(size);
            
            
            return posNSizeCached;
        }

     

        public virtual Vector4 UpdateShaderVariables()
        {
            Vector4 res = GetPositionAndSizeForShader();
            PositionAndScaleProperty.SetGlobal(res);
            SlicesShadeProperty.SetGlobal(GetSlices4Shader());
            TextureInShaderProperty.SetGlobal(Texture); //ImageMeta.CurrentTexture());
          

            SetTextureArray();

            return res;
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
        protected int inspectedMaterial = -1;
        protected pegi.EnterExitContext context = new();
        

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
            if (Application.isPlaying && !all.Contains(this))
                "Not registered in the list of volumes".PegiLabel().WriteWarning().Nl();

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
                   // "Static Position".PegiLabel("Volume will use the exact position of the volume and ignore if the position changes").ToggleIcon(ref _staticPosition).Nl();

                    "Position chunk size".PegiLabel().Edit(ref changePositionOnOffset);

                    pegi.FullWindow
                        .DocumentationClickOpen("For Baking optimisations, how often position is changed");

                    pegi.Nl();
                }


                if ("Volume Texture ({0})".F(Texture ? NameForInspector : "NULL").PegiLabel().IsEntered().Nl())
                {
                    if (!cubeArray)
                    {
                        if ("Create Cubemap Array".PegiLabel().Click().Nl())
                            ToTextureArray();
                    }
                    else
                        pegi.Edit_Property(() => cubeArray, this).Nl();

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

                    "Last Value: {0}".F(posNSizeCached).PegiLabel().Nl();

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
                            var needsReimport = imp.WasWrongIsColor_Editor(targetIsColor: false) | imp.WasWrongAlphaIsTransparency_Editor(isTransparency: false);
                            if (needsReimport)
                                imp.SaveAndReimport();

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

                    if (Mathf.IsPowerOfTwo(hSlices))
                        "When not power of 2, the result may be imperfect".PegiLabel().Write_Hint().Nl();

                    if (changed)
                        UpdateImageMeta();

                    var w = TextureWidth;
                    ("Will result in X:{0} Z:{0} Y:{1} volume".F(w,TextureHeight)).PegiLabel().Nl();

                    pegi.Draw(tex, width: 256);
                    pegi.Nl();
                }

                if (changed | Icon.Refresh.Click("Update Materials"))
                {
                    Dirty.ShaderData.IsDirty = true;
                }

                pegi.Nl();
            }
        }
        
        #endregion





        public void Update()
        {
            if (ManagedExternally)
                return;

            var currentTexture = Texture; 

            if (currentTexture != _previousTarget)
            {
                _previousTarget = currentTexture;
                Dirty.ShaderData.IsDirty = true;
            }

            if (Dirty.PreviousPosition.TryChange(transform.position))
                Dirty.ShaderData.IsDirty = true;

            if (Dirty.ShaderData.TryClear())
                UpdateShaderVariables();
        }

        public virtual void OnEnable()
        {
            all.Add(this);
            UpdateShaderVariables();
            TracedVolume.HasValidData = true;//VOLUME_VISIBILITY.GlobalValue = 1;
        }

        public virtual void OnDisable()
        {
            if (all.Contains(this))
            {
                all.Remove(this);
                if (all.Count > 0)
                    all.Last().UpdateShaderVariables();
            }

            if (all.Count == 0)
                TracedVolume.HasValidData = false;//VOLUME_VISIBILITY.GlobalValue = 0;

            Release();

        }

        public virtual void OnDrawGizmosSelected()
        {
            if (ImageMeta == null || ManagedExternally) 
                return;

            var w = TextureWidth;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(VolumeCenter, new Vector3(w, TextureHeight, w) * size);
        }

        public Vector3 VolumeCenter 
        {
            get 
            {
                var center = transform.position;
                center.y += TextureHeight * 0.5f * size;
                return center;
            }

            set 
            {
                value.y -= TextureHeight * 0.5f * size;
                transform.position = value;
            }
        }

        public virtual void DrawGizmosOnPainter(PainterComponent painter) { }

    }

    public static class VolumeCubeMapped
    {

        public enum Direction { RIGHT = 0, LEFT = 1, UP = 2, DOWN = 3, FRONT = 4, BACK = 5};

        public static Vector3 ToVector(this Direction direction) 
        {
            switch (direction) 
            {
                case Direction.UP: return Vector3.up;
                case Direction.RIGHT: return Vector3.right;
                case Direction.LEFT: return Vector3.left;
                case Direction.FRONT: return Vector3.forward;
                case Direction.BACK: return Vector3.back;
                case Direction.DOWN: return Vector3.down;
                default:
                    Debug.LogError(QcLog.CaseNotImplemented(direction, nameof(ToVector)));
                    return Vector3.down;
            }
        }
    }

    [PEGI_Inspector_Override(typeof(C_VolumeTexture))] internal class VolumeTextureEditor : PEGI_Inspector_Override { }


}
