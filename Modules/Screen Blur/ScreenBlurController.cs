using System;
using System.Collections.Generic;
using QuizCanners.Inspect;
using QuizCanners.Lerp;
using QuizCanners.Utils;
using UnityEditor;
using UnityEngine;

namespace PlaytimePainter
{
    [ExecuteInEditMode]
    public class ScreenBlurController : MonoBehaviour, IPEGI
    {
        public Camera MyCamera;

        protected Shader copyShader;
        protected Shader postProcessShader;
        protected PostProcessMethod postProcessMethod;
        protected int postProcessIteration = 100;
        protected Material materialPrototype;
        public bool allowScreenGrabToRt;
        public bool mousePositionToShader;
        public bool useSecondBufferForRenderTextureScreenGrab;

        [NonSerialized] protected ProcessCommand command;
        [NonSerialized] protected BlurStep step = BlurStep.Off;
        [NonSerialized] protected Material _effectMaterialInstance;
        [NonSerialized] protected Action onFirstRender;
        [NonSerialized] protected int blurIteration;

        protected readonly ShaderProperty.TextureValue screenGradTexture = new ShaderProperty.TextureValue("_qcPp_Global_Screen_Read");
        protected readonly ShaderProperty.TextureValue processedScreenTexture = new ShaderProperty.TextureValue("_qcPp_Global_Screen_Effect");
        protected readonly ShaderProperty.VectorValue mousePosition = new ShaderProperty.VectorValue("_qcPp_MousePosition");
        protected readonly ShaderProperty.ShaderKeyword UseMousePosition = new ShaderProperty.ShaderKeyword("_qcPp_FEED_MOUSE_POSITION");

        private float mouseDownStrengthOneDirectional = 0;
        private float mouseDownStrength = 0.1f;
        private bool downClickFullyShown = true;
        private Vector2 mouseDownPosition;

        #region Textures and buffers


        [SerializeField] protected CustomRenderTexture effetBuffer;

        [SerializeField] private Texture2D _screenReadTexture2D;
        public Texture2D ScreenReadTexture2D
        {
            get
            {
                if (!_screenReadTexture2D || _screenReadTexture2D.width != Screen.width || _screenReadTexture2D.height != Screen.height)
                {
                    if (_screenReadTexture2D)
                    {
                        Destroy(_screenReadTexture2D);
                    }

                    _screenReadTexture2D = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false)
                    {
                        name = "Screen Grab"
                    };
                }

                return _screenReadTexture2D;
            }
        }

        private RenderTexture _screenReadRenderTexture;
        private RenderTexture ScreenReadRenderTexture
        {
            get
            {
                if (!_screenReadRenderTexture || _screenReadRenderTexture.width != Screen.width || _screenReadRenderTexture.height != Screen.height)
                {
                    if (_screenReadRenderTexture)
                    {
                        Destroy(_screenReadRenderTexture);
                    }
                    _screenReadRenderTexture = new RenderTexture(width: (int)(Screen.width), height: (int)(Screen.height), 32) { name = "Screen Grab Tmp" };
                }
                return _screenReadRenderTexture;
            }
        }

        #endregion

        public Texture CurrentEffectTexture => IsUsingCrt ? effetBuffer : MainEffectTexture;

        public Texture CurrentScreenReadTexture => allowScreenGrabToRt
            ? (useSecondBufferForRenderTextureScreenGrab ? ScreenReadSecondBufferRt : (Texture)ScreenReadRenderTexture)
            : ScreenReadTexture2D;

        private bool IsUsingCrt => postProcessMethod == PostProcessMethod.CustomRenderTexture;


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

        private void OnPostRender()
        {
            if (!allowScreenGrabToRt && step == BlurStep.Requested)
            {
                step = BlurStep.ReturnedFromCamera;
                var tex = ScreenReadTexture2D;
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                tex.Apply();
            }
        }


        #region Swap Textures
        private bool _latestIsZero;
        private void Swap() => _latestIsZero = !_latestIsZero;

        [SerializeField] protected List<RenderTexture> _renderTextures = new List<RenderTexture>();
        private RenderTexture MainEffectTexture => IsUsingCrt ? effetBuffer : (_latestIsZero ? _renderTextures[0] : _renderTextures[1]);
        private RenderTexture PreviousTexture => IsUsingCrt ? effetBuffer : (_latestIsZero ? _renderTextures[1] : _renderTextures[0]);
        #endregion

        /*   private Texture CurrentScreenGrabTexture => allowScreenGrabToRt ? (Texture)ScreenTextureRt : ScreenTexture2D;

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
           }*/

        private RenderTexture _screenReadSecondBuffer;
        private RenderTexture ScreenReadSecondBufferRt
        {
            get
            {
                if (!_screenReadSecondBuffer || _screenReadSecondBuffer.width != Screen.width || _screenReadSecondBuffer.height != Screen.height)
                {
                    if (_screenReadSecondBuffer)
                    {
                        Destroy(_screenReadSecondBuffer);
                    }
                    _screenReadSecondBuffer = new RenderTexture(width: (int)(Screen.width), height: (int)(Screen.height), 0)
                    {
                        name = "Screen Rt" //, filterMode = FilterMode.Point
                    };
                }
                return _screenReadSecondBuffer;
            }
        }

        private void BlitBetweenEffectBuffers(Shader shader)
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
                Graphics.Blit(PreviousTexture, MainEffectTexture, mat);
            }

            processedScreenTexture.GlobalValue = MainEffectTexture;
        }

        private void BlitToEffectBuffer(Texture tex, Shader shader = null)
        {
            Blit(tex, MainEffectTexture, shader);

            processedScreenTexture.GlobalValue = MainEffectTexture;
        }

        private void Blit(Texture from, RenderTexture to, Shader shader)
        {
            if (shader)
            {
                EffectMaterial.shader = shader;

                if (postProcessMethod == PostProcessMethod.GlBlit)
                    QcUnity.BlitGL(from, to, EffectMaterial);
                else
                    Graphics.Blit(from, to, EffectMaterial);
            }
            else
                Graphics.Blit(from, to);
        }

        public void Update()
        {
            switch (step)
            {
                case BlurStep.Requested:

                    if (allowScreenGrabToRt)
                    {
                        step = BlurStep.ReturnedFromCamera;

                        MyCamera.enabled = false;
                        MyCamera.targetTexture = ScreenReadRenderTexture;

                        MyCamera.Render();
                        MyCamera.targetTexture = null;
                        MyCamera.enabled = true;

                        if (useSecondBufferForRenderTextureScreenGrab)
                        {
                            Blit(ScreenReadRenderTexture, ScreenReadSecondBufferRt, copyShader);
                        }

                        goto case BlurStep.ReturnedFromCamera;
                    }
                    break;

                case BlurStep.ReturnedFromCamera:

                    BlitToEffectBuffer(CurrentScreenReadTexture, copyShader);

                    screenGradTexture.GlobalValue = CurrentScreenReadTexture;
                    processedScreenTexture.GlobalValue = CurrentScreenReadTexture;

                    step = BlurStep.Blurring;
                    blurIteration = 0;

                    InvokeOnCaptured();

                    if (command == ProcessCommand.Nothing)
                    {
                        step = BlurStep.Off;
                    }

                    break;
                case BlurStep.Blurring:

                    blurIteration++;

                    BlitBetweenEffectBuffers(postProcessShader);

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

        public void Inspect()
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

            if ("Request Update".Click().nl())
                RequestUpdate();

            if (postProcessMethod == PostProcessMethod.CustomRenderTexture && !effetBuffer)
            {
                pegi.writeWarning("Custom render texture not assigned");

                pegi.nl();

                "Effect Buffer".edit(ref effetBuffer).nl();
            }

            if ("Config".foldout(ref _showDependencies).nl())
            {
                "Allow Scren Grab To Rt".toggleIcon(ref allowScreenGrabToRt).nl();

                if (allowScreenGrabToRt)
                {
                    "Use Second Buffer For Screen Read".toggleIcon(ref useSecondBufferForRenderTextureScreenGrab);

                    pegi.FullWindow.DocumentationClickOpen("If you are expecting to make a Screen Grab while screen grab is showing on a screen, enable this." +
                        " Same Texture can't be read from and written to at the same time. Black screen or other will show.");
                }

                pegi.nl();

                if ("Mouse Position to shader".toggleIcon(ref mousePositionToShader, hideTextWhenTrue: true))
                {
                    UseMousePosition.Enabled = mousePositionToShader;
                }

                if (mousePositionToShader)
                    mousePosition.ToString().write_ForCopy();

                pegi.nl();

                "Screen".edit(ref _screenReadTexture2D).nl();

                "Copy shader".edit(ref copyShader).nl();

                "Post-Process".edit(ref postProcessShader).nl();

                "Blur Iterations:".edit(ref postProcessIteration).nl();

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

                if ("Screen Capture -> Effect Buffer".Click())
                    BlitToEffectBuffer(CurrentScreenReadTexture, copyShader);

                if ("Blur".Click())
                    BlitBetweenEffectBuffers(postProcessShader);

                pegi.nl();

                "Screen Texture (Optional):".edit(ref _screenReadTexture2D).nl();

                if (_screenReadTexture2D)
                    _screenReadTexture2D.write(250, alphaBlend: false);

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
                        "Main Texture: {0}".F(MainEffectTexture).nl();

                        MainEffectTexture.write(250, alphaBlend: false);
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
        }
        #endregion

        void Reset()
        {
            MyCamera = GetComponent<Camera>();
            step = BlurStep.Off;
        }

        void OnEnable()
        {
            UseMousePosition.Enabled = mousePositionToShader;
        }

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
    public class ScreenBlurControllerDrawer : PEGI_Inspector_Mono<ScreenBlurController> { }
#endif

}