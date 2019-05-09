using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using Unity.Collections;
using UnityEngine.Serialization;

namespace PlaytimePainter {
    
    [ExecuteInEditMode]
    [Serializable]
    public class VolumeTexture : PainterSystemMono, IGotName {

        [SerializeField] public bool setForGlobal;

        public static Dictionary<ShaderProperty.TextureValue, VolumeTexture> currentlyActiveVolumes = new Dictionary<ShaderProperty.TextureValue, VolumeTexture>();

        protected VolumeTexture CurrentGlobal
        {
            get { return currentlyActiveVolumes.TryGet(MaterialPropertyNameGlobal);  }
            set
            {
                currentlyActiveVolumes[MaterialPropertyNameGlobal] = value;
                if (value)
                    value.OnBecomeActive();
            }
        }

        public static VolumeTexture GetGlobal(ShaderProperty.TextureValue propertyName)
        {
            foreach (var v in currentlyActiveVolumes)
            {
                var val = v.Value;
                if (val && val.MaterialPropertyNameGlobal.Equals(propertyName))
                    return val;
            }

            return null;
        }

        protected bool IsCurrentGlobalVolume => setForGlobal && this == currentlyActiveVolumes.TryGet(MaterialPropertyNameGlobal);
        
        public static List<VolumeTexture> all = new List<VolumeTexture>();

        public virtual bool VolumeJobIsRunning => volumeIsProcessed;

        public bool volumeIsProcessed = false;

        private static int _tmpWidth = 1024;

        public int hSlices = 1;
        public float size = 1;

        [NonSerialized] protected NativeArray<Color> unsortedVolume;

        [SerializeField] public Texture2D texture;

        public ImageMeta ImageMeta
        {
            get { return texture.GetImgData(); }
            set { texture = value?.texture2D; }
        }

        protected virtual string PropertyNameRoot => "DefaultVolume";

        public ShaderProperty.TextureValue MaterialPropertyName => new ShaderProperty.TextureValue(PropertyNameRoot + VolumePaintingModule.VolumeTextureTag);

        public ShaderProperty.TextureValue MaterialPropertyNameGlobal => new ShaderProperty.TextureValue( PainterDataAndConfig.GlobalPropertyPrefix+ PropertyNameRoot + VolumePaintingModule.VolumeTextureTag);
        
        public List<Material> materials;

        public string NameForPEGI { get { return name; } set { name = value; } }

        public int Height => hSlices * hSlices;

        public int Width => (ImageMeta?.width ?? (TexturesPool.inst ? TexturesPool.inst.width : _tmpWidth)) / hSlices;

        public Vector4 PosSize4Shader => transform.position.ToVector4(1f / size);

        public Vector4 Slices4Shader { get { float w = ((ImageMeta?.width ?? (TexturesPool.inst ? TexturesPool.inst.width : _tmpWidth)) - hSlices * 2) / hSlices;
            return new Vector4(hSlices, w * 0.5f, 1f / w, 1f / hSlices); } }

        public virtual bool NeedsToManageMaterials => true;

        public virtual Color GetColorFor(Vector3 pos) => Color.white * (Width * size * 0.5f - (LastCenterPosTMP - pos).magnitude);

        public virtual void AddIfNew(PlaytimePainter p) => AddIfNew(p.Material);

        Vector3 LastCenterPosTMP;
        public virtual void RecalculateVolume()
        {
            var center = transform.position;
            LastCenterPosTMP = center;
            var w = Width;

            CheckVolume();

            var hw = Width / 2;

            var pos = Vector3.zero;

            for (var h = 0; h < Height; h++)
            {
                pos.y = center.y + h * size;
                for (var y = 0; y < w; y++)
                {
                    pos.z = center.z + (y - hw) * size;
                    var index = (h * Width + y) * Width;

                    for (var x = 0; x < w; x++)
                    {
                        pos.x = center.x + (x - hw) * size;
                        unsortedVolume[index + x] = GetColorFor(pos);
                    }
                }
            }
        }

        private bool CheckSizeChange()
        {
            var volumeLength = Width * Width * Height;

            if (unsortedVolume.IsCreated && unsortedVolume.Length == volumeLength) return false;
            
            if (unsortedVolume.IsCreated)
                unsortedVolume.Dispose();

            unsortedVolume = new NativeArray<Color>(volumeLength, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            return true;


        }

        private bool AddIfNew(Material mat)
        {
            if (materials.Contains(mat)) return false;
            
            materials.Add(mat);
            
            if (NeedsToManageMaterials)
                UpdateMaterials();

            return true;
        }

        protected void CheckVolume()
        {
            if (CheckSizeChange())
                VolumeFromTexture();
        }

        #region Volume Processing 
        public virtual void VolumeFromTexture()
        {

            if (ImageMeta == null)
            {

                if (TexturesPool.inst != null)
                    ImageMeta = TexturesPool.inst.GetTexture2D().GetImgData();
                else
                {
                    Debug.Log("No Texture for Volume");
                    return;
                }
                UpdateTextureName();
            }

            CheckSizeChange();

            Color[] pixels = ImageMeta.Pixels;

            int texSectorW = ImageMeta.width / hSlices;
            int w = Width;

            for (int hy = 0; hy < hSlices; hy++)
            {
                for (int hx = 0; hx < hSlices; hx++)
                {

                    int hTex_index = (hy * ImageMeta.width + hx) * texSectorW;

                    int h = hy * hSlices + hx;

                    for (int y = 0; y < w; y++)
                    {
                        int yTex_index = hTex_index + y * ImageMeta.width;

                        int yVolIndex = h * w * w + y * w;

                        for (int x = 0; x < w; x++)
                        {
                            int texIndex = yTex_index + x;
                            int volIndex = yVolIndex + x;

                            unsortedVolume[volIndex] = pixels[texIndex];
                        }
                    }
                }
            }

        }

        public virtual void VolumeToTexture()
        {

            if (ImageMeta == null)
            {

                if (TexturesPool.inst)
                    ImageMeta = TexturesPool.inst.GetTexture2D().GetImgData();
                else {
                    Debug.Log("No Texture for Volume");
                    return;
                }
                UpdateTextureName();
            }

            var im = ImageMeta;

            var pixels = im.Pixels;

            var texSectorW = im.width / hSlices;
            var w = Width;

            for (var hy = 0; hy < hSlices; hy++)
            {
                for (var hx = 0; hx < hSlices; hx++)
                {

                    var hTexIndex = (hy * im.width + hx) * texSectorW;

                    var h = hy * hSlices + hx;

                    for (var y = 0; y < w; y++)
                    {
                        var yTexIndex = hTexIndex + y * im.width;

                        var yVolIndex = h * w * w + y * w;

                        for (var x = 0; x < w; x++)
                        {
                            var texIndex = yTexIndex + x;
                            var volIndex = yVolIndex + x;

                            pixels[texIndex] = unsortedVolume[volIndex];
                        }
                    }
                }
            }

            im.SetAndApply(false);
            UpdateTextureName();
        }

        public int PositionToVolumeIndex(Vector3 pos)
        {
            pos /= size;
            int hw = Width / 2;

            var y = (int)Mathf.Clamp(pos.y, 0, Height - 1);
            var z = (int)Mathf.Clamp(pos.z + hw, 0, hw - 1);
            var x = (int)Mathf.Clamp(pos.x + hw, 0, hw - 1);

            return (y * Width + z) * Width + x;
        }

        public int PositionToPixelIndex(Vector3 pos)
        {
            pos /= size;
            int hw = Width / 2;

            var y = (int)Mathf.Clamp(pos.y, 0, Height - 1);
            var z = (int)Mathf.Clamp(pos.z + hw, 0, hw - 1);
            var x = (int)Mathf.Clamp(pos.x + hw, 0, hw - 1);

            int hy = y / hSlices;
            int hx = y % hSlices;

            return VolumeToPixelIndex(hx, hy, z, x);
        }

        public int VolumeToPixelIndex(int hx, int hy, int y, int x)
        {

            int hTex_index = (hy * ImageMeta.width + hx) * ImageMeta.width / hSlices;

            int yTex_index = hTex_index + (y) * ImageMeta.width;

            int texIndex = yTex_index + x;

            return texIndex;
        }

        public Vector3 VolumeIndexToPosition(int index)
        {

            int plane = Width * Width;
            int y = index / plane;
            int z = (index - y * plane);

            int x = z;
            z /= Width;
            x -= z * Width;

            int hw = Width / 2;
            Vector3 v3 = new Vector3(x - hw, y - hw, z - hw) * size;

            return v3;
        }
        #endregion

        private void UpdateTextureName()
        {
            if (ImageMeta == null) return;
            ImageMeta.isAVolumeTexture = true;
            ImageMeta.saveName = name + VolumePaintingModule.VolumeTextureTag + hSlices.ToString() + VolumePaintingModule.VolumeSlicesCountTag;
            if (ImageMeta.texture2D) ImageMeta.texture2D.name = ImageMeta.saveName;
            if (ImageMeta.renderTexture) ImageMeta.renderTexture.name = ImageMeta.saveName;
        }

        protected virtual void OnBecomeActive()
        {

        }

        #region Inspect
        #if PEGI
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
            
            pegi.fullWindowDocumentationClickOpen(VolumeDocumentation);

            if (inspectedElement == -1)
            {
                "Also set for Global shader parameters".toggleIcon(ref setForGlobal, true).changes(ref changed);
                if (setForGlobal)
                {
                    "Current for {0} is {1}".F(MaterialPropertyNameGlobal, CurrentGlobal).nl();
                    if (this != CurrentGlobal && "Clear".Click(ref changed))
                        CurrentGlobal = null;

                }

                pegi.nl();
            }

            if ("Volume Texture".enter(ref inspectedElement, 1).nl())
            {

                var n = name;

                if ("Name".editDelayed(50, ref n).nl(ref changed))
                    name = n;

                var tex = ImageMeta.CurrentTexture();

                if (tex == null)
                    ImageMeta = null;

                if ("Texture".edit(60, ref tex).nl(ref changed))
                    ImageMeta = tex ? tex.GetImgData() : null;

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
                    else if ("Get From Pool".Click().nl(ref changed))
                        ImageMeta = TexturesPool.inst.GetTexture2D().GetImgData();


                }
                pegi.nl();

                "Slices:".edit("How texture will be sliced for height", 80, ref hSlices, 1, 8).nl(ref changed);

                if (changed)
                    UpdateTextureName();

                var w = Width;
                ("Will result in X:" + w + " Z:" + w + " Y:" + Height + "volume").nl();
            }

            if ("Materials".enter(ref inspectedElement, 2).nl_ifFoldedOut()) {

                "Materials".edit_List_UObj(ref materials, ref inspectedMaterial);

                if (inspectedMaterial == -1 && InspectedPainter)
                {
                    var pMat = InspectedPainter.Material;
                    if (pMat != null && materials.Contains(pMat) && "Remove This Material".Click().nl(ref changed))
                        materials.Remove(pMat);
                }

                if (changed || (inspectedMaterial == -1 && "Update Materials".Click().nl(ref changed)))
                    UpdateMaterials();
            }

            if (inspectedElement == -1 && icon.Refresh.Click("Update Materials"))
                UpdateMaterials();

            pegi.nl();

            return changed;
        }

        #endif
        #endregion

        public virtual void UpdateMaterials() {
            
            materials.SetVolumeTexture(this);

            if (!setForGlobal) return;

            if (!CurrentGlobal)
                CurrentGlobal = this;

            if (CurrentGlobal == this)
            {
                VolumePaintingModule.VOLUME_POSITION_N_SIZE_Global.SetGlobal(PosSize4Shader);
                VolumePaintingModule.VOLUME_H_SLICES_Global.SetGlobal(Slices4Shader);
                MaterialPropertyNameGlobal.SetGlobal(ImageMeta.CurrentTexture());
            }
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
        }

        public virtual void OnDisable()
        {
            if (all.Contains(this))
                all.Remove(this);
            if (unsortedVolume.IsCreated)
                unsortedVolume.Dispose();
            if (this == CurrentGlobal)
                CurrentGlobal = null;
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
}
