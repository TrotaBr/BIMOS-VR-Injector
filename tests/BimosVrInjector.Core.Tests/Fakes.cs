using System.Collections.Generic;
using BimosVrInjector.Core.Abstractions;
using BimosVrInjector.Core.Config;
using BimosVrInjector.Core.Resolve;

namespace BimosVrInjector.Core.Tests;

internal sealed class FakeNode : ITreeNode
{
    private readonly List<ITreeNode> _children = new();
    public string Name { get; }
    public int SiblingIndex { get; internal set; }
    public ITreeNode? Parent { get; private set; }
    public IList<ITreeNode> Children => _children;
    public IList<string> ComponentTypeNames { get; }

    public bool Active { get; set; } = true;
    public bool Destroyed { get; set; }

    public FakeNode(string name, params string[] components)
    {
        Name = name;
        ComponentTypeNames = components;
    }

    public FakeNode Add(FakeNode child)
    {
        child.Parent = this;
        child.SiblingIndex = _children.Count;
        _children.Add(child);
        return child;
    }
}

internal sealed class FakeScene : ISceneAccess
{
    private readonly List<ITreeNode> _roots;

    public FakeScene(string sceneName, params FakeNode[] roots)
    {
        ActiveSceneName = sceneName;
        _roots = new List<ITreeNode>(roots);
    }

    public string ActiveSceneName { get; }
    public IList<ITreeNode> Roots => _roots;
    public int RefreshCount { get; private set; }

    public void Refresh() => RefreshCount++;
    public void SetActive(ITreeNode node, bool active) => ((FakeNode)node).Active = active;
    public void Destroy(ITreeNode node) => ((FakeNode)node).Destroyed = true;
}

internal sealed class FakeRigSpawner : IRigSpawner
{
    public int SpawnCount { get; private set; }
    public int DespawnCount { get; private set; }
    public float[]? LastPos { get; private set; }

    public void Spawn(float[] pos, float[] rotEuler, float[] scale)
    {
        SpawnCount++;
        LastPos = pos;
    }

    public void DespawnExisting() => DespawnCount++;
}

internal sealed class FakeGrabTagger : IGrabTagger
{
    public List<string> Tagged { get; } = new();
    public int AutoTagCalls { get; private set; }

    public void Tag(ITreeNode node, GrabbableEntry entry) => Tagged.Add(node.GetPath());

    public int AutoTagAllBodies()
    {
        AutoTagCalls++;
        return 7;
    }
}

internal sealed class FakeLog : ILog
{
    public List<string> Warnings { get; } = new();
    public void Info(string message) { }
    public void Warn(string message) => Warnings.Add(message);
    public void Error(string message) => Warnings.Add("ERROR: " + message);
}
