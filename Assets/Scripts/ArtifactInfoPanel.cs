using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

[ExecuteAlways]
public sealed class ArtifactInfoPanel : MonoBehaviour
{
    [SerializeField] private Graphic[] graphics;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private ScrollRect bodyScroll;
    [Header("Introduction Completion Mark")]
    [SerializeField] private Text completionMark;
    [SerializeField] private Color pendingMarkColor = new Color(0.55f, 0.55f, 0.55f);
    [SerializeField] private Color completedMarkColor = new Color(0.15f, 0.65f, 0.25f);
    [SerializeField, Min(0f)] private float fadeSeconds = 0.25f;
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField] private string previewTitle = "Bartmann Bottle";
    [TextArea(4, 12)]
    [SerializeField] private string previewDescription =
        "This Bartmann bottle was made in Frechen, Germany, around 1550–1700. Its durable brown-glazed stoneware body, narrow neck and handle suited storing and carrying liquids. Bartmann means “bearded man”, referring to the face moulded on the neck. In Britain, some bottles were later reused as protective “witch bottles”.";

    private Coroutine fade;
    private Component currentOwner;

    private void Awake()
    {
        if (Application.isPlaying)
            HideImmediate();
        else
            RefreshEditModePreview();
    }

    private void OnValidate()
    {
        RefreshEditModePreview();
    }

    public void Show(string title, string body, Component owner = null)
    {
        currentOwner = owner;
        if (completionMark != null) completionMark.gameObject.SetActive(false);
        gameObject.SetActive(true);

        if (titleText != null) titleText.text = title;
        EnsureScrollableBody();
        SetBody(body);
        FadeTo(1f);
    }

    [ContextMenu("Create Completion Mark Preview")]
    private void CreateCompletionMarkPreview()
    {
        if (Application.isPlaying) return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Create completion mark preview");
#endif
        SetCompletionStatus(currentOwner, false);
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    public void SetCompletionStatus(Component owner, bool completed)
    {
        if (owner != currentOwner || titleText == null) return;
        if (completionMark == null)
        {
            completionMark = new GameObject("IntroductionCompletionMark", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.RegisterCreatedObjectUndo(completionMark.gameObject, "Create completion mark");
#endif
            completionMark.transform.SetParent(titleText.transform, false);
            completionMark.gameObject.layer = titleText.gameObject.layer;
            completionMark.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            completionMark.fontSize = 24;
            completionMark.alignment = TextAnchor.UpperCenter;
            completionMark.horizontalOverflow = HorizontalWrapMode.Overflow;
            completionMark.verticalOverflow = VerticalWrapMode.Overflow;
            completionMark.raycastTarget = false;
            completionMark.material = titleText.material;
            completionMark.text = "\u2713";
            completionMark.rectTransform.anchorMin = Vector2.zero;
            completionMark.rectTransform.anchorMax = new Vector2(1f, 0f);
            completionMark.rectTransform.pivot = new Vector2(0.5f, 1f);
            completionMark.rectTransform.sizeDelta = new Vector2(0f, 28f);
            float titleOverflow = Mathf.Max(0f, titleText.preferredHeight - titleText.rectTransform.rect.height);
            completionMark.rectTransform.anchoredPosition = new Vector2(0f, -6f - titleOverflow * 0.5f);
            int count = graphics != null ? graphics.Length : 0;
            System.Array.Resize(ref graphics, count + 1);
            graphics[count] = completionMark;
        }

        Color color = completed ? completedMarkColor : pendingMarkColor;
        color.a = titleText.color.a;
        completionMark.color = color;
        completionMark.gameObject.SetActive(true);
    }

    public void Hide(Component owner = null)
    {
        if (owner != null && currentOwner != owner) return;
        currentOwner = null;
        FadeTo(0f);
    }

    public void HideImmediate()
    {
        SetAlpha(0f);
        if (Application.isPlaying)
            gameObject.SetActive(false);
    }

    public IEnumerator HideAndWait(Component owner = null)
    {
        if (owner != null && currentOwner != owner) yield break;
        Hide(owner);
        while (currentOwner == null && gameObject.activeSelf && fade != null)
            yield return null;
    }

    public void EnsureScrollableBody()
    {
        if (bodyScroll != null || bodyText == null) return;
        Text body = bodyText;

        RectTransform bodyRect = body.rectTransform;
        var area = new GameObject("BodyScrollView", typeof(RectTransform),
            typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        area.layer = body.gameObject.layer;
        var viewport = area.GetComponent<RectTransform>();
        viewport.SetParent(bodyRect.parent, false);
        viewport.SetSiblingIndex(bodyRect.GetSiblingIndex());
        viewport.anchorMin = bodyRect.anchorMin;
        viewport.anchorMax = bodyRect.anchorMax;
        viewport.pivot = bodyRect.pivot;
        viewport.sizeDelta = bodyRect.sizeDelta;
        viewport.anchoredPosition3D = bodyRect.anchoredPosition3D;
        viewport.localRotation = bodyRect.localRotation;
        viewport.localScale = bodyRect.localScale;
        area.GetComponent<Image>().color = Color.clear;

        bodyRect.SetParent(viewport, false);
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = Vector2.one;
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition3D = Vector3.zero;
        bodyRect.sizeDelta = Vector2.zero;
        bodyRect.localScale = Vector3.one;
        bodyRect.localRotation = Quaternion.identity;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        body.raycastTarget = true;

        var scroll = area.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = bodyRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        bodyScroll = scroll;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
    }

    private void SetBody(string body)
    {
        if (bodyText == null) return;
        bodyText.text = body;
        if (bodyScroll == null) return;

        Canvas.ForceUpdateCanvases();
        bodyText.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            Mathf.Max(bodyScroll.viewport.rect.height, bodyText.preferredHeight));
        Canvas.ForceUpdateCanvases();
        bodyScroll.StopMovement();
        bodyScroll.verticalNormalizedPosition = 1f;
    }

    private void OnDisable()
    {
        fade = null;
    }

    public void RefreshEditModePreview()
    {
        if (Application.isPlaying || !previewInEditMode)
            return;

        if (titleText != null)
            titleText.text = previewTitle;
        SetBody(previewDescription);

        SetAlpha(1f);
    }

    private void FadeTo(float alpha)
    {
        if (graphics == null || graphics.Length == 0)
            return;

        if (fade != null)
            StopCoroutine(fade);

        fade = StartCoroutine(FadeCanvas(alpha));
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        float startAlpha = graphics[0] != null ? graphics[0].color.a : 0f;
        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(
                startAlpha,
                targetAlpha,
                fadeSeconds <= 0f ? 1f : elapsed / fadeSeconds));
            yield return null;
        }

        SetAlpha(targetAlpha);
        if (Mathf.Approximately(targetAlpha, 0f))
            gameObject.SetActive(false);

        fade = null;
    }

    private void SetAlpha(float alpha)
    {
        if (graphics == null)
            return;

        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
                continue;

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
