using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter {

#if UNITY_EDITOR

    using UnityEditor;

    [CustomEditor(typeof(BakedShadowMaterialController))]
    public class BakedShadowMaterialControllerEditor : Editor {

        public override void OnInspectorGUI() {
            ef.start(serializedObject);
            ((BakedShadowMaterialController)target).PEGI();
            ef.end();
        }
    }
#endif

    [ExecuteInEditMode]
    public class BakedShadowMaterialController : MonoBehaviour {
        
        public Material material;
        public List<int>[] probes;
        public float[] bounceCoefficient = new float[3];

        public void OnEnable() {
            if (material == null) {
                var rendy = GetComponent<MeshRenderer>();
                if (rendy) material = rendy.sharedMaterial;
            }

            if (probes == null) {
                probes = new List<int>[3];
                for (int c = 0; c < 3; c++) probes[c] = new List<int>();
            } 

        }

        public int browsedNode = -1;
        public virtual bool PEGI() {

            bool changed = false;

  

            changed |= "Material".edit(60, ref material).nl();

           

            for (int c = 0; c < 3; c++) {

                pegi.write(((ColorChanel)c).getIcon());
                if (icon.Add.Click().nl())
                    probes[c].Add(0);

                var lst = probes[c];
               

                for (int i=0; i< lst.Count; i++) {
                    var tmp = lst[i];
                    var prb = BakedShadowsLightProbe.allProbes[tmp];

                    if (icon.Delete.Click())
                        lst.RemoveAt(i);
                    else
                    {

                      //  if (prb != null && pegi.foldout("Probe:", ref browsedNode, tmp))
                       //     changed |= prb.PEGI().nl();
                       // else
                        if (pegi.select(ref tmp, BakedShadowsLightProbe.allProbes).nl())
                        {
                            probes[c][i] = tmp;
                            changed = true;
                        }
                    }
                }

                if (lst.Count > 0)
                    changed |= "Bounce Coefficient".edit(ref bounceCoefficient[c]).nl();
                pegi.Space();
                pegi.newLine();
            }

            if (changed && !Application.isPlaying)
                Update();

            return changed;
        }
        
        void Update() {

            if (material != null) 
                for (int c=0; c<3; c++) {

                    Color col = Color.black;
                    Vector3 pos = Vector3.zero;
                    int cnt = 0;

                    foreach(var i in probes[c]) {
                        var p = BakedShadowsLightProbe.allProbes[i];
                        if (p!= null) {
                            col += p.ecol * p.brightness;
                            cnt++;
                            pos += p.transform.position;
                        }
                    }

                    if (cnt>0)
                        pos /= cnt;

                    col.a = bounceCoefficient[c];

                    material.SetVector("l" + c + "col", col.ToVector4());
                    material.SetVector("l" + c + "pos", pos);
                }
        }
    }
}