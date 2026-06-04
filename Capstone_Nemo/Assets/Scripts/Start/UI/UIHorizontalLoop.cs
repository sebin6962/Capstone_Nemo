using UnityEngine;
using UnityEngine.UI;

public class UIHorizontalLoop : MonoBehaviour
{
    [SerializeField] RectTransform tileA; // 왼쪽
    [SerializeField] RectTransform tileB; // 오른쪽
    [SerializeField] float pxPerSec = 120f; // 왼쪽(-) 이동
    [SerializeField] bool moveLeft = true;

    float tileWidth;  // sizeDelta.x를 사용(캔버스 스케일 영향 X)
    float leftEdgeX;  // 시작 기준선(고정)
    float rightEdgeX;

    void Awake()
    {
        if (!tileA || !tileB)
        {
            tileA = transform.GetChild(0) as RectTransform;
            tileB = transform.GetChild(1) as RectTransform;
        }
        // 스프라이트/알파 체크
        ValidateImage(tileA);
        ValidateImage(tileB);

        // 타일 폭은 sizeDelta.x 로 고정 취득 (Layout/스케일 영향 최소화)
        tileWidth = tileA.sizeDelta.x;
        if (tileWidth <= 0.1f)
        {
            // sizeDelta가 0이면 SetNativeSize가 안 된 경우 → 강제 보정
            var img = tileA.GetComponent<Image>();
            if (img && img.sprite) tileWidth = img.sprite.rect.width;
        }

        // 초기 배치: A 왼쪽, B는 바로 오른쪽
        var pA = tileA.anchoredPosition;
        tileA.anchoredPosition = new Vector2(0f, pA.y);
        tileB.anchoredPosition = new Vector2(tileWidth, pA.y);

        // 고정 기준선(이 값은 Update 내내 변하지 않음)
        leftEdgeX = 0f;
        rightEdgeX = tileWidth;
    }

    void ValidateImage(RectTransform rt)
    {
        var img = rt.GetComponent<Image>();
        if (!img || img.sprite == null) Debug.LogError($"{rt.name}: Image/Sprite가 필요합니다.");
        if (img && img.color.a < 0.99f) img.color = Color.white;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    void Update()
    {
        float dir = moveLeft ? -1f : 1f;
        float dx = pxPerSec * dir * Time.deltaTime;

        tileA.anchoredPosition += new Vector2(dx, 0f);
        tileB.anchoredPosition += new Vector2(dx, 0f);

        // wrap: 기준선을 '절대값'으로 사용 (기준이 움직이지 않도록)
        if (moveLeft)
        {
            if (tileA.anchoredPosition.x <= leftEdgeX - tileWidth)
            {
                tileA.anchoredPosition = new Vector2(MaxX(tileA, tileB) + tileWidth, tileA.anchoredPosition.y);
            }
            if (tileB.anchoredPosition.x <= leftEdgeX - tileWidth)
            {
                tileB.anchoredPosition = new Vector2(MaxX(tileA, tileB) + tileWidth, tileB.anchoredPosition.y);
            }
        }
        else
        {
            if (tileA.anchoredPosition.x >= rightEdgeX + tileWidth)
            {
                tileA.anchoredPosition = new Vector2(MinX(tileA, tileB) - tileWidth, tileA.anchoredPosition.y);
            }
            if (tileB.anchoredPosition.x >= rightEdgeX + tileWidth)
            {
                tileB.anchoredPosition = new Vector2(MinX(tileA, tileB) - tileWidth, tileB.anchoredPosition.y);
            }
        }
    }

    float MaxX(RectTransform a, RectTransform b) => Mathf.Max(a.anchoredPosition.x, b.anchoredPosition.x);
    float MinX(RectTransform a, RectTransform b) => Mathf.Min(a.anchoredPosition.x, b.anchoredPosition.x);
}


