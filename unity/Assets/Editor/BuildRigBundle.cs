using System.IO;
using UnityEditor;

public static class BuildRigBundle
{
    [MenuItem("BIMOS/Build rig.bundle (Windows64)")]
    public static void Build()
    {
        const string outDir = "AssetBundles";
        Directory.CreateDirectory(outDir);
        BuildPipeline.BuildAssetBundles(
            outDir,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64);
        EditorUtility.RevealInFinder(Path.Combine(outDir, "rig.bundle"));
    }
}
