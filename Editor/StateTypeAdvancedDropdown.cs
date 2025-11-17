using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Editor
{
    public class StateTypeAdvancedDropdown : AdvancedDropdown
    {
        private readonly Action<Type> _onTypeSelected;
        private readonly GearboxTypeCache.StateTypeInfo[] _stateTypes;

        public StateTypeAdvancedDropdown(AdvancedDropdownState state, Action<Type> onTypeSelected)
            : base(state)
        {
            _onTypeSelected = onTypeSelected;
            _stateTypes = GearboxTypeCache.GetStateDefinitionTypesWithInfo();
            minimumSize = new UnityEngine.Vector2(300, 200);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("State Types");

            // Group by category hierarchy
            var groupedTypes = _stateTypes
                .GroupBy(info => string.IsNullOrEmpty(info.CategoryPath) ? "" : info.CategoryPath)
                .OrderBy(g => g.Key);

            foreach (var group in groupedTypes)
            {
                if (string.IsNullOrEmpty(group.Key))
                {
                    // No category - add directly to root
                    foreach (var info in group.OrderBy(i => i.Type.Name))
                    {
                        root.AddChild(new AdvancedDropdownItem(info.Type.Name) { id = info.Type.GetHashCode() });
                    }
                }
                else
                {
                    // Create hierarchy
                    var categoryParts = group.Key.Split('/');
                    var currentParent = root;

                    for (int i = 0; i < categoryParts.Length; i++)
                    {
                        var part = categoryParts[i];
                        var existingItem = currentParent.children?.FirstOrDefault(c => c.name == part);

                        if (existingItem == null)
                        {
                            var newItem = new AdvancedDropdownItem(part);
                            currentParent.AddChild(newItem);
                            currentParent = newItem;
                        }
                        else
                        {
                            currentParent = existingItem;
                        }

                        // Only add types at the leaf level
                        if (i == categoryParts.Length - 1)
                        {
                            foreach (var info in group.OrderBy(t => t.Type.Name))
                            {
                                currentParent.AddChild(new AdvancedDropdownItem(info.Type.Name) { id = info.Type.GetHashCode() });
                            }
                        }
                    }
                }
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            var selectedType = _stateTypes.FirstOrDefault(t => t.Type.GetHashCode() == item.id)?.Type;
            if (selectedType != null)
            {
                _onTypeSelected(selectedType);
            }
        }
    }
}