using System.Collections.Generic;
using BimosVrInjector.Core.Resolve;
using UnityEngine;

namespace BimosVrInjector.Mod.Runtime
{
    internal sealed class UnityTreeNode : ITreeNode
    {
        public GameObject Go { get; }

        private readonly UnityTreeNode? _parent;
        private List<ITreeNode>? _children;
        private List<string>? _components;

        public UnityTreeNode(GameObject go, UnityTreeNode? parent)
        {
            Go = go;
            _parent = parent;
        }

        public bool IsAlive => Go != null;

        public string Name => Go != null ? Go.name : "<destroyed>";
        public int SiblingIndex => Go != null ? Go.transform.GetSiblingIndex() : -1;
        public ITreeNode? Parent => _parent;

        public IList<ITreeNode> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new List<ITreeNode>();
                    if (Go == null)
                        return _children;
                    var t = Go.transform;
                    int count = t.childCount;
                    for (int i = 0; i < count; i++)
                    {
                        var child = t.GetChild(i);
                        if (child != null)
                            _children.Add(new UnityTreeNode(child.gameObject, this));
                    }
                }
                return _children;
            }
        }

        public IList<string> ComponentTypeNames
        {
            get
            {
                if (_components == null)
                {
                    _components = new List<string>();
                    if (Go == null)
                        return _components;
                    var comps = Go.GetComponents<Component>();
                    foreach (var comp in comps)
                    {
                        if (comp == null)
                            continue;
                        _components.Add(comp.GetType().Name);
                    }
                }
                return _components;
            }
        }

        public static UnityTreeNode Wrap(GameObject go)
        {
            var chain = new List<Transform>();
            for (var t = go.transform; t != null; t = t.parent)
                chain.Add(t);
            chain.Reverse();

            UnityTreeNode? parent = null;
            UnityTreeNode node = null!;
            foreach (var t in chain)
            {
                node = new UnityTreeNode(t.gameObject, parent);
                parent = node;
            }
            return node;
        }
    }
}
