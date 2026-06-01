using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Collections;

[System.Serializable]
public class GrassLootPoint
{
    public Transform point;

    [Tooltip("실제로 흔들릴 오브젝트. 비워두면 point가 흔들립니다.")]
    public Transform shakeTarget;

    [Tooltip("획득 가능 상태일 때 풀 위에 표시할 말풍선 오브젝트")]
    public GameObject speechBubble;

    [Tooltip("저장용 고유 ID. 비워두면 오브젝트 이름+좌표로 자동 생성되지만, 직접 적는 것을 추천합니다.")]
    public string id;

    [Tooltip("-1이면 GrassLootManager의 기본 거리 사용")]
    public float radiusOverride = -1f;
}

[Serializable]
public class GrassLootRecord
{
    public string id;
    public int lastLootDay;
}

[Serializable]
public class GrassLootSaveData
{
    public List<GrassLootRecord> records = new List<GrassLootRecord>();
}

[System.Serializable]
public class GrassLootItem
{
    public string itemKey;

    [Min(1)]
    public int unlockLevel = 1;
}

public class GrassLootManager : MonoBehaviour
{
    private bool lastTutorialBlockingState;

    private readonly HashSet<Transform> shakingGrassObjects = new HashSet<Transform>();

    [Header("기본 설정")]
    public Transform player;
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 1.4f;

    [Header("풀 오브젝트 리스트")]
    public List<GrassLootPoint> grassPoints = new List<GrassLootPoint>();

    [Header("랜덤 획득 아이템")]
    [Tooltip("비워두면 ItemTooltipDB의 모든 아이템 중 랜덤. 특정 재료만 나오게 하려면 여기에 키를 직접 넣으세요.")]
    public List<GrassLootItem> lootItems = new List<GrassLootItem>();

    [Header("아이콘 스프라이트 Resources 경로")]
    public string[] spriteResourceFolders =
    {
        "Sprites/Ingredients",
        "Sprites/Items",
        "Sprites/Foods"
    };

    [Header("알림 UI")]
    public ItemAcquireNoticeUI acquireNoticeUI;

    [Header("연출")]
    public Vector3 flyStartOffset = new Vector3(0f, 0.5f, 0f);

    private GrassLootSaveData saveData = new GrassLootSaveData();

    private string CurrentServer => PlayerPrefs.GetString("SelectedSave", "");

    private string SavePath
    {
        get
        {
            if (string.IsNullOrEmpty(CurrentServer))
                return null;

            return Path.Combine(Application.persistentDataPath, $"grassLoot_{CurrentServer}.json");
        }
    }

    private void Awake()
    {
        Load();
    }

    private void Start()
    {
        lastTutorialBlockingState = IsTutorialBlockingGrassLoot();
        Debug.Log(
        $"[GrassLoot Debug/Start] " +
        $"managerActive={gameObject.activeInHierarchy}, " +
        $"enabled={enabled}, " +
        $"player={(player != null ? player.name : "NULL")}, " +
        $"grassPoints={grassPoints.Count}, " +
        $"lootItems={lootItems.Count}, " +
        $"today={GetCurrentDay()}, " +
        $"tutorialBlocking={IsTutorialBlockingGrassLoot()}, " +
        $"hasAnyLoot={HasAnyUnlockedLootItem()}, " +
        $"save={CurrentServer}"
    );
        UpdateAllSpeechBubbles();
    }

    private void OnEnable()
    {
        TimeManager.OnNewDayStarted += HandleNewDayStarted;
    }

    private void OnDisable()
    {
        TimeManager.OnNewDayStarted -= HandleNewDayStarted;
        Save();
    }

    private void HandleNewDayStarted()
    {
        UpdateAllSpeechBubbles();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void Update()
    {
        bool tutorialBlockingNow = IsTutorialBlockingGrassLoot();

        if (tutorialBlockingNow != lastTutorialBlockingState)
        {
            lastTutorialBlockingState = tutorialBlockingNow;
            UpdateAllSpeechBubbles();
        }

        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    private void UpdateAllSpeechBubbles()
    {
        // 튜토리얼 중에는 모든 풀 말풍선 숨김
        if (IsTutorialBlockingGrassLoot())
        {
            foreach (GrassLootPoint grass in grassPoints)
            {
                if (grass == null)
                    continue;

                if (grass.speechBubble != null)
                    grass.speechBubble.SetActive(false);
            }

            return;
        }

        int today = GetCurrentDay();

        bool hasAnyUnlockedLoot = HasAnyUnlockedLootItem();

        foreach (GrassLootPoint grass in grassPoints)
        {
            if (grass == null || grass.point == null)
                continue;

            if (grass.speechBubble == null)
                continue;

            string spotId = GetPointId(grass);

            bool alreadyLootedToday = IsLootedToday(spotId, today);

            // 핵심:
            // 거리 체크 X
            // 창고 공간 체크 X
            // 오늘 뒤졌는지 + 현재 레벨에서 나올 재료가 있는지만 체크
            bool canLoot = !alreadyLootedToday && hasAnyUnlockedLoot;

            grass.speechBubble.SetActive(canLoot);
        }
    }

    private bool HasAnyUnlockedLootItem()
    {
        int currentLevel = GetCurrentPlayerLevel();

        foreach (GrassLootItem lootItem in lootItems)
        {
            if (lootItem == null)
                continue;

            if (string.IsNullOrWhiteSpace(lootItem.itemKey))
                continue;

            if (lootItem.unlockLevel <= currentLevel)
                return true;
        }

        return false;
    }

    private void TryInteract()
    {
        // 튜토리얼 중에는 아이템 획득 불가
        Debug.Log("[GrassLoot Debug] TryInteract 호출됨");

        if (IsTutorialBlockingGrassLoot())
        {
            Debug.LogWarning("[GrassLoot Debug] 차단됨: 튜토리얼 상태로 판단됨");
            UpdateAllSpeechBubbles();
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("[GrassLoot Debug] 차단됨: player가 연결되어 있지 않음");
            return;
        }

        if (grassPoints == null || grassPoints.Count == 0)
        {
            Debug.LogWarning("[GrassLoot Debug] 차단됨: grassPoints가 비어 있음");
            return;
        }

        GrassLootPoint nearest = FindNearestPointInRange();

        if (nearest == null)
        {
            Debug.LogWarning(
                $"[GrassLoot Debug] 차단됨: 범위 안의 풀이 없음 / playerPos={player.position}, radius={interactRadius}"
            );
            return;
        }

        string spotId = GetPointId(nearest);
        int today = GetCurrentDay();

        // 이미 오늘 뒤진 풀이면 종료
        if (IsLootedToday(spotId, today))
        {
            if (acquireNoticeUI != null)
                acquireNoticeUI.ShowMessage("오늘은 이미 뒤져봤다");

            return;
        }

        string itemKey = PickRandomItemKey();

        if (string.IsNullOrEmpty(itemKey))
        {
            Debug.LogWarning("[GrassLoot] 랜덤으로 지급할 아이템이 없습니다.");
            return;
        }

        int amount = 1;
        string displayName = GetDisplayName(itemKey);

        // FarmManager 수확 로직처럼 창고 공간 먼저 확인
        if (StorageInventory.Instance == null)
        {
            Debug.LogWarning("[GrassLoot] StorageInventory.Instance가 없습니다.");
            return;
        }

        if (!StorageInventory.Instance.HasRoomFor(itemKey, amount))
        {
            FarmManager farmManager = FindObjectOfType<FarmManager>();

            if (farmManager != null)
                farmManager.ShowStorageFull();

            return;
        }

        // 여기부터는 실제 획득 성공 처리
        StorageInventory.Instance.TryAddItem(itemKey, amount);
        StorageInventory.Instance.SaveStorage();

        if (StorageInventoryUIManager.Instance != null)
        {
            StorageInventoryUIManager.Instance.UpdateSlots();
        }

        // 풀 흔들림 효과
        Transform shakeTarget = nearest.shakeTarget != null ? nearest.shakeTarget : nearest.point;

        if (shakeTarget != null)
        {
            StartCoroutine(PlayGrassShake(shakeTarget));
        }

        // 아이콘 날아가는 효과
        Sprite sprite = LoadItemSprite(itemKey);

        if (sprite != null && StorageIconFlyEffect.Instance != null)
        {
            Vector3 startWorldPos = nearest.point.position + flyStartOffset;
            StorageIconFlyEffect.Instance.Play(sprite, startWorldPos);
        }

        // 효과음
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.HarvestItemSFX();
        }

        // 획득 알림
        if (acquireNoticeUI != null)
        {
            acquireNoticeUI.ShowAcquire(displayName);
        }

        // 중요: 아이템 획득 성공 후에만 오늘 뒤짐 저장
        SetLootedToday(spotId, today);
        Save();

        UpdateAllSpeechBubbles();

        Debug.Log($"[GrassLoot] {spotId}에서 {itemKey} 획득");
    }

    private GrassLootPoint FindNearestPointInRange()
    {
        GrassLootPoint nearest = null;
        float bestSqrDistance = float.MaxValue;

        foreach (GrassLootPoint point in grassPoints)
        {
            if (point == null || point.point == null)
                continue;

            float radius = point.radiusOverride > 0f ? point.radiusOverride : interactRadius;
            float sqrRange = radius * radius;

            float sqrDistance = (player.position - point.point.position).sqrMagnitude;

            if (sqrDistance <= sqrRange && sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearest = point;
            }
        }

        return nearest;
    }

    private string PickRandomItemKey()
    {
        int currentLevel = GetCurrentPlayerLevel();

        List<string> candidates = new List<string>();

        foreach (GrassLootItem lootItem in lootItems)
        {
            if (lootItem == null)
                continue;

            if (string.IsNullOrWhiteSpace(lootItem.itemKey))
                continue;

            if (lootItem.unlockLevel <= currentLevel)
            {
                candidates.Add(lootItem.itemKey.Trim());
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"[GrassLoot] 현재 레벨 {currentLevel}에서 획득 가능한 재료가 없습니다.");
            return null;
        }

        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private int GetCurrentPlayerLevel()
    {
        // 1순위: 현재 씬에 PlayerLevelManager가 살아있으면 그 값을 사용
        if (PlayerLevelManager.Instance != null)
        {
            return Mathf.Max(1, PlayerLevelManager.Instance.Level);
        }

        // 2순위: 현재 선택된 세이브 파일에서 직접 레벨 로드
        string serverName = PlayerPrefs.GetString("SelectedSave", "");

        if (string.IsNullOrEmpty(serverName))
            return 1;

        string path = Path.Combine(
            Application.persistentDataPath,
            $"player_level_data_{serverName}.json"
        );

        if (!File.Exists(path))
            return 1;

        try
        {
            string json = File.ReadAllText(path);
            PlayerLevelData data = JsonUtility.FromJson<PlayerLevelData>(json);

            if (data != null)
                return Mathf.Max(1, data.Level);
        }
        catch
        {
            Debug.LogWarning("[GrassLoot] 플레이어 레벨 데이터를 읽는 중 오류가 발생했습니다.");
        }

        return 1;
    }

    private string GetDisplayName(string itemKey)
    {
        if (string.IsNullOrEmpty(itemKey))
            return "";

        string displayName;

        if (ItemTooltipDB.TooltipTexts.TryGetValue(itemKey.Trim(), out displayName))
            return displayName;

        return itemKey;
    }

    private Sprite LoadItemSprite(string itemKey)
    {
        if (string.IsNullOrEmpty(itemKey))
            return null;

        foreach (string folder in spriteResourceFolders)
        {
            if (string.IsNullOrEmpty(folder))
                continue;

            Sprite sprite = Resources.Load<Sprite>($"{folder}/{itemKey}");

            if (sprite != null)
                return sprite;
        }

        // 폴더 없이 바로 Resources 안에 있는 경우 대비
        return Resources.Load<Sprite>(itemKey);
    }

    private int GetCurrentDay()
    {
        if (TimeManager.Instance != null)
            return TimeManager.Instance.currentDay;

        return 1;
    }

    private string GetPointId(GrassLootPoint point)
    {
        if (point == null || point.point == null)
            return "";

        if (!string.IsNullOrWhiteSpace(point.id))
            return point.id.Trim();

        Vector3 pos = point.point.position;

        return $"{point.point.name}_{Mathf.RoundToInt(pos.x * 10f)}_{Mathf.RoundToInt(pos.y * 10f)}";
    }

    private bool IsLootedToday(string id, int today)
    {
        GrassLootRecord record = FindRecord(id);
        return record != null && record.lastLootDay == today;
    }

    private void SetLootedToday(string id, int today)
    {
        GrassLootRecord record = FindRecord(id);

        if (record == null)
        {
            record = new GrassLootRecord
            {
                id = id,
                lastLootDay = today
            };

            saveData.records.Add(record);
        }
        else
        {
            record.lastLootDay = today;
        }
    }

    private GrassLootRecord FindRecord(string id)
    {
        if (saveData == null || saveData.records == null)
            return null;

        foreach (GrassLootRecord record in saveData.records)
        {
            if (record != null && record.id == id)
                return record;
        }

        return null;
    }

    private void Load()
    {
        string path = SavePath;

        if (string.IsNullOrEmpty(path))
            return;

        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);
        GrassLootSaveData loaded = JsonUtility.FromJson<GrassLootSaveData>(json);

        if (loaded != null)
            saveData = loaded;

        if (saveData.records == null)
            saveData.records = new List<GrassLootRecord>();
    }

    private void Save()
    {
        string path = SavePath;

        if (string.IsNullOrEmpty(path))
            return;

        if (saveData == null)
            saveData = new GrassLootSaveData();

        File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
    }

    private IEnumerator PlayGrassShake(Transform target)
    {
        if (target == null) yield break;

        // 중복 실행 방지
        if (shakingGrassObjects.Contains(target))
            yield break;

        shakingGrassObjects.Add(target);

        Vector3 originalPos = target.localPosition;
        Quaternion originalRot = target.localRotation;

        float duration = 0.22f;      // 전체 흔들리는 시간
        float maxAngle = 7f;         // 좌우 회전 각도
        float maxOffset = 0.03f;     // 좌우 이동량
        float frequency = 24f;       // 흔들림 속도

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            // 점점 약해지도록
            float damping = 1f - t;

            float wave = Mathf.Sin(time * frequency);

            float angle = wave * maxAngle * damping;
            float offsetX = wave * maxOffset * damping;

            target.localRotation = Quaternion.Euler(0f, 0f, angle);
            target.localPosition = originalPos + new Vector3(offsetX, 0f, 0f);

            yield return null;
        }

        target.localPosition = originalPos;
        target.localRotation = originalRot;

        shakingGrassObjects.Remove(target);
    }

    private bool IsTutorialBlockingGrassLoot()
    {
        // 마을 2차 튜토리얼 진행 중
        //if (TutorialManager.Instance != null &&
        //    TutorialManager.Instance.IsVillageSecondTutorialRunning)
        //{
        //    return true;
        //}

        // 전체 튜토리얼 플로우가 아직 Done이 아니면 차단
        if (TutorialFlowManager.Instance != null &&
            TutorialFlowManager.Instance.currentStep != GlobalTutorialStep.Done)
        {
            return true;
        }

        return false;
    }
}
