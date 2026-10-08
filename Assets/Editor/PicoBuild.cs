using System;
using System.IO;
using Unity.XR.PXR;
using Unity.XR.OpenXR.Features.PICOSupport;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class PicoBuild
{
    public const string ApkPath = "Builds/PICO/Whitechapel-Echoes.apk";
    private const string MainScenePath = "Assets/Scenes/Tavern.unity";

    [MenuItem("Tools/PICO/Configure Project")]
    public static void Configure()
    {
        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Could not switch the active build target to Android.");

        PlayerSettings.productName = "Whitechapel Echoes";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mavice.whitechapelechoes");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        PlayerSettings.vulkanEnablePreTransform = false;

        EnsureMobileGsplatFeature();

        PXR_PlatformSetting.Instance.appID ??= string.Empty;
        EditorUtility.SetDirty(PXR_PlatformSetting.Instance);

        var picoFeatures = OpenXRFeatureSetManager.GetFeatureSetWithId(BuildTargetGroup.Android, "com.picoxr.openxr.features")
            ?? throw new InvalidOperationException("PICO OpenXR feature set is missing.");
        picoFeatures.isEnabled = true;
        OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(BuildTargetGroup.Android);

        var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android)
            ?? throw new InvalidOperationException("Android OpenXR settings are missing.");

        foreach (var feature in settings.GetFeatures<OpenXRFeature>())
        {
            if (feature is OpenXRInteractionFeature)
                feature.enabled = feature is PICO4UltraControllerProfile;
            else if (feature is PICOFeature || feature is OpenXRExtensions)
                feature.enabled = true;
            else if (feature.GetType().Name.Contains("Quest", StringComparison.OrdinalIgnoreCase))
                feature.enabled = false;

            EditorUtility.SetDirty(feature);
        }

        var mobilePipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        if (mobilePipeline != null)
        {
            var pipelineSettings = new SerializedObject(mobilePipeline);
            var supportsHdr = pipelineSettings.FindProperty("m_SupportsHDR");
            if (supportsHdr != null)
            {
                supportsHdr.boolValue = false;
                pipelineSettings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("PICO configuration complete: OpenXR, PICO 4 Ultra controller, IL2CPP, ARM64, API 29+, HDR off.");
    }

    private static void EnsureMobileGsplatFeature()
    {
        const string rendererPath = "Assets/Settings/Mobile_Renderer.asset";
        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath)
            ?? throw new InvalidOperationException($"Mobile renderer is missing: {rendererPath}");

        foreach (var existingFeature in rendererData.rendererFeatures)
        {
            if (existingFeature != null && existingFeature.GetType().FullName == "Gsplat.GsplatURPFeature")
                return;
        }

        var featureType = Type.GetType("Gsplat.GsplatURPFeature, Gsplat")
            ?? throw new InvalidOperationException("Gsplat URP feature type is missing.");
        var feature = ScriptableObject.CreateInstance(featureType) as ScriptableRendererFeature
            ?? throw new InvalidOperationException("Could not create the Gsplat URP feature.");

        feature.name = "GsplatURPFeature";
        feature.SetActive(true);
        AssetDatabase.AddObjectToAsset(feature, rendererData);
        rendererData.rendererFeatures.Add(feature);
        rendererData.SetDirty();
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();

        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId))
        {
            var serializedRenderer = new SerializedObject(rendererData);
            var featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
            featureMap.arraySize = rendererData.rendererFeatures.Count;
            featureMap.GetArrayElementAtIndex(featureMap.arraySize - 1).longValue = localId;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    [MenuItem("Tools/PICO/Build Development APK")]
    public static void BuildDevelopmentApk()
    {
        Configure();
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null)
            throw new InvalidOperationException($"Main scene is missing: {MainScenePath}");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { MainScenePath },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"PICO APK build failed: {report.summary.result}");

        Debug.Log($"PICO APK built: {Path.GetFullPath(ApkPath)} ({report.summary.totalSize / 1024 / 1024} MB)");
    }
}
