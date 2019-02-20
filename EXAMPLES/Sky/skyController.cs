using UnityEngine;
using QuizCannersUtilities;

namespace Playtime_Painter.Examples
{

    [ExecuteInEditMode]
    public class SkyController : MonoBehaviour
    {

        public Light directional;
        public MeshRenderer skeRenderer;

        private void FindComponents()
        {
            if (!skeRenderer)
                skeRenderer = GetComponent<MeshRenderer>();

            if (directional) return;

            var ls = FindObjectsOfType<Light>();

            for (var i = 0; i < ls.Length; i++)
                if (ls[i].type == LightType.Directional)
                {
                    directional = ls[i];
                    i = ls.Length;
                }
        }

        private void OnEnable()
        {
            FindComponents();
            skeRenderer.enabled = Application.isPlaying;
        }

        public float skyDynamics = 0.1f;

        private readonly ShaderProperty.VectorValue _sunDirectionProperty = new ShaderProperty.VectorValue("_SunDirection");
        private readonly ShaderProperty.ColorValue _directionalColorProperty = new ShaderProperty.ColorValue("_Directional");
        private readonly ShaderProperty.VectorValue _offsetProperty = new ShaderProperty.VectorValue("_Off");

        public virtual void Update() {

            if (directional != null) {
                var v3 = directional.transform.rotation * Vector3.back;
                _sunDirectionProperty.GlobalValue = new Vector4(v3.x, v3.y, v3.z);
                _directionalColorProperty.GlobalValue = directional.color;
            }

            var c = Camera.main;
            if (!c) return;

            var pos = c.transform.position * skyDynamics;

            _offsetProperty.GlobalValue = new Vector4(pos.x, pos.z, 0f, 0f);
        }

        private void LateUpdate() => transform.rotation = Quaternion.identity;
    }
}