using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class QuestBuild
{
    public const string ApkPath = "Builds/Quest/Whitechapel-Echoes.apk";
    private const string MainScenePath = "Assets/Scenes/Tavern.unity";

    [MenuItem("Tools/Quest/Configure Project")]
    public static void Configure()
    {
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Could not switch the active build target to Android.");

        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android)
            ?? throw new InvalidOperationException("Android OpenXR settings are missing.");
        if (settings.GetFeature<MetaQuestFeature>() == null ||
            settings.GetFeature<OculusTouchControllerProfile>() == null ||
            settings.GetFeature<MetaQuestTouchPlusControllerProfile>() == null)
            throw new InvalidOperationException("Quest support or controller profiles are missing.");

        var picoFeatures = OpenXRFeatureSetManager.GetFeatureSetWithId(BuildTargetGroup.Android, "com.picoxr.openxr.features");
        if (picoFeatures != null)
            picoFeatures.isEnabled = false;
        OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(BuildTargetGroup.Android);

        foreach (var feature in settings.GetFeatures<OpenXRFeature>())
        {
            if (feature is OpenXRInteractionFeature)
                feature.enabled = feature is OculusTouchControllerProfile || feature is MetaQuestTouchPlusControllerProfile;
            else if (feature is MetaQuestFeature)
                feature.enabled = true;
            else if (feature.GetType().Namespace != null &&
                     feature.GetType().Namespace.IndexOf("PICO", StringComparison.OrdinalIgnoreCase) >= 0)
                feature.enabled = false;

            EditorUtility.SetDirty(feature);
        }

        PlayerSettings.productName = "Whitechapel Echoes";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mavice.whitechapelechoes");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log("Quest configuration complete: Meta Quest support, Touch controllers, IL2CPP, ARM64, Vulkan.");
    }

    [MenuItem("Tools/Quest/Build Development APK")]
    public static void BuildDevelopmentApk()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            throw new InvalidOperationException($"Main scene is missing: {MainScenePath}");

        Configure();
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.buildAppBundle = false;
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { MainScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Quest APK build failed: {report.summary.result}");

        Debug.Log($"Quest APK built: {Path.GetFullPath(ApkPath)} ({report.summary.totalSize / 1024 / 1024} MB)");
    }
}
