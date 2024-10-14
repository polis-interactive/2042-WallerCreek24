using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ConfigurableObject
{
    public void OnConfigChange(InstallationConfig config);
}

[RequireComponent(typeof(InstallationConfig))]
public class InstallationConfig : MonoBehaviour
{
    private List<ConfigurableObject> configCallbacks = new List<ConfigurableObject>();

    public BaseFishConfig baseFishConfig = new BaseFishConfig()
    {
        minLowValue = 0,
        maxLowValue = 20,
        holdValue = 128,
        minHighValue = 180,
        maxHighValue = 200,
        minHoldTimeInMs = 2000,
        maxHoldTimeInMs = 5000,
        minTransitionInTimeInMs = 350,
        maxTransitionInTimeInMs = 1000,
        minPauseTimeInMs = 1500,
        maxPauseTimeInMs = 3000,
        minTransitionOutTimeInMs = 500,
        maxTransitionOutTimeInMs = 1500,
        weightChoseHigh = 0.5f
    };

    public bool loopbackArtnet = false;
    public int frameRate = 30;


    public Color lightTemperatureColor = new Color(1.0f, 0.9f, 0.8f);

    public void AddConfigCallback(ConfigurableObject obj)
    {
        configCallbacks.Add(obj);
        obj.OnConfigChange(this);
    }

}
