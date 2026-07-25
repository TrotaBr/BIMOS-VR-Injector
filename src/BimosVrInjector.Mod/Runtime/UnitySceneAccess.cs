using System.Collections.Generic;
using BimosVrInjector.Core.Abstractions;
using BimosVrInjector.Core.Resolve;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BimosVrInjector.Mod.Runtime
{
    internal sealed class UnitySceneAccess : ISceneAccess
    {
        private List<ITreeNode> _roots = new List<ITreeNode>();

        public string ActiveSceneName { get; private set; } = "";
        public IList<ITreeNode> Roots => _roots;

        public void Refresh()
        {
            var scene = SceneManager.GetActiveScene();
            ActiveSceneName = scene.name;

            var roots = new List<ITreeNode>();
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go != null)
                    roots.Add(new UnityTreeNode(go, null));
            }
            _roots = roots;
        }

        public void SetActive(ITreeNode node, bool active)
        {
            var go = ((UnityTreeNode)node).Go;
            if (go != null)
                go.SetActive(active);
        }

        public void Destroy(ITreeNode node)
        {
            var go = ((UnityTreeNode)node).Go;
            if (go != null)
                Object.Destroy(go);
        }
    }
}
