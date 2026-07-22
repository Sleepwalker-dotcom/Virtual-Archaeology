using UnityEngine;

[System.Serializable]
public class ExtraLayerUnlock
{
    [Header("Round Settings")]
    public string layerName = "Extra Layer";
    public int unlockRoundIndex = 2;

    [Header("FMOD Parameter")]
    public string fmodParameterName = "ExtraLayer1Active";

    [Header("Trigger Object")]
    public GameObject triggerObject;

    [Header("Round Reveal Object")]
    [Tooltip("Visual object revealed automatically when this layer's round starts.")]
    public GameObject newItemObject;

    [Header("Glow Renderers")]
    public Renderer[] glowRenderers;
    public Color glowColor = Color.cyan;
    public float glowIntensity = 3f;

    [HideInInspector] public bool unlocked;
    [HideInInspector] public bool activated;
}
