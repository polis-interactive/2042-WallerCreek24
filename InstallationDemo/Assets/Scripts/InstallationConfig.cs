using System;
using System.Collections.Generic;
using UnityEngine;

public interface ConfigurableObject
{
    public void OnConfigChange(InstallationConfig config);
}

public class InstallationConfig : MonoBehaviour
{
    private Dictionary<Type, List<ConfigurableObject>> onUpdateCallbacks = new Dictionary<Type, List<ConfigurableObject>>();

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
        useLoopback = false
    };

    public RenderConfig renderConfig = new RenderConfig()
    {
        frameRate = 30
    };


    public DisplayConfig displayConfig = new DisplayConfig() {
        lightTemperatureColor = new Color(1.0f, 0.9f, 0.8f)
    };

    public void RegisterForUpdates<T>(ConfigurableObject obj, bool forceUpdate = true) where T : IConfigurable
    {
        Type configType = typeof(T);
        if (!onUpdateCallbacks.ContainsKey(configType))
        {
            onUpdateCallbacks[configType] = new List<ConfigurableObject>();
        }
        onUpdateCallbacks[configType].Add(obj);
        if (forceUpdate)
        {
            obj.OnConfigChange(this);
        }
    }

}
