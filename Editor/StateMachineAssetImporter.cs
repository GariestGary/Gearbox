using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using VolumeBox.Gearbox.Core;

namespace VolumeBox.Gearbox.Editor
{
    [ScriptedImporter(1, StateMachineGraph.AssetExtension)]
    public class StateMachineAssetImporter: ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var graph = GraphDatabase.LoadGraphForImporter<StateMachineGraph>(ctx.assetPath);
            var runtimeAsset = ScriptableObject.CreateInstance<GearboxStateMachineGraph>();
            ctx.AddObjectToAsset("RuntimeAsset", runtimeAsset);
            ctx.SetMainObject(runtimeAsset);
        }
    }
}