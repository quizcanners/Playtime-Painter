using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlayerAndEditorGUI;

namespace Playtime_Painter {

    [Serializable]
    public class VolumeTexture : MonoBehaviour, iPEGI, iGotName  {

        public int h_slices = 1;
        public float size = 1;
        [NonSerialized]
        Color[] volume;
        public imgData tex;

        public string Name { get { return name; } set { name = value; } }

        public int height { get { return h_slices * h_slices; } }
        public int width
        {
            get
            {
                return ((tex == null ?
(TexturesPool._inst == null ? tmpWidth : TexturesPool._inst.width)
: tex.width) //- h_slices * 2
) / h_slices;
            }
        }

        public VolumeTexture() {
            if (VolumePaintingPlugin._inst != null)
                VolumePaintingPlugin._inst.volumes.Add(this);
        }

        public virtual Color GetColorFor(Vector3 pos) {
            float magnitude = (LastCenterPos - pos).magnitude;

            return Color.white * (width * size * 0.5f - magnitude); 
        }

        public virtual void AssignTo(PlaytimePainter p) {
              p.ChangeTexture(tex.currentTexture());
        }

        Vector3 LastCenterPos;
        public virtual void RecalculateVolume(Vector3 center) {
            LastCenterPos = center;
            int w = width;
            int volumeLength = w * w * height;

            if (volume == null || volume.Length != volumeLength)
                volume = new Color[volumeLength];

            int hw = width / 2;

            Vector3 pos = Vector3.zero;

            for (int h = 0; h < height; h++)  {
                pos.y = center.y + h * size;
                for (int y = 0; y < w; y++)  {
                    pos.z = center.z + ((float)(y - hw)) * size;
                    int index = (h * width + y) * width;

                    for (int x = 0; x < w; x++) {
                        pos.x = center.x + ((float)(x - hw)) * size;
                        volume[index + x] = GetColorFor(pos);
                    }
                }
            }



        }

        public virtual void DrawGizmo (Vector3 center, Color col)
        {
            if (tex != null) {
                var w = width;
                center.y += height * 0.5f * size;
                Gizmos.DrawWireCube(center, new Vector3(w, height, w) * size);
            }
        }
        
        public virtual void VolumeToTexture()
        {
            if (tex == null)
            {
                if (TexturesPool._inst != null)
                    tex = TexturesPool._inst.GetTexture2D().getImgData();
                else
                {
                    Debug.Log("No Texture for Volume");
                    return;
                }
                UpdateTextureName();
            }

            Color[] pixels = tex.pixels;//new Color32[tex.width * tex.width];

            int texSectorW = tex.width / h_slices;
            int w = width;

            for (int hy = 0; hy < h_slices; hy++)
            {
                for (int hx = 0; hx < h_slices; hx++)
                {

                    int hTex_index = (hy * tex.width + hx) * texSectorW;

                    int h = hy * h_slices + hx;

                    for (int y = 0; y < w; y++)
                    {
                        int yTex_index = hTex_index + y * tex.width;

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

            tex.SetAndApply(false);//SetPixels32(pixels);
            //tex.//Apply(false);
            UpdateTextureName();


        }

        public int positionToVolumeIndex(Vector3 pos)
        {
            pos /= size;
            int hw = width / 2;

            int y = (int)Mathf.Clamp(pos.y, 0, height - 1);
            int z = (int)Mathf.Clamp(pos.z + hw, 0, hw - 1);
            int x = (int)Mathf.Clamp(pos.x + hw, 0, hw - 1);

            return ((y * width + z) * width + x);
        }
        
        public int positionToPixelIndex(Vector3 pos) {
            pos /= size;
            int hw = width / 2;

            int y = (int)Mathf.Clamp(pos.y, 0, height - 1);
            int z = (int)Mathf.Clamp(pos.z + hw, 0, hw - 1);
            int x = (int)Mathf.Clamp(pos.x + hw, 0, hw - 1);

            int hy = y / h_slices;
            int hx = y % h_slices;

            return volumeToPixelIndex(hx, hy, z, x);
        }

        public int volumeToPixelIndex(int hx, int hy, int y, int x)
        {

            int hTex_index = (hy * tex.width + hx) * tex.width / h_slices;

            int yTex_index = hTex_index + (y) * tex.width;

            int texIndex = yTex_index + x;

            return texIndex;
        }
        
        public Vector3 VolumeIndexToPosition(int index)
        {

            int plane = width * width;
            int y = index / plane;
            int z = (index - y * plane);

            int x = z;
            z /= width;
            x -= z * width;

            int hw = width / 2;
            Vector3 v3 = new Vector3(x - hw, y - hw, z - hw) * size;

            return v3;
        }

        public void UpdateTextureName() {
            if (tex != null) {
                tex.SaveName = name + VolumePaintingPlugin.VolumeTextureTag + h_slices.ToString() + VolumePaintingPlugin.VolumeSlicesCountTag; ;
                if (tex.texture2D != null) tex.texture2D.name = tex.SaveName;
                if (tex.renderTexture != null) tex.renderTexture.name = tex.SaveName;
            }
        }
        
        public static int tmpWidth = 1024;
        public virtual bool PEGI() {
            bool changed = false;

            if ((VolumePaintingPlugin._inst != null) && (!VolumePaintingPlugin._inst.volumes.Contains(this)))
                VolumePaintingPlugin._inst.volumes.Add(this);

            string n = name;
            if ("Name".editDelayed(30, ref n).nl()) {
                name = n;
                changed = true;
                UpdateTextureName();
            }
            
            if (volume != null && volume.Length != width * width * height)  {
                volume = null;
                Debug.Log("Clearing volume");
            }

            var texture = tex.currentTexture();
         
            if ("Texture".edit(60, ref texture).nl() || (texture == null)) {
                changed = true;
                tex = texture == null ? null : texture.getImgData();
            }

            changed |= "Volume Scale".edit(70,ref size).nl();
            size = Mathf.Max(0.0001f, size);

            if (volume != null)
            {
                ("Got " + width + " * " + width + " * " + height + " volume").nl();
                if ("Clear".Click())
                    volume = null;
            }

                if (tex == null) {
                    if (TexturesPool._inst == null)
                    {
                        pegi.nl();
                        changed |= "Texture Width".edit(ref tmpWidth);
                        if ("Create Pool".Click().nl())
                        {
                            tmpWidth = Mathf.ClosestPowerOfTwo(Mathf.Clamp(tmpWidth, 128, 2048));
                            TexturesPool.inst.width = tmpWidth;
                        }
                    }
                    else {
                        if ("Get From Pool".Click().nl())
                            tex = TexturesPool._inst.GetTexture2D().getImgData();
                    }
                }
                pegi.nl();

                changed |= "Slices:".edit("How texture will be sliced for height", 80, ref h_slices, 1, 8).nl();



                // if (tex != null) {
                int w = width;
                ("Will result in X:" + w + " Z:" + w + " Y:" + height + "volume").nl();

                //}
            


            return changed;
        }

    }
}
