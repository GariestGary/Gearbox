using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace VolumeBox.Gearbox.Editor
{
    [Serializable]
    [Graph(AssetExtension)]
    public class StateMachineGraph: Graph
    {
        public const string AssetExtension = "grb";

        [MenuItem("Assets/Create/Gearbox/State Machine", false)]
        static void CreateAssetFile()
        {
            GraphDatabase.PromptInProjectBrowserToCreateNewAsset<StateMachineGraph>();
        }

        public override void OnGraphChanged(GraphLogger graphLogger)
        {
            base.OnGraphChanged(graphLogger);
        }
    }
}
