using System;
using System.Collections.Generic;
using UnityEngine;



public class InstallationConfig : MonoBehaviour
{

    private Dictionary<Type, List<Action<InstallationConfig>>> onUpdateCallbacks =
           new Dictionary<Type, List<Action<InstallationConfig>>>();

    public ArtnetConfig artnetConfig = new ArtnetConfig()
    {
        artnetStrategy = ArtnetStrategy.Loopback
    };

    public FrameRateConfig frameRateConfig = new FrameRateConfig()
    {
        frameRate = 30
    };


    public DisplayConfig displayConfig = new DisplayConfig() {
        lightTemperatureColor = new Color(1.0f, 0.9f, 0.8f)
    };

    public RenderConfig renderConfig = new RenderConfig()
    {
        gamma = 1.4f,
        gammaWhite = 1.4f,
        whiteColor = new Color(0, 0, 0, 1.0f)
    };

    public ParameterConfig parameterConfig = new ParameterConfig()
    {
        tBucketStep = 0.01f,
        rBucketStep = 0.5f,
        thetaBucketStep = 5
    };

    public EffectConfig effectConfig = new EffectConfig()
    {
        minFadeTime = 3.0f,
        maxFadeTime = 5.0f,
    };

    public WaterShaderConfig waterShaderConfig = new WaterShaderConfig()
    {
        gamma = 2.0f,
        speed = 2.0f,
        scale = 1.0f,
        brightness = 0.3f,
        contrast = 2.2f,

        minFadeTime = 1.0f,
        maxFadeTime = 3.0f,
        minHoldTime = 3.0f,
        maxHoldTime = 5.0f,
        minCycles = 3,
        maxCycles = 6
    };

    public ColorWheelConfig colorWheelConfig = new ColorWheelConfig()
    {
        minSpread = 2f,
        maxSpread = 10f,
        minHoldTime = 1f,
        maxHoldTime = 5f,
        minFadeTime = 5f,
        maxFadeTime = 10f,
        rotationMult = 0.2f,
        speedDivisor = 15f,
        spreadSpeed = true,
        spreadRotation = true,
        minCycles = 6,
        maxCycles = 10,
    };

    [HideInInspector]
    public byte[] renderGammaTable;
    [HideInInspector]
    public byte[] renderInverseGammaTable;
    [HideInInspector]
    public byte[] renderGammaWhiteTable;
    [HideInInspector]
    public byte[] renderInverseGammaWhiteTable;
    [HideInInspector]
    public byte[] renderWhiteColor;

    // not really sure what to do here
    public void Awake()
    {
        updateRenderTables();
    }

    public void RegisterForUpdates<T>(Action<InstallationConfig> callback)
        where T : IConfigurable
    {
        Type configType = typeof(T);
        if (!onUpdateCallbacks.ContainsKey(configType))
        {
            onUpdateCallbacks[configType] = new List<Action<InstallationConfig>>();
        }
        onUpdateCallbacks[configType].Add(callback);
        callback(this);
    }

    public void updateRenderTables()
    {
        var (gamma, inverseGamma) = generateGammaTables(renderConfig.gamma);
        var (gammaWhite, inverseGammaWhite) = generateGammaTables(renderConfig.gammaWhite);
        renderGammaTable = gamma;
        renderInverseGammaTable = inverseGamma;
        renderGammaWhiteTable = gammaWhite;
        renderInverseGammaWhiteTable = inverseGammaWhite;
    }

    (byte[], byte[]) generateGammaTables(float gamma)
    {
        byte[] gammaTable = new byte[256];
        byte[] inverseGammaTable = new byte[256];

        // Create the gamma correction table
        for (int i = 0; i < 256; i++)
        {
            var normalized = i / 255.0f;
            var corrected = Mathf.Pow(normalized, gamma);
            var val = (byte)(corrected * 255.0f);
            gammaTable[i] = val;
        }

        for (int i = 0; i < 256; i++)
        {
            var normalized = i / 255.0f;
            var corrected = Mathf.Pow(normalized, 1.0f / gamma);
            var val = (byte)(corrected * 255.0f);
            inverseGammaTable[i] = val;
        }

        return (gammaTable, inverseGammaTable);
    }

}
