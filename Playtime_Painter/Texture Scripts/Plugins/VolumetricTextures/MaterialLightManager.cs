using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
using QuizCannersUtilities;

namespace Playtime_Painter {
    
    [System.Serializable]
    public class MaterialLightManager : PainterStuff, IPEGI
    {
        public const int maxLights = 2;

        public int[] probes;

        public LightCaster GetLight (int number) => LightCaster.AllProbes[probes[number]];

        private static List<ShaderProperty.VectorValue> _positionProperties;

        private static List<ShaderProperty.VectorValue> _colorVectorProperties;


        private static List<ShaderProperty.VectorValue> _positionPropertiesGlobal;

        private static List<ShaderProperty.VectorValue> _colorVectorPropertiesGlobal;


        public MaterialLightManager() {

            if (_positionProperties.IsNullOrEmpty())
            {
                _positionProperties = new List<ShaderProperty.VectorValue>();
                _colorVectorProperties = new List<ShaderProperty.VectorValue>();
                _positionPropertiesGlobal = new List<ShaderProperty.VectorValue>();
                _colorVectorPropertiesGlobal = new List<ShaderProperty.VectorValue>();

                for (var c = 0; c < maxLights; c++) {
                    _positionProperties.Add(new ShaderProperty.VectorValue("l{0}pos".F(c)));
                    _colorVectorProperties.Add(new ShaderProperty.VectorValue("l{0}col".F(c)));

                    _positionPropertiesGlobal.Add(new ShaderProperty.VectorValue("{0}l{1}pos".F(PainterDataAndConfig.GlobalPropertyPrefix, c)));
                    _colorVectorPropertiesGlobal.Add(new ShaderProperty.VectorValue("{0}l{1}col".F(PainterDataAndConfig.GlobalPropertyPrefix, c)));
                }
            }

            if (probes == null) 
                probes = new int[maxLights];
        }

        #if PEGI
        public static int probeChanged = -1;

        public virtual bool Inspect() {

            var changed = false;

            probeChanged = -1;

            if (probes == null)
                probes = new int[maxLights];
            
            for (var c = 0; c < maxLights; c++) {

                var ind = probes[c];

                if (ind < 0)
                {
                    pegi.write(((ColorChanel)c).GetIcon());
                    if (icon.Add.Click().nl(ref changed))
                    {
                        probes[c] = 0;
                        probeChanged = c;
                    }

                }
                else
                {
                    
                    var prb = LightCaster.AllProbes[ind];

                    if (!prb)
                        ("Probe " + ind).write(50);
                    else
                        if (icon.Delete.Click(ref changed)) {
                            probes[c] = -1;
                            probeChanged = c;
                        }
                    
                    if ("Light:".select_iGotIndex("Select Light Source" ,50, ref ind, LightCaster.AllProbes.GetAllObjsNoOrder()).nl(ref changed)) {
                        probes[c] = ind;
                        probeChanged = c;
                    }
                        
                }
                    
            }
                pegi.space();
                pegi.newLine();
        
            return changed;
        }

#endif

        public void UpdateLightsGlobal() {

            for (var c = 0; c < maxLights; c++)  {

                var col = Color.black;
                var pos = Vector3.zero;

                var l = GetLight(c);

                if (l) {
                    col = l.ecol * l.brightness;
                    pos = l.transform.position;
                }

                col.a = 0;

                _colorVectorPropertiesGlobal[c].SetGlobal(col.ToVector4());
                _positionPropertiesGlobal[c].SetGlobal(pos);
                    
            }
        }

        public void UpdateLightOnMaterials(List<Material> materials)
        {
            if (materials.Count <= 0) return;
            
            for (var c = 0; c < maxLights; c++) {

                var col = Color.black;
                var pos = Vector3.zero;

                var l = GetLight(c);

                if (l) {
                    col = l.ecol * l.brightness;
                    pos = l.transform.position;
                }

                col.a = 0;

                foreach (var m in materials)
                    if (m) {
                        m.Set(_colorVectorProperties[c], col.ToVector4());
                        m.Set(_positionProperties[c], pos);
                    }
            }
        }
    }
}