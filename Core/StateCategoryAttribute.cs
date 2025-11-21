using System;

namespace VolumeBox.Gearbox.Core
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class StateCategoryAttribute : Attribute
    {
        public string CategoryPath { get; }

        public StateCategoryAttribute(string categoryPath)
        {
            CategoryPath = categoryPath;
        }
    }
}