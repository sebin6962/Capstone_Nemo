using UnityEngine;

public class SpeechBubbleFloat : MonoBehaviour
{
    [Header("위아래 움직임")]
    public float floatHeight = 0.08f;
    public float floatSpeed = 3f;

    [Header("살짝 튀는 스케일")]
    public float scaleAmount = 0.04f;
    public float scaleSpeed = 4f;

    [Header("옵션")]
    public bool randomPhase = true;

    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;
    private float phase;

    private void OnEnable()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalScale = transform.localScale;

        phase = randomPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed + phase) * floatHeight;

        transform.localPosition = originalLocalPosition + new Vector3(0f, yOffset, 0f);

        float scaleOffset = Mathf.Sin(Time.time * scaleSpeed + phase) * scaleAmount;
        transform.localScale = originalLocalScale * (1f + scaleOffset);
    }

    private void OnDisable()
    {
        transform.localPosition = originalLocalPosition;
        transform.localScale = originalLocalScale;
    }
}
