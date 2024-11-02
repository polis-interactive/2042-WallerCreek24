using System;
using System.Collections.Generic;
using UnityEngine;



public class InstallationConfig : MonoBehaviour
{

    private Dictionary<Type, List<Action<InstallationConfig>>> onUpdateCallbacks =
           new Dictionary<Type, List<Action<InstallationConfig>>>();

    public BaseFishConfig baseFishConfig = new BaseFishConfig()
    {
        minLowValue = 20,
        maxLowValue = 60,
        holdValue = 128,
        minHighValue = 190,
        maxHighValue = 220,
        minHoldTimeInMs = 2000,
        maxHoldTimeInMs = 5000,
        minTransitionInTimeInMs = 350,
        maxTransitionInTimeInMs = 1000,
        minPauseTimeInMs = 1500,
        maxPauseTimeInMs = 3000,
        minTransitionOutTimeInMs = 500,
        maxTransitionOutTimeInMs = 1500,
        weightChoseHigh = 0.6f
    };

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
        Debug.Log($"{renderConfig.whiteColor.r}, {renderConfig.whiteColor.g}, {renderConfig.whiteColor.b}, {renderConfig.whiteColor.a}");
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

    void updateRenderTables()
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
