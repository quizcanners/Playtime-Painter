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

        [SerializeField] protected Shader copyShader;
        [SerializeField] protected Shader postProcessShader;
        [SerializeField] protected Texture2D screenTexture;
        [SerializeField] protected CustomRenderTexture effetBuffer;
        [SerializeField] protected PostProcessMethod postProcessMethod;
        [SerializeField] protected int postProcessIteration = 10;
        [SerializeField] protected Material materialPrototype;
        [SerializeField] public bool allowScreenGrabToRt;
        [SerializeField] public bool mousePositionToShader;

        [NonSerialized] protected ProcessCommand command;
        [NonSerialized] protected BlurStep step = BlurStep.Off;
        [NonSerialized] protected Material _effectMaterialInstance;
        [NonSerialized] protected Action onFirstRender;

        protected int blurIteration;
        protected readonly ShaderProperty.TextureValue screenGradTexture = new ShaderProperty.TextureValue("_qcPp_Global_Screen_Read");
        protected readonly ShaderProperty.TextureValue processedScreenTexture = new ShaderProperty.TextureValue("_qcPp_Global_Screen_Effect");
        protected readonly ShaderProperty.VectorValue mousePosition = new ShaderProperty.VectorValue("_qcPp_MousePosition");

        private float mouseDownStrengthOneDirectional = 0;
        private float mouseDownStrength = 0.1f;
        private bool downClickFullyShown = true;
        private Vector2 mouseDownPosition;

        public Camera MyCamera;
        private bool IsUsingCrt => postProcessMethod == PostProcessMethod.CustomRenderTexture;
        public Texture CurrentTexture => IsUsingCrt ? effetBuffer : MainTexture;

        private Material EffectMaterial
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

        public void RequestUpdate(Action OnFirstRendered = null)
        {
            InvokeOnCaptured();

            onFirstRender = OnFirstRendered;

            step = BlurStep.Requested;
        }

        private void InvokeOnCaptured()
        {
            try
            {
                onFirstRender?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }

            onFirstRender = null;
        }

        public Texture2D ScreenTexture
        {
            get
            {
                if (!screenTexture || screenTexture.width != Screen.width || screenTexture.height != Screen.height)
                {
                    if (screenTexture)
                    {
                        Destroy(screenTexture);
                    }

                    screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false)
                    {
                        name = "Screen Grab"
                    };
                }

                return screenTexture;
            }
        }

        private void OnPostRender()
        {
            if (!allowScreenGrabToRt && step == BlurStep.Requested)
            {
                step = BlurStep.ReturnedFromCamera;
                var tex = ScreenTexture;
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                tex.Apply();
            }
        }


        #region Swap Textures
        private bool _latestIsZero;
        private void Swap() => _latestIsZero = !_latestIsZero;

        [SerializeField] protected List<RenderTexture> _renderTextures;
        private RenderTexture MainTexture => IsUsingCrt ? effetBuffer : (_latestIsZero ? _renderTextures[0] : _renderTextures[1]);
        private RenderTexture PreviousTexture => IsUsingCrt ? effetBuffer : (_latestIsZero ? _renderTextures[1] : _renderTextures[0]);
        #endregion

        private Texture CurrentScreenGrabTexture => allowScreenGrabToRt ? (Texture)ScreenTextureRt : ScreenTexture2D;

        private Texture2D _screenTexture2D;
        private Texture2D ScreenTexture2D
        {
            get
            {
                if (!_screenTexture2D || _screenTexture2D.width != Screen.width || _screenTexture2D.height != Screen.height)
                {
                    if (_screenTexture2D)
                    {
                        Destroy(_screenTexture2D);
                    }
                    _screenTexture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false)
                    {
                        name = "Screen Texture 2D" //, filterMode = FilterMode.Point
                    };
                }
                return _screenTexture2D;
            }
        }

        private RenderTexture _screenTextureRt;
        private RenderTexture ScreenTextureRt
        {
            // This one is very large

            get
            {
                if (!_screenTextureRt || _screenTextureRt.width != Screen.width || _screenTextureRt.height != Screen.height)
                {
                    if (_screenTextureRt)
                    {
                        Destroy(_screenTextureRt);
                    }
                    _screenTextureRt = new RenderTexture(width: (int)(Screen.width), height: (int)(Screen.height), 0)
                    {
                        name = "Screen Rt" //, filterMode = FilterMode.Point
                    };
                }
                return _screenTextureRt;
            }
        }

        private RenderTexture _captureScreenBufferTmp;
        private RenderTexture CaptureScreenBufferTmp
        {
            get
            {
                if (!_captureScreenBufferTmp || _captureScreenBufferTmp.width != Screen.width || _captureScreenBufferTmp.height != Screen.height)
                {
                    if (_captureScreenBufferTmp)
                    {
                        Destroy(_captureScreenBufferTmp);
                    }
                    _captureScreenBufferTmp = new RenderTexture(width: (int)(Screen.width), height: (int)(Screen.height), 32) { name = "Screen Grab Tmp" };
                }
                return _captureScreenBufferTmp;
            }
        }

        private void BlitRt(Shader shader)
        {
            var mat = EffectMaterial;
            mat.shader = shader;

            if (IsUsingCrt)
            {
                effetBuffer.material = mat;
                effetBuffer.Update();
            }
            else
            {
                Swap();
                Graphics.Blit(PreviousTexture, MainTexture, mat);
            }

            processedScreenTexture.GlobalValue = MainTexture;


        }
        
        private void BlitRt(Texture tex, Shader shader = null)
        {
            if (shader)
            {
                EffectMaterial.shader = shader;
            }

            if (IsUsingCrt)
            {
                effetBuffer.material = EffectMaterial;
                effetBuffer.Update();
            }
            else
            {
                if (shader)
                {
                    if (postProcessMethod == PostProcessMethod.GlBlit)
                        RenderTextureBuffersManager.BlitGL(tex, MainTexture, EffectMaterial);
                    else
                        Graphics.Blit(tex, MainTexture, EffectMaterial);
                }
                else
                    Graphics.Blit(tex, MainTexture);

            }

            processedScreenTexture.GlobalValue = MainTexture;

        }

        public void LateUpdate()
        {            
            switch (step)
            {
                case BlurStep.Requested:

                    if (allowScreenGrabToRt)
                    {
                        step = BlurStep.ReturnedFromCamera;

                        MyCamera.enabled = false;
                        MyCamera.targetTexture = CaptureScreenBufferTmp;

                        MyCamera.Render();
                        MyCamera.targetTexture = null;
                        MyCamera.enabled = true;
                    }
                    break;

                case BlurStep.ReturnedFromCamera:

                    BlitRt(screenTexture, copyShader);

                    screenGradTexture.GlobalValue = CurrentTexture;

                    step = BlurStep.Blurring;
                    blurIteration = 0;

                    if (command == ProcessCommand.Nothing)
                    {
                        step = BlurStep.Off;
                        InvokeOnCaptured();
                    }

                    break;
                case BlurStep.Blurring:

                    blurIteration++;

                    BlitRt(postProcessShader);

                    if (blurIteration > postProcessIteration)
                    {
                        step = BlurStep.Off;
                    }

                    if (blurIteration == 1) 
                    {
                        InvokeOnCaptured();
                    }

                    break;
            }

            #region Press Position

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
            #endregion

        }

        #region Inspector

        private bool _showDebug;
        private bool _showDependencies;

        public bool Inspect()
        {
            var changed = pegi.toggleDefaultInspector(this).nl();

            if (!MyCamera)
            {
                "No Camera Detected on this Game Object. I need camera".writeWarning();
                if ("Try Find".Click())
                    MyCamera = gameObject.GetComponent<Camera>();

                pegi.nl();
            }

            "Post Process Method".editEnum(ref postProcessMethod);
            
            if (icon.Refresh.Click().nl())
                 RequestUpdate();
            
            if (postProcessMethod == PostProcessMethod.CustomRenderTexture && !effetBuffer) 
            {
                pegi.writeWarning("Custom render texture not assigned");

                pegi.nl();

                "Effect Buffer".edit(ref effetBuffer).nl();
            }

            if ("Dependencies".foldout(ref _showDependencies).nl()) 
            {
                "Mouse Position to shader".toggleIcon(ref mousePositionToShader, hideTextWhenTrue: true);

                if (mousePositionToShader) 
                    mousePosition.ToString().write_ForCopy();
                
                pegi.nl();

                "Screen".edit(ref screenTexture).nl();

                "Copy shader".edit(ref copyShader).nl();

                "Post-Process".edit(ref postProcessShader).nl();

                "Iterations:".edit(ref postProcessIteration).nl();

                if (!IsUsingCrt)
                {
                    if (_renderTextures.Count < 2)
                        pegi.writeWarning("Need 2 Render Textures for Post-Process");

                    "Render Textures".edit_List_UObj(ref _renderTextures).nl();
                }
                else
                {
                    "Custom Render Texture".edit(ref effetBuffer).nl();
                }
            }

            if ("Debug".foldout(ref _showDebug).nl())
            {
                if (copyShader)
                    "Copy Shader. Supported={0} | Pass Count = {1}".F(copyShader.isSupported, copyShader.passCount).nl();

                "State".editEnum(ref step).nl();

                "Buffers".write();

                if ("Swap".Click())
                    Swap();

                if ("Screen->Buffer".Click())
                    BlitRt(screenTexture, copyShader);

                if ("Blur".Click())
                    BlitRt(postProcessShader);

                pegi.nl();

                "Screen Texture (Optional):".edit(ref screenTexture).nl();

                if (screenTexture)
                    screenTexture.write(250, alphaBlend: false);
                
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
                    effetBuffer.write(250, alphaBlend: false);
                    pegi.nl();
                }
            }

            return changed;
        }
        #endregion
        
        void Reset() 
        {
            MyCamera = GetComponent<Camera>();
            step = BlurStep.Off;
        }

        void OnEnable() => instances.Add(this);

        void OnDisable() => instances.Remove(this);

        public enum ProcessCommand
        {
            Blur, Nothing
        }

        public enum PostProcessMethod
        {
            GraphicsBlit,
            CustomRenderTexture,
            GlBlit
        }

        protected enum BlurStep
        {
            Off,
            Requested,
            ReturnedFromCamera,
            Blurring
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ScreenBlurController))]
    public class ScreenBlurControllerDrawer : PEGI_Inspector_Mono<ScreenBlurController> {}
#endif

}