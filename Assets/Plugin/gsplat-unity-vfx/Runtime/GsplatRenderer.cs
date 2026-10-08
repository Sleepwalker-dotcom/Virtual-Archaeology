// Copyright (c) 2025 CocoLinux0101

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Gsplat
{
    public enum GsplatEffectType
    {
        None = 0,
        DeepMeditation = 1, // visualization effect
        Waves = 2, // visualization effect
        Flare = 3, // reveal effects
        Disintegrate = 4,  // reveal effects
        Wind = 5, // visualization effect
        PerlinWave = 6, // visualization effect
        Magic = 7, // reveal effects

        Spread = 8, // reveal effects

        Unroll = 9, // reveal effects

        Twister = 10, // reveal effects

        Rain = 11, // reveal effects
        Glitter = 12,  // visualization effect
        GalaxyGlitter = 13, // visualization effect

        FlyDissolve = 14, // dissolve effect
        GlowDissolve = 15,  // glow dissolve effect

        RadialExpansion = 16, // radial expansion effect
    }

    [ExecuteAlways]
    public class GsplatRenderer : MonoBehaviour, IGsplat
    {
        public GsplatAsset GsplatAsset;
        [Range(0, 3)] public int SHDegree = 3;
        public bool GammaToLinear;

        [Header("Mobile Performance")]
        [Min(0)] public uint mobileSplatLimit = 1000000;
        [Range(1f, 1.3f)] public float mobileScaleCompensation = 1.15f;

        GsplatAsset m_prevAsset;
        GsplatRendererImpl m_renderer;

        public bool Valid => GsplatAsset;
        public uint SplatCount => m_renderer?.SplatCount ?? 0;
        public ISorterResource SorterResource => m_renderer.SorterResource;
        public GsplatEffectType effectType = GsplatEffectType.PerlinWave;
        [Range(0f, 1f)] public float intensity = 0.5f;
        [Range(-0.2f, 0.2f)] public float waveAmplitude = 0.1f;
        [Range(0.1f, 6f)] public float waveFrequency = 2f;
        [Range(0f, 2f)] public float waveSpeed = 0.5f;
        [Range(0f, 1f)] public float blendScale = 0.5f;
        [Range(-5f, -1f)] public float lightWaveAmplitude = -2.0f;
        [Range(-5f, 5f)] public float lightWaveFrequency = 2.0f;
        [Range(0.1f, 10f)] public float lightWaveSpeed = 2.0f;
        [Range(0.0f, 1.0f)] public float glitterDensity = 0.2f;
        [Range(0.1f, 3.0f)] public float burnDuration = 0.5f;
        [Range(-1f, 1f)] public float rainDirection = 1f;

        [Header("Optional Spatial Split")]
        public bool useSplitMask;
        public Vector3 splitPlaneNormal = Vector3.forward;
        public float splitPlaneOffset;
        public bool keepPositiveSide = true;

        [Header("Optional Box Region")]
        public bool useBoxMask;
        public Transform boxMaskTransform;
        public bool keepInsideBox = true;

        float baseTime = 0;

        void SetBufferData()
        {
            uint splatCount = m_renderer.SplatCount;
            int[] sampleIndices = CreateSpatialSampleIndices(GsplatAsset.Positions, GsplatAsset.Scales,
                GsplatAsset.Colors, (int)splatCount, GsplatAsset.Bounds);
            SetSampledBufferData(m_renderer.PositionBuffer, GsplatAsset.Positions, sampleIndices);
            SetSampledScaleBufferData(m_renderer.ScaleBuffer, GsplatAsset.Scales, sampleIndices);
            SetSampledBufferData(m_renderer.RotationBuffer, GsplatAsset.Rotations, sampleIndices);
            SetSampledBufferData(m_renderer.ColorBuffer, GsplatAsset.Colors, sampleIndices);

            if (GsplatAsset.SHBands > 0)
            {
                int coefficientsPerSplat = GsplatUtils.SHBandsToCoefficientCount(GsplatAsset.SHBands);
                SetSampledBufferData(m_renderer.SHBuffer, GsplatAsset.SHs, sampleIndices, coefficientsPerSplat);
            }
        }

        static void SetSampledBufferData<T>(GraphicsBuffer buffer, T[] source, int[] sampleIndices,
            int elementsPerSplat = 1) where T : struct
        {
            int sourceSplatCount = source.Length / elementsPerSplat;
            if (sampleIndices == null)
            {
                buffer.SetData(source);
                return;
            }

            var sampled = new T[sampleIndices.Length * elementsPerSplat];
            for (int targetIndex = 0; targetIndex < sampleIndices.Length; targetIndex++)
            {
                int sourceIndex = sampleIndices[targetIndex];
                Array.Copy(source, sourceIndex * elementsPerSplat, sampled,
                    targetIndex * elementsPerSplat, elementsPerSplat);
            }

            buffer.SetData(sampled);
        }

        void SetSampledScaleBufferData(GraphicsBuffer buffer, Vector3[] source, int[] sampleIndices)
        {
            if (sampleIndices == null)
            {
                buffer.SetData(source);
                return;
            }

            var sampled = new Vector3[sampleIndices.Length];
            for (int i = 0; i < sampleIndices.Length; i++)
                sampled[i] = source[sampleIndices[i]] * mobileScaleCompensation;
            buffer.SetData(sampled);
        }

        public static int[] CreateSpatialSampleIndices(Vector3[] positions, Vector3[] scales, Vector4[] colors,
            int targetCount, Bounds bounds)
        {
            if (targetCount >= positions.Length)
                return null;

            const int gridSize = 64;
            const int cellCount = gridSize * gridSize * gridSize;
            var counts = new int[cellCount];
            for (int i = 0; i < positions.Length; i++)
                counts[GetCellIndex(positions[i], bounds, gridSize)]++;

            var quotas = new int[cellCount];
            var occupiedCells = new List<int>();
            for (int cell = 0; cell < cellCount; cell++)
            {
                if (counts[cell] == 0)
                    continue;
                occupiedCells.Add(cell);
            }

            bool canCoverEveryCell = targetCount >= occupiedCells.Count;
            int quotaTotal = 0;
            foreach (int cell in occupiedCells)
            {
                quotas[cell] = (int)((long)counts[cell] * targetCount / positions.Length);
                if (canCoverEveryCell)
                    quotas[cell] = Math.Max(1, quotas[cell]);
                quotaTotal += quotas[cell];
            }

            occupiedCells.Sort((a, b) => quotas[b].CompareTo(quotas[a]));
            for (int i = 0; quotaTotal > targetCount; i++)
            {
                int cell = occupiedCells[i % occupiedCells.Count];
                if (quotas[cell] > (canCoverEveryCell ? 1 : 0))
                {
                    quotas[cell]--;
                    quotaTotal--;
                }
            }

            occupiedCells.Sort((a, b) =>
                ((long)counts[b] * targetCount % positions.Length)
                .CompareTo((long)counts[a] * targetCount % positions.Length));
            for (int i = 0; quotaTotal < targetCount; i++)
            {
                int cell = occupiedCells[i % occupiedCells.Count];
                if (quotas[cell] < counts[cell])
                {
                    quotas[cell]++;
                    quotaTotal++;
                }
            }

            var accumulators = new int[cellCount];
            var bestIndices = new int[cellCount];
            var bestScores = new float[cellCount];
            Array.Fill(bestIndices, -1);
            var result = new int[targetCount];
            int resultIndex = 0;
            for (int i = 0; i < positions.Length; i++)
            {
                int cell = GetCellIndex(positions[i], bounds, gridSize);
                if (quotas[cell] == 0)
                    continue;

                Vector3 scale = scales[i];
                float score = colors[i].w * (scale.x * scale.y + scale.x * scale.z + scale.y * scale.z);
                if (bestIndices[cell] < 0 || score > bestScores[cell])
                {
                    bestIndices[cell] = i;
                    bestScores[cell] = score;
                }

                accumulators[cell] += quotas[cell];
                if (accumulators[cell] < counts[cell])
                    continue;

                accumulators[cell] -= counts[cell];
                result[resultIndex++] = bestIndices[cell];
                bestIndices[cell] = -1;
                bestScores[cell] = 0f;
            }

            Array.Sort(result);
            return result;
        }

        static int GetCellIndex(Vector3 position, Bounds bounds, int gridSize)
        {
            Vector3 size = bounds.size;
            int x = Mathf.Clamp((int)((position.x - bounds.min.x) / Math.Max(size.x, 0.000001f) * gridSize), 0, gridSize - 1);
            int y = Mathf.Clamp((int)((position.y - bounds.min.y) / Math.Max(size.y, 0.000001f) * gridSize), 0, gridSize - 1);
            int z = Mathf.Clamp((int)((position.z - bounds.min.z) / Math.Max(size.z, 0.000001f) * gridSize), 0, gridSize - 1);
            return x + gridSize * (y + gridSize * z);
        }

        public static int GetEvenlySampledSourceIndex(int sampleIndex, int sampleCount, int sourceCount)
        {
            return (int)((long)sampleIndex * sourceCount / sampleCount);
        }

        uint GetRenderSplatCount()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (mobileSplatLimit > 0)
                return Math.Min(GsplatAsset.SplatCount, mobileSplatLimit);
#endif
            return GsplatAsset.SplatCount;
        }

        void OnEnable()
        {
            GsplatSorter.Instance.RegisterGsplat(this);
            if (!GsplatAsset)
                return;
            uint splatCount = GetRenderSplatCount();
            m_renderer = new GsplatRendererImpl(splatCount, GsplatAsset.SHBands);
            SetBufferData();
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log($"[Gsplat] Mobile splat budget: {splatCount:N0}/{GsplatAsset.SplatCount:N0}", this);
#endif
        }

        void OnDisable()
        {
            GsplatSorter.Instance.UnregisterGsplat(this);
            m_renderer?.Dispose();
            m_renderer = null;
        }

        void Update()
        {


            if (m_prevAsset != GsplatAsset)
            {
                m_prevAsset = GsplatAsset;
                if (GsplatAsset)
                {
                    uint splatCount = GetRenderSplatCount();
                    if (m_renderer == null)
                        m_renderer = new GsplatRendererImpl(splatCount, GsplatAsset.SHBands);
                    else
                        m_renderer.RecreateResources(splatCount, GsplatAsset.SHBands);
                    SetBufferData();
                }
            }

            if (Valid)
            {
                baseTime = baseTime + Time.deltaTime;
                m_renderer.SetEffectParameters(
                    (int)effectType,
                    intensity,
                    baseTime,
                    new Vector3(1, 0, 0),
                    waveAmplitude,
                    waveFrequency,
                    waveSpeed,
                    blendScale,
                    lightWaveAmplitude,
                    lightWaveFrequency,
                    lightWaveSpeed,
                    glitterDensity,
                    0.3f,
                    burnDuration,
                    rainDirection,
                    useSplitMask,
                    splitPlaneNormal,
                    splitPlaneOffset,
                    keepPositiveSide
                );
                Matrix4x4 modelToBox = boxMaskTransform != null
                    ? boxMaskTransform.worldToLocalMatrix * transform.localToWorldMatrix
                    : Matrix4x4.identity;
                m_renderer.SetBoxMaskParameters(
                    useBoxMask && boxMaskTransform != null,
                    modelToBox,
                    keepInsideBox);

                m_renderer.Render(m_renderer.SplatCount, transform, GsplatAsset.Bounds, gameObject.layer,
                    GammaToLinear, SHDegree);
            }

        }

        public void resetAnimationTime()
        {
            baseTime = 0;
        }


        #region visual effects

        public IEnumerator BlendReveal(float duration)
        {
            float startValue = 0.001f;
            float endValue = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blendScale = Mathf.Lerp(startValue, endValue, elapsed / duration);
                yield return null;
            }

            blendScale = endValue; // ensure it ends exactly at 1
        }

        public void SetMagicEffect()
        {
            effectType = GsplatEffectType.Magic;
            resetAnimationTime();
            StartCoroutine(WaitUntilRevealFinish());
        }
        public void SetRainEffect()
        {
            effectType = GsplatEffectType.Rain;
            resetAnimationTime();
            StartCoroutine(WaitUntilRevealFinish());
        }
        public void SetTwisterEffect()
        {
            effectType = GsplatEffectType.Twister;
            resetAnimationTime();
            StartCoroutine(WaitUntilRevealFinish());
        }
        public IEnumerator WaitUntilRevealFinish()
        {
            yield return new WaitForSeconds(10.5f);
            effectType = GsplatEffectType.PerlinWave;

        }
        public void StartBlendAppear(float effectTime=2.0f)
        {
            StartCoroutine(BlendAppear(effectTime));
        }
        public void StartBlendDisappear(float effectTime = 2.0f)
        {
            StartCoroutine(BlendDisappear(effectTime));
        }

        public void StartShockWaveEffect(float effectTime = 1.0f)
        {
            StartCoroutine(ShockWaveEffect(effectTime));
        }

        public IEnumerator BlendAppear(float duration)
        {
            float startValue = 0.001f;
            float endValue = 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blendScale = Mathf.Lerp(startValue, endValue, elapsed / duration);
                yield return null;
            }

            blendScale = endValue; 
        }

        public IEnumerator BlendDisappear(float duration)
        {
            float startValue = 1f;
            float endValue = 0.001f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blendScale = Mathf.Lerp(startValue, endValue, elapsed / duration);
                yield return null;
            }

            blendScale = endValue; 
        }


        public IEnumerator ShockWaveEffect(float duration)
        {
            yield return new WaitForSeconds(1.0f);

            float startValue = waveFrequency;
            float startWaveSpeed = waveSpeed;
            float targetValue = 1.5f;
            float elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                waveFrequency = Mathf.Lerp(startValue, targetValue, t);
                waveSpeed = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }

            elapsed = 0f;

            while (elapsed < duration / 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration / 2f);
                waveFrequency = Mathf.Lerp(targetValue, startValue, t);
                waveSpeed = Mathf.Lerp(targetValue, startWaveSpeed, t);
                yield return null;
            }

            waveFrequency = startValue; // ensure exact reset
        }
    }
    #endregion

}
