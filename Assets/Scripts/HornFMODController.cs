using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class HornFMODController : MonoBehaviour
{
    [Header("FMOD Event")]
    public EventReference hornEvent;

    [Header("VR References")]
    public Transform playerHead;
    public Transform hornObject;

    [Header("Distance Settings")]
    public float maxDistance = 1.2f;

    [Header("Smoothing")]
    public float distanceSmoothing = 8f;
    public float angleSmoothing = 8f;

    private EventInstance hornInstance;

    private float smoothedDistance = 1.2f;
    private float smoothedAngle = 0f;

    void Start()
    {
        hornInstance = RuntimeManager.CreateInstance(hornEvent);

        // 如果你希望声音位置跟随乐器，可以保留这句。
        hornInstance.set3DAttributes(RuntimeUtils.To3DAttributes(hornObject));

        hornInstance.start();
    }

    void Update()
    {
        if (playerHead == null || hornObject == null)
            return;

        // 1. 计算乐器到玩家头部的距离
        float rawDistance = Vector3.Distance(hornObject.position, playerHead.position);
        rawDistance = Mathf.Clamp(rawDistance, 0f, maxDistance);

        // 2. 计算乐器与水平面的夹角
        // 这里默认 hornObject.forward 是乐器朝向 / 出声方向
        Vector3 forward = hornObject.forward.normalized;
        float rawAngle = Mathf.Asin(Vector3.Dot(forward, Vector3.up)) * Mathf.Rad2Deg;
        rawAngle = Mathf.Clamp(rawAngle, -90f, 90f);

        // 3. 平滑，避免 VR 手柄轻微抖动导致声音抖动
        smoothedDistance = Mathf.Lerp(smoothedDistance, rawDistance, Time.deltaTime * distanceSmoothing);
        smoothedAngle = Mathf.Lerp(smoothedAngle, rawAngle, Time.deltaTime * angleSmoothing);

        // 4. 把参数传给 FMOD
        hornInstance.setParameterByName("DistanceToHead", smoothedDistance);
        hornInstance.setParameterByName("HornAngle", smoothedAngle);

        // 5. 如果是 3D 声音，让声音位置跟随乐器
        hornInstance.set3DAttributes(RuntimeUtils.To3DAttributes(hornObject));
    }

    void OnDestroy()
    {
        hornInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        hornInstance.release();
    }
}