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

    public void Play(Sprite sprite, Vector3 worldPos)
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        GameObject icon = Instantiate(flyIconPrefab, canvas.transform);
        Image image = icon.GetComponent<Image>();
        image.sprite = sprite;

        RectTransform rt = icon.GetComponent<RectTransform>();
        rt.position = screenPos;
        rt.localScale = Vector3.one * 1.7f;

        StartCoroutine(FlyToTarget(rt));
    }

    private IEnumerator FlyToTarget(RectTransform icon)
    {
        Vector3 start = icon.position;
        Vector3 end = targetUI.position;

        float time = 0f;
        float duration = 1f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // 살짝 곡선 느낌
            float curve = Mathf.Sin(t * Mathf.PI);
            Vector3 mid = Vector3.Lerp(start, end, t) + new Vector3(0, curve * 50f, 0);
            icon.position = mid;

            icon.localScale = Vector3.Lerp(Vector3.one * 1.7f, Vector3.one * 0.85f, t);

            yield return null;
        }

        Destroy(icon.gameObject);
    }
}
