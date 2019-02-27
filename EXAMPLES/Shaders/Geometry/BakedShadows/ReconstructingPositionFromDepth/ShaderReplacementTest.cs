using QuizCannersUtilities;
using UnityEngine;


[ExecuteInEditMode]
public class ShaderReplacementTest : MonoBehaviour
{
    [SerializeField] private Camera _cameraToReplaceShader;

    [SerializeField] private Camera _depthCamera;
    [SerializeField] private RenderTexture _depthTarget;
    public int targetSize = 512;
    public float shadowBias = 0.005f;

    void UpdateDepthCamera()
    {
        if (!_depthCamera) return;


        _depthCamera.enabled = false;
        _depthCamera.enabled = Application.isPlaying;
        _depthCamera.depthTextureMode = DepthTextureMode.None;
        _depthCamera.depth = -1000;

        if (_depthTarget && _depthTarget.width == targetSize) return;

        if (_depthTarget)
            _depthTarget.DestroyWhatever_UObj();

        var sz = Mathf.Max(targetSize, 16);

        _depthTarget = new RenderTexture(sz, sz, 32, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            autoGenerateMips = false,
            useMipMap = false
        };

        _depthCamera.targetTexture = _depthTarget;
    }

    void OnEnable() {

        if (_cameraToReplaceShader) 
            _cameraToReplaceShader.SetReplacementShader(Shader.Find("Playtime Painter/ReplacementShaderTest"), "RenderType");
        
        UpdateDepthCamera();
        
    }

    void Update()
    {
        if (!Application.isPlaying && _depthCamera)
        {
            
            _depthCamera.Render();
        }
    }

    private void OnPostRender() {
        if (!_depthCamera) return;
        
        _shadowMatrix.GlobalValue = _depthCamera.projectionMatrix * _depthCamera.worldToCameraMatrix;
        _shadowTexture.GlobalValue = _depthTarget;
        _shadowCameraPositionProperty.GlobalValue = transform.position.ToVector4(0);

        var far = _depthCamera.farClipPlane;
        var near = _depthCamera.nearClipPlane;

        var zBuff = new Vector4(1f - far / near, far / near, 0 ,0);

        zBuff.z = zBuff.x / far;
        zBuff.w = zBuff.y / far;

        _zBufferParams.GlobalValue = zBuff;

        _cameraParameters.GlobalValue = new Vector4(_depthCamera.aspect, _depthCamera.fieldOfView, _depthCamera.nearClipPlane, _depthCamera.farClipPlane);


    }

    private readonly ShaderProperty.MatrixValue _shadowMatrix = new ShaderProperty.MatrixValue("c_ShadowMatrix");

    private readonly ShaderProperty.TextureValue _shadowTexture = new ShaderProperty.TextureValue("c_ShadowTex");
    

    private readonly ShaderProperty.VectorValue _shadowCameraPositionProperty = new ShaderProperty.VectorValue("c_ShadowCamPos");
    private readonly ShaderProperty.VectorValue _zBufferParams = new ShaderProperty.VectorValue("c_ZBufferParameters");
    private readonly ShaderProperty.VectorValue _cameraParameters = new ShaderProperty.VectorValue("c_CamParams");
}
