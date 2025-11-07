using System;
using UnityEngine;

namespace VolumeBox.Gearbox.Core
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class StateVariableAttribute : PropertyAttribute
    {
    }
}


