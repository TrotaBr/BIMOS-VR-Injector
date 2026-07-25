using System.Collections.Generic;
using BimosVrInjector.Core.Resolve;

namespace BimosVrInjector.Core.Abstractions
{
    public interface ISceneAccess
    {
        string ActiveSceneName { get; }

        IList<ITreeNode> Roots { get; }

        void Refresh();

        void SetActive(ITreeNode node, bool active);

        void Destroy(ITreeNode node);
    }
}
