using System.Collections;
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

    public void Show(string title, string body)
    {
        gameObject.SetActive(true);

        if (titleText != null)
            titleText.text = title;
        if (bodyText != null)
            bodyText.text = body;

        FadeTo(1f);
    }

    public void Hide()
    {
        FadeTo(0f);
    }

    public void HideImmediate()
    {
        SetAlpha(0f);
        if (Application.isPlaying)
            gameObject.SetActive(false);
    }

    public void RefreshEditModePreview()
    {
        if (Application.isPlaying || !previewInEditMode)
            return;

        if (titleText != null)
            titleText.text = previewTitle;
        if (bodyText != null)
            bodyText.text = previewDescription;

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
