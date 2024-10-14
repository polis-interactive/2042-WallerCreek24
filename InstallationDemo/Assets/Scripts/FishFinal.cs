using Polis.UArtnet.Device;
using UnityEngine;

[RequireComponent(typeof(Node))]
public class FishFinal : MonoBehaviour, ConfigurableObject
{
    private Material material;

    private bool hasWhiteDisplay = false;
    private BaseFish baseFish;

    [SerializeField]
    private Node node;
    [SerializeField]
    private Color fishColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

    private Color lightTemperatureColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    private Color rgbColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    private Color wColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

    public void Setup(int channel, int channels)
    {
        node = GetComponent<Node>();
        if (!node)
        {
            throw new System.Exception("FishFinal.Setup() Node component not attached");
        }
        node.Setup(channel, channels);

        var renderer = GetComponentInChildren<Renderer>();
        if (!renderer)
        {
            throw new System.Exception("FishFinal.Setup() Matieral not found in object or children");
        }
        if (renderer.sharedMaterial.name != "Fish")
        {
            throw new System.Exception($"FishFinal.Setup() material {material.name} is not Fish");
        }

        var config = GetComponentInParent<InstallationConfig>();
        if (!config)
        {
            throw new System.Exception($"FishFinal.Setup() material {material.name} is not Fish");
        }
    }

    private void Start()
    {
        var renderer = GetComponentInChildren<Renderer>();
        material = renderer.material;

        var config = GetComponentInParent<InstallationConfig>();
        config.AddConfigCallback(this);
    }

    public void OnConfigChange(InstallationConfig config)
    {
        baseFish = new BaseFish(config.baseFishConfig);
        lightTemperatureColor = config.lightTemperatureColor;
    }

    public void RunUpdate(bool writeToFish)
    {
        // run and maybe apply base animation
        baseFish.RunUpdate();
        if (!hasWhiteDisplay)
        {
            node.data[3] = baseFish.value;
        }

        if (writeToFish)
        {
            // doing this here before we apply post processing
            fishColor.r = node.data[0] / 255.0f;
            fishColor.g = node.data[1] / 255.0f;
            fishColor.b = node.data[2] / 255.0f;
            fishColor.a = node.data[3] / 255.0f;
        }

        // apply post processing
    }

    public void SetFromArtnet()
    {
        // deapply postprocessing if running from 

        fishColor.r = node.data[0] / 255.0f;
        fishColor.g = node.data[1] / 255.0f;
        fishColor.b = node.data[2] / 255.0f;
        fishColor.a = node.data[3] / 255.0f;
    }

    public void RunDisplay() {
        rgbColor.r = fishColor.r;
        rgbColor.g = fishColor.g;
        rgbColor.b = fishColor.b;
        material.SetColor("_BaseColor", rgbColor);
        wColor = lightTemperatureColor * fishColor.a;
        material.SetColor("_EmissionColor", wColor);

        hasWhiteDisplay = false;
    }

}
