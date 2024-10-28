using UnityEngine;

public interface IConfigurable { }

[System.Serializable]
public struct ArtnetConfig: IConfigurable
{
    public bool useLoopback;
}

[System.Serializable]
public struct DisplayConfig: IConfigurable
{
    public Color lightTemperatureColor;
}

[System.Serializable]
public struct RenderConfig: IConfigurable
{
    public int frameRate;
}