// ShootingStar.cs
using UnityEngine;
using UnityEngine.UI;

public class ShootingStar : MonoBehaviour
{
    public RectTransform rect;   // 자기 RectTransform (자동 할당)
    public float speedPxPerSec;  // 이동 속도(픽셀/초): x, y에 방향 곱해서 사용
    public Vector2 dirNorm;      // 정규화된 방향 (예: 왼쪽아래 = (-1, -0.5) 정규화)
    public System.Action<ShootingStar> OnDespawn; // 화면 밖에서 스폰너로 반환

    RectTransform canvasRT;
    float halfWidth, halfHeight;

    public Image image;

    void Awake()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (image == null) image = GetComponent<Image>();
        var canvas = GetComponentInParent<Canvas>();
        canvasRT = canvas != null ? canvas.GetComponent<RectTransform>() : null;
    }

    public void SetSprite(Sprite s)
    {
        if (image != null && s != null) image.sprite = s;
    }

    public void Init(Vector2 startAnchoredPos, Vector2 dirNormalized, float speedPx)
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        rect.anchoredPosition = startAnchoredPos;
        dirNorm = dirNormalized.normalized;
        speedPxPerSec = speedPx;

        // 현재 별똥별의 크기 절반 (화면 밖 판정 여유)
        halfWidth = rect.rect.width * 0.5f;
        halfHeight = rect.rect.height * 0.5f;
        gameObject.SetActive(true);
    }

    void Update()
    {
        // 이동
        Vector2 p = rect.anchoredPosition;
        p += dirNorm * speedPxPerSec * Time.deltaTime;
        rect.anchoredPosition = p;

        // 화면 밖 판정 (캔버스 기준)
        if (canvasRT == null) return;
        float halfCanvasW = canvasRT.rect.width * 0.5f;
        float halfCanvasH = canvasRT.rect.height * 0.5f;

        bool outLeft = p.x < -halfCanvasW - halfWidth;
        bool outRight = p.x > halfCanvasW + halfWidth;
        bool outDown = p.y < -halfCanvasH - halfHeight;
        bool outUp = p.y > halfCanvasH + halfHeight;

        if (outLeft || outRight || outDown || outUp)
        {
            // 풀로 반환 (비활성화는 스폰너가 맡음)
            OnDespawn?.Invoke(this);
        }
    }
}
