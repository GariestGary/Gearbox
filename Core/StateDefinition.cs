using System;
using Cysharp.Threading.Tasks;

namespace VolumeBox.Gearbox.Core
{
    [Serializable]
    public abstract class StateDefinition
    {
        public virtual UniTask OnEnter()
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask OnExit()
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask OnUpdate()
        {
            return UniTask.CompletedTask;
        }
    }
}


