using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public class InstallationEffects : MonoBehaviour
{

    private enum EffectState
    {
        FadingIn,
        Running,
        FadingOut,
        Off
    };

    [HideInInspector]
    public List<IEffect> effectPool;
    [HideInInspector]
    public IEffect runningEffect;

    private float minFadeTime = 1.0f;
    private float maxFadeTime = 5.0f;

    private float fadeTime;
    private float lastStateChangeTimestamp;
    private EffectState state;

    void Start()
    {
        var config = GetComponent<InstallationConfig>();
        if (!config)
        {
            throw new System.Exception("InstallationEffects.Start() config not found");
        }
        config.RegisterForUpdates<EffectConfig>(OnEffectConfigChange);
        var longLivedEffects = GetComponentsInChildren<IEffect>().ToList();
        if (longLivedEffects.Count == 0)
        {
            throw new System.Exception("InstallationEffects.Start(): no long lived effects found");
        }
        effectPool = new List<IEffect>(longLivedEffects);
        effectPool.Add(new ColorWheel());
        // effectPool.Add(new Bulging());
        foreach (var effect in effectPool)
        {
            effect.InitializeEffect(config);
        }
        effectPool.OrderBy(o => o.Order);
        SetupEffect(effectPool[0]);
    }

    public void OnEffectConfigChange(InstallationConfig config)
    {
        minFadeTime = config.effectConfig.minFadeTime;
        maxFadeTime = config.effectConfig.maxFadeTime;
    }

    public void RunEffects(InstallationController controller)
    {
        HandleKeyPresses();
        if (state == EffectState.Off)
        {
            return;
        }
        runningEffect.ApplyEffect(controller);
        if (state == EffectState.FadingIn)
        {
            var pctVal = (Time.time - lastStateChangeTimestamp) / fadeTime;
            pctVal = Mathf.Clamp(pctVal, 0f, 1f);
            if (pctVal >= 1.0f)
            {
                lastStateChangeTimestamp = Time.time;
                state = EffectState.Running;
            }
            foreach (var fish in controller.fishes)
            {
                fish.FadeFish(pctVal);
            }
        } else if (state == EffectState.FadingOut)
        {
            var pctVal = 1.0f - (Time.time - lastStateChangeTimestamp) / fadeTime;
            pctVal = Mathf.Clamp(pctVal, 0f, 1f);
            if (pctVal <= 0.0f)
            {
                SetupNextEffect();
            }
            foreach (var fish in controller.fishes)
            {
                fish.FadeFish(pctVal);
            }
        } else
        {
            if (runningEffect.IsRunning)
            {
                return;
            }
            if (runningEffect.CanFade)
            {
                lastStateChangeTimestamp = Time.time;
                state = EffectState.FadingOut;
            } else
            {
                SetupNextEffect();
            }
        }

    }

    private void HandleKeyPresses()
    {
        foreach (var effect in effectPool)
        {
            if (!Input.GetKeyDown(effect.KeyCode))
            {
                continue;
            }
            if (effect == runningEffect)
            {
                SetupRunningEffect();
            } else
            {
                SetupEffect(effect);
            }
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (state == EffectState.Off)
            {
                SetupRunningEffect();
            } else
            {
                state = EffectState.Off;
            }
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            SetupNextEffect();
        }
    }

    private void SetupNextEffect()
    {
        var currentIndex = effectPool.FindIndex(e => e == runningEffect);
        var nextIndex = ++currentIndex % effectPool.Count;
        SetupEffect(effectPool[nextIndex]);
    }

    private void SetupEffect(IEffect effect)
    {
        if (runningEffect != null)
        {
            runningEffect.StopEffect();
        }
        runningEffect = effect;
        SetupRunningEffect();
    }

    private void SetupRunningEffect()
    {
        runningEffect.StartEffect();
        lastStateChangeTimestamp = Time.time;
        if (!runningEffect.CanFade)
        {
            state = EffectState.Running;
        }
        else
        {
            state = EffectState.FadingIn;
            fadeTime = minFadeTime + (maxFadeTime - minFadeTime) * UnityEngine.Random.Range(0.0f, 1.0f);
        }
    }

}
