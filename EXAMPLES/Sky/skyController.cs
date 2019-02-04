using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{

    [ExecuteInEditMode]
    public class SkyController : MonoBehaviour
    {

        public Light directional;
        public MeshRenderer _rendy;

        void FindComponents()
        {
            if (!_rendy)
                _rendy = GetComponent<MeshRenderer>();

            if (!directional)
            {
                Light[] ls = FindObjectsOfType<Light>();
                for (int i = 0; i < ls.Length; i++)
                    if (ls[i].type == LightType.Directional)
                    {
                        directional = ls[i];
                        i = ls.Length;
                    }
            }
        }

        private void OnEnable()
        {
            FindComponents();
            _rendy.enabled = Application.isPlaying;
        }

        public float skyDynamics = 0.1f;

        ShaderProperty.VectorValue sunDirection_Property = new ShaderProperty.VectorValue("_SunDirection");
        ShaderProperty.ColorValue directionalColor_Property = new ShaderProperty.ColorValue("_Directional");
        ShaderProperty.VectorValue offset_Property = new ShaderProperty.VectorValue("_Off");

        public virtual void Update() {

            if (directional != null) {
                Vector3 v3 = directional.transform.rotation * Vector3.back;
                sunDirection_Property.GlobalValue = new Vector4(v3.x, v3.y, v3.z);
                directionalColor_Property.GlobalValue = directional.color;
            }
            Camera c = Camera.main;
            if (c != null)
            {
                Vector3 pos = c.transform.position * skyDynamics;
                offset_Property.GlobalValue = new Vector4(pos.x, pos.z, 0f, 0f);
            }
        }

        void LateUpdate() => transform.rotation = Quaternion.identity;
    }
}