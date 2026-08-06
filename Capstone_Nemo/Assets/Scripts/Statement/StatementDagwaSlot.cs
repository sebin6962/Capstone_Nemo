using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class StatementDagwaSlot : MonoBehaviour
{
    [Header("표시 대상")]
    [SerializeField]
    private Image itemImage;

    [Header("스프라이트 경로")]
    [SerializeField]
    private string spriteFolder = "Sprites/Ingredients/";

    [Header("활성화 연출")]
    [SerializeField, Range(0.1f, 1f)]
    private float startScale = 0.72f;

    [SerializeField, Range(1f, 1.3f)]
    private float overshootScale = 1.08f;

    [SerializeField, Range(0.1f, 0.9f)]
    private float overshootPoint = 0.72f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private void Awake()
    {
        CacheComponents();
    }

    /// <summary>
    /// 슬롯의 다과 이미지를 숨긴다.
    /// 슬롯 배경은 이 오브젝트 밖에 두면 계속 표시된다.
    /// </summary>
    public void Hide()
    {
        CacheComponents();

        if (itemImage != null)
        {
            itemImage.sprite = null;
            itemImage.enabled = false;
        }

        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * startScale;
    }

    /// <summary>
    /// 다과 key에 맞는 스프라이트를 불러와 표시한다.
    /// </summary>
    public IEnumerator Show(string dagwaKey, float duration)
    {
        CacheComponents();

        if (itemImage == null)
        {
            Debug.LogWarning(
                $"[StatementDagwaSlot] {name}의 Item Image가 연결되지 않았습니다."
            );

            yield break;
        }

        string normalizedKey = string.IsNullOrWhiteSpace(dagwaKey)
            ? string.Empty
            : dagwaKey.Trim();

        string loadPath = spriteFolder + normalizedKey;
        Sprite sprite = Resources.Load<Sprite>(loadPath);

        if (sprite == null)
        {
            Debug.LogWarning(
                $"[StatementDagwaSlot] 스프라이트를 찾지 못했습니다: {loadPath}"
            );

            Hide();
            yield break;
        }

        itemImage.sprite = sprite;
        itemImage.preserveAspect = true;
        itemImage.enabled = true;

        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.one * startScale;

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / safeDuration
            );

            float eased = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            canvasGroup.alpha = eased;

            float scale;

            if (progress < overshootPoint)
            {
                float firstPart = Mathf.InverseLerp(
                    0f,
                    overshootPoint,
                    progress
                );

                scale = Mathf.Lerp(
                    startScale,
                    overshootScale,
                    Mathf.SmoothStep(0f, 1f, firstPart)
                );
            }
            else
            {
                float secondPart = Mathf.InverseLerp(
                    overshootPoint,
                    1f,
                    progress
                );

                scale = Mathf.Lerp(
                    overshootScale,
                    1f,
                    Mathf.SmoothStep(0f, 1f, secondPart)
                );
            }

            rectTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        canvasGroup.alpha = 1f;
        rectTransform.localScale = Vector3.one;
    }

    private void CacheComponents()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }
    }
}
