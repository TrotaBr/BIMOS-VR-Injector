using System.Collections.Generic;
using BimosVrInjector.Core.Config;

namespace BimosVrInjector.Core.Resolve
{
    public sealed class ResolveResult
    {
        public ITreeNode? Node { get; }
        public float Confidence { get; }
        public string Reason { get; }

        public bool Matched => Node != null;

        public ResolveResult(ITreeNode? node, float confidence, string reason)
        {
            Node = node;
            Confidence = confidence;
            Reason = reason;
        }
    }

    public static class ObjectResolver
    {
        public const float AcceptThreshold = 0.5f;

        public static ResolveResult Resolve(IEnumerable<ITreeNode> roots, ObjectKey key)
        {
            var all = new List<ITreeNode>(roots.Flatten());

            foreach (var node in all)
            {
                if (node.GetPath() == key.Path)
                    return new ResolveResult(node, 1.0f, "exact path match");
            }

            ITreeNode? best = null;
            float bestScore = 0f;
            int nameMatches = 0;

            foreach (var node in all)
            {
                if (node.Name != key.Name)
                    continue;

                nameMatches++;
                var score = Score(node, key);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }

            if (best != null && bestScore >= AcceptThreshold)
            {
                return new ResolveResult(best, bestScore,
                    $"fuzzy match on '{key.Name}' (confidence {bestScore:0.00})");
            }

            if (nameMatches == 0)
                return new ResolveResult(null, 0f, $"no object named '{key.Name}' in scene");

            return new ResolveResult(null, bestScore,
                $"{nameMatches} object(s) named '{key.Name}' but none matched well enough " +
                $"(best {bestScore:0.00} < {AcceptThreshold:0.00})");
        }

        private static float Score(ITreeNode node, ObjectKey key)
        {
            const float wName = 0.50f;
            const float wParent = 0.30f;
            const float wComponents = 0.15f;
            const float wSibling = 0.05f;

            float parent = SuffixMatchRatio(node.ParentChain(), key.ParentChain);
            float components = OverlapRatio(node.ComponentTypeNames, key.Components);
            float sibling = node.SiblingIndex == key.SiblingIndex ? 1f : 0f;

            return wName + wParent * parent + wComponents * components + wSibling * sibling;
        }

        private static float SuffixMatchRatio(IList<string> live, IList<string> stored)
        {
            if (stored.Count == 0)
                return live.Count == 0 ? 1f : 0f;

            int matched = 0;
            int max = live.Count < stored.Count ? live.Count : stored.Count;
            for (int i = 0; i < max; i++)
            {
                if (live[live.Count - 1 - i] == stored[stored.Count - 1 - i])
                    matched++;
                else
                    break;
            }
            return (float)matched / stored.Count;
        }

        private static float OverlapRatio(IList<string> live, IList<string> stored)
        {
            if (stored.Count == 0)
                return 1f;

            var set = new HashSet<string>(live);
            int hit = 0;
            for (int i = 0; i < stored.Count; i++)
                if (set.Contains(stored[i]))
                    hit++;
            return (float)hit / stored.Count;
        }
    }
}
