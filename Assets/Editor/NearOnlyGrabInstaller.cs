using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

public static class NearOnlyGrabInstaller
{
    private const string ScenePath = "Assets/Scenes/Tavern.unity";
    // Physics layers keep close grabbing independent from the controller UI rays.
    private const string LayerName = "NearGrabbable";
    private static readonly string[] RootNames = { "Object Group", "French Natural Horn", "Horn_Piece" };

    [MenuItem("Tools/Virtual Archaeology/Configure Near Only Grabbing")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode before configuring grabbing.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Open Tavern before configuring grabbing.");

        var transforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var roots = RootNames.Select(name => transforms.Single(t => t.name == name)).ToArray();
        var interactors = transforms.Select(t => t.GetComponent<NearFarInteractor>())
            .Where(interactor => interactor != null).ToArray();
        if (interactors.Length != 2 || interactors.Any(interactor =>
                !(interactor.nearInteractionCaster is SphereInteractionCaster) ||
                !(interactor.farInteractionCaster is CurveInteractionCaster)))
            throw new InvalidOperationException("Expected both controller Near-Far Interactors with valid casters.");

        int layer = LayerMask.NameToLayer(LayerName);
        if (layer < 0)
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            layer = Enumerable.Range(8, 24).First(index => string.IsNullOrEmpty(layers.GetArrayElementAtIndex(index).stringValue));
            layers.GetArrayElementAtIndex(layer).stringValue = LayerName;
            tags.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (var root in roots)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                Undo.RecordObject(child.gameObject, "Configure near-only grab layer");
                child.gameObject.layer = layer;
                PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
            }
        }

        foreach (var interactor in interactors)
        {
            var near = (SphereInteractionCaster)interactor.nearInteractionCaster;
            var far = (CurveInteractionCaster)interactor.farInteractionCaster;
            Undo.RecordObjects(new UnityEngine.Object[] { interactor, near, far }, "Configure near-only grabbing");
            interactor.enableNearCasting = true;
            interactor.enableFarCasting = true;
            interactor.enableUIInteraction = true;
            near.physicsLayerMask = near.physicsLayerMask.value | (1 << layer);
            far.raycastMask = far.raycastMask.value & ~(1 << layer);
            foreach (var component in new Component[] { interactor, near, far })
            {
                EditorUtility.SetDirty(component);
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }

        Validate();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[NearOnlyGrab] Saved Tavern: Object Group, French Natural Horn and Horn_Piece are near-only; both UI rays remain enabled.");
    }

    public static void Validate()
    {
        int layer = LayerMask.NameToLayer(LayerName);
        if (layer < 0) throw new InvalidOperationException("Missing NearGrabbable layer.");
        var transforms = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (string name in RootNames)
        {
            var root = transforms.Single(t => t.name == name);
            if (root.GetComponentsInChildren<Transform>(true).Any(t => t.gameObject.layer != layer))
                throw new InvalidOperationException("Incorrect layer under " + name);
            Debug.Log("[NearOnlyGrab] Verified " + name + ": " + root.GetComponentsInChildren<Collider>(true).Length + " colliders.");
        }
        var interactors = transforms.Select(t => t.GetComponent<NearFarInteractor>()).Where(i => i != null).ToArray();
        if (interactors.Length != 2) throw new InvalidOperationException("Expected two Near-Far Interactors.");
        foreach (var interactor in interactors)
        {
            var near = interactor.nearInteractionCaster as SphereInteractionCaster;
            var far = interactor.farInteractionCaster as CurveInteractionCaster;
            if (!interactor.enableNearCasting || !interactor.enableFarCasting || !interactor.enableUIInteraction ||
                near == null || far == null || (near.physicsLayerMask.value & (1 << layer)) == 0 ||
                (far.raycastMask.value & (1 << layer)) != 0)
                throw new InvalidOperationException("Incorrect caster configuration: " + interactor.name);
        }
    }
}
