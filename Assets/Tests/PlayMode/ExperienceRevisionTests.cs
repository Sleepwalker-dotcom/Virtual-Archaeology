using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ExperienceRevisionTests
{
    [TestCase("Cockerel")]
    [TestCase("Half Size Mallet-Type Wine Bottle")]
    public void TapAudioArtefactsBlockPerformanceGrabsAndUnlockIntroductionsAfterMelody(string objectName)
    {
        var host = new GameObject(objectName);
        var hornObject = new GameObject("Test Performance Horn");
        Component horn = null;
        try
        {
            horn = hornObject.AddComponent(RuntimeType("HornFMODController"));
            ((Behaviour)horn).enabled = false;
            var info = host.AddComponent(RuntimeType("ArtifactInfoOnGrab"));
            Set(info, "hornController", horn);
            Set(info, "requireFinishedMelody", true);
            var eventField = info.GetType().GetField("tapFeedbackEvent", BindingFlags.Instance | BindingFlags.NonPublic);
            object audioEvent = Activator.CreateInstance(eventField.FieldType);
            var guidField = eventField.FieldType.GetField("Guid");
            object guid = Activator.CreateInstance(guidField.FieldType);
            guidField.FieldType.GetField("Data1").SetValue(guid, 1);
            guidField.SetValue(audioEvent, guid);
            eventField.SetValue(info, audioEvent);
            object filter = Get(info, "performanceSelectFilter");
            var process = filter.GetType().GetMethod("Process");
            var canShow = info.GetType().GetProperty("CanShow");
            Assert.That(canShow.GetValue(info), Is.False);
            Set(horn, "eventCreated", true);
            Set(horn, "isPlaying", true);
            Set(horn, "isFullMelodyRepeatPlaying", true);
            Assert.That(process.Invoke(filter, new object[] { null, null }), Is.False,
                "Performance taps must not grab the artefact.");
            Assert.That(canShow.GetValue(info), Is.False);
            Set(horn, "isFullMelodyRepeatPlaying", false);
            Set(horn, "<HasFinishedFullMelodyRepeats>k__BackingField", true);
            Assert.That(process.Invoke(filter, new object[] { null, null }), Is.True);
            Assert.That(canShow.GetValue(info), Is.True,
                "The next grab must show the introduction, just like Domino.");
        }
        finally
        {
            if (horn != null) Set(horn, "eventCreated", false);
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(hornObject);
        }
    }

    [UnityTest]
    public IEnumerator HornIntroductionBlocksGuideThenLeavesPickupTutorialPending()
    {
        var managerObject = new GameObject("Test Grab Horn Introduction");
        var hornObject = new GameObject("Test Intro Horn");
        var panelObject = new GameObject("Test Intro Panel", typeof(RectTransform), typeof(Canvas));
        var guide = new GameObject("Test Angle Guide");
        float previousTimeScale = Time.timeScale;
        try
        {
            var horn = (Behaviour)hornObject.AddComponent(RuntimeType("HornFMODController"));
            horn.enabled = false;
            horn.GetType().GetField("guidePanel").SetValue(horn, guide);
            var panel = panelObject.AddComponent(RuntimeType("ArtifactInfoPanel"));
            var manager = managerObject.AddComponent(RuntimeType("NarrationManager"));
            Set(manager, "hornController", horn);
            Set(manager, "grabHornInfoPanel", panel);
            Set(manager, "grabHornInfoDuration", 0.05f);
            Time.timeScale = 0f;
            manager.GetType().GetMethod("PlayGrabHornVoiceOver").Invoke(manager, null);
            Assert.That(Get(horn, "grabHornIntroductionVisible"), Is.False,
                "GrabHornVO must no longer show the introduction.");
            manager.GetType().GetMethod("PlayHornIntroVoiceOver").Invoke(manager, null);
            Assert.That(Get(horn, "grabHornIntroductionVisible"), Is.True);
            Assert.That(guide.activeSelf, Is.False);
            horn.GetType().GetMethod("ActivateHornAndShowCanvas").Invoke(horn, null);
            Assert.That(Get(horn, "isActivated"), Is.False);
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(Get(horn, "grabHornIntroductionVisible"), Is.False);
            Assert.That(Get(horn, "hasShownFirstPickupTutorial"), Is.False,
                "The interaction instructions must still run after the introduction.");

            Set(manager, "grabHornInfoDuration", 10f);
            manager.GetType().GetMethod("ShowGrabHornIntroduction",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, null);
            managerObject.SetActive(false);
            Assert.That(Get(horn, "grabHornIntroductionVisible"), Is.False);
            Assert.That(Get(manager, "grabHornInfoSequence"), Is.Null);
        }
        finally
        {
            Time.timeScale = previousTimeScale;
            UnityEngine.Object.Destroy(managerObject);
            UnityEngine.Object.Destroy(hornObject);
            UnityEngine.Object.Destroy(panelObject);
            UnityEngine.Object.Destroy(guide);
        }
    }

    [UnityTest]
    public IEnumerator ArtifactBodyScrollsWithoutLosingTextAndResetsForNextIntroduction()
    {
        var host = new GameObject("Test Scroll Panel", typeof(RectTransform), typeof(Canvas));
        try
        {
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(ScrollRect));
            viewport.transform.SetParent(host.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.sizeDelta = new Vector2(300f, 100f);
            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(Text));
            bodyObject.transform.SetParent(viewport.transform, false);
            var body = bodyObject.GetComponent<Text>();
            body.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            body.fontSize = 24;
            body.verticalOverflow = VerticalWrapMode.Overflow;
            body.rectTransform.anchorMin = new Vector2(0f, 1f);
            body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.pivot = new Vector2(0.5f, 1f);
            body.rectTransform.sizeDelta = Vector2.zero;
            var scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = body.rectTransform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var panel = host.AddComponent(RuntimeType("ArtifactInfoPanel"));
            Set(panel, "bodyText", body);
            Set(panel, "bodyScroll", scroll);
            Set(panel, "graphics", new Graphic[] { body });
            Set(panel, "fadeSeconds", 0f);
            string longText = string.Join("\n", new string[40]).Replace("\n", "A line of introduction.\n");
            var show = panel.GetType().GetMethod("Show");
            show.Invoke(panel, new object[] { "Long introduction", longText, null });
            yield return null;
            Assert.That(body.text, Is.EqualTo(longText));
            Assert.That(body.rectTransform.rect.height, Is.GreaterThan(viewportRect.rect.height));
            scroll.verticalNormalizedPosition = 0f;
            yield return null;
            Assert.That(body.rectTransform.anchoredPosition.y, Is.GreaterThan(0f));
            show.Invoke(panel, new object[] { "Next introduction", longText, null });
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f));
            show.Invoke(panel, new object[] { "Short introduction", "Short text.", null });
            Assert.That(body.text, Is.EqualTo("Short text."));
            Assert.That(body.rectTransform.rect.height, Is.EqualTo(viewportRect.rect.height).Within(0.001f));
        }
        finally
        {
            UnityEngine.Object.Destroy(host);
        }
    }

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static void Set(object target, string name, object value) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static object Get(object target, string name) => target.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static IEnumerator Play(object target, string method) =>
        (IEnumerator)target.GetType().GetMethod(method).Invoke(target, null);

    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(2, false)]
    [TestCase(3, false)]
    [TestCase(4, false)]
    [TestCase(5, true)]
    public void OnlyFiveDistinctRequiredVoiceoversAdvance(int completed, bool shouldAdvance)
    {
        var host = new GameObject("Test Object Intro Completion");
        try
        {
            var manager = host.AddComponent(RuntimeType("NarrationManager"));
            Set(manager, "ambienceToTavernAliveDelay", 60f);
            var required = (System.Collections.Generic.HashSet<string>)Get(manager, "requiredObjectKeys");
            string[] keys = { "Bartmann bottles", "Punch Whistle", "Domino", "Cockerel", "Half Size Mallet-Type Wine Bottle" };
            foreach (string key in keys) required.Add(key);
            var register = manager.GetType().GetMethod("RegisterObjectCompletion",
                BindingFlags.Instance | BindingFlags.NonPublic);
            register.Invoke(manager, new object[] { "French Natural Horn" });
            for (int i = 0; i < completed; i++)
            {
                register.Invoke(manager, new object[] { keys[i] });
                register.Invoke(manager, new object[] { keys[i] });
            }
            Assert.That(Get(manager, "tavernAliveStarted"), Is.EqualTo(shouldAdvance));
            Assert.That(((System.Collections.Generic.HashSet<string>)Get(manager, "completedObjectKeys")).Count,
                Is.EqualTo(completed));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void EmptyEventAndInvalidPlaybackDoNotCompleteIntroduction()
    {
        var host = new GameObject("Test Empty Object Intro");
        try
        {
            var manager = host.AddComponent(RuntimeType("NarrationManager"));
            ((System.Collections.Generic.HashSet<string>)Get(manager, "requiredObjectKeys")).Add("Cockerel");
            var play = manager.GetType().GetMethod("PlayArtifactIntroVoiceOver");
            var emptyEvent = Activator.CreateInstance(play.GetParameters()[0].ParameterType);
            play.Invoke(manager, new object[] { emptyEvent, "Cockerel" });
            Assert.That(Get(manager, "objectIntroCompletion"), Is.Null);
            var wait = (IEnumerator)manager.GetType().GetMethod("WaitForObjectIntroVoiceOver",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, new object[] { "Cockerel", "Test" });
            Assert.That(wait.MoveNext(), Is.False);
            Assert.That(((System.Collections.Generic.HashSet<string>)Get(manager, "completedObjectKeys")).Count,
                Is.Zero);
            Assert.That(Get(manager, "tavernAliveStarted"), Is.False);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    [UnityTest]
    public IEnumerator HeadLockedPresentationAndHornLock()
    {
        var cameraObject = new GameObject("Test Head", typeof(Camera));
        var host = new GameObject("Test Presentation");
        var hornObject = new GameObject("Test Horn Controller");
        var grabObject = new GameObject("Test Horn Grab");
        var narrationObject = new GameObject("Test Narration Manager");
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
            var narration = (Behaviour)narrationObject.AddComponent(RuntimeType("NarrationManager"));
            narration.enabled = false;
            Set(narration, "hornController", horn);
            var info = grabObject.AddComponent(RuntimeType("ArtifactInfoOnGrab"));
            Set(info, "requireFinishedMelody", true);
            Set(info, "hornController", horn);
            var canShow = info.GetType().GetProperty("CanShow");
            Assert.That(canShow.GetValue(info), Is.False);
            Assert.That(narration.GetType().GetProperty("IsObjectInteractionBlocked").GetValue(narration), Is.True);
            Set(horn, "<HasFinishedFullMelodyRepeats>k__BackingField", true);
            Assert.That(canShow.GetValue(info), Is.True);
            Assert.That(narration.GetType().GetProperty("IsObjectInteractionBlocked").GetValue(narration), Is.False);
            Set(info, "hornController", null);
            Assert.That(canShow.GetValue(info), Is.False);
        }
        finally
        {
            UnityEngine.Object.Destroy(host);
            UnityEngine.Object.Destroy(cameraObject);
            UnityEngine.Object.Destroy(grabObject);
            UnityEngine.Object.Destroy(hornObject);
            UnityEngine.Object.Destroy(narrationObject);
            if (material != null) UnityEngine.Object.Destroy(material);
        }
        yield return null;
    }
}
