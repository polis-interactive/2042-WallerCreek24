using Polis.UArtnet;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class ForceRenderRate : MonoBehaviour
{

    void Start()
    {
        var config = GetComponent<InstallationConfig>();
        if (!config)
        {
            throw new System.Exception("InstallationController.Start() config not found");
        }
        config.RegisterForUpdates<FrameRateConfig>(OnFrameRateConfigChange);
    }

    public void OnFrameRateConfigChange(InstallationConfig config)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = config.frameRateConfig.frameRate;
    }
}
