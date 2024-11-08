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
            return cycles > 0;
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

    private int minCycles = 6;
    private int maxCycles = 10;
    private int cycles = 7;

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
        minCycles = config.colorWheelConfig.minCycles;
        maxCycles = config.colorWheelConfig.maxCycles;
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
                cycles--;
                currentSpread = nextSpread;
            } else
            {
                changeTime = Random.Range(minFadeTime, maxFadeTime);
                nextSpread = Random.Range(minSpread, maxSpread);
                Debug.Log($"Last spread: {currentSpread}, next spread: {nextSpread}");
            }

        }
        var tStamp = Time.time - startTimestamp;
        var spread = isHolding
            ? currentSpread
            : Mathf.Lerp(currentSpread, nextSpread, changeStamp / changeTime)
        ;
        var useRotation = spreadRotation ? rotationMult * spread : rotationMult;
        var useTstamp = spreadSpeed ? tStamp / speedDivisor / spread : tStamp / speedDivisor;
        foreach (var tBucket in controller.tThenThetaSortedFishes)
        {
            foreach (var fish in tBucket)
            {
                Color32 c = Color.HSVToRGB(
                    Mathf.Repeat((fish.tValue - fish.thetaValue / 360.0f * useRotation) / spread - offset - useTstamp, 1.0f),
                    1f, 1f
                );
                fish.data[0] = c.r;
                fish.data[1] = c.g;
                fish.data[2] = c.b;
                fish.data[3] = 0;
            }
        }
    }

    public void StartEffect()
    {
        offset = Random.Range(0.0f, 1.0f);
        lastChangeTimestamp = Time.time;
        changeTime = Random.Range(minHoldTime, maxHoldTime);
        currentSpread = Random.Range(minSpread, maxSpread);
        startTimestamp = Time.time;
        cycles = Random.Range(minCycles, maxCycles);
    }

    public void StopEffect()
    {
    }
}
