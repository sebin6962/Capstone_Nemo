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

    string FarmSavePath => Path.Combine(Application.persistentDataPath, "farm_state.json");
    
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

        HandleRightClickHarvest();
    }

    public void SaveFarmState()
    {
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
                harvestItemName = t.cropData.harvestItemName,
                currentStage = t.currentStage,
                timer = t.timer,
                isWatered = t.isWatered
            });
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
            if (!farmPositions.Contains(pos)) continue;

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

        if (IsFarmTile(worldPos))
        {
            overlayTilemap.SetTile(cellPos, wetSoilTile);
            wateredTiles.Add(cellPos); // 물 준 위치 저장

            if (growingTiles.TryGetValue(cellPos, out var tileInfo))
            {
                tileInfo.isWatered = true;
                Debug.Log($"작물 타일 {cellPos}에 물을 줌 → 성장 시작");
            }
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
        var cropData = growingTiles[pos].cropData;

        // 1) 수확 예정 수량 계산
        int amount = TreeLevelUnlocker.CurrentLevel >= 1 ? 2 : 1;

        // 2) 수확물 키
        string itemKey = cropData.harvestItemName;

        // 3) 창고 공간 확인 (없으면 경고 패널만 띄우고 return)
        if (!StorageInventory.Instance.HasRoomFor(itemKey, amount))
        {
            ShowStorageFull();
            return;
        }

        SFXManager.Instance.PlayBbyongSFX();

        StorageInventory.Instance.TryAddItem(itemKey, amount);
        StorageInventory.Instance.SaveStorage();

        // 작물 스프라이트 제거
        if (growingTiles[pos].cropOverlayObject != null)
            Destroy(growingTiles[pos].cropOverlayObject);

        // 젖은 흙 제거
        overlayTilemap.SetTile(pos, null);
        wateredTiles.Remove(pos);

        // 상태 제거
        growingTiles.Remove(pos);

        //string itemKey = cropData.harvestItemName; // 수확물 이름 사용
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

        Debug.Log($"작물 {cropName} 수확됨 → 창고로 이동");

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
