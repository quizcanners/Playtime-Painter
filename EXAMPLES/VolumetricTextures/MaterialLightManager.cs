using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter {
    
    [System.Serializable]
    public class MaterialLightManager {
        
        public List<Material> materials;
        public List<int>[] probes;
        public float[] bounceCoefficient = new float[3];

        public MaterialLightManager() {
            
            if (probes == null) {
                probes = new List<int>[3];
                for (int c = 0; c < 3; c++) probes[c] = new List<int>();
            }

            materials = new List<Material>();

        }

        public int browsedNode = -1;
        public virtual bool PEGI() {

            bool changed = false;

            if (materials == null) materials = new List<Material>();

            "Materials:".nl();

            materials.PEGI<Material>(true);

            for (int c = 0; c < 3; c++) {

                pegi.write(((ColorChanel)c).getIcon());
                if (icon.Add.Click().nl())
                    probes[c].Add(0);

                var lst = probes[c];
               
                for (int i=0; i< lst.Count; i++) {
                    var tmp = lst[i];
                    var prb = BakedShadowsLightProbe.allProbes[tmp];

                    if (icon.Delete.Click())
                    {
                        changed = true;
                        lst.RemoveAt(i);
                    }
                    else
                    {
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
                UpdateMaterials();

            return changed;
        }
        
        public void UpdateMaterials() {

            if (materials.Count>0) 
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

                    foreach (var m in materials)
                        if (m!= null) {
                        m.SetVector("l" + c + "col", col.ToVector4());
                        m.SetVector("l" + c + "pos", pos);
                    }
                }
        }
    }
}