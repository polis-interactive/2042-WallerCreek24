using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wave
{
    public float StartTime;
    public int CurrentOffset;
    public int NodeCount;
    public int NodeDuration;
    public float MaxFraction;
    public int WaveTailPointer;
    public int WaveLength;
    public bool WaveIsLight;
    public float Period;
    public float TimeConst;

    private bool IsFinished()
    {
        return WaveTailPointer >= WaveLength;
    }

    public bool ShouldWaveRun()
    {
        if (IsFinished()) return false;
        return IncrementPointer();
    }

    private bool IncrementPointer()
    {
        var currentOffset = (Time.time - StartTime) * 1000f;
        var tailPointer = Mathf.FloorToInt(currentOffset / NodeDuration) - NodeCount + 1;
        if (tailPointer > WaveTailPointer)
        {
            ++WaveTailPointer;
            if (IsFinished()) return false;
        }
        var timeStep = currentOffset % NodeDuration;
        TimeConst = timeStep / NodeDuration + WaveTailPointer - 1.0f;
        return true;
    }

    public float GetWaveFraction (int nodeNumber)
    {
        return MaxFraction * Mathf.Sin((nodeNumber - TimeConst) * Period);
    }

}

public class WaveGenerator
{
    public int minNodes;
    public int maxNodes;

    public float minHeight;
    public float heightMult;
    public int minHeightMult;
    public int maxHeightMult;

    public int durationMult;
    public int minDurations;
    public int maxDurations;

    public int segments;

    public Wave GenerateWave()
    {
        var wave = new Wave();
        wave.StartTime = Time.time;
        wave.NodeCount = Random.Range(minNodes, maxNodes);
        wave.NodeDuration = durationMult * Random.Range(minDurations, maxDurations);
        wave.MaxFraction = minHeight + heightMult * Random.Range(minHeightMult, maxHeightMult);
        wave.WaveIsLight = Random.Range(0f, 1f) > 0.5f;
        wave.WaveTailPointer = -wave.NodeCount + 1;
        wave.Period = Mathf.PI / (float)wave.NodeCount;
        wave.WaveLength = segments;
        return wave;
    }
}