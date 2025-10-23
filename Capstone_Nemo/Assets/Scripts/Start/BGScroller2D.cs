using UnityEngine;

public class BGScroller2D : MonoBehaviour
{
    [SerializeField] private Transform tileA;   // 첫 타일(왼쪽)
    [SerializeField] private Transform tileB;   // 둘째 타일(오른쪽)
    [SerializeField] private float unitsPerSec = 1f; // 왼쪽(-) 이동 속도
    [SerializeField] private bool moveLeft = true;

    Camera cam;
    float tileWidth;

    void Awake()
    {
        cam = Camera.main;
        if (!tileA || !tileB)
        {
            tileA = transform.GetChild(0);
            tileB = transform.GetChild(1);
        }

        var sr = tileA.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("tileA에 SpriteRenderer/Sprite가 필요합니다.");
            enabled = false; return;
        }
        tileWidth = sr.bounds.size.x;

        // 카메라 기준 중앙에 배치(플레이 시 갑자기 사라지는 증상 방지)
        Vector3 center = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        tileA.position = center - new Vector3(tileWidth * 0.5f, 0f, 0f);
        tileB.position = tileA.position + new Vector3(tileWidth, 0f, 0f);

        // 안전: Z=0 고정, 정렬 기본값
        tileA.position = new Vector3(tileA.position.x, tileA.position.y, 0f);
        tileB.position = new Vector3(tileB.position.x, tileB.position.y, 0f);
        SetDefaultRender(tileA);
        SetDefaultRender(tileB);
    }

    void SetDefaultRender(Transform t)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default"; // 필요시 "Background"
        sr.sortingOrder = 0;
        sr.material = null; // Sprites/Default
        if (sr.color.a < 0.99f) sr.color = new Color(1, 1, 1, 1);
    }

    void Update()
    {
        float dir = moveLeft ? -1f : 1f;
        Vector3 dx = new Vector3(unitsPerSec * dir * Time.deltaTime, 0f, 0f);

        tileA.position += dx;
        tileB.position += dx;

        // 루프(왼쪽 스크롤 기준)
        if (moveLeft)
        {
            if (tileA.position.x <= tileB.position.x - tileWidth)
                tileA.position = tileB.position + new Vector3(tileWidth, 0f, 0f);
            else if (tileB.position.x <= tileA.position.x - tileWidth)
                tileB.position = tileA.position + new Vector3(tileWidth, 0f, 0f);
        }
        else
        {
            if (tileA.position.x >= tileB.position.x + tileWidth)
                tileA.position = tileB.position - new Vector3(tileWidth, 0f, 0f);
            else if (tileB.position.x >= tileA.position.x + tileWidth)
                tileB.position = tileA.position - new Vector3(tileWidth, 0f, 0f);
        }
    }
}

