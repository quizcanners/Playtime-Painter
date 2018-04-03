using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Playtime_Painter
{

    [CustomEditor(typeof(WaterController))]
    public class WaterEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            ef.start(serializedObject);
            WaterController trg = (WaterController)target;

            bool modified = false;

            ef.write("Thickness:", 70);
            modified |= ef.edit(ref trg.thickness, 5, 300);
            ef.newLine();
            ef.write("Noise:", 50);
            modified |= ef.edit(ref trg.noise, 0, 100);
            ef.newLine();
            ef.write("Upscale:", 50);
            modified |= ef.edit(ref trg.upscale, 1, 64);
            ef.newLine();
            ef.write("Wet Area Height:", 50);
            modified |= ef.edit(ref trg.wetAreaHeight, 0.1f, 10);
            ef.newLine();
            if (modified) { trg.setFoamDynamics();  UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
            ef.end();

        }
    }
#endif



    [ExecuteInEditMode]
    public class WaterController : MonoBehaviour
    {

        //    public Texture2D foamTexture;

        private void OnEnable()
        {

            setFoamDynamics();
            Shader.EnableKeyword("WATER_FOAM");
        }

        private void OnDisable()
        {
            Shader.DisableKeyword("WATER_FOAM");
        }

        public Vector4 foamParameters;
        float MyTime = 0;
        public float thickness;
        public float noise;
        public float upscale;
        public float wetAreaHeight;
      

        public void setFoamDynamics() {
            Shader.SetGlobalVector("_foamDynamics", new Vector4(thickness, noise, upscale, (300 - thickness)));

        }
        
        // Update is called once per frame
        void Update()
        {

            if ((Application.isPlaying) || (gameObject.isFocused()))
            {
                MyTime += Time.deltaTime;
            }

            foamParameters.x = MyTime;
            foamParameters.y = MyTime * 0.6f;
            foamParameters.z = transform.position.y;
            foamParameters.w = wetAreaHeight;

            Shader.SetGlobalVector("_foamParams", foamParameters);


        }
    }
}