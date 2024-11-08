using UnityEngine;

public interface IConfigurable { }

public enum ArtnetStrategy
{
    None,
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

[System.Serializable]
public struct ParameterConfig: IConfigurable
{
    public float tBucketStep;
    public float rBucketStep;
    public float thetaBucketStep;
}

[System.Serializable]
public struct EffectConfig : IConfigurable
{
    public float minFadeTime;
    public float maxFadeTime;
}

[System.Serializable]
public struct WaterShaderConfig : IConfigurable
{
    public float gamma;
    public float speed;
    public float scale;
    public float brightness;
    public float contrast;

    public float minHoldTime;
    public float maxHoldTime;
    public float minFadeTime;
    public float maxFadeTime;
    public int minCycles;
    public int maxCycles;
}

[System.Serializable]
public struct ColorWheelConfig : IConfigurable
{
    public float minSpread;
    public float maxSpread;
    public float minHoldTime;
    public float maxHoldTime;
    public float minFadeTime;
    public float maxFadeTime;
    public float speedDivisor;
    public bool spreadSpeed;
    public float rotationMult;
    public bool spreadRotation;
}