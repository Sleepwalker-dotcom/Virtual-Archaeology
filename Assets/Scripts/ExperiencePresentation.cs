using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class ExperiencePresentation : MonoBehaviour
{
    private const string CompactCredits = "<size=55><b>WHITECHAPEL ECHOES</b></size>\n\nMavice & Yixin Zhou (Ella)\n\nmavicexie@gmail.com\nzhouyx0802@outlook.com\n\n<b>Supervision</b>\nMarco Gillies\nGoldsmiths, University of London\n\n<b>With thanks</b>\nUCL Archaeology South-East\nSarah Wolferstan · Elke Raemen\n\n<b>Music & sound</b>\nTower Hamlets March — Anneke Scott\nAdditional music and sound effects: Pixabay\n\n<b>Voice</b>\nAI-generated voice created using MiniMax\n\n<b>Sources</b>\nUCL Whitechapel · Sound Heritage · V&A\n\nThank you for listening.";
    [SerializeField] private Camera headCamera;
    [SerializeField] private Texture logo;
    [SerializeField] private Material overlayMaterial;
    [SerializeField] private Font font;
    [SerializeField] private int fontSize = 32;
    [SerializeField, Min(0f)] private float fadeSeconds = 1.5f;
    [SerializeField, Min(0f)] private float logoHoldSeconds = 5f;
    [SerializeField, Min(1f)] private float creditsPixelsPerSecond = 85f;
    [SerializeField, TextArea(10, 40)] private string credits;

    private GameObject root;
    private CanvasGroup background;
    private CanvasGroup logoGroup;
    private RectTransform viewport;
    private Text creditsText;
    private bool creditsStarted;

    private bool EnsureUI()
    {
        if (root != null) return true;
        if (headCamera == null) headCamera = Camera.main;
        if (headCamera == null || overlayMaterial == null)
        {
            Debug.LogError("[ExperiencePresentation] Camera or overlay material missing.", this);
            return false;
        }

        if (credits.Length > 900) credits = CompactCredits;
        creditsPixelsPerSecond = Mathf.Max(creditsPixelsPerSecond, 85f);
        fontSize = 32;
        root = new GameObject("HeadLockedPresentation", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(headCamera.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, 1.2f);
        root.transform.localScale = Vector3.one * 0.002f;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = headCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32760;
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 700f);

        var black = CreateGraphic<Image>("Black", root.transform, new Vector2(10000f, 10000f));
        black.color = Color.black;
        background = black.gameObject.AddComponent<CanvasGroup>();
        background.alpha = 0f;
        var image = CreateGraphic<RawImage>("UCL Archaeology South-East", root.transform,
            new Vector2(900f, logo != null ? 900f * logo.height / logo.width : 140f));
        image.texture = logo;
        logoGroup = image.gameObject.AddComponent<CanvasGroup>();
        logoGroup.alpha = 0f;

        viewport = new GameObject("CreditsViewport", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        viewport.SetParent(root.transform, false);
        viewport.sizeDelta = new Vector2(920f, 650f);
        creditsText = CreateGraphic<Text>("Credits", viewport, new Vector2(860f, 650f));
        creditsText.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        creditsText.fontSize = fontSize;
        creditsText.color = Color.white;
        creditsText.alignment = TextAnchor.UpperCenter;
        creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        creditsText.verticalOverflow = VerticalWrapMode.Overflow;
        creditsText.supportRichText = true;
        creditsText.text = credits;
        creditsText.rectTransform.pivot = new Vector2(0.5f, 1f);
        viewport.gameObject.SetActive(false);
#if UNITY_EDITOR
        creditsText.fontSize = fontSize;
#endif
        return true;
    }

    private T CreateGraphic<T>(string label, Transform parent, Vector2 size) where T : Graphic
    {
        var graphic = new GameObject(label, typeof(RectTransform), typeof(CanvasRenderer), typeof(T)).GetComponent<T>();
        graphic.transform.SetParent(parent, false);
        graphic.rectTransform.sizeDelta = size;
        graphic.material = overlayMaterial;
        graphic.raycastTarget = false;
        return graphic;
    }

    public IEnumerator PlayOpening()
    {
        if (!EnsureUI()) yield break;
        root.SetActive(true);
        background.alpha = 1f;
        yield return Fade(logoGroup, 1f);
        yield return new WaitForSecondsRealtime(logoHoldSeconds);
        yield return Fade(logoGroup, 0f);
        root.SetActive(false);
    }

    public IEnumerator PlayCredits()
    {
        if (creditsStarted || !EnsureUI()) yield break;
        creditsStarted = true;
        root.SetActive(true);
        logoGroup.alpha = 0f;
        background.alpha = 0f;
        yield return Fade(background, 1f);
        viewport.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        float height = creditsText.preferredHeight;
        creditsText.rectTransform.sizeDelta = new Vector2(860f, height);
        float y = -viewport.rect.height * 0.5f;
        float end = viewport.rect.height * 0.5f + height;
        while (y < end)
        {
            creditsText.rectTransform.anchoredPosition = new Vector2(0f, y);
            y += Mathf.Max(1f, creditsPixelsPerSecond) * Time.unscaledDeltaTime;
            yield return null;
        }
        viewport.gameObject.SetActive(false);
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator Fade(CanvasGroup group, float target)
    {
        float from = group.alpha;
        for (float time = 0f; time < fadeSeconds; time += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, target, time / fadeSeconds);
            yield return null;
        }
        group.alpha = target;
    }

    private void OnDisable()
    {
        if (root != null) root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root);
    }
}
