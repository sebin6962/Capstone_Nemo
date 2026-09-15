using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;

public class NewGamePopupManager : MonoBehaviour
{
    [System.Serializable]
    private class DefaultSkinSaveData
    {
        public int equippedIndex = 0;
        public int[] ownedIndexes = new int[] { 0 };

        public bool hasCustomColor = false;
        public float colorR = 1f;
        public float colorG = 1f;
        public float colorB = 1f;
        public float colorA = 1f;
    }

    [Header("이름 입력")]
    public TMP_InputField inputServerName;   // 마을 이름 + 세이브 파일 구분용 이름
    public TMP_InputField inputPlayerName;   // 캐릭터 이름

    [SerializeField] private int maxServerNameLength = 5;
    [SerializeField] private int maxPlayerNameLength = 5;

    public Button btnCreate;
    public Button btnCancel;

    [Header("이름 길이 초과 시 팝업 패널")]
    public GameObject nameTooLongPanel;
    public CanvasGroup nameTooLongGroup;

    private Coroutine nameTooLongCo;
    private bool suppressWarnings;

    private RectTransform nameTooLongRect;
    private Vector2 nameTooLongShownPosition;

    void OnEnable()
    {
        suppressWarnings = false;

        ResetWarningPanel();

        ConfigureInputField(inputServerName);
        ConfigureInputField(inputPlayerName);

        if (inputServerName != null)
        {
            inputServerName.onValueChanged.RemoveListener(OnServerNameChanged);
            inputServerName.onValueChanged.AddListener(OnServerNameChanged);
        }

        if (inputPlayerName != null)
        {
            inputPlayerName.onValueChanged.RemoveListener(OnPlayerNameChanged);
            inputPlayerName.onValueChanged.AddListener(OnPlayerNameChanged);
        }
    }

    void Start()
    {
        if (btnCancel != null)
            btnCancel.onClick.AddListener(CancelCreate);

        if (btnCreate != null)
            btnCreate.onClick.AddListener(CreateNewGame);
    }

    void OnDisable()
    {
        if (inputServerName != null)
            inputServerName.onValueChanged.RemoveListener(OnServerNameChanged);

        if (inputPlayerName != null)
            inputPlayerName.onValueChanged.RemoveListener(OnPlayerNameChanged);

        StopWarningRoutine();
        ResetWarningPanel();

        suppressWarnings = false;
    }

    private void ConfigureInputField(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        inputField.characterLimit = 0;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.characterValidation = TMP_InputField.CharacterValidation.None;
        inputField.inputType = TMP_InputField.InputType.Standard;
        inputField.onValidateInput = null;

        ResetInputField(inputField);
    }

    private void ResetInputField(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        inputField.DeactivateInputField();
        inputField.SetTextWithoutNotify("");
        inputField.caretPosition = 0;
        inputField.selectionStringAnchorPosition = 0;
        inputField.selectionStringFocusPosition = 0;
    }

    private void OnServerNameChanged(string text)
    {
        LimitInputLength(inputServerName, text, maxServerNameLength);
    }

    private void OnPlayerNameChanged(string text)
    {
        LimitInputLength(inputPlayerName, text, maxPlayerNameLength);
    }

    private void LimitInputLength(TMP_InputField inputField, string text, int maxLength)
    {
        if (inputField == null)
            return;

        maxLength = Mathf.Max(1, maxLength);

        // 현재 포커스된 입력창에서 한글이 조합 중인 경우
        string composition = inputField.isFocused ? Input.compositionString : "";

        if (!string.IsNullOrEmpty(composition))
        {
            int committedLength = Mathf.Max(0, text.Length - composition.Length);

            // 이미 확정된 글자가 최대 길이에 도달했다면 추가 조합을 제거
            if (committedLength >= maxLength && text.Length > maxLength)
            {
                string clipped = text.Substring(0, maxLength);
                inputField.SetTextWithoutNotify(clipped);
                inputField.caretPosition = clipped.Length;
                ShowNameTooLong();
            }

            return;
        }

        // 영문, 숫자 또는 한글 조합이 끝난 뒤 최대 길이 초과 처리
        if (text.Length > maxLength)
        {
            string clipped = text.Substring(0, maxLength);
            inputField.SetTextWithoutNotify(clipped);
            inputField.caretPosition = clipped.Length;
            ShowNameTooLong();
        }
    }

    private void CancelCreate()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayBtnClickSFX();

        suppressWarnings = true;

        StopWarningRoutine();
        ResetWarningPanel();

        ResetInputField(inputServerName);
        ResetInputField(inputPlayerName);

        gameObject.SetActive(false);
    }

    private void CreateNewGame()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayBtnClickSFX();

        if (inputServerName == null || inputPlayerName == null)
        {
            Debug.LogError("[NewGamePopupManager] 마을 이름 또는 캐릭터 이름 InputField가 연결되지 않았습니다.");
            return;
        }

        string serverName = inputServerName.text.Trim();
        string playerName = inputPlayerName.text.Trim();

        // 두 이름을 모두 입력해야 생성 가능
        if (string.IsNullOrEmpty(serverName) || string.IsNullOrEmpty(playerName))
            return;

        int serverNameLimit = Mathf.Max(1, maxServerNameLength);
        int playerNameLimit = Mathf.Max(1, maxPlayerNameLength);

        if (serverName.Length > serverNameLimit)
        {
            inputServerName.SetTextWithoutNotify(serverName.Substring(0, serverNameLimit));
            inputServerName.caretPosition = inputServerName.text.Length;
            ShowNameTooLong();
            return;
        }

        if (playerName.Length > playerNameLimit)
        {
            inputPlayerName.SetTextWithoutNotify(playerName.Substring(0, playerNameLimit));
            inputPlayerName.caretPosition = inputPlayerName.text.Length;
            ShowNameTooLong();
            return;
        }

        // 마을 이름은 파일명으로 사용되므로 중복 생성 금지
        if (ProfileRepository.ContainsSave(serverName) ||
            SaveRepository.Exists(serverName))
        {
            return;
        }

        // 새 게임의 통합 세이브 데이터 생성
        SaveData newSaveData = new SaveData
        {
            serverName = serverName,
            playerName = playerName,

            starData = new StarSaveData
            {
                starlight = 0
            },

            // 새 세이브는 기존 별빛 파일을 옮길 필요가 없음
            starDataMigrationCompleted = true,

            levelData = new LevelSaveData
            {
                level = 1,
                exp = 0
            },
            levelDataMigrationCompleted = true,

            worldTimeData = new WorldTimeSaveData
            {
                day = 1,
                hour = 9,
                minute = 0
            },
            worldTimeMigrationCompleted = true,

            playtimeData = new PlaytimeSaveData
            {
                seconds = 0,
                lastPlayed = ""
            },
            playtimeMigrationCompleted = true,

            tutorialData = new TutorialStateData
            {
                tutorialDone = false
            },
            tutorialMigrationCompleted = true,

            treeUnlockData = new TreeUnlockData
            {
                currentUnlockedLevel = 0
            },
            treeUnlockMigrationCompleted = true,

            unlockProgressData =
    new UnlockProgressSaveData
    {
        pendingLevels = new List<int>(),
        appliedLevels = new List<int> { 1 },
        initialized = true
    },
            unlockProgressMigrationCompleted = true,

            endingData = new EndingData
            {
                hasSeenEnding = false
            },
            endingMigrationCompleted = true,

            npcDialogueProgressData =
                new NPCDialogueProgressDataList(),
            npcDialogueProgressMigrationCompleted = true,

            storageMigrationCompleted = true,

            makerData = new MakerSaveData(),
            makerMigrationCompleted = true,

            tableData = new TableSaveData(),
            tableMigrationCompleted = true,

            farmData = new FarmSaveData(),
            farmMigrationCompleted = true,

            playerLocationData = new PlayerLocationSaveData
            {
                initialized = false
            },
            playerLocationMigrationCompleted = true
        };

        // 통합 JSON 저장은 SaveRepository가 담당
        if (!SaveRepository.Save(serverName, newSaveData))
            return;

        if (!ProfileRepository.TryAddSave(serverName, DateTime.Now))
        {
            SaveRepository.Delete(serverName);
            return;
        }

        SaveService.SetCurrent(serverName, newSaveData);

        var defaultSkinData = new DefaultSkinSaveData();

        File.WriteAllText(
            Path.Combine(Application.persistentDataPath, $"playerSkin_{serverName}.json"),
            JsonUtility.ToJson(defaultSkinData, true)
        );

        PlayerPrefs.SetString("SelectedSave", serverName);
        PlayerPrefs.Save();

        // 새 파일 생성 직후 스킨 데이터를 새 세이브 기준으로 전환
        if (PlayerSkinManager.Instance != null)
            PlayerSkinManager.Instance.SwitchToSave(serverName);

        // 별빛 데이터도 새 세이브 기준으로 다시 로드
        if (StarDataManager.Instance != null)
            StarDataManager.Instance.InitFromSelectedSave();

        TutorialFlowManager.ForceResetInstance();

        if (UnlockManager.Instance != null)
            UnlockManager.Instance.SwitchToServer(serverName);

        FadeManager.Instance.FadeToScene("CutScene");
    }

    private void ShowNameTooLong()
    {
        if (suppressWarnings || !isActiveAndEnabled)
            return;

        if (nameTooLongCo != null)
            StopCoroutine(nameTooLongCo);

        nameTooLongCo = StartCoroutine(NameTooLongRoutine());
    }

    private void StopWarningRoutine()
    {
        if (nameTooLongCo == null)
            return;

        StopCoroutine(nameTooLongCo);
        nameTooLongCo = null;
    }

    private void ResetWarningPanel()
    {
        if (nameTooLongGroup != null)
            nameTooLongGroup.alpha = 1f;

        if (nameTooLongPanel != null)
        {
            if (nameTooLongRect == null)
            {
                nameTooLongRect =
                    nameTooLongPanel.GetComponent<RectTransform>();

                if (nameTooLongRect != null)
                {
                    nameTooLongShownPosition =
                        nameTooLongRect.anchoredPosition;
                }
            }

            if (nameTooLongRect != null)
            {
                nameTooLongRect.anchoredPosition =
                    nameTooLongShownPosition;
            }

            nameTooLongPanel.SetActive(false);
        }
    }

    private IEnumerator ShakeWarningPanel(RectTransform target)
    {
        if (target == null)
            yield break;

        Vector2 originalPos =
            target.anchoredPosition;

        float duration = 0.22f;
        float strength = 8f;
        float frequency = 32f;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float normalized =
                Mathf.Clamp01(
                    time / duration
                );

            float damping =
                1f - normalized;

            float offsetX =
                Mathf.Sin(
                    time * frequency
                ) *
                strength *
                damping;

            target.anchoredPosition =
                originalPos +
                new Vector2(
                    offsetX,
                    0f
                );

            yield return null;
        }

        target.anchoredPosition =
            originalPos;
    }

    private IEnumerator NameTooLongRoutine()
    {
        if (nameTooLongPanel == null ||
            nameTooLongGroup == null)
        {
            yield break;
        }

        if (nameTooLongRect == null)
        {
            nameTooLongRect =
                nameTooLongPanel.GetComponent<RectTransform>();

            if (nameTooLongRect == null)
                yield break;

            nameTooLongShownPosition =
                nameTooLongRect.anchoredPosition;
        }

        nameTooLongGroup.alpha = 1f;

        nameTooLongPanel.SetActive(true);

        float slideDuration = 0.5f;

        float elapsed = 0f;

        float parentWidth =
            Screen.width;

        if (nameTooLongRect.parent
            is RectTransform parentRect)
        {
            parentWidth =
                parentRect.rect.width;
        }

        Vector2 hiddenPosition =
            nameTooLongShownPosition +
            Vector2.left *
            (
                parentWidth +
                nameTooLongRect.rect.width
            );

        // 화면 왼쪽 밖에서 시작
        nameTooLongRect.anchoredPosition =
            hiddenPosition;

        // -------------------------
        // 슬라이드 인
        // -------------------------
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed /
                        slideDuration
                    )
                );

            nameTooLongRect.anchoredPosition =
                Vector2.Lerp(
                    hiddenPosition,
                    nameTooLongShownPosition,
                    progress
                );

            yield return null;
        }

        nameTooLongRect.anchoredPosition =
            nameTooLongShownPosition;

        // -------------------------
        // 도착 후 좌우 흔들림
        // -------------------------
        yield return StartCoroutine(
            ShakeWarningPanel(
                nameTooLongRect
            )
        );

        // -------------------------
        // 유지 시간
        // -------------------------
        yield return new WaitForSeconds(3f);

        // -------------------------
        // 슬라이드 아웃
        // -------------------------
        elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(
                        elapsed /
                        slideDuration
                    )
                );

            nameTooLongRect.anchoredPosition =
                Vector2.Lerp(
                    nameTooLongShownPosition,
                    hiddenPosition,
                    progress
                );

            yield return null;
        }

        // 다음에 다시 사용할 수 있도록
        // 원래 위치로 돌려놓고 끄기
        nameTooLongRect.anchoredPosition =
            nameTooLongShownPosition;

        nameTooLongPanel.SetActive(false);

        nameTooLongCo = null;
    }
}
