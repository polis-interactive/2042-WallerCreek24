
using UnityEngine;

public enum BaseFishState
{
    Holding,
    FadingInLow,
    FadingOutLow,
    Low,
    FadingInHigh,
    FadingOutHigh,
    High
}

[System.Serializable]
public struct BaseFishConfig: IConfigurable
{
    public byte minLowValue;
    public byte maxLowValue;
    public byte holdValue;
    public byte minHighValue;
    public byte maxHighValue;
    public int minHoldTimeInMs;
    public int maxHoldTimeInMs;
    public int minTransitionInTimeInMs;
    public int maxTransitionInTimeInMs;
    public int minPauseTimeInMs;
    public int maxPauseTimeInMs;
    public int minTransitionOutTimeInMs;
    public int maxTransitionOutTimeInMs;
    public float weightChoseHigh;
}

public class BaseFish
{
    private BaseFishConfig config;
    private BaseFishState state;
    private float timeInState;
    private float stateTimer;
    private byte _value;

    private byte pauseValue;

    public byte value
    {
        get
        {
            return _value;
        }
    }

    public BaseFish(BaseFishConfig config)
    {
        this.config = config;
        timeInState = Random.Range(config.minHoldTimeInMs, config.maxHoldTimeInMs) / 1000.0f;
        stateTimer = timeInState;
        _value = config.holdValue;
    }

    public void RunUpdate()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            TransitionState();
        }
        var fadePct = 1.0f - stateTimer / timeInState;
        switch (state)
        {
            case BaseFishState.Holding:
                _value = config.holdValue;
                break;
            case BaseFishState.High:
            case BaseFishState.Low:
                _value = pauseValue;
                break;
            case BaseFishState.FadingInHigh:
            case BaseFishState.FadingInLow:
                _value = (byte)(config.holdValue + (pauseValue - config.holdValue) * fadePct);
                break;
            case BaseFishState.FadingOutHigh:
            case BaseFishState.FadingOutLow:
                _value = (byte)(pauseValue + (config.holdValue - pauseValue) * fadePct);
                break;
        }
    }

    public void TransitionState()
    {
        int minTime = 0;
        int maxTime = 0;
        switch(state)
        {
            case BaseFishState.Holding:
                var rState = Random.Range(0.0f, 1.0f);
                var rValue = Random.Range(0.0f, 1.0f);
                if (rState > config.weightChoseHigh)
                {
                    state = BaseFishState.FadingInLow;
                    pauseValue = (byte)(config.minLowValue + (config.maxLowValue - config.minLowValue) * rValue);
                } else
                {
                    state = BaseFishState.FadingInHigh;
                    pauseValue = (byte)(config.minHighValue + (config.maxHighValue - config.minHighValue) * rValue);
                }
                minTime = config.minTransitionInTimeInMs;
                maxTime = config.minTransitionInTimeInMs;
                break;
            case BaseFishState.FadingInHigh:
                state = BaseFishState.High;
                minTime = config.minPauseTimeInMs;
                maxTime = config.maxPauseTimeInMs;
                break;
            case BaseFishState.High:
                state = BaseFishState.FadingOutHigh;
                minTime = config.minTransitionOutTimeInMs;
                maxTime = config.maxTransitionOutTimeInMs;
                break;
            case BaseFishState.FadingOutHigh:
                state = BaseFishState.Holding;
                minTime = config.minHoldTimeInMs;
                maxTime = config.maxHoldTimeInMs;
                break;
            case BaseFishState.FadingInLow:
                state = BaseFishState.Low;
                minTime = config.minPauseTimeInMs;
                maxTime = config.maxPauseTimeInMs;
                break;
            case BaseFishState.Low:
                state = BaseFishState.FadingOutLow;
                minTime = config.minTransitionOutTimeInMs;
                maxTime = config.maxTransitionOutTimeInMs;
                break;
            case BaseFishState.FadingOutLow:
                state = BaseFishState.Holding;
                minTime = config.minHoldTimeInMs;
                maxTime = config.maxHoldTimeInMs;
                break;
        }
        timeInState = Random.Range(minTime, maxTime) / 1000.0f;
        stateTimer = timeInState;
    }
}