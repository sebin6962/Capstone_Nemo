using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;

[System.Serializable]
public class CropTileSave
{
    public int x, y;                 // 타일 좌표
    public string harvestItemName;          // CropData.cropName (또는 ID)
    public int currentStage;         // 현재 단계
    public float timer;              // 현 단계 진행 타이머
    public bool isWatered;           // 물 유무
    public string lastWaterTime;

    public bool isTree;
}

[System.Serializable]
public class FarmSaveData
{
    public List<CropTileSave> crops = new List<CropTileSave>();
    public List<int> wetXs = new List<int>();   // 젖은 흙 좌표
    public List<int> wetYs = new List<int>();
    public double lastSavedUtcSeconds;
}

public class FarmManager : MonoBehaviour
{
    public GameObject storageFullPanel;     // "창고가 가득 찼습니다" 패널
    public CanvasGroup storageFullGroup;    // 위 패널에 붙은 CanvasGroup
    private Coroutine storageFullCo;        // 중복 실행 방지

    public Tilemap fieldTilemap; // 밭이 칠해진 Tilemap
    public Tilemap overlayTilemap; //상태 변화 시 겹쳐질 Tilemap
    public TileBase farmTile;  // 밭으로 간주할 타일 (FarmSoilTile.asset) // 마른 흙 타일
    public TileBase wetSoilTile; // 젖은 흙 타일
    public Tilemap seedOverlayTilemap;   // 씨앗 타일 전용
    public TileBase seedTile;           // 씨앗 스프라이트 타일 (ex: seedTile.asset)

    public GameObject cropOverlayPrefab; // 스프라이트용 오브젝트 (SpriteRenderer 포함)
    public CropData testCropData; // 테스트용 작물 데이터

    private Dictionary<Vector3Int, CropTile> growingTiles = new Dictionary<Vector3Int, CropTile>();

    private HashSet<Vector3Int> farmPositions = new HashSet<Vector3Int>();

    private HashSet<Vector3Int> wateredTiles = new();

    [Header("나무 레벨 부족 패널")]
    public GameObject levelTooLowPanel;
    public CanvasGroup levelTooLowGroup;
    private Coroutine levelTooLowCo = null;

    [Header("상호작용 세팅")]
    public Transform player;           // 플레이어 Transform 할당
    public float interactRadius = 1.6f; // E키 범위

    string CurrentServer => PlayerPrefs.GetString("SelectedSave", "");

    string FarmSavePath
        => string.IsNullOrEmpty(CurrentServer)
           ? null
           : System.IO.Path.Combine(Application.persistentDataPath, $"farm_{CurrentServer}.json");

    private bool IsTreeLocked(CropData data)
    {
        if (data == null || !data.isTree) return false;
        var lvMgr = PlayerLevelManager.Instance; // null-safe
        int playerLv = (lvMgr != null) ? lvMgr.Level : 1;
        int needLv = Mathf.Max(1, data.minLevelToInteract); // 기본 7로 세팅됨
        return playerLv < needLv;
    }

    void Start()
    {
        RegisterFarmTiles();
        LoadFarmState();

        if (StorageInventory.Instance != null)
        {
            StorageInventory.Instance.LoadStorage();                  // 실제 데이터 재로딩
        }
        StorageInventoryUIManager.Instance?.SyncMaxSlotsToInventory(); // UI ↔ maxSlots 동기화
        StorageInventoryUIManager.Instance?.UpdateSlots();

        RegisterAllTreeAnchorsInScene();
        if (levelTooLowPanel) levelTooLowPanel.SetActive(false);
        if (levelTooLowGroup) levelTooLowGroup.alpha = 0f;
    }

    void OnDisable() { SaveFarmState(); }
    void OnApplicationQuit() { SaveFarmState(); }

    private void Update()
    {
        List<Vector3Int> readyToAdvance = new();

        foreach (var kvp in growingTiles)
        {
            var tile = kvp.Value;

            if (tile.isWatered && tile.currentStage < tile.cropData.stages.Count - 1)
            {
                tile.timer += Time.deltaTime;

                if (tile.timer >= tile.cropData.stages[tile.currentStage].timeToNextStage)
                {
                    readyToAdvance.Add(kvp.Key);
                }
            }
        }

        foreach (var pos in readyToAdvance)
        {
            AdvanceCropStage(pos);
        }
        HandleTreeLevelWarningByInput();
        HandleRightClickHarvest();
    }

    public void SaveFarmState()
    {
        if (string.IsNullOrEmpty(FarmSavePath)) return;   // 세이브 미선택 시 스킵

        var data = new FarmSaveData();

        // 1) 심어진 작물 저장
        foreach (var kv in growingTiles)
        {
            var pos = kv.Key;
            var t = kv.Value;
            data.crops.Add(new CropTileSave
            {
                x = pos.x,
                y = pos.y,
                harvestItemName = t.cropData.cropName,
                currentStage = t.currentStage,
                timer = t.timer,
                isWatered = t.isWatered,
                isTree = t.cropData.isTree
            });

            System.IO.File.WriteAllText(FarmSavePath, JsonUtility.ToJson(data, true));
        }

        // 2) 젖은 흙 저장
        foreach (var pos in wateredTiles)
        {
            data.wetXs.Add(pos.x);
            data.wetYs.Add(pos.y);
        }

        // 3) 마지막 저장 시각 기록
        data.lastSavedUtcSeconds = (System.DateTime.UtcNow - System.DateTime.UnixEpoch).TotalSeconds;

        System.IO.File.WriteAllText(FarmSavePath, JsonUtility.ToJson(data, true));
    }

    public void LoadFarmState()
    {
        if (string.IsNullOrEmpty(FarmSavePath)) return;
        if (!File.Exists(FarmSavePath)) return;

        var json = File.ReadAllText(FarmSavePath);
        var data = JsonUtility.FromJson<FarmSaveData>(json);
        if (data == null) return;

        // 경과 시간 계산(초)
        double nowUtc = (System.DateTime.UtcNow - System.DateTime.UnixEpoch).TotalSeconds;
        float elapsed = 0f;
        if (data.lastSavedUtcSeconds > 0)
            elapsed = Mathf.Max(0f, (float)(nowUtc - data.lastSavedUtcSeconds));

        // 1) 기존 상태 정리
        foreach (var kv in growingTiles)
            if (kv.Value.cropOverlayObject) Destroy(kv.Value.cropOverlayObject);
        growingTiles.Clear();
        wateredTiles.Clear();

        // 2) 젖은 흙 복원 (overlay 타일/집합)
        for (int i = 0; i < data.wetXs.Count; i++)
        {
            var pos = new Vector3Int(data.wetXs[i], data.wetYs[i], 0);
            overlayTilemap.SetTile(pos, wetSoilTile);
            wateredTiles.Add(pos);
        }

        // 3) 작물 복원
        foreach (var c in data.crops)
        {
            var pos = new Vector3Int(c.x, c.y, 0);
            // 밭 영역만 복원(혹시 밭 확장이 바뀐 경우 대비)
            //if (!farmPositions.Contains(pos)) continue;
            bool isFarm = farmPositions.Contains(pos);
            if (!isFarm && !c.isTree) continue;

            // CropData 찾기 (프로젝트 매니저에 맞게)
            var cropData = CropDataManager.Instance.GetCropDataByItemName(c.harvestItemName);
            if (cropData == null || cropData.stages.Count == 0) continue;

            // 남은 상태 로드
            int stage = Mathf.Clamp(c.currentStage, 0, cropData.stages.Count - 1);
            float timer = Mathf.Max(0f, c.timer);
            bool watered = c.isWatered;

            // 오프라인 성장: elapsed를 현재/이후 단계에 순차적으로 적용
            float remain = elapsed;
            while (remain > 0f && watered && stage < cropData.stages.Count - 1)
            {
                float need = cropData.stages[stage].timeToNextStage - timer;
                if (need <= 0f)
                {
                    // 즉시 한 단계 진급 처리
                    stage = Mathf.Min(stage + 1, cropData.stages.Count - 1);
                    timer = 0f;
                    watered = false;
                    break; 
                }

                if (remain >= need)
                {
                    // 다음 단계로 성장
                    remain -= need;
                    stage += 1;
                    timer = 0f;

                    // 다음 단계로 넘어오면 다시 "물"이 필요하다면 여기서 watered=false로 바꾸세요.
                    // (현재 시스템이 '단계마다 물 한 번 필요'라면 아래 줄 활성화)
                    watered = false;
                }
                else
                {
                    // 아직 다음 단계 못 감: 타이머만 누적
                    timer += remain;
                    remain = 0f;
                }
            }
            if (!watered && overlayTilemap.GetTile(pos) == wetSoilTile)
            {
                overlayTilemap.SetTile(pos, null);
                wateredTiles.Remove(pos);
            }

            // 오버레이 스프라이트 오브젝트 재생성
            Vector3 overlayWorldPos = overlayTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0f);
            GameObject overlay = Instantiate(cropOverlayPrefab, overlayWorldPos, Quaternion.identity, transform);
            overlay.GetComponent<SpriteRenderer>().sprite = cropData.stages[Mathf.Clamp(stage, 0, cropData.stages.Count - 1)].sprite;

            if (cropData.isTree)
            {
                SetupTreeComponents(overlay);
            }

            var cropInfo = new CropTile(pos, cropData, overlay)
            {
                currentStage = stage,   
                timer = timer,          
                isWatered = watered     
            };
            growingTiles.Add(pos, cropInfo);
        }

        foreach (var pos in wateredTiles)
        {
            if (growingTiles.TryGetValue(pos, out var tile))
            {
                if (!tile.isWatered)
                {
                    overlayTilemap.SetTile(pos, null);
                }
            }
        }
        Debug.Log($"[Farm] Loaded: {data.crops.Count} crops, {data.wetXs.Count} wet tiles");
    }

    private void RegisterAllTreeAnchorsInScene()
    {
        var anchors = FindObjectsOfType<TreeAnchor>();
        foreach (var a in anchors)
        {
            RegisterTreeAtWorldPos(a.transform.position, a.treeData, a.startStage);
        }
    }

    public void RegisterTreeAtWorldPos(Vector3 worldPos, CropData treeData, int startStage)
    {
        Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);
        if (growingTiles.ContainsKey(cellPos)) return;

        // 오버레이 스프라이트(나무 본체 스프라이트 역할) 생성
        Vector3 overlayWorldPos = overlayTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);
        GameObject overlay = Instantiate(cropOverlayPrefab, overlayWorldPos, Quaternion.identity, transform);

        // 시작 단계 스프라이트 적용
        var sr = overlay.GetComponent<SpriteRenderer>();
        int clampedStage = Mathf.Clamp(startStage, 0, treeData.stages.Count - 1);
        sr.sprite = treeData.stages[clampedStage].sprite;

        if (treeData.isTree)
        {
            SetupTreeComponents(overlay);
        }

        // growingTiles에 등록
        var tile = new CropTile(cellPos, treeData, overlay)
        {
            currentStage = clampedStage,
            timer = 0f,
            isWatered = false
        };
        growingTiles.Add(cellPos, tile);
    }

    private void SetupTreeComponents(GameObject overlay)
    {

        // 1) YSort
        if (!overlay.TryGetComponent<YSort>(out var ysort))
            ysort = overlay.AddComponent<YSort>();

        // 2) 중앙 줄기 충돌 박스 (비-트리거)
        if (!overlay.TryGetComponent<BoxCollider2D>(out var box))
            box = overlay.AddComponent<BoxCollider2D>();

        box.isTrigger = false;

        box.offset = new Vector2(0f, 1.33518f);
        box.size = new Vector2(0.9f, 0.89344f);

        var sr = overlay.GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.sortingLayerName = "Obj";     // ← 반드시 프로젝트에 "Obj" 레이어가 있어야 함
                                             // sr.sortingOrder는 YSort가 조정하도록 둠
        }

        // 3) 레이어(오타 수정): 필요 시 프로젝트에서 "Interactable" or "Obstacle" 사용
        int layer = LayerMask.NameToLayer("Interactable"); // ← 존재하는 레이어명으로
        if (layer != -1) overlay.layer = layer;
    }


    // 1. 타일맵에서 밭 범위 자동 등록
    void RegisterFarmTiles()
    {
        farmPositions.Clear();

        // 범위를 스캔 (유효한 영역 내에서만)
        BoundsInt bounds = fieldTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileBase tile = fieldTilemap.GetTile(pos);

                if (tile == farmTile)
                {
                    farmPositions.Add(pos);
                }
            }
        }

        Debug.Log($"밭 위치 {farmPositions.Count}개 등록 완료");
    }

    // 2. 이 위치가 밭인가?
    public bool IsFarmTile(Vector3 worldPos)
    {
        Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);
        return farmPositions.Contains(cellPos);
    }

    // 3. 추후 밭 범위 확장 (예: 레벨업)
    public void AddFarmTile(Vector3Int cellPos)
    {
        farmPositions.Add(cellPos);
        fieldTilemap.SetTile(cellPos, farmTile);
    }

    //밭에 물 뿌렸을 때 변화
    public void WaterSoil(Vector3 worldPos)
    {
        SFXManager.Instance.PlayFarmWaterSFX();
        Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);

        //if (IsFarmTile(worldPos))
        //{
        //    overlayTilemap.SetTile(cellPos, wetSoilTile);
        //    wateredTiles.Add(cellPos); // 물 준 위치 저장

        //    if (growingTiles.TryGetValue(cellPos, out var tileInfo))
        //    {
        //        tileInfo.isWatered = true;
        //        Debug.Log($"작물 타일 {cellPos}에 물을 줌 → 성장 시작");
        //    }
        //}

        // 1) 이미 작물/나무가 있다면 → 밭 여부와 무관하게 물주기 + 젖은 흙 표시
        if (growingTiles.TryGetValue(cellPos, out var tileInfo))
        {
            if (IsTreeLocked(tileInfo.cropData))
            {
                Debug.Log("[Tree Locked] 레벨 미만이라 나무에 물을 줄 수 없습니다.");
                return; // 젖은 흙도 깔지 않음
            }

            overlayTilemap.SetTile(cellPos, wetSoilTile);  // 젖은 흙 연출
            wateredTiles.Add(cellPos);
            tileInfo.isWatered = true;                     // 성장 타이머가 돌도록
            return;
        }

        // 2) 심어진 게 없고 '밭'이면 기존처럼 젖은 흙만 표시 (씨앗 심을 준비)
        if (IsFarmTile(worldPos))
        {
            overlayTilemap.SetTile(cellPos, wetSoilTile);
            wateredTiles.Add(cellPos);
        }

        bool IsFarmTile(Vector3 worldPos)
        {
            Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);
            // 변경: "밭"이거나 "이미 작물/나무가 심어진 칸"이면 true
            return farmPositions.Contains(cellPos) || growingTiles.ContainsKey(cellPos);
        }
    }

    //씨앗 뿌렸을 때 변화
    //public void PlantSeed(Vector3 worldPos)
    //{
    //    Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);

    //    if (!IsFarmTile(worldPos) || growingTiles.ContainsKey(cellPos))
    //        return;

    //    // 젖은 흙 여부도 검사하려면 여기 추가

    //    // 덮을 스프라이트 생성 (기존 seedOverlayTilemap 쓰려면 TileBase 스프라이트 처리 필요)
    //    Vector3 overlayWorldPos = overlayTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);
    //    GameObject overlay = Instantiate(cropOverlayPrefab, overlayWorldPos, Quaternion.identity, transform);
    //    overlay.GetComponent<SpriteRenderer>().sprite = testCropData.stages[0].sprite;

    //    var cropInfo = new CropTile(cellPos, testCropData, overlay);

    //    // 이미 물 준 곳이면 바로 성장 시작
    //    if (wateredTiles.Contains(cellPos))
    //    {
    //        cropInfo.isWatered = true;
    //        Debug.Log($"씨앗이 심어진 타일 {cellPos}은 이미 물이 있음 → 즉시 성장 시작");
    //    }

    //    growingTiles.Add(cellPos, cropInfo);
    //}

    public void PlantSeed(Vector3 worldPos, CropData cropData)
    {
        SFXManager.Instance.PlayFarmSeedSFX();
        Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);

        if (!IsFarmTile(worldPos) || growingTiles.ContainsKey(cellPos))
            return;

        Vector3 overlayWorldPos = overlayTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);
        GameObject overlay = Instantiate(cropOverlayPrefab, overlayWorldPos, Quaternion.identity, transform);
        overlay.GetComponent<SpriteRenderer>().sprite = cropData.stages[0].sprite;

        var cropInfo = new CropTile(cellPos, cropData, overlay);

        if (wateredTiles.Contains(cellPos))
        {
            cropInfo.isWatered = true;
        }

        growingTiles.Add(cellPos, cropInfo);
    }

    //작물 성장
    private void AdvanceCropStage(Vector3Int pos)
    {
        var tile = growingTiles[pos];
        tile.currentStage++;
        tile.timer = 0f;
        tile.isWatered = false;

        if (tile.cropOverlayObject != null)
        {
            tile.cropOverlayObject.GetComponent<SpriteRenderer>().sprite = tile.cropData.stages[tile.currentStage].sprite;

            overlayTilemap.SetTile(pos, null);
            wateredTiles.Remove(pos);
        }

        //overlayTilemap.ClearTile(pos);
        

        Debug.Log($"작물 {tile.cropData.cropName}이 {tile.currentStage}단계로 성장함");
    }

    //수확 처리 함수
    private void HandleRightClickHarvest()
    {
        if (Input.GetMouseButtonDown(1)) // 우클릭
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);

            if (growingTiles.TryGetValue(cellPos, out var tile))
            {
                bool isFullyGrown = tile.currentStage == tile.cropData.stages.Count - 1;

                if (isFullyGrown)
                {
                    HarvestCrop(cellPos, tile.cropData.cropName);
                }
            }
        }
    }

    private void HarvestCrop(Vector3Int pos, string cropName)
    {
        //var cropData = growingTiles[pos].cropData;
        if (!growingTiles.TryGetValue(pos, out var tile)) return;
        var data = tile.cropData;

        if (IsTreeLocked(data))
        {
            Debug.Log("[Tree Locked] 레벨 미만이라 나무를 수확할 수 없습니다.");
            return;
        }

        // 1) 수확 예정 수량 계산
        int amount = TreeLevelUnlocker.CurrentLevel >= 1 ? 2 : 1;

        // 2) 수확물 키
        string itemKey = data.harvestItemName;

        // 3) 창고 공간 확인 (없으면 경고 패널만 띄우고 return)
        if (!StorageInventory.Instance.HasRoomFor(itemKey, amount))
        {
            ShowStorageFull();
            return;
        }

        SFXManager.Instance.PlayBbyongSFX();

        StorageInventory.Instance.TryAddItem(itemKey, amount);
        StorageInventory.Instance.SaveStorage();

        // 창고 인벤토리에 추가
        //StorageInventory.Instance.AddItem(cropData.harvestItemName, 1);
        Debug.Log("현재 나무 레벨: " + TreeLevelUnlocker.CurrentLevel);
        //int amount = TreeLevelUnlocker.CurrentLevel >= 1 ? 2 : 1;
        Debug.Log("수확 개수: " + amount);
        //StorageInventory.Instance.AddItem(cropData.harvestItemName, amount);
        //StorageInventory.Instance.SaveStorage(); //  반드시 추가
        // 스프라이트 가져오기
        //Sprite cropSprite = Resources.Load<Sprite>("Sprites" + cropName);

        Sprite cropSprite = Resources.Load<Sprite>("Sprites/Ingredients/" + itemKey); // 수확물 스프라이트 로드

        // 이 타일의 월드 위치 기준
        Vector3 worldPos = fieldTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);

        // 날아가는 애니메이션 실행
        StorageIconFlyEffect.Instance.Play(cropSprite, worldPos);

        // 0.5초 뒤 알림 등록
        StorageAlertManager.Instance.NotifyNewHarvestedItem(cropName);

        if (data.isTree)
        {
            // 나무: 제거하지 않고 1단계로 되감기
            tile.currentStage = Mathf.Clamp(data.harvestResetStage, 0, data.stages.Count - 1);
            tile.timer = 0f;
            tile.isWatered = false;

            // 스프라이트 갱신
            if (tile.cropOverlayObject != null)
            {
                var sr = tile.cropOverlayObject.GetComponent<SpriteRenderer>();
                sr.sprite = data.stages[tile.currentStage].sprite;
            }

            // 젖은 흙 비주얼은 제거(수확 후 바로 젖어있지 않음)
            overlayTilemap.SetTile(pos, null);
            wateredTiles.Remove(pos);
        }
        else
        {
            // 밭 작물: 기존처럼 제거
            if (tile.cropOverlayObject != null) Destroy(tile.cropOverlayObject);
            overlayTilemap.SetTile(pos, null);
            wateredTiles.Remove(pos);
            growingTiles.Remove(pos);
        }

        Debug.Log($"작물 {cropName} 수확됨 → 창고로 이동");

    }

    private void HandleTreeLevelWarningByInput()
    {
        // 1) 마우스 왼클릭: 커서 아래 나무 잠금이면 경고
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);

            if (growingTiles.TryGetValue(cellPos, out var tile))
            {
                var data = tile.cropData;
                if (data != null && data.isTree && IsTreeLocked(data)) // IsTreeLocked는 FarmManager에 이미 존재:contentReference[oaicite:4]{index=4}
                {
                    ShowLevelTooLowByInput();
                    return;
                }
            }
        }

        // 2) E키: 플레이어 주변 반경 내에 '나무 잠금'이 하나라도 있으면 경고
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (player == null) return;

            // growingTiles는 <cellPos, CropTile> 딕셔너리 (FarmManager 내부):contentReference[oaicite:5]{index=5}
            foreach (var kv in growingTiles)
            {
                var cell = kv.Key;
                var cropTile = kv.Value;
                var data = cropTile.cropData;

                if (data == null || !data.isTree) continue;

                // 타일의 월드 중앙 좌표
                Vector3 tileCenter = overlayTilemap.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0f);

                // 플레이어와 거리 체크
                if (Vector2.Distance(player.position, tileCenter) <= interactRadius)
                {
                    if (IsTreeLocked(data))
                    {
                        ShowLevelTooLowByInput();
                        return; // 하나라도 걸리면 경고 후 종료
                    }
                }
            }
        }
    }

    public void ShowLevelTooLowByInput()
    {
        if (levelTooLowPanel == null || levelTooLowGroup == null) return;
        if (levelTooLowCo != null) StopCoroutine(levelTooLowCo);
        levelTooLowCo = StartCoroutine(LevelTooLowRoutine());
    }

    private IEnumerator LevelTooLowRoutine()
    {
        levelTooLowPanel.SetActive(true);

        float duration = 0.5f;
        float t = 0f;

        // Fade In
        while (t < duration)
        {
            t += Time.deltaTime;
            levelTooLowGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        levelTooLowGroup.alpha = 1f;

        // 유지
        yield return new WaitForSeconds(1f);

        // Fade Out
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            levelTooLowGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        levelTooLowGroup.alpha = 0f;

        levelTooLowPanel.SetActive(false);
        levelTooLowCo = null;
    }


    public bool HasPlantedAt(Vector3 worldPos)
    {
        var cell = fieldTilemap.WorldToCell(worldPos);
        return growingTiles.ContainsKey(cell);
    }

    public void ShowStorageFull()
    {
        if (storageFullCo != null) StopCoroutine(storageFullCo);
        storageFullCo = StartCoroutine(StorageFullRoutine());
    }

    private IEnumerator StorageFullRoutine()
    {
        storageFullPanel.SetActive(true);

        float duration = 0.5f;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            storageFullGroup.alpha = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }
        storageFullGroup.alpha = 1f;

        yield return new WaitForSeconds(1f);

        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            storageFullGroup.alpha = Mathf.Lerp(1f, 0f, t / duration);
            yield return null;
        }
        storageFullGroup.alpha = 0f;

        storageFullPanel.SetActive(false);
        storageFullCo = null;
    }
}
