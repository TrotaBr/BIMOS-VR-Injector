using System.Collections.Generic;
using BimosVrInjector.Core.Resolve;

namespace BimosVrInjector.Core.Config
{
    public sealed class ObjectKey
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public List<string> ParentChain { get; set; } = new List<string>();
        public int SiblingIndex { get; set; }
        public List<string> Components { get; set; } = new List<string>();

        public ObjectKey() { }

        public static ObjectKey From(ITreeNode node)
        {
            return new ObjectKey
            {
                Path = node.GetPath(),
                Name = node.Name,
                ParentChain = new List<string>(node.ParentChain()),
                SiblingIndex = node.SiblingIndex,
                Components = new List<string>(node.ComponentTypeNames),
            };
        }

        public override string ToString() => Path.Length > 0 ? Path : Name;
    }
}
