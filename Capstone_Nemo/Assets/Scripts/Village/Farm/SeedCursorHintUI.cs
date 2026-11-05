using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shows the currently held seed-bag sprite above and to the right of the cursor
/// only while hovering an empty, plantable farm cell within the player's 3x3 reach.
/// This is visual-only and never intercepts clicks or changes planting behavior.
/// </summary>
public class SeedCursorHintUI : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private FarmManager farmManager;
    [SerializeField] private VillagePlayerManager villagePlayer;
    [SerializeField] private Canvas screenCanvas;
    [SerializeField] private Camera worldCamera;

    [Header("UI references")]
    [SerializeField] private RectTransform hintRoot;
    [SerializeField] private Image seedIconImage;

    [Header("Cursor appearance")]
    [SerializeField] private Vector2 cursorOffsetPixels = new Vector2(24f, 24f);
    [SerializeField, Min(0f)] private float appearDuration = 0.12f;

    private RectTransform canvasRect;
    private float shownScale;
    private float appearProgress;
    private bool visible;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (screenCanvas != null)
            canvasRect = screenCanvas.transform as RectTransform;
        if (hintRoot != null)
        {
            shownScale = hintRoot.localScale.x;
            if (Mathf.Approximately(shownScale, 0f)) shownScale = 1f;
            hintRoot.localScale = Vector3.zero;
            hintRoot.gameObject.SetActive(false);
        }
        if (seedIconImage != null)
            seedIconImage.raycastTarget = false;
        // Cursor hints must not block clicks on field tiles.
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
    }

    private void LateUpdate()
    {
        Sprite seedSprite;
        if (!TryGetPlantableHover(out seedSprite) ||
            screenCanvas == null || canvasRect == null ||
            hintRoot == null || seedIconImage == null)
        {
            Hide();
            return;
        }

        if (!visible)
        {
            visible = true;
            appearProgress = 0f;
            hintRoot.gameObject.SetActive(true);
            hintRoot.localScale = Vector3.zero;
        }

        if (seedIconImage.sprite != seedSprite)
            seedIconImage.sprite = seedSprite;

        Vector2 mouseScreen = (Vector2)Input.mousePosition + cursorOffsetPixels;
        Camera uiCamera = screenCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : screenCanvas.worldCamera;

        Vector2 anchoredPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, mouseScreen, uiCamera, out anchoredPosition))
        {
            // hintRoot should have center anchors (0.5, 0.5) on screenCanvas.
            hintRoot.anchoredPosition = anchoredPosition;
        }

        if (appearDuration <= 0f)
            appearProgress = 1f;
        else
            appearProgress = Mathf.Clamp01(appearProgress + Time.unscaledDeltaTime / appearDuration);

        float eased = Mathf.SmoothStep(0f, 1f, appearProgress);
        hintRoot.localScale = Vector3.one * (shownScale * eased);
    }

    private bool TryGetPlantableHover(out Sprite seedSprite)
    {
        seedSprite = null;
        if (farmManager == null || farmManager.fieldTilemap == null ||
            villagePlayer == null || worldCamera == null ||
            HeldItemManager.Instance == null || CropDataManager.Instance == null ||
            !HeldItemManager.Instance.IsHoldingItem() ||
            TutorialDialogueManager.IsDialogueOpen ||
            NPCInteractable.BlocksOtherWorldInteraction)
            return false;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (BoxInventoryManager.Instance != null &&
            BoxInventoryManager.Instance.IsHoldingWateringCan())
            return false;

        string heldName = HeldItemManager.Instance.GetHeldItemName();
        if (string.IsNullOrEmpty(heldName) ||
            !heldName.EndsWith("_seedBag", System.StringComparison.OrdinalIgnoreCase) ||
            CropDataManager.Instance.GetCropDataByItemName(heldName) == null)
            return false;

        seedSprite = HeldItemManager.Instance.GetHeldItemSprite();
        if (seedSprite == null)
            return false;

        Vector3 worldPoint = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        worldPoint.z = 0f;
        Vector3Int hoveredCell = farmManager.fieldTilemap.WorldToCell(worldPoint);
        Vector3Int playerCell = farmManager.fieldTilemap.WorldToCell(villagePlayer.transform.position);

        return Mathf.Abs(hoveredCell.x - playerCell.x) <= 1 &&
               Mathf.Abs(hoveredCell.y - playerCell.y) <= 1 &&
               farmManager.IsEmptyFarmCellForBubbles(hoveredCell);
    }

    private void Hide()
    {
        if (!visible) return;
        visible = false;
        appearProgress = 0f;
        if (hintRoot != null)
        {
            hintRoot.localScale = Vector3.zero;
            hintRoot.gameObject.SetActive(false);
        }
    }

    private void OnDisable() => Hide();
}
