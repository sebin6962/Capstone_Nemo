using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public class FarmManager : MonoBehaviour
{
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

    void Start()
    {
        RegisterFarmTiles();
    }

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
        // 작물 스프라이트 제거
        if (growingTiles[pos].cropOverlayObject != null)
            Destroy(growingTiles[pos].cropOverlayObject);

        // 젖은 흙 제거
        overlayTilemap.SetTile(pos, null);

        // 상태 제거
        growingTiles.Remove(pos);

        string itemKey = cropData.harvestItemName; // 수확물 이름 사용
        // 창고 인벤토리에 추가
        //StorageInventory.Instance.AddItem(cropData.harvestItemName, 1);
        Debug.Log("현재 나무 레벨: " + TreeLevelUnlocker.CurrentLevel);
        int amount = TreeLevelUnlocker.CurrentLevel >= 1 ? 2 : 1;
        Debug.Log("수확 개수: " + amount);
        StorageInventory.Instance.AddItem(cropData.harvestItemName, amount);
        StorageInventory.Instance.SaveStorage(); //  반드시 추가
        // 스프라이트 가져오기
        //Sprite cropSprite = Resources.Load<Sprite>("Sprites" + cropName);

        Sprite cropSprite = Resources.Load<Sprite>("Sprites/" + itemKey); // 수확물 스프라이트 로드

        // 이 타일의 월드 위치 기준
        Vector3 worldPos = fieldTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);

        // 날아가는 애니메이션 실행
        StorageIconFlyEffect.Instance.Play(cropSprite, worldPos);

        // 0.5초 뒤 알림 등록
        StorageAlertManager.Instance.NotifyNewHarvestedItem(cropName);

        Debug.Log($"작물 {cropName} 수확됨 → 창고로 이동");

    }
}
