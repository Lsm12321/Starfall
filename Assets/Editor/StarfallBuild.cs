using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class StarfallBuild
{
    [MenuItem("Starfall/Prepare scene and settings")]
    public static void Prepare()
    {
        // Keep the committed scene: only create it when missing, and repair a missing bootstrap component.
        const string scenePath = "Assets/Scenes/Main.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Starfall Game").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, scenePath);
        }
        else
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (UnityEngine.Object.FindFirstObjectByType<GameBootstrap>() == null)
            {
                new GameObject("Starfall Game").AddComponent<GameBootstrap>();
                EditorSceneManager.SaveScene(scene);
            }
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
        PlayerSettings.companyName = "Starfall Studio";
        PlayerSettings.productName = "Starfall - The Last Light";
        PlayerSettings.defaultScreenWidth = 1440;
        PlayerSettings.defaultScreenHeight = 810;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input = settings.FindProperty("activeInputHandler");
        if (input != null) input.intValue = 0;
        settings.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        ApplyAppIcon();
        AssetDatabase.SaveAssets();
    }

    private static void ApplyAppIcon()
    {
        const string path = "Assets/Icon/AppIcon.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (icon == null) return;
        int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
        if (sizes == null || sizes.Length == 0) return;
        var icons = new Texture2D[sizes.Length];
        for (int i = 0; i < icons.Length; i++) icons[i] = icon;
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, icons);
    }

    [MenuItem("Starfall/Build Windows game")]
    public static void Build()
    { BuildPlayer(false); }

    [MenuItem("Starfall/Build development validation player")]
    public static void BuildDevelopment()
    { BuildPlayer(true); }

    private static void BuildPlayer(bool development)
    {
        Prepare();
        string output = Path.GetFullPath("outputs/StarfallWindows/Starfall.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = output,
            target = BuildTarget.StandaloneWindows64, options = development ? BuildOptions.Development : BuildOptions.None
        });
        Directory.CreateDirectory("work");
        File.WriteAllText("work/build-result.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors + "\n" + output);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Starfall build failed.");
    }
}
