using BimosVrInjector.Core.Config;
using BimosVrInjector.Core.Resolve;

namespace BimosVrInjector.Core.Abstractions
{
    public interface IGrabTagger
    {
        void Tag(ITreeNode node, GrabbableEntry entry);

        int AutoTagAllBodies();
    }
}
