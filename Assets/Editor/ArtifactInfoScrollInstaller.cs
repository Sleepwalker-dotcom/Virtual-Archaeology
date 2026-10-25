using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ArtifactInfoScrollInstaller
{
    [MenuItem("Tools/Virtual Archaeology/Enable Scrolling Artifact Info")]
    public static void Install()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Tavern.unity");
        var panel = Object.FindFirstObjectByType<ArtifactInfoPanel>(FindObjectsInactive.Include);
        Configure(panel);
        panel.RefreshEditModePreview();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Configure(ArtifactInfoPanel panel)
    {
        panel.EnsureScrollableBody();
        EditorUtility.SetDirty(panel);
    }
}
