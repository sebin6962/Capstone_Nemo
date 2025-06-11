using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.IO;

public class TreeLevelUnlocker : MonoBehaviour
{
    public static int CurrentLevel = 0; // 어디서든 접근 가능하도록 선언

    public Button[] levelButtons;                // 0: 첫 레벨
    public TMP_Text[] levelDescTexts;            // 0: 첫 레벨 설명 텍스트
    public int[] starlightNeededForLevel;        // 0: 첫 레벨에 필요한 별빛
    public string[] levelDescriptions;           // 0: 첫 레벨 해금시 보여줄 텍스트
    public Color unlockedColor;
    public Color lockedColor;

    // 툴팁 관련 필드 추가
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    private int currentUnlockedLevel = 0; // 0=첫번째 레벨만 해금

    public GameObject notEnoughStarlightPanel; // 별빛 부족 알림 패널
    public CanvasGroup notEnoughStarlightGroup; // 알림 패널의 CanvasGroup

    public TMP_Text currentStateText; // 인스펙터에 현재 상태 텍스트 연결

    public GameObject unlockEffectPanel;  // 씬의 패널 오브젝트
    public TMP_Text levelText;            // 레벨 텍스트
    public TMP_Text effectText;           // 효과 텍스트
    public string[] unlockEffectDescriptions;


    private Coroutine notEnoughCoroutine = null;

    private TreeUnlockData unlockData;
    private string savePath;

    void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "treeUnlock.json");
        LoadUnlockData();
    }

    void Start()
    {
        UpdateLevelButtons();

        // (Start에서 currentUnlockedLevel을 unlockData.currentUnlockedLevel로 대입)
        currentUnlockedLevel = unlockData.currentUnlockedLevel;

        // 모든 버튼에 이벤트 리스너 추가
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i; // 지역 변수로 캡처
            EventTrigger trigger = levelButtons[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = levelButtons[i].gameObject.AddComponent<EventTrigger>();

            // 마우스 오버
            EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener((_) => ShowTooltip(idx));
            trigger.triggers.Add(entryEnter);

            // 마우스 아웃
            EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((_) => HideTooltip());
            trigger.triggers.Add(entryExit);
        }

        // 저장된 값을 불러와서 CurrentLevel에도 반영
        CurrentLevel = currentUnlockedLevel;
    }

    // 툴팁 표시/숨김 메서드
    public void ShowTooltip(int levelIdx)
    {
        // 이미 해금된 버튼이면 툴팁 패널을 끈다
        bool unlocked = levelIdx < currentUnlockedLevel;
        if (unlocked)
        {
            tooltipPanel.SetActive(false);
            return;
        }

        tooltipPanel.SetActive(true);
        tooltipText.text = $"{starlightNeededForLevel[levelIdx]} 개의 별빛";

        // 위치 이동
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
        tooltipRect.anchoredPosition = localPoint + new Vector2(0, 80f); // 버튼 위로 40 픽셀 이동
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    // 레벨 해금 시 저장!
    public void TryUnlockLevel(int levelIdx)
    {
        if (levelIdx > currentUnlockedLevel) return;

        int needStarlight = starlightNeededForLevel[levelIdx];
        int currentStarlight = StarDataManager.Instance.playerData.starlight;

        if (currentStarlight < needStarlight)
        {
            ShowNotEnoughStarlight();
            return;
        }

        // 별빛 차감
        StarDataManager.Instance.SpendStarlight(needStarlight);

        // 해금 현황 갱신
        currentUnlockedLevel = Mathf.Max(currentUnlockedLevel, levelIdx + 1);
        unlockData.currentUnlockedLevel = currentUnlockedLevel;
        CurrentLevel = currentUnlockedLevel;  // static 값도 동기화
        SaveUnlockData(); // 해금할 때마다 저장

        UpdateLevelButtons();

        // 해금 현황 갱신 이후에 효과 패널만 활성화 & 텍스트 교체
        ShowUnlockEffectPanel(currentUnlockedLevel);
    }

    void SaveUnlockData()
    {
        string json = JsonUtility.ToJson(unlockData, true);
        File.WriteAllText(savePath, json);
    }

    void LoadUnlockData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            unlockData = JsonUtility.FromJson<TreeUnlockData>(json);
        }
        else
        {
            unlockData = new TreeUnlockData();
        }
        currentUnlockedLevel = unlockData.currentUnlockedLevel;
    }

    void UpdateLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool unlocked = i < currentUnlockedLevel;
            bool canUnlock = i == currentUnlockedLevel;
            var colors = levelButtons[i].colors;
            levelButtons[i].interactable = canUnlock;
            levelButtons[i].GetComponent<Image>().color = unlocked ? unlockedColor : lockedColor;

            // 레벨 별 텍스트 해금 시 나타나도록 설정
            if (unlocked)
                levelDescTexts[i].text = levelDescriptions[i];
            else
                levelDescTexts[i].text = "???";
        }

        // 현재 상태 텍스트 표시
        if (currentUnlockedLevel > 0)
        {
            currentStateText.text = $"현재 상태: {levelDescriptions[currentUnlockedLevel - 1]}";
        }
        else
        {
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

        // 페이드 인
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            notEnoughStarlightGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        notEnoughStarlightGroup.alpha = 1f;

        // 1초간 유지
        yield return new WaitForSeconds(1f);

        // 페이드 아웃
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
        if (idx < 0 || idx >= unlockEffectDescriptions.Length) return;

        unlockEffectPanel.SetActive(true); // 패널만 켜고
        levelText.text = $"{level}단계 해금";
        effectText.text = unlockEffectDescriptions[idx];
        // 이후 원하는 연출(애니메이션 등) 추가
    }
    // 이 메서드를 OnClick에 연결하세요!
    public void ClosePanel()
    {
        unlockEffectPanel.SetActive(false); // 패널만 숨김
    }
}
