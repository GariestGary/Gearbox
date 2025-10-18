using Unity.GraphToolkit.Editor;

namespace VolumeBox.Gearbox.Editor.Nodes
{
    public class TestNode: Node
    {
        protected override void OnDefinePorts(IPortDefinitionContext context)
        {
            context.AddInputPort<float>("Input").Build();
            context.AddOutputPort("Output").Build();
        }
    }
}