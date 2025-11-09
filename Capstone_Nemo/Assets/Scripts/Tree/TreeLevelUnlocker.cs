using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.IO;

public class TreeLevelUnlocker : MonoBehaviour
{
    public static TreeLevelUnlocker Instance;

    public static int CurrentLevel = 0;

    public Button[] levelButtons;
    public TMP_Text[] levelDescTexts;
    public int[] starlightNeededForLevel;
    public string[] levelDescriptions;
    //public Color unlockedColor;
    //public Color lockedColor;

    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    private int currentUnlockedLevel = 0;

    public GameObject notEnoughStarlightPanel;
    public CanvasGroup notEnoughStarlightGroup;

    public TMP_Text currentStateText;

    public GameObject unlockEffectPanel;
    public TMP_Text levelText;
    public TMP_Text effectText;
    public string[] unlockEffectDescriptions;

    private Coroutine notEnoughCoroutine = null;

    private TreeUnlockData unlockData;
    private string savePath;

    [Header("나무 해금 패널 UI")]
    public Image unlockPopupPanelImage;          // 나무 해금 팝업(전체 패널)의 Image
    public Sprite lockedPanelSprite;             // 레벨 0(잠김)용
    public Sprite[] levelUnlockedPanelSprites;   // 레벨 1..N 해금용 (index = level-1)

    //해금 이펙트
    public GameObject unlockEffectPrefab;

    [Header("해금 7단계 맵 스프라이트 교체")]
    public SpriteRenderer mapSpriteRenderer;   // 교체 대상 (씬 배경 SpriteRenderer)
    public Sprite[] mapSpritesByLevel;

    void Awake()
    {
        Instance = this;

        // 서버 선택값으로 경로 보장
        var serverName = PlayerPrefs.GetString("SelectedSave", string.Empty);
        if (!string.IsNullOrEmpty(serverName))
        {
            SetServerName(serverName);
        }
        else
        {
            Debug.LogWarning("[TreeLevelUnlocker] SelectedSave is empty. SetServerName() later before saving.");
        }
    }

    public void SetServerName(string serverName)
    {
        savePath = Path.Combine(Application.persistentDataPath, $"treeUnlock_{serverName}.json");
    }

    void Start()
    {
        // 1) 데이터 로드(경로가 없으면 메모리만)
        LoadUnlockData();

        // 2) 동기화
        currentUnlockedLevel = unlockData != null ? unlockData.currentUnlockedLevel : 0;
        CurrentLevel = currentUnlockedLevel;

        // 3) 버튼/텍스트 갱신
        UpdateLevelButtons();
        // 패널 스프라이트 동기화
        ApplyPanelSprite();
        // 맵 스프라이트 동기화
        ApplyMapSprite();

        // 4) 버튼들에 툴팁 트리거 연결
        if (levelButtons != null)
        {
            for (int i = 0; i < levelButtons.Length; i++)
            {
                int idx = i;
                EventTrigger trigger = levelButtons[i].gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = levelButtons[i].gameObject.AddComponent<EventTrigger>();

                var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entryEnter.callback.AddListener((_) => ShowTooltip(idx));
                trigger.triggers.Add(entryEnter);

                var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                entryExit.callback.AddListener((_) => HideTooltip());
                trigger.triggers.Add(entryExit);
            }
        }
    }

    public void ApplyPanelSprite()
    {
        if (unlockPopupPanelImage == null) return;

        if (currentUnlockedLevel <= 0)
        {
            if (lockedPanelSprite != null)
                unlockPopupPanelImage.sprite = lockedPanelSprite;
        }
        else
        {
            int idx = Mathf.Clamp(currentUnlockedLevel - 1, 0, levelUnlockedPanelSprites.Length - 1);
            if (levelUnlockedPanelSprites != null && levelUnlockedPanelSprites.Length > 0)
                unlockPopupPanelImage.sprite = levelUnlockedPanelSprites[idx];
        }

        // 필요 시 원본 크기 반영
        // unlockPopupPanelImage.SetNativeSize();
    }

    public void ApplyMapSprite()
    {
        if (mapSpriteRenderer == null)
        {
            Debug.LogWarning("[TreeLevelUnlocker] mapSpriteRenderer가 연결되지 않았습니다.");
            return;
        }

        // 기본 잠금 상태: index = -1
        if (currentUnlockedLevel <= 0)
        {
            // mapSpritesByLevel[0] 이전의 "어두운 기본 맵"이 필요하면 따로 Sprite 지정 가능
            return;
        }

        int idx = Mathf.Clamp(currentUnlockedLevel - 1, 0, mapSpritesByLevel.Length - 1);
        if (mapSpritesByLevel != null && mapSpritesByLevel.Length > 0 && mapSpritesByLevel[idx] != null)
        {
            mapSpriteRenderer.sprite = mapSpritesByLevel[idx];
            Debug.Log($"[TreeLevelUnlocker] 맵 스프라이트 {idx + 1}레벨 버전으로 교체됨.");
        }
    }

    public void ShowTooltip(int levelIdx)
    {
        bool unlocked = levelIdx < currentUnlockedLevel;
        if (unlocked)
        {
            tooltipPanel.SetActive(false);
            return;
        }

        tooltipPanel.SetActive(true);
        tooltipText.text = $"{starlightNeededForLevel[levelIdx]} 개의 별빛";

        RectTransform buttonRect = levelButtons[levelIdx].GetComponent<RectTransform>();
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, buttonRect.position);

        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tooltipRect.parent as RectTransform,
            screenPos,
            null,
            out localPoint
        );
        tooltipRect.anchoredPosition = localPoint + new Vector2(0, 80f);
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    public void TryUnlockLevel(int levelIdx)
    {
        if (levelIdx > currentUnlockedLevel) return;

        // StarDataManager가 서버 기반으로 로드되어 있어야 함
        if (StarDataManager.Instance == null)
        {
            Debug.LogError("[TreeLevelUnlocker] StarDataManager.Instance is null.");
            return;
        }

        int needStarlight = starlightNeededForLevel[levelIdx];
        int currentStarlight = StarDataManager.Instance.playerData.starlight;

        if (currentStarlight < needStarlight)
        {
            ShowNotEnoughStarlight();
            return;
        }

        StarDataManager.Instance.SpendStarlight(needStarlight);

        if (SFXManager.Instance) SFXManager.Instance.PlayTreeOpenSFX();

        currentUnlockedLevel = Mathf.Max(currentUnlockedLevel, levelIdx + 1);
        if (unlockData == null) unlockData = new TreeUnlockData();
        unlockData.currentUnlockedLevel = currentUnlockedLevel;
        CurrentLevel = currentUnlockedLevel;
        SaveUnlockData();

        UpdateLevelButtons();
        ApplyPanelSprite();

        ApplyMapSprite();

        PlayUnlockEffect(levelIdx);

        //중간발표 대비 비활성화
        //ShowUnlockEffectPanel(currentUnlockedLevel);

        if (levelIdx == levelButtons.Length - 1)
        {
            // FadeManager 싱글톤이 살아 있다면 페이드 전환
            if (FadeManager.Instance != null)
            {
                FadeManager.Instance.FadeToScene("EndingScene");
            }
        }
    }

    public void SaveUnlockData()
    {
        if (unlockData == null) unlockData = new TreeUnlockData();

        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("[TreeLevelUnlocker] savePath is null/empty. Call SetServerName() first.");
            return;
        }

        string json = JsonUtility.ToJson(unlockData, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadUnlockData()
    {
        // unlockData 객체는 최소한 생성
        if (unlockData == null) unlockData = new TreeUnlockData();

        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogWarning("[TreeLevelUnlocker] savePath is null/empty. Load will use memory-only defaults.");
            return;
        }

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            var loaded = JsonUtility.FromJson<TreeUnlockData>(json);
            if (loaded != null) unlockData = loaded;
        }
        else
        {
            // 파일이 없으면 현재 메모리 상태(기본 0)로 저장해서 생성
            SaveUnlockData();
        }
    }

    void UpdateLevelButtons()
    {
        if (levelButtons == null || levelDescTexts == null || levelDescriptions == null) return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool unlocked = i < currentUnlockedLevel;
            bool canUnlock = i == currentUnlockedLevel;
            //var colors = levelButtons[i].colors;
            levelButtons[i].interactable = canUnlock;
            //var img = levelButtons[i].GetComponent<Image>();
            //if (img != null) img.color = unlocked ? unlockedColor : lockedColor;

            if (unlocked)
                levelDescTexts[i].text = levelDescriptions[i];
            else
                levelDescTexts[i].text = "???";
        }

        if (currentStateText != null)
        {
            if (currentUnlockedLevel > 0)
                currentStateText.text = $"현재 상태: {levelDescriptions[currentUnlockedLevel - 1]}";
            else
                currentStateText.text = "현재 상태: 시들어 있는 계수나무";
        }
    }

    public void ShowNotEnoughStarlight()
    {
        if (notEnoughCoroutine != null)
            StopCoroutine(notEnoughCoroutine);

        notEnoughCoroutine = StartCoroutine(NotEnoughRoutine());
    }

    private IEnumerator NotEnoughRoutine()
    {
        notEnoughStarlightPanel.SetActive(true);

        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            notEnoughStarlightGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        notEnoughStarlightGroup.alpha = 1f;

        yield return new WaitForSeconds(1f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            notEnoughStarlightGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            yield return null;
        }
        notEnoughStarlightGroup.alpha = 0f;

        notEnoughStarlightPanel.SetActive(false);
        notEnoughCoroutine = null;
    }

    void ShowUnlockEffectPanel(int level)
    {
        int idx = level - 1;
        if (idx < 0 || unlockEffectDescriptions == null || idx >= unlockEffectDescriptions.Length) return;

        unlockEffectPanel.SetActive(true);
        levelText.text = $"{level}단계 해금";
        effectText.text = unlockEffectDescriptions[idx];
    }

    public void ClosePanel()
    {
        if (SFXManager.Instance) SFXManager.Instance.PlayBtnClickSFX();
        unlockEffectPanel.SetActive(false);
    }

    public void SetCurrentUnlockedLevel(int level)
    {
        currentUnlockedLevel = Mathf.Max(0, level);
        if (unlockData == null) unlockData = new TreeUnlockData();
        unlockData.currentUnlockedLevel = currentUnlockedLevel;
        CurrentLevel = currentUnlockedLevel;
        SaveUnlockData();
        UpdateLevelButtons();
        ApplyPanelSprite();
        ApplyMapSprite();
    }

    //해금 이펙트
    private void PlayUnlockEffect(int levelIdx)
    {
        if (unlockEffectPrefab == null) return;
        if (levelButtons == null || levelIdx < 0 || levelIdx >= levelButtons.Length) return;

        var btn = levelButtons[levelIdx];
        if (btn == null) return;

        RectTransform btnRect = btn.GetComponent<RectTransform>();
        if (btnRect == null) return;

        //버튼 자식
        GameObject fx = Instantiate(unlockEffectPrefab, btnRect);
        var fxRect = fx.GetComponent<RectTransform>();
        if (fxRect != null)
        {
            fxRect.localScale = Vector3.one;
        }

        //클릭 방해
        var cg = fx.GetComponent<CanvasGroup>();
        if (cg == null) cg = fx.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        Destroy(fx, 5f);
    }
}

