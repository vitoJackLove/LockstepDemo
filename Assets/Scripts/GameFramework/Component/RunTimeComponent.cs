using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class RunTimeComponent : MonoBehaviour
{
    public void Awake() => GameEntryRunTime.Register(this);

    public virtual void Init(){}

    public virtual UniTask InitAsync()
    {
        Init();
        return UniTask.CompletedTask;
    }

    public virtual void Shutdown(){}
}
