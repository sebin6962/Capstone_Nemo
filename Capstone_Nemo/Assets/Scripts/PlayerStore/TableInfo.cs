using UnityEngine;

public class TableInfo : MonoBehaviour
{
    [Header("필수")]
    public Transform itemSpot;                 // 이미 쓰는 자리
    [HideInInspector] public GameObject currentPlacedObject;

    [Header("초기 아이템(선택)")]
    public bool spawnInitialItemOnStart = true;
    public string initialItemSpriteName = ""; // 예: "YakgwaMold"
    public string spriteResourceDir = "Sprites/Ingredients/"; // 프로젝트 구조에 맞게
    public Vector3 initialScale = new Vector3(1f, 1f, 1f);
    public string sortingLayerName = "Obj";
    public int sortingOrder = 15;

    void Start()
    {
        if (spawnInitialItemOnStart && !string.IsNullOrEmpty(initialItemSpriteName))
        {
            TrySpawnInitialItem();
        }
    }

    public bool TrySpawnInitialItem()
    {
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


