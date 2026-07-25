using System.Collections.Generic;

namespace BimosVrInjector.Core.Resolve
{
    public interface ITreeNode
    {
        string Name { get; }

        int SiblingIndex { get; }

        ITreeNode? Parent { get; }

        IList<ITreeNode> Children { get; }

        IList<string> ComponentTypeNames { get; }
    }
}
