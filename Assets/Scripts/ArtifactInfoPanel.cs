using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class ArtifactInfoPanel : MonoBehaviour
{
    [SerializeField] private Graphic[] graphics;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField, Min(0f)] private float fadeSeconds = 0.25f;
    [SerializeField] private bool previewInEditMode = true;
    [SerializeField] private string previewTitle = "Bartmann Bottle";
    [TextArea(4, 12)]
    [SerializeField] private string previewDescription =
        "This Bartmann bottle was made in Frechen, Germany, around 1550-1700. Made from durable brown-glazed stoneware, it has a rounded body, narrow neck and sturdy handle for storing and carrying liquids. Its moulded flower medallion reflects the decorative style of Frechen stoneware, which was widely traded across Europe and commonly used in domestic and drinking settings.";

    private Coroutine fade;
    private Coroutine pageSequence;
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

        StopPages();
        if (bodyText != null)
        {
            Canvas.ForceUpdateCanvases();
            List<string> pages = BuildPages(body);
            if (pages.Count > 1)
                pageSequence = StartCoroutine(CyclePages(title, pages));
            else
            {
                if (titleText != null) titleText.text = title;
                bodyText.text = body;
            }
        }
        FadeTo(1f);
    }

    public void Hide(Component owner = null)
    {
        if (owner != null && currentOwner != owner) return;
        currentOwner = null;
        StopPages();
        FadeTo(0f);
    }

    public void HideImmediate()
    {
        StopPages();
        SetAlpha(0f);
        if (Application.isPlaying)
            gameObject.SetActive(false);
    }

    private List<string> BuildPages(string body)
    {
        var pages = new List<string>();
        string page = "";
        foreach (Match word in Regex.Matches(body, @"\S+\s*"))
        {
            string candidate = page + word.Value;
            bodyText.text = candidate.TrimEnd();
            if (page.Length > 0 && bodyText.preferredHeight > bodyText.rectTransform.rect.height)
            {
                pages.Add(page.TrimEnd());
                page = word.Value;
            }
            else page = candidate;
        }
        if (page.Length > 0) pages.Add(page.TrimEnd());
        return pages;
    }

    private IEnumerator CyclePages(string title, List<string> pages)
    {
        for (int index = 0; ; index = (index + 1) % pages.Count)
        {
            if (titleText != null) titleText.text = title + " (" + (index + 1) + "/" + pages.Count + ")";
            bodyText.text = pages[index];
            // Allow roughly 160 words per minute, with a minimum twelve-second dwell.
            float seconds = Mathf.Max(12f, Regex.Matches(pages[index], @"\S+").Count / 2.7f);
            yield return new WaitForSecondsRealtime(seconds);
        }
    }

    private void StopPages()
    {
        if (pageSequence != null) StopCoroutine(pageSequence);
        pageSequence = null;
    }

    private void OnDisable()
    {
        StopPages();
        fade = null;
    }

    public void RefreshEditModePreview()
    {
        if (Application.isPlaying || !previewInEditMode)
            return;

        if (titleText != null)
            titleText.text = previewTitle;
        if (bodyText != null && bodyText.rectTransform.rect.height > 0f)
        {
            List<string> pages = BuildPages(previewDescription);
            bodyText.text = pages.Count > 0 ? pages[0] : previewDescription;
        }

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
