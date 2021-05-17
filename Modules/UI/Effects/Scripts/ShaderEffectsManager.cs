using QuizCanners.Inspect;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizCanners.Utils;
using QuizCanners.Lerp;
using static QuizCanners.Utils.ShaderProperty;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PlaytimePainter
{
    public class ShaderEffectsManager : MonoBehaviour, IPEGI
    {
        public static ShaderEffectsManager instance;

        #region Time
        private readonly FloatValue _shaderTime = new FloatValue("_qcPp_Time");
        #endregion

        #region Mouse Position
        public bool mousePositionToShader;
        protected readonly VectorValue mousePosition = new VectorValue("_qcPp_MousePosition");
        protected readonly ShaderKeyword UseMousePosition = new ShaderKeyword("_qcPp_FEED_MOUSE_POSITION");

        private float mouseDownStrengthOneDirectional;
        private float mouseDownStrength = 0.1f;
        private bool downClickFullyShown = true;
        private Vector2 mouseDownPosition;
        #endregion

        public void ResetTime() => _shaderTime.GlobalValue = 0;

        private void LateUpdate()
        {
            if (_shaderTime.GlobalValue > 64)
                _shaderTime.GlobalValue = 0;
            else
                _shaderTime.SetGlobal(_shaderTime.latestValue + Time.deltaTime);
        }

        public void Update()
        {
            if (mousePositionToShader)
            {
                bool down = Input.GetMouseButton(0);

                if (down || mouseDownStrength > 0)
                {
                    bool downThisFrame = Input.GetMouseButtonDown(0);

                    if (downThisFrame)
                    {
                        mouseDownStrength = 0;
                        mouseDownStrengthOneDirectional = 0;
                        downClickFullyShown = false;
                    }

                    mouseDownStrengthOneDirectional = LerpUtils.LerpBySpeed(mouseDownStrengthOneDirectional,
                        down ? 0 : 1,
                        down ? 4f : (3f - mouseDownStrengthOneDirectional * 3f));

                    mouseDownStrength = LerpUtils.LerpBySpeed(mouseDownStrength,
                        downClickFullyShown ? 0 :
                        (down ? 0.9f : 1f),
                        (down) ? 5 : (downClickFullyShown ? 0.75f : 2.5f));

                    if (mouseDownStrength > 0.99f)
                        downClickFullyShown = true;

                    if (down)
                        mouseDownPosition = Input.mousePosition.XY() / new Vector2(Screen.width, Screen.height);

                    mousePosition.GlobalValue = mouseDownPosition.ToVector4(mouseDownStrength, ((float)Screen.width) / Screen.height);
                }
            }
        }

        private void OnEnable()
        {
            UseMousePosition.Enabled = mousePositionToShader;
            instance = this;
        }

        public void Inspect()
        {
            "Custom Time Parameter".write_ForCopy(_shaderTime.ToString());

            if ("Reset Time".Click())
                ResetTime();

            pegi.nl();

            if ("Mouse Position to shader".toggleIcon(ref mousePositionToShader, hideTextWhenTrue: true))
            {
                UseMousePosition.Enabled = mousePositionToShader;
            }

            if (mousePositionToShader)
                mousePosition.ToString().write_ForCopy();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ShaderEffectsManager))] internal class ShaderEffectsManagerDrawer : PEGI_Inspector { }
#endif
}