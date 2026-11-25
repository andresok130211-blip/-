#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Меню CityRunner → настроить проект под Android и собрать APK.</summary>
public static class AndroidBuild
{
    [MenuItem("CityRunner/Настроить под Android")]
    public static void Configure()
    {
        PlayerSettings.companyName = "CityRunner";
        PlayerSettings.productName = "CityRunner";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.cityrunner.lifesim");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3, UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
        Debug.Log("Проект настроен под Android.");
    }

    [MenuItem("CityRunner/Собрать APK")]
    public static void BuildApk()
    {
        Configure();
        Directory.CreateDirectory("Builds");
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/CityRunner.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("Сборка: " + report.summary.result + " → " + opts.locationPathName);
    }
}
#endif
