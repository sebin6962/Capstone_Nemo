using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using System.Collections;

public class StorageIconFlyEffect : MonoBehaviour
{
    public static StorageIconFlyEffect Instance;

    public RectTransform targetUI;            // 날아갈 UI 타겟 (예: 창고 버튼)
    public Canvas canvas;                     // UI Canvas
    public GameObject flyIconPrefab;          // 날아가는 스프라이트 프리팹

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void Play(Sprite sprite, Vector3 worldPos, Camera sourceCamera = null)
    {
        if (!canvas || !targetUI || !flyIconPrefab) return;
        if (sourceCamera == null) sourceCamera = Camera.main;

        // 1) 아이콘을 '도착지와 같은 Canvas' 아래에 생성
        var go = Instantiate(flyIconPrefab, canvas.transform);
        var icon = go.GetComponent<RectTransform>();
        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img) img.sprite = sprite;

        icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
        icon.localScale = Vector3.one;

        // 2) 시작점: 월드 -> 스크린 -> Canvas 로컬(anchoredPosition)
        Vector2 screenStart = RectTransformUtility.WorldToScreenPoint(sourceCamera, worldPos);
        Vector2 startAP;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            screenStart,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out startAP
        );
        icon.anchoredPosition = startAP;

        // 3) 도착점: targetUI의 '월드' -> 스크린 -> Canvas 로컬
        Vector2 screenEnd = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            targetUI.position
        );
        Vector2 endAP;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            screenEnd,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out endAP
        );

        // 4) 트윈/코루틴으로 anchoredPosition을 startAP -> endAP로 이동
        StartCoroutine(Fly(icon, startAP, endAP));
    }

    IEnumerator Fly(RectTransform icon, Vector2 a, Vector2 b)
    {
        float t = 0f, dur = 0.8f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float s = Mathf.SmoothStep(0f, 1f, t);
            // 살짝 위로 휘는 포물선
            float hump = Mathf.Sin(s * Mathf.PI) * 50f;
            icon.anchoredPosition = Vector2.Lerp(a, b, s) + new Vector2(0, hump);
            yield return null;
        }
        Destroy(icon.gameObject);
    }

}
