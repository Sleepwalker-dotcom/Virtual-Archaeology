using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ExperienceRevisionTests
{
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static IEnumerator Play(object target, string method) =>
        (IEnumerator)target.GetType().GetMethod(method).Invoke(target, null);

    [UnityTest]
    public IEnumerator HeadLockedPresentationAndHornLock()
    {
        var cameraObject = new GameObject("Test Head", typeof(Camera));
        var host = new GameObject("Test Presentation");
        var hornObject = new GameObject("Test Horn Controller");
        var grabObject = new GameObject("Test Horn Grab");
        Material material = null;
        try
        {
            var presentation = host.AddComponent(RuntimeType("ExperiencePresentation"));
            material = new Material(Shader.Find("UI/NoZTest"));
            Set(presentation, "headCamera", cameraObject.GetComponent<Camera>());
            Set(presentation, "overlayMaterial", material);
            Set(presentation, "logo", Texture2D.whiteTexture);
            Set(presentation, "credits", "Thank you for listening.");
            Set(presentation, "fadeSeconds", 0f);
            Set(presentation, "logoHoldSeconds", 0.03f);
            Set(presentation, "creditsPixelsPerSecond", 100000f);
            yield return Play(presentation, "PlayOpening");
            var root = (GameObject)Get(presentation, "root");
            Assert.That(root.activeSelf, Is.False, "Opening must finish before original timeline starts");
            Assert.That(root.transform.parent, Is.EqualTo(cameraObject.transform));
            cameraObject.transform.SetPositionAndRotation(new Vector3(1, 2, 3), Quaternion.Euler(0, 80, 0));
            Assert.That(Vector3.Distance(root.transform.localPosition, new Vector3(0, 0, 1.2f)), Is.LessThan(0.001f));
            yield return Play(presentation, "PlayCredits");
            Assert.That(root.activeSelf, Is.True);
            Assert.That(((CanvasGroup)Get(presentation, "background")).alpha, Is.EqualTo(1f));
            Assert.That(((RectTransform)Get(presentation, "viewport")).gameObject.activeSelf, Is.False);
            yield return Play(presentation, "PlayCredits");
            Assert.That(cameraObject.transform.childCount, Is.EqualTo(1), "Credits must not duplicate");

            var horn = (Behaviour)hornObject.AddComponent(RuntimeType("HornFMODController"));
            horn.enabled = false;
            var info = grabObject.AddComponent(RuntimeType("ArtifactInfoOnGrab"));
            Set(info, "requireFinishedMelody", true);
            Set(info, "hornController", horn);
            var canShow = info.GetType().GetProperty("CanShow");
            Assert.That(canShow.GetValue(info), Is.False);
            Set(horn, "<HasFinishedFullMelodyRepeats>k__BackingField", true);
            Assert.That(canShow.GetValue(info), Is.True);
            Set(info, "hornController", null);
            Assert.That(canShow.GetValue(info), Is.False);
        }
        finally
        {
            UnityEngine.Object.Destroy(host);
            UnityEngine.Object.Destroy(cameraObject);
            UnityEngine.Object.Destroy(grabObject);
            UnityEngine.Object.Destroy(hornObject);
            if (material != null) UnityEngine.Object.Destroy(material);
        }
        yield return null;
    }
}
