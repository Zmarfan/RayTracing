using UnityEngine;
using System.Collections.Generic;

public class RayTracingController : MonoBehaviour
{
    static readonly float SHADER_X_GROUP_AMOUNT = 8f;
    static readonly float SHADER_Y_GROUP_AMOUNT = 8f;

    [SerializeField] ComputeShader _rayTracerShader;
    [SerializeField] Texture _skyboxTexture;
    [SerializeField] Light _directionalLight;

    [SerializeField] Vector2 _sphereRadius = new Vector2(3.0f, 8.0f);
    [SerializeField] uint _maxSpheres = 100;
    [SerializeField] float _spherePlacementRadius = 100.0f;

    ComputeBuffer _sphereBuffer;
    uint _currentSample = 0;
    Material _addMaterial;
    Camera _camera;
    RenderTexture _renderTexture;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        _currentSample = 0;
        SetupScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }

    void SetupScene()
    {
        List<Sphere> spheres = new List<Sphere>();

        for (int i = 0; i < _maxSpheres; i++)
        {
            Sphere sphere = new Sphere();

            sphere.radius = _sphereRadius.x + Random.value * (_sphereRadius.y - _sphereRadius.x);
            Vector2 randomPosition = Random.insideUnitCircle * _spherePlacementRadius;
            sphere.position = new Vector3(randomPosition.x, sphere.radius, randomPosition.y);

            bool skip = false;
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                {
                    skip = true;
                    break;
                }
            }
            if (skip)
                continue;

            Color color = Random.ColorHSV();
            bool metal = Random.value < 0.5f;
            sphere.albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;

            spheres.Add(sphere);
        }

        _sphereBuffer = new ComputeBuffer(spheres.Count, Sphere.StructSize);
        _sphereBuffer.SetData(spheres);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Render(destination);
    }

    private void Update()
    {
        if (transform.hasChanged || _directionalLight.transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
            _directionalLight.transform.hasChanged = false;
        }
    }

    void Render(RenderTexture destination)
    {
        //Correct Render Target
        InitRenderTexture();
        SetShaderParameters();

        int threadGroupX = Mathf.CeilToInt(Screen.width / SHADER_X_GROUP_AMOUNT);
        int threadGroupY = Mathf.CeilToInt(Screen.height / SHADER_Y_GROUP_AMOUNT);
        _rayTracerShader.Dispatch(0, threadGroupX, threadGroupY, 1);

        //Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat("_sample", _currentSample);
        Graphics.Blit(_renderTexture, destination, _addMaterial);
        _currentSample++;
    }

    /// <summary>
    /// If current doesn't match Screen size -> create a new one
    /// </summary>
    void InitRenderTexture()
    {
        if (_renderTexture == null || _renderTexture.width != Screen.width || _renderTexture.height != Screen.height)
        {
            //Get rid of last one
            if (_renderTexture != null)
                _renderTexture.Release();

            //New render target for compute shader
            _renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true
            };
            _renderTexture.Create();

            _currentSample = 0;
        }
    }

    void SetShaderParameters()
    {
        _rayTracerShader.SetTexture(0, "Result", _renderTexture);

        Vector3 lightForward = _directionalLight.transform.forward;
        _rayTracerShader.SetBuffer(0, "_spheres", _sphereBuffer);
        _rayTracerShader.SetVector("_directionalLight", new Vector4(lightForward.x, lightForward.y, lightForward.z, _directionalLight.intensity));
        _rayTracerShader.SetTexture(0, "_skyboxTexture", _skyboxTexture);
        _rayTracerShader.SetMatrix("_cameraToWorld", _camera.cameraToWorldMatrix);
        _rayTracerShader.SetMatrix("_cameraInverseProjection", _camera.projectionMatrix.inverse);
        _rayTracerShader.SetVector("_pixelOffset", new Vector2(Random.value, Random.value));
    }
}

public struct Sphere
{
    public Vector3 position;
    public float radius;
    public Vector3 albedo;
    public Vector3 specular;

    public static int StructSize
    {
        get
        {
            return sizeof(float) * 10;
        }
    }
}
