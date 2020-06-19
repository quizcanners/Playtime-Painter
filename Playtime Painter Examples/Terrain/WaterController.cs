using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEngine;

namespace PlaytimePainter.Examples
{
    [ExecuteInEditMode]
    public class WaterController : MonoBehaviour, IPEGI
    {
        private readonly ShaderProperty.VectorValue _foamParametersProperty = new ShaderProperty.VectorValue("_qcPp_foamParams");
        private readonly ShaderProperty.TextureValue _qcPp_waterBumpMap = new ShaderProperty.TextureValue("_qcPp_WaterBump");
        private readonly ShaderProperty.ShaderKeyword _waterFoam = new ShaderProperty.ShaderKeyword("_qcPp_WATER_FOAM");
        

        private void OnEnable()
        {
            SetFoamDynamics();
            _waterFoam.Enabled = true;
        }

        private void OnDisable()
        {
            _waterFoam.Enabled = false;
        }
        
        public Texture waterBump;
        public Vector4 foamParameters;
        private float _myTime;
        public float wetAreaHeight;

        private void SetFoamDynamics() {

            _qcPp_waterBumpMap.GlobalValue = waterBump;
        }

        private void Update() {
            
            _myTime += Time.deltaTime;
            
            foamParameters.x = _myTime;
            foamParameters.y = _myTime * 0.6f;
            foamParameters.z = transform.position.y;
            foamParameters.w = wetAreaHeight;

            _foamParametersProperty.GlobalValue = foamParameters;
        }

        #region Inspector

        public bool Inspect() {
            var changed = false;

            if (pegi.FullWindowService.DocumentationClick("About Water Controller"))  
            pegi.FullWindowService.FullWindwDocumentationOpen("This water works only with Merging Terrain shaders. The method is as follows: {0}" +
            "Terrain shaders compare it's Y(up) position with water height and calculate where the foam should be." +
            "THos shaders paint foam onto themselves. Below the foam they pain screen alpha channel 1, and above - 0." +
            "Water just renders the plane, but multiplies it by screen's alpha rendered by underlying objects. " +
            "This is one of those methods I labeled as EXPERIMENTAL in the Asset description. And it will probably will stay in that category" +
            "since it is not a robust method. And I only recommend working on those method at the later stages of your project, " +
            "when you can be sure it will not conflict with other effects.");

            "Bump".edit(70, ref waterBump).nl(ref changed);

            "Wet Area Height:".edit(50, ref wetAreaHeight, 0.1f, 10).nl(ref changed);

            if (changed) {
                SetFoamDynamics();
                QcUnity.RepaintViews(); 
                this.SetToDirty();
            }
            
            return changed;
        }

        
        #endregion


        
    }
}