using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace VolumeBox.Gearbox.Core
{
    [Serializable]
    public abstract class StateDefinition
    {
        [field: SerializeField] public string Name { get; set; }
        
        /// <summary>
        /// Reference to the StateMachine component that owns this state.
        /// Use this to access transform, GetComponent, etc.
        /// </summary>
        public StateMachine StateMachine { get; internal set; }

        /// <summary>
        /// Shortcut to access the StateMachine's transform.
        /// </summary>
        protected Transform transform => StateMachine?.transform;

        /// <summary>
        /// Shortcut to access the StateMachine's gameObject.
        /// </summary>
        protected GameObject gameObject => StateMachine?.gameObject;

        internal async UniTask Enter(StateDefinition from = null, object data = null)
        {
            await OnEnter(from, data);
        }

        internal async UniTask Exit(StateDefinition to = null)
        {
            await OnExit(to);
        }

        protected virtual UniTask OnEnter(StateDefinition from, object data)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask OnExit(StateDefinition to)
        {
            return UniTask.CompletedTask;
        }

        public void Update(float delta)
        {
            OnUpdate(delta);
        }

        protected virtual void OnUpdate(float delta)
        {
            
        }

        /// <summary>
        /// Helper method to get components from the StateMachine's GameObject.
        /// </summary>
        protected T GetComponent<T>() where T : Component
        {
            return StateMachine?.GetComponent<T>();
        }

        /// <summary>
        /// Helper method to get components in children from the StateMachine's GameObject.
        /// </summary>
        protected T GetComponentInChildren<T>() where T : Component
        {
            return StateMachine?.GetComponentInChildren<T>();
        }

        /// <summary>
        /// Helper method to get components in parent from the StateMachine's GameObject.
        /// </summary>
        protected T GetComponentInParent<T>() where T : Component
        {
            return StateMachine?.GetComponentInParent<T>();
        }
        
    }
}
