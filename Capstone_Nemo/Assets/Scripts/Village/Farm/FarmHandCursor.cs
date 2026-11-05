
using UnityEngine;
using UnityEngine.EventSystems;

public class FarmHandCursor : MonoBehaviour
{
    [Header("농사 시스템")]
    [SerializeField] private FarmManager farmManager;
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;

    [Header("커서 설정")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    private bool handRegistered;

    private void Update()
    {
        bool shouldShowHand = CanShowFarmHand();

        if (shouldShowHand)
            RegisterHand();
        else
            UnregisterHand();
    }

    private bool CanShowFarmHand()
    {
        if (farmManager == null ||
            farmManager.fieldTilemap == null ||
            player == null)
            return false;

        if (TutorialDialogueManager.IsDialogueOpen)
            return false;

        if (NPCInteractable.BlocksOtherWorldInteraction)
            return false;

        if (ignoreWhenPointerOverUI &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return false;

        if (HeldItemManager.Instance == null ||
            !HeldItemManager.Instance.IsHoldingItem())
            return false;

        Camera cam = worldCamera != null
            ? worldCamera
            : Camera.main;

        if (cam == null)
            return false;

        Vector3 worldPos =
            cam.ScreenToWorldPoint(Input.mousePosition);

        worldPos.z = 0f;

        Vector3Int mouseCell =
            farmManager.fieldTilemap.WorldToCell(worldPos);

        Vector3Int playerCell =
            farmManager.fieldTilemap.WorldToCell(player.position);

        // 기존 VillagePlayerManager의 상호작용 범위와 동일
        int dx = Mathf.Abs(mouseCell.x - playerCell.x);
        int dy = Mathf.Abs(mouseCell.y - playerCell.y);

        if (dx > 1 || dy > 1)
            return false;

        bool isWateringCan = IsHoldingWateringCan();

        // 물뿌리개
        if (isWateringCan)
            return farmManager.CanWaterAt(worldPos);

        // 씨앗봉투
        string itemName =
            HeldItemManager.Instance.GetHeldItemName();

        if (string.IsNullOrEmpty(itemName))
            return false;

        // 현재 게임에서 사용하는 씨앗봉투 이름 기준
        if (!itemName.EndsWith("_seedBag"))
            return false;

        if (CropDataManager.Instance == null)
            return false;

        CropData data =
            CropDataManager.Instance
                .GetCropDataByItemName(itemName);

        if (data == null)
            return false;

        return farmManager.IsFarmTile(worldPos) &&
               !farmManager.HasPlantedAt(worldPos);
    }

    private bool IsHoldingWateringCan()
    {
        if (BoxInventoryManager.Instance != null &&
            BoxInventoryManager.Instance.IsHoldingWateringCan())
            return true;

        var held = HeldItemManager.Instance;

        if (held == null || !held.IsHoldingItem())
            return false;

        string itemName = held.GetHeldItemName();

        if (itemName == "village_object_wateringcan")
            return true;

        Sprite sprite = held.GetHeldItemSprite();

        return sprite != null &&
               sprite.name == "village_object_wateringcan";
    }

    private void RegisterHand()
    {
        if (handRegistered)
            return;

        if (GameCursorManager.Instance == null)
            return;

        GameCursorManager.Instance.EnterHandCursor(this);
        handRegistered = true;
    }

    private void UnregisterHand()
    {
        if (!handRegistered)
            return;

        handRegistered = false;

        if (GameCursorManager.Instance != null)
            GameCursorManager.Instance.ExitHandCursor(this);
    }

    private void OnDisable()
    {
        UnregisterHand();
    }

    private void OnDestroy()
    {
        UnregisterHand();
    }
}
