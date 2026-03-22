using System.Collections;
using UnityEngine;

public class TreeAuroraEffect : MonoBehaviour
{
    [Header("오로라 레이어")]
    public SpriteRenderer whiteLayer;
    public SpriteRenderer blueLayer;
    public SpriteRenderer purpleLayer;

    [Header("움직임")]
    public float floatAmplitude = 0.12f;
    public float floatSpeed = 1.2f;
    public float swayAmplitude = 0.08f;
    public float swaySpeed = 0.9f;

    [Header("최대 투명도")]
    [Range(0f, 1f)] public float whiteAlpha = 0.45f;
    [Range(0f, 1f)] public float blueAlpha = 0.35f;
    [Range(0f, 1f)] public float purpleAlpha = 0.32f;

    [Header("스케일")]
    public Vector3 baseScale = Vector3.one;
    public Vector3 pulseScale = new Vector3(1.08f, 1.12f, 1f);

    private Vector3 _basePos;
    private Vector3 _whiteLocalPos;
    private Vector3 _blueLocalPos;
    private Vector3 _purpleLocalPos;

    private void Awake()
    {
        _basePos = transform.position;

        if (whiteLayer != null) _whiteLocalPos = whiteLayer.transform.localPosition;
        if (blueLayer != null) _blueLocalPos = blueLayer.transform.localPosition;
        if (purpleLayer != null) _purpleLocalPos = purpleLayer.transform.localPosition;

        SetLayerAlpha(whiteLayer, 0f);
        SetLayerAlpha(blueLayer, 0f);
        SetLayerAlpha(purpleLayer, 0f);

        transform.localScale = baseScale;
    }

    public IEnumerator PlayRoutine(float fadeIn, float stay, float fadeOut)
    {
        float total = fadeIn + stay + fadeOut;
        float t = 0f;

        while (t < total)
        {
            t += Time.deltaTime;

            float alphaFactor;
            if (t < fadeIn)
            {
                alphaFactor = Mathf.Clamp01(t / fadeIn);
            }
            else if (t < fadeIn + stay)
            {
                alphaFactor = 1f;
            }
            else
            {
                float outT = (t - fadeIn - stay) / fadeOut;
                alphaFactor = 1f - Mathf.Clamp01(outT);
            }

            AnimateLayers(t, alphaFactor);

            yield return null;
        }

        SetLayerAlpha(whiteLayer, 0f);
        SetLayerAlpha(blueLayer, 0f);
        SetLayerAlpha(purpleLayer, 0f);
    }

    private void AnimateLayers(float t, float alphaFactor)
    {
        float yOffset = Mathf.Sin(t * floatSpeed) * floatAmplitude;
        float pulse = (Mathf.Sin(t * 1.6f) + 1f) * 0.5f;

        transform.position = _basePos + new Vector3(0f, yOffset, 0f);
        transform.localScale = Vector3.Lerp(baseScale, pulseScale, pulse);

        if (whiteLayer != null)
        {
            float x = Mathf.Sin(t * swaySpeed) * swayAmplitude;
            whiteLayer.transform.localPosition = _whiteLocalPos + new Vector3(x, 0f, 0f);
            SetLayerAlpha(whiteLayer, whiteAlpha * alphaFactor);
        }

        if (blueLayer != null)
        {
            float x = Mathf.Sin(t * (swaySpeed * 1.25f) + 1.1f) * (swayAmplitude * 1.2f);
            blueLayer.transform.localPosition = _blueLocalPos + new Vector3(x, 0.03f, 0f);
            SetLayerAlpha(blueLayer, blueAlpha * alphaFactor);
        }

        if (purpleLayer != null)
        {
            float x = Mathf.Sin(t * (swaySpeed * 0.85f) + 2.2f) * (swayAmplitude * 1.35f);
            purpleLayer.transform.localPosition = _purpleLocalPos + new Vector3(x, -0.02f, 0f);
            SetLayerAlpha(purpleLayer, purpleAlpha * alphaFactor);
        }
    }

    private void SetLayerAlpha(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
