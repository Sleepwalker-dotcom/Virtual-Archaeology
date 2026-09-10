using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ExperienceRevisionSceneTests
{
    [Serializable] private class Item { public string objectName; public string title; public string description; }
    [Serializable] private class Content { public Item[] items; public string credits; }

    [Test]
    public void SavedSceneContainsRevisedTextAndPresentationReferences()
    {
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Tavern.unity");
            var content = JsonUtility.FromJson<Content>(File.ReadAllText("Docs/Revised-Experience-Content.json"));
            var infoType = Type.GetType("ArtifactInfoOnGrab, Assembly-CSharp", true);
            var infos = UnityEngine.Object.FindObjectsByType(infoType, FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var item in content.items)
            {
                bool matched = false;
                foreach (var info in infos)
                {
                    var data = new SerializedObject(info);
                    if (data.FindProperty("title").stringValue != item.title) continue;
                    Assert.AreEqual(item.description, data.FindProperty("description").stringValue);
                    Assert.NotNull(data.FindProperty("panel").objectReferenceValue);
                    if (item.title == "Horn")
                    {
                        Assert.IsTrue(data.FindProperty("requireFinishedMelody").boolValue);
                        Assert.NotNull(data.FindProperty("hornController").objectReferenceValue);
                    }
                    matched = true;
                }
                Assert.IsTrue(matched, "Missing " + item.title);
            }
            foreach (string type in new[] { "MuseumExperienceController", "NarrationManager" })
            {
                var component = UnityEngine.Object.FindFirstObjectByType(Type.GetType(type + ", Assembly-CSharp", true));
                Assert.NotNull(new SerializedObject(component).FindProperty("presentation").objectReferenceValue);
            }

            var touchType = Type.GetType("TapToggleGlow, Assembly-CSharp", true);
            var touchObjects = UnityEngine.Object.FindObjectsByType(touchType, FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(3, touchObjects.Length);
            foreach (var touchObject in touchObjects)
            {
                var touchData = new SerializedObject(touchObject);
                var effect = touchData.FindProperty("performanceTouchEffectPrefab").objectReferenceValue;
                Assert.NotNull(effect);
                Assert.AreEqual(
                    "Assets/Lana Studio/Hyper Casual FX/Prefabs/Flash/Flash_magic_ellow_blue.prefab",
                    AssetDatabase.GetAssetPath(effect));
                Assert.LessOrEqual(touchData.FindProperty("effectScaleRange").vector2Value.y, 0.012f);
            }

            Transform huntingHornVisual = null;
            foreach (Transform candidate in UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == "Hunting Horn Visual")
                {
                    huntingHornVisual = candidate;
                    break;
                }
            }
            Assert.NotNull(huntingHornVisual);
            Assert.IsTrue(huntingHornVisual.gameObject.activeSelf);
            var hornMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Model/Mavice/Hunting_Horn/Material.001.mat");
            Assert.AreEqual("Universal Render Pipeline/Unlit", hornMaterial.shader.name);

            var hornType = Type.GetType("HornFMODController, Assembly-CSharp", true);
            var horn = UnityEngine.Object.FindFirstObjectByType(hornType);
            var hornData = new SerializedObject(horn);
            var firstPerformanceEffect = hornData.FindProperty("firstPerformanceEffectPrefab");
            Assert.NotNull(firstPerformanceEffect.objectReferenceValue);
            Assert.AreEqual(
                "Assets/Lana Studio/Hyper Casual FX/Prefabs/Confetti/Confetti_blast_multicolor.prefab",
                AssetDatabase.GetAssetPath(firstPerformanceEffect.objectReferenceValue));
            Assert.AreEqual(0.02f, hornData.FindProperty("firstPerformanceEffectScale").floatValue);

            var museumType = Type.GetType("MuseumExperienceController, Assembly-CSharp", true);
            var museum = UnityEngine.Object.FindFirstObjectByType(museumType);
            Assert.AreEqual(
                10f,
                new SerializedObject(museum).FindProperty("delayedTavernRevealDelay").floatValue);
        }
        finally
        {
            if (Array.Exists(setup, scene => scene.isLoaded && scene.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        }
    }
}
