using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter {

#if UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(VolumeShadowProbe))]
    public class VolumeShadowProbeEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            ef.start(serializedObject);
            ((VolumeShadowProbe)target).PEGI();
            ef.end();
        }
    }
#endif

    public class VolumeShadowProbe : BakedShadowMaterialController {

        public Texture2D tex;
        public int height = 1;
        public float size;
        public int[] volume;

        protected int width { get { return ((tex == null ? tmpWidth : tex.width) - height * 2) / height;  } }


        public int positionToIndex (Vector3 pos) {
            pos /= size;
            int hw = width / 2;

            int y = (int)Mathf.Clamp(pos.y, 0, height-1);
            int z = (int)Mathf.Clamp(pos.z + hw, 0, hw - 1);
            int x = (int)Mathf.Clamp(pos.x+hw, 0, hw-1);
            
            return ((y*width + z)*width + x);
        }

        public Vector3 IndexToPosition(int index)  {

            int plane = width * width;
            int y = index / plane;
            int z = (index - y * plane);

            int x = z;
            z /= width;
            x -= z * width;

            int hw = width / 2;
            Vector3 v3 = new Vector3(x-hw, y-hw, z-hw) * size;

            return v3;
        }

        public Color RunRaycasts(Vector3 position) {
            return new Color(position.x, position.y, position.z, 0.5f);
        }

        public void RecalculateVolume() {
            int w = width;
            int size = w * w * height;

            if (volume == null || volume.Length != size)
                volume = new int[size];

            int hw = width / 2;


        }


        public void OnDrawGizmosSelected() {
            if (tex != null) {
                var w = width;
                Gizmos.DrawCube(transform.position, new Vector3(w,height,w)*size);
            }
        }

        // Update is called once per frame

        public static int tmpWidth = 1024;
        public override bool PEGI() {
            bool changed = base.PEGI();

            if (volume != null) {
                ("Got " + width + " * " + width + " * " + height + " volume").nl();
                if ("Clear".Click())
                    volume = null;
            } else {

                changed |= "Texture".edit(ref tex);

                if (tex == null) {
                    if (TexturesPool._inst == null)  {
                        pegi.nl();
                        changed |= "Texture Width".edit(ref tmpWidth);
                        if ("Create Pool".Click().nl())
                        {
                            tmpWidth = Mathf.ClosestPowerOfTwo(Mathf.Clamp(tmpWidth, 128, 2048));
                            TexturesPool.inst.width = tmpWidth;
                        }
                    } else {
                        if ("Get From Pool".Click().nl())
                            tex = TexturesPool._inst.GetTexture2D();
                    }
                }

                changed |= "Texture Height".edit(ref height, 1, 32).nl();

                if (tex != null) {
                    int w = width;
                    ("Will result in " + w + "*" + w + "*" + height + "volume").nl();

                   
                }
            }


            "Size".edit(ref size).nl();

            if (tex != null && "Recalculate".Click().nl()) {
                changed = true;
                RecalculateVolume();
            }

            return changed;
        }
    }
}