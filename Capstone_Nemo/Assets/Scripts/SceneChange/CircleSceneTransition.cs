using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CircleSceneTransition : MonoBehaviour
{
    public static CircleSceneTransition Instance;

    [Header("UI")]
    [SerializeField] private Canvas transitionCanvas;
    [SerializeField] private Image transitionImage;

    [Header("Circle Wipe")]
    [SerializeField] private float openRadius = 1.25f;
    [SerializeField] private float closedRadius = -0.1f;
    [SerializeField] private float closeDuration = 0.6f;
    [SerializeField] private float openDuration = 0.7f;

    [Header("Center")]
    [SerializeField] private Vector2 center = new Vector2(0.5f, 0.5f);

    private Material runtimeMaterial;
    private bool isTransitioning = false;

    public bool IsTransitioning => isTransitioning;

    public bool IsCoverVisible
    {
        get
        {
            return transitionImage != null && transitionImage.gameObject.activeSelf;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 중요: 이 스크립트가 붙은 오브젝트가 Root여야 함
        DontDestroyOnLoad(gameObject);

        if (transitionCanvas != null)
        {
            transitionCanvas.sortingOrder = 9999;
        }

        if (transitionImage != null)
        {
            runtimeMaterial = Instantiate(transitionImage.material);
            transitionImage.material = runtimeMaterial;
            transitionImage.raycastTarget = true;

            SetRadius(openRadius);
            transitionImage.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning)
            return;

        StartCoroutine(TransitionToSceneRoutine(sceneName));
    }

    public IEnumerator TransitionToSceneRoutine(string sceneName)
    {
        if (isTransitioning)
            yield break;

        isTransitioning = true;

        if (transitionImage == null || runtimeMaterial == null)
        {
            Debug.LogWarning("[CircleTransition] Image 또는 Material이 없습니다. 일반 씬 전환 실행");
            SceneManager.LoadScene(sceneName);
            isTransitioning = false;
            yield break;
        }

        Debug.Log("[CircleTransition] 닫힘 시작");

        transitionImage.gameObject.SetActive(true);
        SetRadius(openRadius);

        // 1. 방앗간 씬에서 중앙으로 닫힘
        yield return StartCoroutine(AnimateRadius(openRadius, closedRadius, closeDuration));

        Debug.Log("[CircleTransition] 씬 로드 시작: " + sceneName);

        // 2. 씬 로드
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
        {
            yield return null;
        }

        // 새 씬 초기화 대기
        yield return null;
        yield return null;

        Debug.Log("[CircleTransition] 열림 시작");

        // 혹시 씬 로드 과정에서 꺼졌거나 값이 바뀌었을 수 있으므로 다시 강제 설정
        transitionImage.gameObject.SetActive(true);
        SetRadius(closedRadius);

        // 3. 가게 씬에서 중앙 원이 바깥으로 열림
        yield return StartCoroutine(AnimateRadius(closedRadius, openRadius, openDuration));

        Debug.Log("[CircleTransition] 전환 완료");

        transitionImage.gameObject.SetActive(false);
        isTransitioning = false;
    }

    private IEnumerator AnimateRadius(float from, float to, float duration)
    {
        float t = 0f;

        SetRadius(from);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            float normalized = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);

            float radius = Mathf.Lerp(from, to, eased);
            SetRadius(radius);

            yield return null;
        }

        SetRadius(to);
    }

    private void SetRadius(float radius)
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat("_Radius", radius);
        runtimeMaterial.SetVector("_Center", new Vector4(center.x, center.y, 0f, 0f));
    }
}
