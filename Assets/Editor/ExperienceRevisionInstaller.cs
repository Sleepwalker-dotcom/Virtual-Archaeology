using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ExperienceRevisionInstaller
{
    private const string ScenePath = "Assets/Scenes/Tavern.unity";
    private const string LogoPath = "Assets/UI/Presentation/UCL-Archaeology-South-East.png";
    private const string CompactCredits = "<b>WHITECHAPEL ECHOES</b>\\n\\nMavice & Yixin Zhou (Ella)\\n\\nmavicexie@gmail.com\\nzhouyx0802@outlook.com\\n\\n<b>Supervision</b>\\nMarco Gillies\\nGoldsmiths, University of London\\n\\n<b>With thanks</b>\\nUCL Archaeology South-East\\nSarah Wolferstan · Elke Raemen\\n\\n<b>Music & sound</b>\\nTower Hamlets March — Anneke Scott\\nAdditional music and sound effects: Pixabay\\n\\n<b>Voice</b>\\nAI-generated voice created using MiniMax\\n\\n<b>Sources</b>\\nUCL Whitechapel · Sound Heritage · V&A\\n\\nThank you for listening.";
    [Serializable] private class Item { public string objectName; public string title; public string description; }
    [Serializable] private class Content { public Item[] items; public string credits; }

    [MenuItem("Tools/Virtual Archaeology/Apply September Text and Presentation")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .ToDictionary(t => t, t => (t.localPosition, t.localRotation, t.localScale));
        var content = JsonUtility.FromJson<Content>(File.ReadAllText("Docs/Revised-Experience-Content.json"));
        var panel = Object.FindFirstObjectByType<ArtifactInfoPanel>(FindObjectsInactive.Include);
        var narration = Object.FindFirstObjectByType<NarrationManager>(FindObjectsInactive.Include);
        var museum = Object.FindFirstObjectByType<MuseumExperienceController>(FindObjectsInactive.Include);
        var horn = Object.FindFirstObjectByType<HornFMODController>(FindObjectsInactive.Include);
        Require(panel != null && narration != null && museum != null && horn != null, "Missing experience components");
        Require(horn.hornGrabInteractable != null, "Missing tavern horn grab reference");
        foreach (var item in content.items)
        {
            var target = item.title == "Horn" ? horn.hornGrabInteractable.gameObject :
                transforms.Keys.Select(t => t.gameObject).FirstOrDefault(g => g.name == item.objectName);
            Require(target != null, "Missing object " + item.objectName);
            var info = target.GetComponent<ArtifactInfoOnGrab>() ?? target.AddComponent<ArtifactInfoOnGrab>();
            Set(info, "panel", panel);
            Set(info, "title", item.title);
            Set(info, "description", item.description);
            Set(info, "completionManager", narration);
            Set(info, "completionKey", item.title);
            Set(info, "countsForEnding", true);
            Set(info, "completionManager", narration);
            Set(info, "completionKey", item.title);
            Set(info, "countsForEnding", true);
            if (item.title == "Horn")
            {
                Set(info, "requireFinishedMelody", true);
                Set(info, "hornController", horn);
            }
            Debug.Log("[ExperienceRevision] Updated " + target.name + ": " + item.description.Length + " characters");
        }
        foreach (var info in Object.FindObjectsByType<ArtifactInfoOnGrab>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = new SerializedObject(info);
            if (string.IsNullOrWhiteSpace(data.FindProperty("completionKey").stringValue))
            {
                Set(info, "completionManager", narration);
                Set(info, "completionKey", info.gameObject.name);
                Set(info, "countsForEnding", true);
            }
        }
        foreach (var info in Object.FindObjectsByType<ArtifactInfoOnGrab>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var data = new SerializedObject(info);
            if (string.IsNullOrWhiteSpace(data.FindProperty("completionKey").stringValue))
            {
                Set(info, "completionManager", narration);
                Set(info, "completionKey", info.gameObject.name);
                Set(info, "countsForEnding", true);
            }
        }
        Set(panel, "previewDescription", content.items.First(i => i.title == "Bartmann Bottle").description);
        // Long descriptions use the existing board and font; show the first fitted page in editor previews too.
        var panelData = new SerializedObject(panel);
        var body = (Text)panelData.FindProperty("bodyText").objectReferenceValue;
        Canvas.ForceUpdateCanvases();
        var pagesMethod = typeof(ArtifactInfoPanel).GetMethod("BuildPages", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var item in content.items)
        {
            var pages = (List<string>)pagesMethod.Invoke(panel, new object[] { item.description });
            Require(pages.Count > 0, "Empty pages");
            Require(Normalize(string.Join(" ", pages)) == Normalize(item.description), "Pagination lost text");
            foreach (string page in pages)
            {
                body.text = page;
                Require(body.preferredHeight <= body.rectTransform.rect.height + 1f, "Page clips: " + item.title);
            }
            Debug.Log("[ExperienceRevision] " + item.title + " fits " + pages.Count + " pages at font " + body.fontSize);
        }
        var previewPages = (List<string>)pagesMethod.Invoke(panel, new object[] { content.items[1].description });
        body.text = previewPages[0];

        var textureImporter = (TextureImporter)AssetImporter.GetAtPath(LogoPath);
        textureImporter.mipmapEnabled = false;
        textureImporter.maxTextureSize = 4096;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.SaveAndReimport();
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Samples/XR Interaction Toolkit/3.0.11/Starter Assets/Shaders/UI-NoZTest.shader");
        Require(shader != null, "Missing existing XR overlay shader");
        const string materialPath = "Assets/UI/Presentation/HeadLockedOverlay.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        var presentation = museum.GetComponent<ExperiencePresentation>() ?? museum.gameObject.AddComponent<ExperiencePresentation>();
        Set(presentation, "headCamera", Camera.main);
        Set(presentation, "logo", AssetDatabase.LoadAssetAtPath<Texture>(LogoPath));
        Set(presentation, "overlayMaterial", material);
        Set(presentation, "font", body.font);
        Set(presentation, "fontSize", new SerializedObject(narration).FindProperty("sceneSubtitleFontSize").intValue);
        Set(presentation, "credits", CompactCredits);
        Set(museum, "presentation", presentation);
        Set(narration, "presentation", presentation);

        foreach (var pair in transforms)
            Require((pair.Key.localPosition, pair.Key.localRotation, pair.Key.localScale) == pair.Value,
                "Existing transform changed: " + pair.Key.name);
        ValidateHornLock(horn);
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "Scene save failed");
        AssetDatabase.SaveAssets();
        Debug.Log("[ExperienceRevision] PASS: text complete, pages fit, horn lock works, all existing transforms preserved, scene saved.");
    }

    private static void ValidateHornLock(HornFMODController horn)
    {
        var info = horn.hornGrabInteractable.GetComponent<ArtifactInfoOnGrab>();
        Require(!info.CanShow, "Horn must start locked");
        var finished = typeof(HornFMODController).GetField("<HasFinishedFullMelodyRepeats>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            finished.SetValue(horn, true);
            Require(info.CanShow, "Horn did not unlock after full melody");
            Set(info, "hornController", (Object)null);
            Require(!info.CanShow, "Missing horn reference must remain locked");
        }
        finally
        {
            finished.SetValue(horn, false);
            Set(info, "hornController", horn);
        }
    }

    private static string Normalize(string value) => System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ").Trim();
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("[ExperienceRevision] " + message);
    }
    private static void Set(Object target, string name, object value)
    {
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(name);
        Require(property != null, "Missing property " + name);
        if (value is string text) property.stringValue = text;
        else if (value is bool flag) property.boolValue = flag;
        else if (value is int number) property.intValue = number;
        else property.objectReferenceValue = value as Object;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
