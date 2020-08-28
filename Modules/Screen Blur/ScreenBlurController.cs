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

    [ExecuteInEditMode]
    public class ScreenBlurController : MonoBehaviour, IPEGI
    {
        public static List<ScreenBlurController> instances = new List<ScreenBlurController>(1);

        private Action _onFirstRender;
        private Action _onUpdated;

       

        public Texture CurrentTexture
        {
            get
            {
                if (IsUsingCrt)
                    return _effetBuffer;
                else
                    return MainTexture;
                    
                /*switch (step)
                {
                    case BlurStep.ReturnedFromCamera: return ScreenTexture;
                    case BlurStep.Blurring: return MainTexture;
                    default: return MainTexture;
                }*/
            }
        }

        public void RequestUpdate(Action OnFirstRendered = null, Action OnUpdated = null)
        {
            _onFirstRender = OnFirstRendered;
            _onUpdated = OnUpdated;
            step = BlurStep.Requested;
        }
        
        [SerializeField] private Texture2D _screenTexture;
        public Texture2D ScreenTexture
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

        [SerializeField] private int postProcessIteration = 10;
        
        [SerializeField] protected Material materialPrototype;
        Material _effectMaterialInstance;
        Material EffectMaterial
        {
            get
            {
                if (_effectMaterialInstance)
                {
                    return _effectMaterialInstance;
                }
                
                _effectMaterialInstance = materialPrototype ? Instantiate(materialPrototype) : new Material(Shader.Find("Diffuse"));

                return _effectMaterialInstance;
            }
        }

        [SerializeField] protected Shader copyShader;
        [SerializeField] protected Shader postProcessShader;
        
        private void OnPostRender()
        {
            if (step == BlurStep.Requested)
            {
                step = BlurStep.ReturnedFromCamera;
                var tex = ScreenTexture;
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                tex.Apply();
                _onFirstRender?.Invoke();
                _onUpdated?.Invoke();
            }
        }

        [SerializeField]
        private PostProcessMethod _postProcessMethod;
        private bool IsUsingCrt => _postProcessMethod == PostProcessMethod.CustomRenderTexture;
        public enum PostProcessMethod
        {
            CustomRenderTexture,
            GraphicsBlit,
            GlBlit
        }


        [SerializeField] protected CustomRenderTexture _effetBuffer;
        
        private bool _latestIsZero;
        private void Swap() => _latestIsZero = !_latestIsZero;

        [SerializeField] protected RenderTexture[] _renderTextures;
        private RenderTexture MainTexture => IsUsingCrt ? _effetBuffer : (_latestIsZero ? _renderTextures[0] : _renderTextures[1]);
        private RenderTexture PreviousTexture => IsUsingCrt ? _effetBuffer : (_latestIsZero ? _renderTextures[1] : _renderTextures[0]);
        
        private void BlitRt(Shader shader)
        {
            var mat = EffectMaterial;
            mat.shader = shader;

            if (IsUsingCrt)
            {
                _effetBuffer.material = mat;
                _effetBuffer.Update();
            }
            else
            {
                Swap();
                Graphics.Blit(PreviousTexture, MainTexture, mat);
            }

            _onUpdated?.Invoke();

        }
        
        private void BlitRt(Texture tex, Shader shader = null)
        {
            if (shader)
            {
                EffectMaterial.shader = shader;
            }

            if (IsUsingCrt)
            {
                _effetBuffer.material = EffectMaterial;
                _effetBuffer.Update();
            }
            else
            {
                if (shader)
                {
                    if (_postProcessMethod == PostProcessMethod.GlBlit)
                        RenderTextureBuffersManager.BlitGL(tex, MainTexture, EffectMaterial);
                    else
                        Graphics.Blit(tex, MainTexture, EffectMaterial);
                }
                else
                    Graphics.Blit(tex, MainTexture);

            }

            _onUpdated?.Invoke();
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
                            _onUpdated?.Invoke();
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
                            _onUpdated?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError(ex);
                        }
                    }

                    blurIteration++;

                    BlitRt(postProcessShader);

                    if (blurIteration > postProcessIteration)
                    {
                        step = BlurStep.Off;
                    }
                    break;
            }
        }

        #region Inspector

        private bool _showDependencies;

        public bool Inspect()
        {
            var changed = pegi.toggleDefaultInspector(this).nl();

            if (!gameObject.GetComponent<Camera>())
                "No Camera Detected on this Game Object. I need camera".writeWarning();

            "Post Process Method".editEnum(ref _postProcessMethod).nl();
            
            pegi.nl();
          
            "Copy shader".edit(ref copyShader).nl();

            "Post-Process".edit(ref postProcessShader).nl();

            "Iterations:".edit(ref postProcessIteration).nl(); 

            pegi.nl();

            if (_screenTexture)
            {
                "Screen".edit(ref _screenTexture);
                if (icon.Refresh.Click().nl())
                    RequestUpdate(() => Debug.Log("Screen Generating done"));
            }
            else if ("Request Screen Render".Click())
                RequestUpdate(() => Debug.Log("Screen Generating done"));

            pegi.nl();

            if (!IsUsingCrt)
            {
                if (_renderTextures.Length < 2)
                {
                    pegi.writeWarning("Need 2 Render Textures for Post-Process");
                    if ("Fix".Click())
                        _renderTextures = new RenderTexture[2];
                }
                else
                {
                    "Render Textures".edit_Array(ref _renderTextures).nl();
                }
            }
            else
            {
                "Custom Render Texture".edit(ref _effetBuffer).nl();
            }

            if ("Debug".foldout(ref _showDependencies).nl())
            {
                if (copyShader)
                    "Copy Shader. Supported={0} | Pass Count = {1}".F(copyShader.isSupported, copyShader.passCount).nl();


                "State".editEnum(ref step).nl();

                "Buffers".write();

                if ("Swap".Click())
                    Swap();

                if ("Screen->Buffer".Click())
                    BlitRt(_screenTexture, copyShader);

                if ("Blur".Click())
                    BlitRt(postProcessShader);

                pegi.nl();

                "Screen Texture (Optional):".edit(ref _screenTexture).nl();

                if (_screenTexture)
                    _screenTexture.write(250, alphaBlend: false);
                
                pegi.nl();

                if (_effectMaterialInstance)
                {
                    "Temp Mat Instance".write(_effectMaterialInstance);
                    if (icon.Clear.Click().nl())
                    {
                        _effectMaterialInstance.DestroyWhateverUnityObject();
                        _effectMaterialInstance = null;
                    }
                }
                else
                {
                    "Material (Optional)".edit(ref materialPrototype).nl(ref changed);
                }


                if (!IsUsingCrt)
                {
                    if (!_renderTextures.IsNullOrEmpty())
                    {
                        "Main Texture: {0}".F(MainTexture).nl();

                        MainTexture.write(250, alphaBlend: false);
                        pegi.nl();

                        "Previous Texture: {0}".F(PreviousTexture).nl();
                        PreviousTexture.write(250, alphaBlend: false);
                        pegi.nl();
                    }
                }
                else
                {
                    _effetBuffer.write(250, alphaBlend: false);
                    pegi.nl();
                }
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