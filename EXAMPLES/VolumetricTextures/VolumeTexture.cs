using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;
using SharedTools_Stuff;

namespace Playtime_Painter {


    [ExecuteInEditMode]
    [Serializable]
    public class VolumeTexture : PainterStuffMono, IPEGI, IGotName
        {

        public static List<VolumeTexture> all = new List<VolumeTexture>();
        
        public static int tmpWidth = 1024;

        public int h_slices = 1;
        public float size = 1;
        [NonSerialized]
        Color[] volume;
        [SerializeField]int imgDataIndex = -1;

        public ImageData ImageData
        {
            get { return TexMGMTdata?.imgDatas.TryGet(imgDataIndex); }
            set { imgDataIndex = TexMGMTdata.imgDatas.TryGetIndex(value); }
        }

        public virtual string MaterialPropertyName {get {return "_DefaultVolume"+ VolumePaintingPlugin.VolumeTextureTag; } }

        public List<Material> materials;

        public string NameForPEGI { get { return name; } set { name = value; } }

        public int Height { get { return h_slices * h_slices; } }

        public int Width{get { return ((ImageData == null ? (TexturesPool._inst == null ? tmpWidth : TexturesPool._inst.width): ImageData.width)) / h_slices;}}

        public Vector4 PosNsize4Shader { get  { Vector3 pos = transform.position;return new Vector4(pos.x, pos.y, pos.z, 1f / size);} }

        public Vector4 Slices4Shader {get { float w = (ImageData.width - h_slices * 2) / h_slices; return new Vector4(h_slices, w * 0.5f, 1f / ((float)w), 1f / ((float)h_slices)); } }
        
        public virtual bool NeedsToManageMaterials { get { return true; } }

        public virtual Color GetColorFor(Vector3 pos)
        {
            float magnitude = (LastCenterPosTMP - pos).magnitude;

            return Color.white * (Width * size * 0.5f - magnitude);
        }

        Vector3 LastCenterPosTMP;
        public virtual void RecalculateVolume(Vector3 center) {
            LastCenterPosTMP = center;
            int w = Width;
            int volumeLength = w * w * Height;

            if (volume == null || volume.Length != volumeLength)
                volume = new Color[volumeLength];

            int hw = Width / 2;

            Vector3 pos = Vector3.zero;

            for (int h = 0; h < Height; h++)  {
                pos.y = center.y + h * size;
                for (int y = 0; y < w; y++)  {
                    pos.z = center.z + ((float)(y - hw)) * size;
                    int index = (h * Width + y) * Width;

                    for (int x = 0; x < w; x++) {
                        pos.x = center.x + ((float)(x - hw)) * size;
                        volume[index + x] = GetColorFor(pos);
                    }
                }
            }



        }

        public virtual void AddIfNew(PlaytimePainter p) {
            AddIfNew(p.Material);
        }

        public bool AddIfNew (Material mat) {
            if (!materials.Contains(mat))
            {
                materials.Add(mat);
                if (NeedsToManageMaterials)
                    UpdateMaterials();

                return true;
            }
            return false;
        }

        public virtual void VolumeToTexture()
        {
            if (ImageData == null)
            {
                if (TexturesPool._inst != null)
                    ImageData = TexturesPool._inst.GetTexture2D().GetImgData();
                else
                {
                    Debug.Log("No Texture for Volume");
                    return;
                }
                UpdateTextureName();
            }

            Color[] pixels = ImageData.Pixels;//new Color32[tex.width * tex.width];

            int texSectorW = ImageData.width / h_slices;
            int w = Width;

            for (int hy = 0; hy < h_slices; hy++)
            {
                for (int hx = 0; hx < h_slices; hx++)
                {

                    int hTex_index = (hy * ImageData.width + hx) * texSectorW;

                    int h = hy * h_slices + hx;

                    for (int y = 0; y < w; y++)
                    {
                        int yTex_index = hTex_index + y * ImageData.width;

                        int yVolIndex = h * w * w + y * w;

                        for (int x = 0; x < w; x++)
                        {
                            int texIndex = yTex_index + x;
                            int volIndex = yVolIndex + x;
                            pixels[texIndex] = volume[volIndex];
                        }
                    }
                }
            }

            ImageData.SetAndApply(false);
            UpdateTextureName();


        }

        public int PositionToVolumeIndex(Vector3 pos)
        {
            pos /= size;
            int hw = Width / 2;

            int y = (int)Mathf.Clamp(pos.y, 0, Height - 1);
            int z = (int)Mathf.Clamp(pos.z + hw, 0, hw - 1);
            int x = (int)Mathf.Clamp(pos.x + hw, 0, hw - 1);

            return ((y * Width + z) * Width + x);
        }
        
        public int PositionToPixelIndex(Vector3 pos) {
            pos /= size;
            int hw = Width / 2;

            int y = (int)Mathf.Clamp(pos.y, 0, Height - 1);
            int z = (int)Mathf.Clamp(pos.z + hw, 0, hw - 1);
            int x = (int)Mathf.Clamp(pos.x + hw, 0, hw - 1);

            int hy = y / h_slices;
            int hx = y % h_slices;

            return VolumeToPixelIndex(hx, hy, z, x);
        }

        public int VolumeToPixelIndex(int hx, int hy, int y, int x)
        {

            int hTex_index = (hy * ImageData.width + hx) * ImageData.width / h_slices;

            int yTex_index = hTex_index + (y) * ImageData.width;

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

        public void UpdateTextureName() {
            if (ImageData != null) {
                ImageData.SaveName = name + VolumePaintingPlugin.VolumeTextureTag + h_slices.ToString() + VolumePaintingPlugin.VolumeSlicesCountTag; ;
                if (ImageData.texture2D != null) ImageData.texture2D.name = ImageData.SaveName;
                if (ImageData.renderTexture != null) ImageData.renderTexture.name = ImageData.SaveName;
            }
        }

        #if PEGI

        public override bool PEGI() {
            bool changed = false;

            string n = name;
            if ("Name".editDelayed(30, ref n).nl()) {
                name = n;
                changed = true;
            }
            
            if (volume != null && volume.Length != Width * Width * Height)  {
                volume = null;
                Debug.Log("Clearing volume");
            }

            var texture = ImageData.CurrentTexture();

            if (texture == null)
                ImageData = null;

            if ("Texture".edit(60, ref texture).nl()) {
                changed = true;
                ImageData = texture?.GetImgData();
            }

            changed |= "Volume Scale".edit(70,ref size).nl();
            size = Mathf.Max(0.0001f, size);

                if (ImageData == null) {

                if (TexturesPool._inst == null)
                    {
                    pegi.nl();
                    changed |= "Texture Width".edit(ref tmpWidth);

                    if ("Create Pool".Click().nl())
                        {
                            tmpWidth = Mathf.ClosestPowerOfTwo(Mathf.Clamp(tmpWidth, 128, 2048));
                            TexturesPool.Inst.width = tmpWidth;
                        }
                    }
                    else {
                    if ("Get From Pool".Click().nl()) {
                        ImageData = TexturesPool._inst.GetTexture2D().GetImgData();
                        changed = true;
                    }
                    }
                }
                pegi.nl();

                changed |= "Slices:".edit("How texture will be sliced for height", 80, ref h_slices, 1, 8).nl();

            if (changed)
                UpdateTextureName();

            int w = Width;
                ("Will result in X:" + w + " Z:" + w + " Y:" + Height + "volume").nl();

            "Matserials".edit_List_Obj(materials, true);

            if (InspectedPainter != null) {
                var pmat = InspectedPainter.Material;
                if (pmat != null &&  materials.Contains(pmat) && "Remove This Material".Click().nl())
                   materials.Remove(pmat);
            }

          

            if (materials.Count>0 && ("Update Materials".Click().nl() || changed))
                UpdateMaterials();
            
            return changed;
        }

#endif

        public virtual void UpdateMaterials() {
            materials.SetVolumeTexture(MaterialPropertyName, this);
        }
        
        Vector3 previousWorldPosition = Vector3.zero;
        public virtual void Update() {
            if (previousWorldPosition != transform.position) {
                previousWorldPosition = transform.position;
                materials.SetVolumeTexture(MaterialPropertyName, this);
            }
        }

        public virtual void OnEnable() {
            if (materials == null)
                materials = new List<Material>();

            ImageData = ImageData.EnsureStaticInstance();

            all.Add(this);
        }

        public virtual void OnDisable() {
            if (all.Contains(this))
            all.Remove(this);
        }

        public virtual void OnDrawGizmosSelected()
        {
            if (ImageData != null)
            {
                Vector3 center = transform.position;
                var w = Width;
                center.y += Height * 0.5f * size;
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(center, new Vector3(w, Height, w) * size);
            }
        }

        public virtual bool DrawGizmosOnPainter (PlaytimePainter pntr) { return false;  }

    }
}
