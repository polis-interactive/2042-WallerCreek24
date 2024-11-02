using UnityEngine;

public interface IConfigurable { }

public enum ArtnetStrategy
{
    Direct,
    Loopback,
    Broadcast
}

[System.Serializable]
public struct ArtnetConfig: IConfigurable
{
    public ArtnetStrategy artnetStrategy;
}

[System.Serializable]
public struct DisplayConfig: IConfigurable
{
    public Color lightTemperatureColor;
}

[System.Serializable]
public struct FrameRateConfig: IConfigurable
{
    public int frameRate;
}

[System.Serializable]
public struct RenderConfig: IConfigurable
{
    public float gamma;
    public float gammaWhite;
    public Color whiteColor;
}