
using UnityEngine;

public interface IEffect
{
    public int Order { get; }
    public KeyCode KeyCode { get; }
    public bool IsRunning { get; }
    public bool IsLongLivedEffect { get; }
    public bool CanFade { get; }
    public void StartEffect();
    public void StopEffect();
    public void InitializeEffect(InstallationConfig config);
    public void ApplyEffect(InstallationController controller);
}
