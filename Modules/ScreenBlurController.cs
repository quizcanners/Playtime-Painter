using System;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
using QuizCannersUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PlaytimePainter
{
    public class ScreenBlurController : MonoBehaviour, IPEGI
    {
        public static List<ScreenBlurController> instances = new List<ScreenBlurController>(1);
        
        private Action _onCaptured;

        public void RequestUpdate(Action OnCaptured)
        {
            _onCaptured = OnCaptured;
            step = BlurStep.Requested;
        }
        
        private Texture2D _screenTexture;
        private Texture2D ScreenTexture
        {
            get
            {
                if (!_screenTexture || _screenTexture.width != Screen.width || _screenTexture.height != Screen.height)
                {
                    if (_screenTexture)
                    {
                        Destroy(_screenTexture);
                    }

                    _screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, mipChain: false)
                    {
                        name = "Screen Grab"
                    };
                }

                return _screenTexture;
            }
        }

        public int blurIterations = 10;
        
        [SerializeField] protected Material _effectMaterial;
        Material _effectMaterialInstance;
        Material EffectMaterial
        {
            get
            {
                if (_effectMaterialInstance)
                {
                    return _effectMaterialInstance;
                }

                _effectMaterialInstance = Instantiate(_effectMaterial);

                return _effectMaterialInstance;
            }
        }

        [SerializeField] protected Shader copyShader;
        [SerializeField] protected Shader blurShader;

        private void OnPostRender()
        {
            if (step == BlurStep.Requested)
            {
                step = BlurStep.ReturnedFromCamera;
                var tex = ScreenTexture;
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                tex.Apply();
                _onCaptured?.Invoke();
            }
        }
        
        public bool useCrt = false;
        public bool useCustomGLblit = false;

        [SerializeField] protected CustomRenderTexture _effetBuffer;
        
        private bool _latestIsZero;
        private void Swap() => _latestIsZero = !_latestIsZero;

        [SerializeField] protected RenderTexture[] _renderTextures;
        private RenderTexture MainTexture => useCrt ? _effetBuffer : (_latestIsZero ? _renderTextures[0] : _renderTextures[1]);
        private RenderTexture PreviousTexture => useCrt ? _effetBuffer : (_latestIsZero ? _renderTextures[1] : _renderTextures[0]);
        
        private void BlitRt(Shader shader)
        {
            EffectMaterial.shader = shader;

            if (useCrt)
            {
                _effetBuffer.material = EffectMaterial;
                _effetBuffer.Update();
            }
            else
            {
                Swap();
                Graphics.Blit(PreviousTexture, MainTexture, EffectMaterial);
            }
        }

        private void BlitRt(Texture tex, Shader shader = null)
        {

            EffectMaterial.shader = shader;

            if (useCrt)
            {
                _effetBuffer.material = EffectMaterial;
                _effetBuffer.Update();
            }
            else
            {
                if (shader)
                {
                    if (useCustomGLblit)
                        RenderTextureBuffersManager.Blit(tex, MainTexture, EffectMaterial);
                    else
                        Graphics.Blit(tex, MainTexture, EffectMaterial);
                }
                else
                    Graphics.Blit(tex, MainTexture);

            }
        }

        protected enum BlurStep
        {
            Off,
            Requested,
            ReturnedFromCamera,
            Blurring
        }

        public enum ProcessCommand
        {
            Blur, Nothing
        }

        [NonSerialized] protected ProcessCommand command;

        [NonSerialized] protected BlurStep step = BlurStep.Off;

        private int blurIteration;
        
        public void Reset() => step = BlurStep.Off;

        public void Update()
        {
            switch (step)
            {
                case BlurStep.ReturnedFromCamera:

                    BlitRt(_screenTexture, copyShader);
                    step = BlurStep.Blurring;
                    blurIteration = 0;

                    if (command == ProcessCommand.Nothing)
                    {
                        try
                        {
                            _onCaptured?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }

                        step = BlurStep.Off;
                    }

                    break;
                case BlurStep.Blurring:

                    if (blurIteration == 0)
                    {
                        try
                        {
                            _onCaptured?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }
                    }

                    blurIteration++;

                    BlitRt(blurShader);

                    if (blurIteration > blurIterations)
                    {
                        step = BlurStep.Off;
                    }
                    break;
            }
        }

        #region Inspector
        public bool Inspect()
        {
            var changed = pegi.toggleDefaultInspector(this).nl();

            if (!gameObject.GetComponent<Camera>())
                "No Camera Detected on this Game Object. I need camera".writeWarning();

            "Use CRT".toggleIcon(ref useCrt).nl();

            if (!useCrt)
                "Use custom GL blit".toggleIcon(ref useCustomGLblit).nl();

            "State".editEnum(ref step).nl();

            if (copyShader)
                "Copy Shader. Supported={0} | Pass Count = {1}".F(copyShader.isSupported, copyShader.passCount).nl();

            pegi.nl();
            "Screen texture".write();

            if (Application.isPlaying && icon.Refresh.Click())
                RequestUpdate(() => Debug.Log("Screen Generating done"));
            
            pegi.nl();

            if (_screenTexture)
                pegi.write(_screenTexture, 250);
            
            pegi.nl();

            "Buffers".write();

            if ("Swap".Click())
                Swap();

            if ("Copy Screen".Click())
                BlitRt(_screenTexture, copyShader);

            if ("Blur".Click())
                BlitRt(blurShader);

            pegi.nl();

            if (!useCrt)
            {
                if (!_renderTextures.IsNullOrEmpty())
                {
                    "Main Texture: {0}".F(MainTexture.name).nl();

                    MainTexture.write(250, alphaBlend: false);
                    pegi.nl();

                    "Previous Texture: {0}".F(PreviousTexture.name).nl();
                    PreviousTexture.write(250, alphaBlend: false);
                    pegi.nl();
                }
            }
            else
            {
                _effetBuffer.write(250, alphaBlend: false);
                pegi.nl();
            }
            
            return changed;
        }
        #endregion
        
        void OnEnable() => instances.Add(this);

        void OnDisable() => instances.Remove(this);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ScreenBlurController))]
    public class ScreenBlurControllerDrawer : PEGI_Inspector_Mono<ScreenBlurController> {}
#endif

}