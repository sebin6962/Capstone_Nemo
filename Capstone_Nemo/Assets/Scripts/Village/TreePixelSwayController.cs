using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TreePixelSwayController : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private float speed = 0.9f;
    [SerializeField] private float amplitude = 0.025f;
    [SerializeField] private float verticalAmplitude = 0.012f;

    [Header("Top Area")]
    [SerializeField, Range(0f, 1f)] private float topStart = 0.42f;
    [SerializeField, Range(0.01f, 0.5f)] private float feather = 0.25f;

    [Header("Shape")]
    [SerializeField] private float bendStrength = 0.03f;
    [SerializeField] private float edgeBoost = 0.35f;
    [SerializeField] private float verticalSquash = 0.01f;

    [Header("Motion Detail")]
    [SerializeField, Range(0f, 1f)] private float secondWaveStrength = 0.35f;
    [SerializeField] private float heightPhaseShift = 1.4f;
    [SerializeField] private float verticalPhaseShift = 2.1f;
    [SerializeField] private float centerLift = 0.35f;
    [SerializeField] private float uvWaveInfluence = 1.2f;

    [Header("Region Motion")]
    [SerializeField] private float regionPhaseAmount = 0.55f;
    [SerializeField] private float regionAmplitudeBoost = 0.18f;
    [SerializeField] private float regionVerticalBoost = 0.15f;

    [Header("Pixel Art")]
    [SerializeField] private bool usePixelSnap = false;
    [SerializeField] private float pixelsPerUnit = 16f;

    [Header("Variation")]
    [SerializeField] private bool randomizeOnStart = true;
    [SerializeField] private Vector2 randomSpeedRange = new Vector2(0.92f, 1.08f);
    [SerializeField] private Vector2 randomAmplitudeRange = new Vector2(0.9f, 1.1f);

    private SpriteRenderer sr;
    private Material runtimeMat;

    private static readonly int SpeedID = Shader.PropertyToID("_Speed");
    private static readonly int AmplitudeID = Shader.PropertyToID("_Amplitude");
    private static readonly int VerticalAmplitudeID = Shader.PropertyToID("_VerticalAmplitude");
    private static readonly int TopStartID = Shader.PropertyToID("_TopStart");
    private static readonly int FeatherID = Shader.PropertyToID("_Feather");
    private static readonly int PhaseOffsetID = Shader.PropertyToID("_PhaseOffset");
    private static readonly int BendStrengthID = Shader.PropertyToID("_BendStrength");
    private static readonly int EdgeBoostID = Shader.PropertyToID("_EdgeBoost");
    private static readonly int VerticalSquashID = Shader.PropertyToID("_VerticalSquash");
    private static readonly int SecondWaveStrengthID = Shader.PropertyToID("_SecondWaveStrength");
    private static readonly int HeightPhaseShiftID = Shader.PropertyToID("_HeightPhaseShift");
    private static readonly int VerticalPhaseShiftID = Shader.PropertyToID("_VerticalPhaseShift");
    private static readonly int CenterLiftID = Shader.PropertyToID("_CenterLift");
    private static readonly int UvWaveInfluenceID = Shader.PropertyToID("_UvWaveInfluence");
    private static readonly int RegionPhaseAmountID = Shader.PropertyToID("_RegionPhaseAmount");
    private static readonly int RegionAmplitudeBoostID = Shader.PropertyToID("_RegionAmplitudeBoost");
    private static readonly int RegionVerticalBoostID = Shader.PropertyToID("_RegionVerticalBoost");
    private static readonly int UsePixelSnapID = Shader.PropertyToID("_UsePixelSnap");
    private static readonly int PixelPerUnitID = Shader.PropertyToID("_PixelPerUnit");

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        runtimeMat = new Material(sr.sharedMaterial);
        sr.material = runtimeMat;
    }

    private void Start()
    {
        float finalSpeed = speed;
        float finalAmplitude = amplitude;
        float phase = 0f;

        if (randomizeOnStart)
        {
            finalSpeed *= Random.Range(randomSpeedRange.x, randomSpeedRange.y);
            finalAmplitude *= Random.Range(randomAmplitudeRange.x, randomAmplitudeRange.y);
            phase = Random.Range(0f, 100f);
        }

        ApplyValues(finalSpeed, finalAmplitude, phase);
    }

    private void ApplyValues(float finalSpeed, float finalAmplitude, float phase)
    {
        if (runtimeMat == null) return;

        runtimeMat.SetFloat(SpeedID, finalSpeed);
        runtimeMat.SetFloat(AmplitudeID, finalAmplitude);
        runtimeMat.SetFloat(VerticalAmplitudeID, verticalAmplitude);
        runtimeMat.SetFloat(TopStartID, topStart);
        runtimeMat.SetFloat(FeatherID, feather);
        runtimeMat.SetFloat(PhaseOffsetID, phase);

        runtimeMat.SetFloat(BendStrengthID, bendStrength);
        runtimeMat.SetFloat(EdgeBoostID, edgeBoost);
        runtimeMat.SetFloat(VerticalSquashID, verticalSquash);

        runtimeMat.SetFloat(SecondWaveStrengthID, secondWaveStrength);
        runtimeMat.SetFloat(HeightPhaseShiftID, heightPhaseShift);
        runtimeMat.SetFloat(VerticalPhaseShiftID, verticalPhaseShift);
        runtimeMat.SetFloat(CenterLiftID, centerLift);
        runtimeMat.SetFloat(UvWaveInfluenceID, uvWaveInfluence);

        runtimeMat.SetFloat(RegionPhaseAmountID, regionPhaseAmount);
        runtimeMat.SetFloat(RegionAmplitudeBoostID, regionAmplitudeBoost);
        runtimeMat.SetFloat(RegionVerticalBoostID, regionVerticalBoost);

        runtimeMat.SetFloat(UsePixelSnapID, usePixelSnap ? 1f : 0f);
        runtimeMat.SetFloat(PixelPerUnitID, pixelsPerUnit);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (runtimeMat == null) return;

        ApplyValues(speed, amplitude, runtimeMat.GetFloat(PhaseOffsetID));
    }

    private void OnDestroy()
    {
        if (runtimeMat != null)
            Destroy(runtimeMat);
    }
}
