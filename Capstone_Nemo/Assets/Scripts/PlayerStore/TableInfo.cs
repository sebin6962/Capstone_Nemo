using UnityEngine;

public class TableInfo : MonoBehaviour
{
    [Header("ID")]
    [Tooltip("세이브/로드용 테이블 고유 ID")]
    public string tableId;

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

    [Header("잠금 상태(틀 생성 테이블)")]
    [Tooltip("레벨에 따라 테이블 상호작용 잠금 사용 여부")]
    public bool lockByLevel = false;

    public Color unlockedColor = Color.white;
    public Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    private bool _isLocked;
    public bool IsLocked() => _isLocked;

    [Header("2D Light Material")]
    [SerializeField] private Material spriteLitMaterial;

    public void ApplyLockState(bool locked)
    {
        _isLocked = locked;

        var srs = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        foreach (var sr in srs)
        {
            sr.color = locked ? lockedColor : unlockedColor;
        }

        var cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
        foreach (var c in cols)
        {
            if (!c.isTrigger) continue;
            c.enabled = !locked;
        }
    }

    void Start()
    {
        if (lockByLevel)
        {
            bool levelEnough = IsLevelEnoughForInitialItem(); // requiredLevelForInitialItem 기준
            ApplyLockState(!levelEnough);  // 레벨 부족이면 locked = true
        }

        //if (spawnInitialItemOnStart && !string.IsNullOrEmpty(initialItemSpriteName) && IsLevelEnoughForInitialItem())
        //{
        //    TrySpawnInitialItem();
        //}
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

        int maxAppliedLevel = UnlockManager.Instance.GetMaxAppliedLevel();
        return maxAppliedLevel >= requiredLevelForInitialItem;
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
        //sr.sortingOrder = sortingOrder;
        sr.sortingOrder = 60;

        // 추가: Global Light 2D 적용용 Material 지정
        if (spriteLitMaterial != null)
        {
            sr.sharedMaterial = spriteLitMaterial;
        }
        else
        {
            Debug.LogWarning("[TableInfo] spriteLitMaterial이 비어 있습니다. Sprite-Lit-Default Material을 연결하세요.");
        }


        go.transform.SetParent(itemSpot, worldPositionStays: false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = initialScale;

        currentPlacedObject = go;
        return true;
    }
}


