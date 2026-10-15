using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UnlockPopupManager : MonoBehaviour
{
    [Header("레벨별 안내 팝업")]
    [SerializeField] private GameObject level4Popup;
    [SerializeField] private GameObject level7Popup;

    [Header("확인 버튼")]
    [SerializeField] private Button level4ConfirmButton;
    [SerializeField] private Button level7ConfirmButton;

    [Header("팝업 배경")]
    [SerializeField] private GameObject dimBackground;

    private int currentPopupLevel = 0;
    private bool previousTimeFlow;
    private PlayerManager lockedPlayer;
    private bool previousActionLocked;

    private void Awake()
    {
        if (level4Popup != null)
            level4Popup.SetActive(false);

        if (level7Popup != null)
            level7Popup.SetActive(false);

        if (level4ConfirmButton != null)
            level4ConfirmButton.onClick.AddListener(ClosePopup);

        if (level7ConfirmButton != null)
            level7ConfirmButton.onClick.AddListener(ClosePopup);

        if (dimBackground != null)
            dimBackground.SetActive(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(CheckPopupAfterSceneLoad());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "IntroScene" ||
            scene.name == "SaveSelectScene" ||
            scene.name == "StatementScene")
            return;

        StartCoroutine(CheckPopupAfterSceneLoad());
    }

    private IEnumerator CheckPopupAfterSceneLoad()
    {
        yield return null;
        yield return null;

        if (currentPopupLevel != 0)
            yield break;

        TryShowPopup();
    }

    private void TryShowPopup()
    {
        Debug.Log(
    $"[UnlockPopup] 검사 시작 / " +
    $"Scene={SceneManager.GetActiveScene().name} / " +
    $"UnlockManager={(UnlockManager.Instance != null)}"
);

        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "IntroScene" ||
            sceneName == "SaveSelectScene" ||
            sceneName == "StatementScene")
            return;

        if (UnlockManager.Instance == null)
            return;

        if (TimeManager.Instance == null)
            return;

        Debug.Log(
    $"[UnlockPopup] " +
    $"4레벨 팝업 조건: " +
    $"{UnlockManager.Instance.HasPendingFeaturePopup(4)}"
);

        // 4레벨 팝업 우선 확인
        if (UnlockManager.Instance.HasPendingFeaturePopup(4))
        {
            ShowPopup(4);
            return;
        }

        // 7레벨 팝업 확인
        if (UnlockManager.Instance.HasPendingFeaturePopup(7))
        {
            ShowPopup(7);
        }
    }

    private void ShowPopup(int level)
    {
        GameObject popup = level == 4
            ? level4Popup
            : level7Popup;

        if (popup == null)
        {
            Debug.LogWarning(
                $"[FeaturePopup] 레벨 {level} 팝업이 연결되지 않았습니다.");
            return;
        }

        currentPopupLevel = level;

        previousTimeFlow = TimeManager.Instance.isTimeFlow;
        TimeManager.Instance.SetTimeFlow(false);

        lockedPlayer = FindObjectOfType<PlayerManager>();

        if (lockedPlayer != null)
        {
            previousActionLocked = lockedPlayer.IsActionLocked;
            lockedPlayer.SetActionLocked(true);
        }

        if (dimBackground != null)
            dimBackground.SetActive(true);

        popup.SetActive(true);

        Debug.Log($"[FeaturePopup] 레벨 {level} 안내 표시");
    }

    public void ClosePopup()
    {
        if (currentPopupLevel == 0)
            return;

        int closedLevel = currentPopupLevel;
        currentPopupLevel = 0;

        if (closedLevel == 4 && level4Popup != null)
            level4Popup.SetActive(false);

        if (closedLevel == 7 && level7Popup != null)
            level7Popup.SetActive(false);

        if (dimBackground != null)
            dimBackground.SetActive(false);

        UnlockManager.Instance?.MarkFeaturePopupShown(closedLevel);

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTimeFlow(previousTimeFlow);

        if (lockedPlayer != null)
        {
            lockedPlayer.SetActionLocked(previousActionLocked);
            lockedPlayer = null;
        }

        // 여러 기능이 같은 날 해금되었을 경우
        // 다음 팝업을 이어서 표시
        TryShowPopup();
    }

    private void OnDestroy()
    {
        if (currentPopupLevel != 0)
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SetTimeFlow(previousTimeFlow);

            if (lockedPlayer != null)
                lockedPlayer.SetActionLocked(previousActionLocked);
        }
    }
}