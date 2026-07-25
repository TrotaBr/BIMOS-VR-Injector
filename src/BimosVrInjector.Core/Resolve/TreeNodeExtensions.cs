using System.Collections.Generic;

namespace BimosVrInjector.Core.Resolve
{
    public static class TreeNodeExtensions
    {
        public static string GetPath(this ITreeNode node)
        {
            var names = new List<string>();
            for (ITreeNode? n = node; n != null; n = n.Parent)
                names.Add(n.Name);
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        public static IList<string> ParentChain(this ITreeNode node)
        {
            var names = new List<string>();
            for (ITreeNode? n = node.Parent; n != null; n = n.Parent)
                names.Add(n.Name);
            names.Reverse();
            return names;
        }

        public static IEnumerable<ITreeNode> DescendantsAndSelf(this ITreeNode node)
        {
            yield return node;
            var children = node.Children;
            for (int i = 0; i < children.Count; i++)
                foreach (var d in children[i].DescendantsAndSelf())
                    yield return d;
        }

        public static IEnumerable<ITreeNode> Flatten(this IEnumerable<ITreeNode> roots)
        {
            foreach (var r in roots)
                foreach (var n in r.DescendantsAndSelf())
                    yield return n;
        }
    }
}
