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
        gameObject.SetActive(true);

        if (titleText != null) titleText.text = title;
        EnsureScrollableBody();
        SetBody(body);
        FadeTo(1f);
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
