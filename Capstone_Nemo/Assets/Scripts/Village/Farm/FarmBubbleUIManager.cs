
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Displays harvest-ready bubbles above the visible top of each crop sprite.
/// Planting cursor hints are independently handled by SeedCursorHintUI.
/// Does not alter crop growth, interactions, inventory or saving.
/// </summary>
public class FarmBubbleUIManager : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private FarmManager farmManager;
    [SerializeField] private Canvas worldCanvas;

    [Header("Harvest bubble prefab")]
    [SerializeField] private FarmBubbleAnimation harvestBubblePrefab;

    [Header("Placement above actual crop sprite (world units)")]
    [SerializeField, Min(0f)] private float gapAboveCrop = 0.18f;
    [SerializeField] private Vector2 positionAdjustment = Vector2.zero;

    [Header("Optional individual crop adjustments (cropName)")]
    [SerializeField] private List<CropBubbleOffset> cropOffsets = new List<CropBubbleOffset>();

    [Header("Harvest sprite resource folder")]
    [SerializeField] private string harvestSpriteResourceFolder = "Sprites/Ingredients/";

    [System.Serializable]
    private class CropBubbleOffset
    {
        public string cropName;
        public Vector2 offset;
    }

    private readonly Dictionary<Vector3Int, FarmBubbleAnimation> harvestBubbles =
        new Dictionary<Vector3Int, FarmBubbleAnimation>();
    private readonly HashSet<Vector3Int> readyCells = new HashSet<Vector3Int>();
    private readonly List<Vector3Int> cellsToRemove = new List<Vector3Int>();
    private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();
    private bool warnedInactiveCanvas;
    private bool warnedWrongCanvasMode;

    // A successful harvest blocks re-creation until that cell is no longer ready.
    // No time-based suppression: failed harvests remain visibly harvestable.
    private readonly HashSet<Vector3Int> harvestedCells = new HashSet<Vector3Int>();

    public void NotifyHarvestSucceeded(Vector3Int cell)
    {
        harvestedCells.Add(cell);
        if (harvestBubbles.TryGetValue(cell, out FarmBubbleAnimation bubble))
        {
            harvestBubbles.Remove(cell);
            if (bubble != null)
                bubble.HideImmediately();
        }
    }

    private void LateUpdate()
    {
        if (farmManager == null || farmManager.fieldTilemap == null || worldCanvas == null)
            return;

        if (!worldCanvas.gameObject.activeInHierarchy)
        {
            if (!warnedInactiveCanvas)
            {
                Debug.LogWarning("[FarmBubble] World Canvas must be active in the Hierarchy.", this);
                warnedInactiveCanvas = true;
            }
            return;
        }

        if (worldCanvas.renderMode != RenderMode.WorldSpace)
        {
            if (!warnedWrongCanvasMode)
            {
                Debug.LogWarning("[FarmBubble] Harvest World Canvas must use World Space render mode.", this);
                warnedWrongCanvasMode = true;
            }
            return;
        }

        RefreshHarvestBubbles();
    }

    private void RefreshHarvestBubbles()
    {
        readyCells.Clear();

        foreach (var pair in farmManager.GetFarmCropsForBubbles())
        {
            Vector3Int cell = pair.Key;
            CropTile crop = pair.Value;

            if (crop == null || crop.cropData == null ||
                crop.cropData.stages == null || crop.cropData.stages.Count == 0 ||
                crop.currentStage < crop.cropData.stages.Count - 1 ||
                crop.cropOverlayObject == null)
                continue;

            readyCells.Add(cell);

            if (harvestedCells.Contains(cell))
                continue;

            if (harvestBubblePrefab == null)
                continue;

            FarmBubbleAnimation bubble;

            if (!harvestBubbles.TryGetValue(cell, out bubble) || bubble == null)
            {
                bubble = Instantiate(harvestBubblePrefab, worldCanvas.transform);

                // Instantiating an inactive prefab creates an inactive clone.
                // StartCoroutine in FarmBubbleAnimation.Show needs an active object.
                bubble.gameObject.SetActive(true);

                harvestBubbles[cell] = bubble;

                bubble.SetIcon(LoadHarvestSprite(crop.cropData.harvestItemName));
                PlaceBubble(bubble, crop);
                bubble.Show();
            }
            else
            {
                // Re-evaluate every frame so growth sprites / sprite swaps cannot
                // leave the bubble floating at an obsolete height.
                PlaceBubble(bubble, crop);
            }
        }

        // Release the block only after a real state change (removed crop or
        // tree reset to a non-final growth stage). Next harvest cycle can show it.
        harvestedCells.RemoveWhere(cell => !readyCells.Contains(cell));

        cellsToRemove.Clear();

        foreach (var pair in harvestBubbles)
        {
            if (!readyCells.Contains(pair.Key))
                cellsToRemove.Add(pair.Key);
        }

        foreach (var cell in cellsToRemove)
        {
            if (harvestBubbles[cell] != null)
                harvestBubbles[cell].HideAndDestroy();

            harvestBubbles.Remove(cell);
        }
    }

    private Sprite LoadHarvestSprite(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
            return null;

        Sprite sprite;

        if (!iconCache.TryGetValue(itemName, out sprite))
        {
            sprite = Resources.Load<Sprite>(harvestSpriteResourceFolder + itemName);
            iconCache[itemName] = sprite;

            if (sprite == null)
                Debug.LogWarning("[FarmBubble] Harvest sprite not found: " + itemName, this);
        }

        return sprite;
    }

    private void PlaceBubble(FarmBubbleAnimation bubble, CropTile crop)
    {
        if (bubble == null || crop == null || crop.cropOverlayObject == null)
            return;

        Vector3 pos = crop.cropOverlayObject.transform.position;

        SpriteRenderer renderer =
            crop.cropOverlayObject.GetComponent<SpriteRenderer>();

        if (renderer == null)
            renderer = crop.cropOverlayObject.GetComponentInChildren<SpriteRenderer>();

        if (renderer != null && renderer.sprite != null)
        {
            // Actual renderer world bounds, including sprite PPU, scale and pivot.
            pos.x = renderer.bounds.center.x;
            pos.y = renderer.bounds.max.y;
        }
        else
        {
            pos = farmManager.fieldTilemap.GetCellCenterWorld(crop.tilePosition);
        }

        Vector2 perCrop = Vector2.zero;

        if (crop.cropData != null && cropOffsets != null)
        {
            foreach (var entry in cropOffsets)
            {
                if (entry != null && entry.cropName == crop.cropData.cropName)
                {
                    perCrop = entry.offset;
                    break;
                }
            }
        }

        pos.x += positionAdjustment.x + perCrop.x;
        pos.y += gapAboveCrop + positionAdjustment.y + perCrop.y;
        pos.z = worldCanvas.transform.position.z;

        bubble.transform.position = pos;
    }

    private void OnDisable()
    {
        // OnDisable is also used at scene unload; do not start a coroutine here.
        foreach (var bubble in harvestBubbles.Values)
        {
            if (bubble != null)
                Destroy(bubble.gameObject);
        }

        harvestBubbles.Clear();
        harvestedCells.Clear();
    }
}
