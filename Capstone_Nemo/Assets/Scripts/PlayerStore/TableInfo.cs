using UnityEngine;

public class TableInfo : MonoBehaviour
{
    [Header("필수")]
    public Transform itemSpot;                 // 이미 쓰는 자리
    [HideInInspector] public GameObject currentPlacedObject;

    [Header("초기 아이템")]
    public bool spawnInitialItemOnStart = true;
    public string initialItemSpriteName = ""; // YakgwaMold
    public string spriteResourceDir = "Sprites/Ingredients/"; // 프로젝트 구조에 맞게
    public Vector3 initialScale = new Vector3(1f, 1f, 1f);
    public string sortingLayerName = "Obj";
    public int sortingOrder = 11;

    [Header("해금 조건")]
    [Tooltip("이 레벨 이상일 때만 초기 아이템을 테이블 위에 올림")]
    public int requiredLevelForInitialItem = 10;

    void Start()
    {
        if (spawnInitialItemOnStart && !string.IsNullOrEmpty(initialItemSpriteName) && IsLevelEnoughForInitialItem())
        {
            TrySpawnInitialItem();
        }
    }

    private bool IsLevelEnoughForInitialItem()
    {
        // 레벨 조건이 1 이하면 항상 허용
        if (requiredLevelForInitialItem <= 1) return true;

        if (PlayerLevelManager.Instance == null)
        {
            Debug.LogWarning("[TableInfo] PlayerLevelManager.Instance 가 없어 초기 아이템 스폰을 건너뜁니다.");
            return false;
        }

        return PlayerLevelManager.Instance.Level >= requiredLevelForInitialItem;
    }

    public bool TrySpawnInitialItem()
    {
        // 레벨 미달이면 스폰하지 않음
        if (!IsLevelEnoughForInitialItem())
            return false;

        if (currentPlacedObject != null) return false; // 이미 올라가 있으면 스킵

        Sprite spr = Resources.Load<Sprite>(spriteResourceDir + initialItemSpriteName);
        if (spr == null)
        {
            Debug.LogWarning($"[TableInfo] 초기 스프라이트 로드 실패: {spriteResourceDir}{initialItemSpriteName}");
            return false;
        }

        GameObject go = new GameObject("TableItem");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        go.transform.SetParent(itemSpot, worldPositionStays: false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = initialScale;

        currentPlacedObject = go;
        return true;
    }
}


