using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.IO;

public class TreeLevelUnlocker : MonoBehaviour
{
    public static int CurrentLevel = 0; // ��𼭵� ���� �����ϵ��� ����

    public Button[] levelButtons;                // 0: ù ����
    public TMP_Text[] levelDescTexts;            // 0: ù ���� ���� �ؽ�Ʈ
    public int[] starlightNeededForLevel;        // 0: ù ������ �ʿ��� ����
    public string[] levelDescriptions;           // 0: ù ���� �رݽ� ������ �ؽ�Ʈ
    public Color unlockedColor;
    public Color lockedColor;

    // ���� ���� �ʵ� �߰�
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;

    private int currentUnlockedLevel = 0; // 0=ù��° ������ �ر�

    public GameObject notEnoughStarlightPanel; // ���� ���� �˸� �г�
    public CanvasGroup notEnoughStarlightGroup; // �˸� �г��� CanvasGroup

    public TMP_Text currentStateText; // �ν����Ϳ� ���� ���� �ؽ�Ʈ ����

    public GameObject unlockEffectPanel;  // ���� �г� ������Ʈ
    public TMP_Text levelText;            // ���� �ؽ�Ʈ
    public TMP_Text effectText;           // ȿ�� �ؽ�Ʈ
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

        // (Start���� currentUnlockedLevel�� unlockData.currentUnlockedLevel�� ����)
        currentUnlockedLevel = unlockData.currentUnlockedLevel;

        // ��� ��ư�� �̺�Ʈ ������ �߰�
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int idx = i; // ���� ������ ĸó
            EventTrigger trigger = levelButtons[i].gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = levelButtons[i].gameObject.AddComponent<EventTrigger>();

            // ���콺 ����
            EventTrigger.Entry entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener((_) => ShowTooltip(idx));
            trigger.triggers.Add(entryEnter);

            // ���콺 �ƿ�
            EventTrigger.Entry entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((_) => HideTooltip());
            trigger.triggers.Add(entryExit);
        }

        // ����� ���� �ҷ��ͼ� CurrentLevel���� �ݿ�
        CurrentLevel = currentUnlockedLevel;
    }

    // ���� ǥ��/���� �޼���
    public void ShowTooltip(int levelIdx)
    {
        // �̹� �رݵ� ��ư�̸� ���� �г��� ����
        bool unlocked = levelIdx < currentUnlockedLevel;
        if (unlocked)
        {
            tooltipPanel.SetActive(false);
            return;
        }

        tooltipPanel.SetActive(true);
        tooltipText.text = $"{starlightNeededForLevel[levelIdx]} ���� ����";

        // ��ġ �̵�
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
        tooltipRect.anchoredPosition = localPoint + new Vector2(0, 80f); // ��ư ���� 40 �ȼ� �̵�
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    // ���� �ر� �� ����!
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

        // ���� ����
        StarDataManager.Instance.SpendStarlight(needStarlight);

        // �ر� ��Ȳ ����
        currentUnlockedLevel = Mathf.Max(currentUnlockedLevel, levelIdx + 1);
        unlockData.currentUnlockedLevel = currentUnlockedLevel;
        CurrentLevel = currentUnlockedLevel;  // static ���� ����ȭ
        SaveUnlockData(); // �ر��� ������ ����

        UpdateLevelButtons();

        // �ر� ��Ȳ ���� ���Ŀ� ȿ�� �гθ� Ȱ��ȭ & �ؽ�Ʈ ��ü
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

            // ���� �� �ؽ�Ʈ �ر� �� ��Ÿ������ ����
            if (unlocked)
                levelDescTexts[i].text = levelDescriptions[i];
            else
                levelDescTexts[i].text = "???";
        }

        // ���� ���� �ؽ�Ʈ ǥ��
        if (currentUnlockedLevel > 0)
        {
            currentStateText.text = $"���� ����: {levelDescriptions[currentUnlockedLevel - 1]}";
        }
        else
        {
            currentStateText.text = "���� ����: �õ�� �ִ� �������";
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

        // ���̵� ��
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            notEnoughStarlightGroup.alpha = Mathf.Lerp(0, 1, elapsed / duration);
            yield return null;
        }
        notEnoughStarlightGroup.alpha = 1f;

        // 1�ʰ� ����
        yield return new WaitForSeconds(1f);

        // ���̵� �ƿ�
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

        unlockEffectPanel.SetActive(true); // �гθ� �Ѱ�
        levelText.text = $"{level}�ܰ� �ر�";
        effectText.text = unlockEffectDescriptions[idx];
        // ���� ���ϴ� ����(�ִϸ��̼� ��) �߰�
    }
    // �� �޼��带 OnClick�� �����ϼ���!
    public void ClosePanel()
    {
        unlockEffectPanel.SetActive(false); // �гθ� ����
    }
}
