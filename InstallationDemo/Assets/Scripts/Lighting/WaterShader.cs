using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WaterShader : MonoBehaviour, IEffect
{

    public KeyCode KeyCode => KeyCode.Alpha0;
    public int Order => 0;
    public bool IsLongLivedEffect => true;
    public bool IsRunning
    {
        get
        {
            return cycles > 0;
        }
    }
    public bool CanFade => true;

    private Material material;
    private RenderTexture dynamicRenderTexture;
    [HideInInspector]
    public Texture2D sampledTexture;

    private Vector2 resolution = new Vector4(481, 144);
    private float speed = 2.0f;
    private float scale = 1.0f;
    private float brightness = 0.3f;
    private float contrast = 2.2f;
    private float gamma = 2.0f;

    private float minHoldTime = 1.0f;
    private float maxHoldTime = 3.0f;
    private float minFadeTime = 3.0f;
    private float maxFadeTime = 5.0f;
    private int minCycles = 3;
    private int maxCycles = 6;


    private bool isHolding = true;
    private float changeTime;
    private float lastStateTimestamp;
    private int cycles = 3;

    private int currentColor = 0;
    private int nextColor = 1;
    private List<Color> colors = new List<Color>
    {
        new Color(0.0f, 0.0f, 0.0f, 1.0f), // whie
        new Color(1.0f, 0.0f, 0.0f, 0.0f), // red
        new Color(1.0f, 0.5f, 0.0f, 0.0f), // orange
        new Color(1.0f, 0.0f, 0.25f, 0.0f), // dark pink
        new Color(1.0f, 0.0f, 0.5f, 0.0f), // bright pink
        new Color(1.0f, 0.0f, 1.0f, 0.0f), // magenta
        new Color(0.75f, 0.0f, 1.0f, 0.0f), // purple
        new Color(0.0f, 0.0f, 1.0f, 0.0f), // blue
        new Color(0.0f, 0.5f, 1.0f, 0.0f), // sky blue
        new Color(0.0f, 1.0f, 1.0f, 0.0f), // aqua
        new Color(0.0f, 1.0f, 0.75f, 0.0f), // seafoam
        new Color(0.0f, 1.0f, 0.75f, 0.0f), // clover
        new Color(0.0f, 1.0f, 0.5f, 0.0f), // green
        new Color(1.0f, 1.0f, 0f, 0.0f), // yellow

    };

    private bool hasPicture = true;

    private void Start()
    {
        var renderer = GetComponent<Renderer>();
        if (!renderer)
        {
            throw new System.Exception("ShaderRunner.Start() Matieral not found in object or children");
        }
        if (!renderer.material.name.StartsWith("WaterShader"))
        {
            throw new System.Exception($"ShaderRunner.Start() material {renderer.material.name} is not Water");
        }
        material = renderer.material;
        var config = GetComponentInParent<InstallationConfig>();
        if (!config)
        {
            throw new System.Exception("ShaderRunner.Start() config not found");
        }
        // config.RegisterForUpdates<ParameterConfig>();
        // config.RegisterForUpdates<WaterShaderConfig>(OnWaterShaderConfigChange);
        hasPicture = true;
    }

    public void InitializeEffect(InstallationConfig config)
    {
        OnParameterConfigChange(config);
        OnWaterShaderConfigChange(config);
    }

    private void OnParameterConfigChange(InstallationConfig config)
    {
        resolution.y = 1.0f / config.parameterConfig.tBucketStep;
        resolution.x = 360.0f / config.parameterConfig.thetaBucketStep;
        transform.localScale = new Vector3(resolution.x / 500.0f, 0, resolution.y / 500.0f);
        if (dynamicRenderTexture != null)
        {
            dynamicRenderTexture.Release();
        }
        dynamicRenderTexture = new RenderTexture((int)resolution.x, (int)resolution.y, 24);
        dynamicRenderTexture.enableRandomWrite = true;
        dynamicRenderTexture.Create();
        sampledTexture = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.RGBA32, false);
    }

    private void OnWaterShaderConfigChange(InstallationConfig config)
    {
        gamma = config.waterShaderConfig.gamma;
        speed = config.waterShaderConfig.speed;
        scale = config.waterShaderConfig.scale;
        brightness = config.waterShaderConfig.brightness;
        contrast = config.waterShaderConfig.contrast;

        minFadeTime = config.waterShaderConfig.minFadeTime;
        maxFadeTime = config.waterShaderConfig.maxFadeTime;
        minHoldTime = config.waterShaderConfig.minHoldTime;
        maxHoldTime = config.waterShaderConfig.maxHoldTime;
        minCycles = config.waterShaderConfig.minCycles;
        maxCycles = config.waterShaderConfig.maxCycles;
    }

    // Update is called once per frame
    void Update()
    {
        // update values
        material.SetVector("_Resolution", resolution);
        material.SetFloat("_Gamma", gamma);
        material.SetFloat("_Speed", speed);
        material.SetFloat("_Scale", scale);
        material.SetFloat("_Brightness", brightness);
        material.SetFloat("_Contrast", contrast);

        Graphics.Blit(null, dynamicRenderTexture, material);

        RenderTexture.active = dynamicRenderTexture;
        sampledTexture.ReadPixels(new Rect(0, 0, dynamicRenderTexture.width, dynamicRenderTexture.height), 0, 0);
        sampledTexture.Apply();
        RenderTexture.active = null;

        if (!hasPicture)
        {
            byte[] pngData = sampledTexture.EncodeToPNG();

            // Save the PNG file to disk
            string path = Application.dataPath + "/SavedRenderTexture.png";
            File.WriteAllBytes(path, pngData);
            Debug.Log($"RenderTexture saved to {path}");
            hasPicture = true;
            Debug.Log(sampledTexture.GetPixel(0, 0));
        }
    }

    private void pickNextColor()
    {
        do
        {
            nextColor = Random.Range(0, colors.Count - 1);
        } while (currentColor == nextColor);
    }

    public void ApplyEffect(InstallationController controller)
    {
        var timeStamp = Time.time - lastStateTimestamp;
        if (timeStamp >= changeTime)
        {
            isHolding = !isHolding;
            lastStateTimestamp = Time.time;
            timeStamp = 0f;
            if (isHolding)
            {
                changeTime = Random.Range(minHoldTime, maxHoldTime);
                cycles--;
                currentColor = nextColor;
                Debug.Log($"Remaining Cycles: {cycles}");
            } else
            {
                pickNextColor();
                changeTime = Random.Range(minFadeTime, maxFadeTime);
                Debug.Log($"Last color: {currentColor}, next color: {nextColor}");
            }
        }
        var c = isHolding
            ? colors[currentColor]
            : Color.Lerp(colors[currentColor], colors[nextColor], timeStamp / changeTime)
        ;
        foreach (var fish in controller.fishes)
        {
            var val = sampledTexture.GetPixel(fish.thetaValueInt, fish.tValueInt).r * 255.0f;
            fish.data[0] = (byte)Mathf.Floor(c.r * val);
            fish.data[1] = (byte)Mathf.Floor(c.g * val);
            fish.data[2] = (byte)Mathf.Floor(c.b * val);
            fish.data[3] = (byte)Mathf.Floor(c.a * val);
        }
    }

    public void StartEffect()
    {
        lastStateTimestamp = Time.time;
        isHolding = true;
        changeTime = Random.Range(minHoldTime, maxHoldTime);
        cycles = Random.Range(minCycles, maxCycles);
        currentColor = 0;
        Debug.Log($"cycles: {cycles}; holding for {changeTime}");
    }

    public void StopEffect()
    {
        cycles = 0;
    }
}
