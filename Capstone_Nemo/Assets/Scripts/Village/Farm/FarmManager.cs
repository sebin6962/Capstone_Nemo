using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public class FarmManager : MonoBehaviour
{
    public Tilemap fieldTilemap; // ���� ĥ���� Tilemap
    public Tilemap overlayTilemap; //���� ��ȭ �� ������ Tilemap
    public TileBase farmTile;  // ������ ������ Ÿ�� (FarmSoilTile.asset) // ���� �� Ÿ��
    public TileBase wetSoilTile; // ���� �� Ÿ��
    public Tilemap seedOverlayTilemap;   // ���� Ÿ�� ����
    public TileBase seedTile;           // ���� ��������Ʈ Ÿ�� (ex: seedTile.asset)

    public GameObject cropOverlayPrefab; // ��������Ʈ�� ������Ʈ (SpriteRenderer ����)
    public CropData testCropData; // �׽�Ʈ�� �۹� ������

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

    // 1. Ÿ�ϸʿ��� �� ���� �ڵ� ���
    void RegisterFarmTiles()
    {
        farmPositions.Clear();

        // ������ ��ĵ (��ȿ�� ���� ��������)
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

        Debug.Log($"�� ��ġ {farmPositions.Count}�� ��� �Ϸ�");
    }

    // 2. �� ��ġ�� ���ΰ�?
    public bool IsFarmTile(Vector3 worldPos)
    {
        Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);
        return farmPositions.Contains(cellPos);
    }

    // 3. ���� �� ���� Ȯ�� (��: ������)
    public void AddFarmTile(Vector3Int cellPos)
    {
        farmPositions.Add(cellPos);
        fieldTilemap.SetTile(cellPos, farmTile);
    }

    //�翡 �� �ѷ��� �� ��ȭ
    public void WaterSoil(Vector3 worldPos)
    {
        Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);

        if (IsFarmTile(worldPos))
        {
            overlayTilemap.SetTile(cellPos, wetSoilTile);
            wateredTiles.Add(cellPos); // �� �� ��ġ ����

            if (growingTiles.TryGetValue(cellPos, out var tileInfo))
            {
                tileInfo.isWatered = true;
                Debug.Log($"�۹� Ÿ�� {cellPos}�� ���� �� �� ���� ����");
            }
        }
    }

    //���� �ѷ��� �� ��ȭ
    //public void PlantSeed(Vector3 worldPos)
    //{
    //    Vector3Int cellPos = fieldTilemap.WorldToCell(worldPos);

    //    if (!IsFarmTile(worldPos) || growingTiles.ContainsKey(cellPos))
    //        return;

    //    // ���� �� ���ε� �˻��Ϸ��� ���� �߰�

    //    // ���� ��������Ʈ ���� (���� seedOverlayTilemap ������ TileBase ��������Ʈ ó�� �ʿ�)
    //    Vector3 overlayWorldPos = overlayTilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);
    //    GameObject overlay = Instantiate(cropOverlayPrefab, overlayWorldPos, Quaternion.identity, transform);
    //    overlay.GetComponent<SpriteRenderer>().sprite = testCropData.stages[0].sprite;

    //    var cropInfo = new CropTile(cellPos, testCropData, overlay);

    //    // �̹� �� �� ���̸� �ٷ� ���� ����
    //    if (wateredTiles.Contains(cellPos))
    //    {
    //        cropInfo.isWatered = true;
    //        Debug.Log($"������ �ɾ��� Ÿ�� {cellPos}�� �̹� ���� ���� �� ��� ���� ����");
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

    //�۹� ����
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
        

        Debug.Log($"�۹� {tile.cropData.cropName}�� {tile.currentStage}�ܰ�� ������");
    }

    //��Ȯ ó�� �Լ�
    private void HandleRightClickHarvest()
    {
        if (Input.GetMouseButtonDown(1)) // ��Ŭ��
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
        // �۹� ��������Ʈ ����
        if (growingTiles[pos].cropOverlayObject != null)
            Destroy(growingTiles[pos].cropOverlayObject);

        // ���� �� ����
        overlayTilemap.SetTile(pos, null);

        // ���� ����
        growingTiles.Remove(pos);

        string itemKey = cropData.harvestItemName; // ��Ȯ�� �̸� ���
        // â�� �κ��丮�� �߰�
        //StorageInventory.Instance.AddItem(cropData.harvestItemName, 1);
        Debug.Log("���� ���� ����: " + TreeLevelUnlocker.CurrentLevel);
        int amount = TreeLevelUnlocker.CurrentLevel >= 1 ? 2 : 1;
        Debug.Log("��Ȯ ����: " + amount);
        StorageInventory.Instance.AddItem(cropData.harvestItemName, amount);
        StorageInventory.Instance.SaveStorage(); //  �ݵ�� �߰�
        // ��������Ʈ ��������
        //Sprite cropSprite = Resources.Load<Sprite>("Sprites" + cropName);

        Sprite cropSprite = Resources.Load<Sprite>("Sprites/" + itemKey); // ��Ȯ�� ��������Ʈ �ε�

        // �� Ÿ���� ���� ��ġ ����
        Vector3 worldPos = fieldTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);

        // ���ư��� �ִϸ��̼� ����
        StorageIconFlyEffect.Instance.Play(cropSprite, worldPos);

        // 0.5�� �� �˸� ���
        StorageAlertManager.Instance.NotifyNewHarvestedItem(cropName);

        Debug.Log($"�۹� {cropName} ��Ȯ�� �� â��� �̵�");

    }
}
