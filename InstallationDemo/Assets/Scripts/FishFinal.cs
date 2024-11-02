using Polis.UArtnet.Device;
using UnityEngine;
using System;

[RequireComponent(typeof(Node))]
public class FishFinal : MonoBehaviour
{
    private Material material;

    private bool hasWhiteDisplay = false;
    private BaseFish baseFish;

    private byte[] gamma;
    private byte[] inverseGamma;
    private byte[] gammaWhite;
    private byte[] inverseGammaWhite;
    private Color renderWhiteColor;

    private byte[] data;

    [SerializeField]
    private Node node;

    private Color fishColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

    private Color displayLightTemperatureColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    private Color displayRgbColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
    private Color displayWhiteColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);

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
    }

    private void Start()
    {
        var renderer = GetComponentInChildren<Renderer>();
        material = renderer.material;

        var config = GetComponentInParent<InstallationConfig>();
        if (!config)
        {
            throw new System.Exception($"FishFinal.Start() config not found");
        }
        config.RegisterForUpdates<BaseFishConfig>(OnBaseFishConfigChange);
        config.RegisterForUpdates<DisplayConfig>(OnDisplayConfigChange);
        config.RegisterForUpdates<RenderConfig>(OnRenderConfigChange);
        data = new byte[4] { 0, 0, 0, 0 };
    }

    public void OnBaseFishConfigChange(InstallationConfig config)
    {
        baseFish = new BaseFish(config.baseFishConfig);
    }

    public void OnDisplayConfigChange(InstallationConfig config)
    {
        displayLightTemperatureColor = config.displayConfig.lightTemperatureColor;
    }

    public void OnRenderConfigChange(InstallationConfig config)
    {
        gamma = config.renderGammaTable;
        inverseGamma = config.renderInverseGammaTable;
        gammaWhite = config.renderGammaWhiteTable;
        inverseGammaWhite = config.renderInverseGammaWhiteTable;
        renderWhiteColor = config.renderConfig.whiteColor;
        if (renderWhiteColor.a == 0.0f)
        {
            throw new SystemException("FishFinal.OnRenderConfigChange() whiteColor white value cannot be 0");
        }
    }

    public void RunUpdate(bool writeToFish)
    {
        Array.Clear(data, 0, data.Length);
        // run and maybe apply base animation
        baseFish.RunUpdate();
        if (!hasWhiteDisplay)
        {
            data[3] = baseFish.value;
        }

        if (writeToFish)
        {
            // doing this here before we apply post processing
            ColorUtils.ByteToColor(data, ref fishColor);
        }

        // apply white correction
        if (data[3] != 0)
        {
            node.data[3] = (byte)Math.Min((int)(data[3] * renderWhiteColor.a), 255);
            if (renderWhiteColor.r != 0.0f)
            {
                node.data[0] = (byte)Math.Min(data[0] + data[3] * renderWhiteColor.r, 255);
            }
            if (renderWhiteColor.g != 0.0f)
            {
                node.data[1] = (byte)Math.Min(data[1] + data[3] * renderWhiteColor.g, 255);
            }
            if (renderWhiteColor.b != 0.0f)
            {
                node.data[2] = (byte)Math.Min(data[2] + data[3] * renderWhiteColor.b, 255);
            }
        }

        // apply gammas
        node.data[0] = gamma[node.data[0]];
        node.data[1] = gamma[node.data[1]];
        node.data[2] = gamma[node.data[2]];
        node.data[3] = gammaWhite[node.data[3]];
    }

    public void SetFromArtnet()
    {
        // deapply gamma
        data[0] = inverseGamma[node.data[0]];
        data[1] = inverseGamma[node.data[1]];
        data[2] = inverseGamma[node.data[2]];
        data[3] = inverseGammaWhite[node.data[3]];

        // deapply white correction
        if (data[3] != 0)
        {
            data[3] = (byte)(data[3] / renderWhiteColor.a);
            if (renderWhiteColor.r != 0)
            {
                data[0] = (byte)Math.Min(0, data[0] - data[3] * renderWhiteColor.r);
            }
            if (renderWhiteColor.g != 0)
            {

                data[1] = (byte)Math.Min(0, data[1] - data[3] * renderWhiteColor.g);
            }
            if (renderWhiteColor.b != 0)
            {

                data[2] = (byte)Math.Min(0, data[2] - data[3] * renderWhiteColor.b);
            }
        }
        // Debug.Log($"{node.data[0]} -> {data[0]} {node.data[1]} -> {data[1]} {node.data[2]} -> {data[2]} {node.data[3]} -> {data[3]}");
        ColorUtils.ByteToColor(data, ref fishColor);
    }

    public void RunDisplay() {
        displayRgbColor.r = fishColor.r;
        displayRgbColor.g = fishColor.g;
        displayRgbColor.b = fishColor.b;
        material.SetColor("_BaseColor", displayRgbColor);
        displayWhiteColor = displayLightTemperatureColor * fishColor.a;
        material.SetColor("_EmissionColor", displayWhiteColor);

        hasWhiteDisplay = false;
    }

}
