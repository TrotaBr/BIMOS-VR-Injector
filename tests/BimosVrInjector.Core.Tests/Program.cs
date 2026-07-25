using BimosVrInjector.Core.Authoring;
using BimosVrInjector.Core.Config;
using BimosVrInjector.Core.Playback;
using BimosVrInjector.Core.Resolve;
using BimosVrInjector.Core.Tests;

var t = new Harness();

var roots = BuildScene();

var flatNode = Find(roots, "Player/Camera/FlatController");
var hudNode = Find(roots, "UI/HUDCanvas");
var crateNode = Find(roots, "Props/Crate_01");

var log = new OperationLog("Level_01");
log.ToggleDisable(ObjectKey.From(flatNode));
log.ToggleDisable(ObjectKey.From(hudNode));
log.ToggleGrabbable(ObjectKey.From(crateNode));
log.SetRig(new[] { 1.2f, 0f, 3.4f }, new[] { 0f, 90f, 0f }, new[] { 1f, 1f, 1f });

t.Eq(2, log.DisableCount, "two objects disabled");
t.Eq(1, log.GrabbableCount, "one grabbable");
t.True(log.HasRig, "rig recorded");

log.ToggleDisable(ObjectKey.From(flatNode));
t.Eq(1, log.DisableCount, "toggle-off removed the disable");
log.ToggleDisable(ObjectKey.From(flatNode));
t.Eq(2, log.DisableCount, "toggle-on restored it");

var cfg = log.Build();
var json = ConfigStore.Serialize(cfg);
Console.WriteLine("---- serialized config ----");
Console.WriteLine(json);
Console.WriteLine("---------------------------");

t.True(json.Contains("\"sceneName\": \"Level_01\""), "camelCase sceneName present");
t.True(json.Contains("\"rigSpawn\""), "rigSpawn present");
t.True(json.Contains("\"path\": \"Player/Camera/FlatController\""), "structured key path present");

var reloaded = ConfigStore.Deserialize(json)!;
t.Eq(cfg.Disable.Count, reloaded.Disable.Count, "disable count survives round-trip");
t.Eq("Level_01", reloaded.SceneName, "scene name survives round-trip");
t.NotNull(reloaded.RigSpawn, "rig survives round-trip");
t.Eq(90f, reloaded.RigSpawn!.Rot[1], "rig rotation survives round-trip");

var freshRoots = BuildScene();
var scene = new FakeScene("Level_01", freshRoots);
var rig = new FakeRigSpawner();
var grab = new FakeGrabTagger();
var plog = new FakeLog();

new PlaybackApplier(scene, rig, grab, plog).Apply(reloaded);

var freshFlat = Find(freshRoots, "Player/Camera/FlatController");
var freshHud = Find(freshRoots, "UI/HUDCanvas");

t.False(freshFlat.Active, "FlatController disabled on replay");
t.False(freshHud.Active, "HUDCanvas disabled on replay");
t.Eq(1, rig.SpawnCount, "rig spawned once on replay");
t.Eq(1, rig.DespawnCount, "existing rig cleared before spawn");
t.Eq(1.2f, rig.LastPos![0], "rig spawned at saved position");
t.True(grab.Tagged.Contains("Props/Crate_01"), "crate tagged grabbable on replay");
t.Eq(0, plog.Warnings.Count, "no resolution warnings on a matching scene");

t.Eq(0, grab.AutoTagCalls, "auto-grab NOT invoked unless the config opts in");

var autoLog = new OperationLog("Level_01");
t.False(autoLog.AutoGrabAllBodies, "auto-grab defaults to off");
t.True(autoLog.ToggleAutoGrabAll(), "toggle turns auto-grab on");

var autoJson = ConfigStore.Serialize(autoLog.Build());
t.True(autoJson.Contains("\"autoGrabAllBodies\": true"), "auto-grab flag serialized");

var autoGrabTagger = new FakeGrabTagger();
new PlaybackApplier(new FakeScene("Level_01", BuildScene()), new FakeRigSpawner(),
    autoGrabTagger, new FakeLog()).Apply(ConfigStore.Deserialize(autoJson)!);
t.Eq(1, autoGrabTagger.AutoTagCalls, "auto-grab invoked on replay when enabled");

var storedKey = ObjectKey.From(flatNode);
var res = ObjectResolver.Resolve(BuildMoved(), storedKey);
t.True(res.Matched, "moved object still resolves via fuzzy fallback");
t.True(res.Confidence < 1.0f && res.Confidence >= ObjectResolver.AcceptThreshold,
    $"fuzzy confidence in range (got {res.Confidence:0.00})");

var missCfg = new SceneConfig { SceneName = "Level_01" };
missCfg.Disable.Add(new ObjectKey { Path = "Ghost/DoesNotExist", Name = "DoesNotExist" });
var missLog = new FakeLog();
new PlaybackApplier(new FakeScene("Level_01", BuildScene()),
    new FakeRigSpawner(), new FakeGrabTagger(), missLog).Apply(missCfg);
t.Eq(1, missLog.Warnings.Count, "missing object produced exactly one warning");
t.True(missLog.Warnings[0].Contains("DoesNotExist"), "warning names the missing object");

return t.Report();

static FakeNode[] BuildScene()
{
    var player = new FakeNode("Player", "Transform");
    var camera = new FakeNode("Camera", "Transform", "Camera", "AudioListener");
    var flat = new FakeNode("FlatController", "Transform", "FlatController");
    player.Add(camera).Add(flat);

    var ui = new FakeNode("UI", "Transform", "Canvas");
    ui.Add(new FakeNode("HUDCanvas", "Transform", "Canvas", "GraphicRaycaster"));

    var props = new FakeNode("Props", "Transform");
    props.Add(new FakeNode("Crate_01", "Transform", "Rigidbody", "BoxCollider"));

    return new[] { player, ui, props };
}

static FakeNode[] BuildMoved()
{
    var player = new FakeNode("Player", "Transform");
    var wrapper = new FakeNode("Rig", "Transform");
    var camera = new FakeNode("Camera", "Transform", "Camera", "AudioListener");
    var flat = new FakeNode("FlatController", "Transform", "FlatController");
    player.Add(wrapper).Add(camera).Add(flat);
    return new[] { player };
}

static FakeNode Find(IList<ITreeNode> roots, string path)
{
    foreach (var n in roots)
        foreach (var d in n.DescendantsAndSelf())
            if (d.GetPath() == path)
                return (FakeNode)d;
    throw new Exception($"test setup: node '{path}' not found");
}
