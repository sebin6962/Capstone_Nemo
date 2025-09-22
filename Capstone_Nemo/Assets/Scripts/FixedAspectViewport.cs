using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FixedAspectViewport : MonoBehaviour
{
    [Tooltip("원하는 기준 비율 (예: 16:9 -> 16,9 / 4:3 -> 4,3)")]
    public Vector2 referenceAspect = new Vector2(16, 9);

    [Tooltip("레터박스/필러박스 색상")]
    public Color barColor = Color.black;

    Camera cam;
    int lastW, lastH;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; // 남는 부분을 단색으로
        cam.backgroundColor = barColor;
        ApplyViewport();
    }

    void Update()
    {
        // 해상도 변화 감지 시 재계산
        if (Screen.width != lastW || Screen.height != lastH)
            ApplyViewport();
    }

    void ApplyViewport()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        float target = referenceAspect.x / referenceAspect.y; // 원하는 비율
        float window = (float)Screen.width / Screen.height;

        // window/target < 1 -> 가로가 더 좁음(레터박스: 위아래 바)
        if (window < target)
        {
            float height = window / target;   // 0~1
            cam.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
        else // 필러박스: 좌우 바
        {
            float width = target / window;    // 0~1
            cam.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }

        cam.backgroundColor = barColor; // 색 재보증
    }
}

