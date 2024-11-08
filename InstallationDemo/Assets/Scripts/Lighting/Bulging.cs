using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bulging : IEffect
{
    public KeyCode KeyCode => KeyCode.Alpha2;
    public int Order => 2;
    public bool IsLongLivedEffect => false;
    public bool CanFade => false;
    public bool IsRunning {
        get
        {
            return true;
        }
    }


    public void InitializeEffect(InstallationConfig config)
    {
        // todo: do from actual config
    }

    public void ApplyEffect(InstallationController controller)
    {
        // foreach (var fish in controller.rBuckets[0])
       // {
        //    fish.data[1] = 128;
        //    fish.data[2] = 255;
       // }
    }

    public void StartEffect()
    {
        
    }

    public void StopEffect()
    {
        
    }
}
