using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorWheel : IEffect
{

    public KeyCode KeyCode => KeyCode.Alpha1;
    public int Order => 1;
    public bool IsLongLivedEffect => false;
    public bool CanFade => true;
    public bool IsRunning
    {
        get
        {
            return waveCount > 0;
        }
    }

    private float minSpread = 10.0f;
    private float maxSpread = 10.0f;

    private float minHoldTime = 1.0f;
    private float maxHoldTime = 5.0f;

    private float minFadeTime = 5.0f;
    private float maxFadeTime = 10.0f;

    private float speedDivisor = 15f;
    private bool spreadSpeed = true;
    private float rotationMult = 0.2f;
    private bool spreadRotation = true;

    private float currentSpread = 1.0f;
    private float nextSpread = 1.0f;

    private bool isHolding = true;

    private float changeTime = 0.0f;
    private float lastChangeTimestamp = 0.0f;
    private float startTimestamp = 0.0f;
    private float offset = 0.0f;

    private WaveGenerator waveGenerator;
    private Wave wave;
    private float lastWaveTimetamp = 0.0f;

    private int minWaveCount;
    private int maxWaveCount;
    private int waveCount;


    private float minNoWaveTime;
    private float maxNoWaveTime;
    private float noWaveTime;


    public void InitializeEffect(InstallationConfig config)
    {
        // wheel config

        minSpread = config.colorWheelConfig.minSpread;
        maxSpread = config.colorWheelConfig.maxSpread;
        minHoldTime = config.colorWheelConfig.minHoldTime;
        maxHoldTime = config.colorWheelConfig.maxHoldTime;
        minFadeTime = config.colorWheelConfig.minFadeTime;
        maxFadeTime = config.colorWheelConfig.maxFadeTime;
        speedDivisor = config.colorWheelConfig.speedDivisor;
        spreadSpeed = config.colorWheelConfig.spreadSpeed;
        rotationMult = config.colorWheelConfig.rotationMult;
        spreadRotation = config.colorWheelConfig.spreadRotation;

        // wave config
        minNoWaveTime = 7.0f;
        maxNoWaveTime = 15.0f;
        minWaveCount = 3;
        maxWaveCount = 7;
        waveGenerator = new WaveGenerator();
        waveGenerator.minDurations = 1;
        waveGenerator.maxDurations = 9;
        waveGenerator.durationMult = 50;
        waveGenerator.minHeight = 0.8f;
        waveGenerator.minHeightMult = 0;
        waveGenerator.maxHeightMult = 5;
        waveGenerator.heightMult = 0.1f;
        var segements = 1.0f / config.parameterConfig.tBucketStep;
        waveGenerator.segments = Mathf.FloorToInt(segements);
        waveGenerator.minNodes = Mathf.FloorToInt(segements * 0.75f);
        waveGenerator.maxNodes = Mathf.FloorToInt(segements * 1.3f);
    }

    public void ApplyEffect(InstallationController controller)
    {
        var changeStamp = Time.time - lastChangeTimestamp;
        if (changeStamp >= changeTime)
        {
            isHolding = !isHolding;
            lastChangeTimestamp = Time.time;
            changeStamp = 0.0f;
            if (isHolding)
            {
                changeTime = Random.Range(minHoldTime, maxHoldTime);
                currentSpread = nextSpread;
            } else
            {
                changeTime = Random.Range(minFadeTime, maxFadeTime);
                nextSpread = Random.Range(minSpread, maxSpread);
                Debug.Log($"Last spread: {currentSpread}, next spread: {nextSpread}");
            }

        }
        var waveStamp = Time.time - lastWaveTimetamp;
        if (wave == null && waveStamp > noWaveTime)
        {
            wave = waveGenerator.GenerateWave();
            Debug.Log($"Created new wave is light? {wave.WaveIsLight}");
        }
        if (wave != null && !wave.ShouldWaveRun())
        {
            Debug.Log("Wave has run");
            wave = null;
            --waveCount;
            noWaveTime = Random.Range(minNoWaveTime, maxNoWaveTime);
        }
        var tStamp = Time.time - startTimestamp;
        var spread = isHolding
            ? currentSpread
            : Mathf.Lerp(currentSpread, nextSpread, changeStamp / changeTime)
        ;
        var useRotation = spreadRotation ? rotationMult * spread : rotationMult;
        var useTstamp = spreadSpeed ? tStamp / speedDivisor / spread : tStamp / speedDivisor;
        var node = 0;
        foreach (var tBucket in controller.tThenThetaSortedFishes)
        {
            int wavePosition = wave == null ? -1 : node + wave.WaveTailPointer;
            float waveFraction = wave == null
                ? 0f
                : wavePosition < 0 || wavePosition >= wave.WaveLength
                ? 0f
                : wave.GetWaveFraction(wavePosition)
            ;
            bool isLightWave = wave == null ? false : wave.WaveIsLight;
            foreach (var fish in tBucket)
            {
                Color32 c = Color.HSVToRGB(
                    Mathf.Repeat((fish.tValue - fish.thetaValue / 360.0f * useRotation) / spread - offset - useTstamp, 1.0f),
                    1f, 1f
                );
                if (waveFraction <= 0f)
                {
                    fish.data[0] = c.r;
                    fish.data[1] = c.g;
                    fish.data[2] = c.b;
                    fish.data[3] = 0;
                } else if (isLightWave)
                {
                    fish.data[0] = c.r;
                    fish.data[1] = c.g;
                    fish.data[2] = c.b;
                    fish.data[3] = (byte)Mathf.FloorToInt(Mathf.Min(waveFraction * 255f, 255f));
                } else
                {
                    fish.data[0] = (byte)Mathf.FloorToInt(Mathf.Max(c.r - waveFraction * 255f, 0f));
                    fish.data[1] = (byte)Mathf.FloorToInt(Mathf.Max(c.g - waveFraction * 255f, 0f)); 
                    fish.data[2] = (byte)Mathf.FloorToInt(Mathf.Max(c.b - waveFraction * 255f, 0f)); 
                    fish.data[3] = 0;
                }
            }
            node++;
        }
    }

    public void StartEffect()
    {
        offset = Random.Range(0.0f, 1.0f);
        lastChangeTimestamp = Time.time;
        changeTime = Random.Range(minHoldTime, maxHoldTime);
        currentSpread = Random.Range(minSpread, maxSpread);
        startTimestamp = Time.time;
        lastWaveTimetamp = Time.time;
        noWaveTime = Random.Range(minNoWaveTime, maxNoWaveTime);
        waveCount = Random.Range(minWaveCount, maxWaveCount);
        wave = null;
        Debug.Log($"waveCount {waveCount}");
    }

    public void StopEffect()
    {
    }
}
