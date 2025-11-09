using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingCutSceneScroller : MonoBehaviour
{
    [Header("스크롤할 카메라 (비워두면 Main Camera 사용)")]
    public Camera targetCamera;

    [Header("컷신 하단 기준점 (배경 맨 아래 위치에 빈 오브젝트 하나 두고 연결)")]
    public Transform bottomLimit;

    [Header("카메라 내려가는 속도")]
    public float scrollSpeed = 1.0f;

    [Header("하단에 도착 후 잠깐 멈추는 시간")]
    public float beforeFadeBlackDelay = 2f;

    [Header("엔딩용 검은 화면 이미지 (UI Image)")]
    public Image blackImage;

    [Tooltip("검은 화면이 완전히 차오르는 시간(초)")]
    public float blackFadeSeconds = 1.5f;

    private bool isEnding = false;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        // 시작할 때 검은 이미지 알파를 0으로 맞춰 둔다.
        if (blackImage != null)
        {
            var c = blackImage.color;
            c.a = 0f;
            blackImage.color = c;
        }
    }

    void Update()
    {
        if (isEnding) return;
        if (targetCamera == null || bottomLimit == null) return;

        // 카메라 하단 y = 카메라 위치 - orthographicSize
        float camBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
        float cutBottom = bottomLimit.position.y;

        // 아직 컷 하단보다 위에 있으면 아래로 이동
        if (camBottom > cutBottom)
        {
            float move = scrollSpeed * Time.deltaTime;
            targetCamera.transform.position -= new Vector3(0f, move, 0f);
            camBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
        }

        // 하단에 닿거나 지나가면 검은 화면 페이드인 시작
        if (camBottom <= cutBottom)
        {
            isEnding = true;
            StartCoroutine(FadeToBlackRoutine());
        }
    }

    private IEnumerator FadeToBlackRoutine()
    {
        // 도착 후 잠깐 멈추기
        if (beforeFadeBlackDelay > 0f)
            yield return new WaitForSeconds(beforeFadeBlackDelay);

        // 검은 이미지 페이드
        if (blackImage != null)
        {
            if (!blackImage.gameObject.activeSelf)
                blackImage.gameObject.SetActive(true);

            yield return StartCoroutine(FadeBlackImage(0f, 1f, blackFadeSeconds));
        }

        // 이 이후
        // 자막 매니저 실행
        // 크레딧 텍스트 타이핑
        // 버튼 활성화 등
    }

    private IEnumerator FadeBlackImage(float from, float to, float duration)
    {
        float t = 0f;
        Color color = blackImage.color;
        color.a = from;
        blackImage.color = color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            color.a = a;
            blackImage.color = color;
            yield return null;
        }

        color.a = to;
        blackImage.color = color;
    }
}

