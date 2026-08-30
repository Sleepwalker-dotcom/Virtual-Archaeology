using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class ArtifactInfoInstaller
{
    private const string ScenePath = "Assets/Scenes/Tavern.unity";
    private const string BoardPath =
        "Assets/UI/ArtifactInfo/artifact_info_board_transparent.png";

    [MenuItem("Tools/Virtual Archaeology/Install Artifact Info UI")]
    public static void Install()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);

        Sprite boardSprite = ImportBoardSprite();
        ArtifactInfoPanel panel = CreateOrUpdatePanel(boardSprite);

        AttachInfo(
            "Bartmann bottles",
            "Bartmann Bottle",
            "This Bartmann bottle was made in Frechen, Germany, around 1550-1700. Made from durable brown-glazed stoneware, it has a rounded body, narrow neck and sturdy handle for storing and carrying liquids. Its moulded flower medallion reflects the decorative style of Frechen stoneware, which was widely traded across Europe and commonly used in domestic and drinking settings.",
            panel
        );
        AttachInfo(
            "Domino",
            "Domino",
            "This small ivory domino dates from the late 18th to early 19th century. Its value is marked by recessed circular pips on either side of the central dividing line. Dominoes were popular pieces for social tabletop games, and their hard ivory surfaces produced a distinctive click when placed or moved across a wooden table.",
            panel
        );
        AttachInfo(
            "Punch Whistle",
            "Punch Whistle",
            "This 19th-century whistle is made from white metal and shaped as Punch, the well-known comic character associated with British Punch and Judy performances. Although small and decorative, it was also designed to produce a clear, piercing whistle. Its combination of sound and playful figurative design reflects the popular entertainment culture of the period.",
            panel
        );
        AttachInfo(
            "Cockerel",
            "Cockerel-Shaped Object",
            "A small cockerel-shaped object from the Whitechapel assemblage. Its exact material, date and function should be checked against the finds record, so this label is provisional. It is presented here as a decorative or personal object connected to the wider everyday material culture of the site.",
            panel,
            true
        );
        AttachInfo(
            "Half Size Mallet-Type Wine Bottle",
            "Mallet Bottle",
            "A half-size mallet-type wine bottle, a compact form associated with early 18th-century English glass bottle traditions. Bottles like this were used for storing and serving drink, fitting the broader tavern and drinking assemblages from Whitechapel. Confirm the exact date and material against the finds record before final public text.",
            panel,
            true
        );

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Sprite ImportBoardSprite()
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(BoardPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(BoardPath);
    }

    private static ArtifactInfoPanel CreateOrUpdatePanel(Sprite boardSprite)
    {
        Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        GameObject oldRoot = FindSceneObject("ArtifactInfoUI");
        if (oldRoot != null)
            Object.DestroyImmediate(oldRoot);

        GameObject root = new("ArtifactInfoUI", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(camera != null ? camera.transform : null, false);
        root.transform.localPosition = new Vector3(-0.75f, -0.05f, 1.35f);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Canvas canvas = root.GetComponent<Canvas>() ?? root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(650f, 866f);
        rootRect.localScale = Vector3.one * 0.0012f;

        Image board = GetOrCreateImage(root.transform, "Board");
        board.sprite = boardSprite;
        board.preserveAspect = true;
        Stretch(board.rectTransform);

        Text title = GetOrCreateText(root.transform, "TitleText", 44, TextAnchor.MiddleCenter);
        title.rectTransform.anchorMin = new Vector2(0.16f, 0.74f);
        title.rectTransform.anchorMax = new Vector2(0.84f, 0.83f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;

        Text body = GetOrCreateText(root.transform, "BodyText", 27, TextAnchor.UpperLeft);
        body.rectTransform.anchorMin = new Vector2(0.13f, 0.14f);
        body.rectTransform.anchorMax = new Vector2(0.87f, 0.67f);
        body.rectTransform.offsetMin = Vector2.zero;
        body.rectTransform.offsetMax = Vector2.zero;

        ArtifactInfoPanel panel = GetOrAdd<ArtifactInfoPanel>(root);
        SerializedObject serialized = new(panel);
        SerializedProperty graphics = serialized.FindProperty("graphics");
        graphics.arraySize = 3;
        graphics.GetArrayElementAtIndex(0).objectReferenceValue = board;
        graphics.GetArrayElementAtIndex(1).objectReferenceValue = title;
        graphics.GetArrayElementAtIndex(2).objectReferenceValue = body;
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("bodyText").objectReferenceValue = body;
        serialized.FindProperty("previewInEditMode").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
        panel.RefreshEditModePreview();
        return panel;
    }

    private static void AttachInfo(
        string objectName,
        string title,
        string description,
        ArtifactInfoPanel panel,
        bool ensureGrabbable = false
    )
    {
        GameObject obj = FindSceneObject(objectName);
        if (obj == null)
        {
            Debug.LogWarning("[ArtifactInfoInstaller] Missing object: " + objectName);
            return;
        }

        if (ensureGrabbable)
            EnsureGrabbableWithoutGravity(obj);

        ArtifactInfoOnGrab info = GetOrAdd<ArtifactInfoOnGrab>(obj);
        SerializedObject serialized = new(info);
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("title").stringValue = title;
        serialized.FindProperty("description").stringValue = description;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        return obj.GetComponent<T>() ?? obj.AddComponent<T>();
    }

    private static void EnsureGrabbableWithoutGravity(GameObject obj)
    {
        Rigidbody body = GetOrAdd<Rigidbody>(obj);
        body.useGravity = false;
        body.isKinematic = true;

        GetOrAdd<XRGrabInteractable>(obj);
    }

    private static GameObject FindSceneObject(string objectName)
    {
        foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name == objectName && obj.scene.IsValid())
                return obj;
        }

        return null;
    }

    private static Image GetOrCreateImage(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        GameObject obj = child != null
            ? child.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<Image>();
    }

    private static Text GetOrCreateText(
        Transform parent,
        string name,
        int fontSize,
        TextAnchor alignment
    )
    {
        Transform child = parent.Find(name);
        GameObject obj = child != null
            ? child.gameObject
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        obj.transform.SetParent(parent, false);

        Text text = obj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.12f, 0.08f, 0.04f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
