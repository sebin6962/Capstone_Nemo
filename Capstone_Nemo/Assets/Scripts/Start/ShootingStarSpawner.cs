using UnityEngine;
using System.Collections.Generic;

public class ShootingStarSpawner : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform spawnParent;   // 별을 붙일 부모(보통 캔버스 또는 전용 컨테이너)
    public ShootingStar prefab;
    public RectTransform moonPanel;

    [Header("Spawn Timing (Less Frequent)")]
    public float spawnIntervalMin = 2.0f; // 간격을 넓혀 빈도↓
    public float spawnIntervalMax = 4.0f;
    [Range(0f, 1f)]
    public float spawnChance = 0.6f;

    [Header("Concurrency Limit")]
    public int maxConcurrent = 1;         // 동시에 존재할 최대 별똥별 수

    [Header("Movement")]
    [Tooltip("x는 항상 왼쪽(-), y는 아래(-). 예) (-1,-0.45) 정도를 정규화하여 사용")]
    public Vector2 baseDirection = new Vector2(-0.4f, -1f);
    public float speedMin = 220f;     // px/sec
    public float speedMax = 420f;

    [Header("Spawn Area")]
    public float topMarginPx = 40f;          // 화면 위쪽 바깥 시작 여유
    public float leftRightPaddingPx = 60f;   // 좌우 너무 끝에 붙지 않도록 패딩
    public float angleJitterDeg = 10f;       // ±각도 난수(자연스러운 퍼짐)

    [Header("Pooling")]
    public int prewarmCount = 6;
    public int maxPool = 16;

    [Header("Star Sprites")]
    public Sprite[] starSprites = new Sprite[4];   // 4개 스프라이트를 순서 상관없이 넣어두기
    public bool randomSpriteEverySpawn = true;     // true면 스폰마다 랜덤

    readonly Queue<ShootingStar> pool = new Queue<ShootingStar>();
    readonly HashSet<ShootingStar> active = new HashSet<ShootingStar>();
    float nextSpawnAt;

    bool isSpawning = false;

    RectTransform canvasRT;

    void Awake()
    {
        if (spawnParent == null) spawnParent = GetComponent<RectTransform>();
        var canvas = GetComponentInParent<Canvas>();
        canvasRT = canvas != null ? canvas.GetComponent<RectTransform>() : null;

        // 달 패널 아래로 레이어 배치 (같은 부모 하에서 달 패널 바로 아래 인덱스)
        if (moonPanel != null && spawnParent != null)
        {
            // spawnParent를 moonPanel과 같은 부모로 옮기고, 전체 스트레치
            spawnParent.SetParent(moonPanel.parent, false);
            spawnParent.anchorMin = Vector2.zero;
            spawnParent.anchorMax = Vector2.one;
            spawnParent.offsetMin = Vector2.zero;
            spawnParent.offsetMax = Vector2.zero;

            int moonIdx = moonPanel.GetSiblingIndex();
            int targetIdx = Mathf.Max(0, moonIdx - 1); // 달 패널 '바로 아래'
            spawnParent.SetSiblingIndex(targetIdx);
        }

        // 풀 미리 채우기
        for (int i = 0; i < prewarmCount; i++)
            pool.Enqueue(CreateOne());
    }

    //void OnEnable()
    //{
    //    ScheduleNext();
    //}

    void Update()
    {
        // 타이틀 연출이 끝나기 전에는 별똥별을 생성하지 않음
        if (!isSpawning)
            return;

        if (Time.time >= nextSpawnAt)
        {
            SpawnOne();
            ScheduleNext();
        }
    }

    public void StartSpawning()
    {
        // 중복 실행 방지
        if (isSpawning)
            return;

        isSpawning = true;
        ScheduleNext();
    }

    void ScheduleNext()
    {
        float interval = Random.Range(spawnIntervalMin, spawnIntervalMax);
        nextSpawnAt = Time.time + interval;
    }

    ShootingStar CreateOne()
    {
        var star = Instantiate(prefab, spawnParent);
        star.gameObject.SetActive(false);
        star.OnDespawn = Despawn;
        return star;
    }

    void SpawnOne()
    {
        if (canvasRT == null || prefab == null) return;

        // 최대 동시 개수 제한
        if (active.Count >= maxConcurrent) return;

        // 확률로 스킵하여 빈도 미세 조정
        if (Random.value > spawnChance) return;

        // 캔버스 기준 범위
        float halfW = canvasRT.rect.width * 0.5f;
        float halfH = canvasRT.rect.height * 0.5f;

        // 시작 위치: 오른쪽 화면 밖, Y는 패딩 내 랜덤
        float startY = halfH + topMarginPx;
        float startX = Random.Range(-halfW + leftRightPaddingPx, halfW - leftRightPaddingPx);

        // 방향/속도 랜덤(±10도 각도 흔들림)
        Vector2 dir = baseDirection.normalized;
        float deg = Random.Range(-10f, 10f);
        float rad = deg * Mathf.Deg2Rad;
        Vector2 dirJitter = new Vector2(
            dir.x * Mathf.Cos(rad) - dir.y * Mathf.Sin(rad),
            dir.x * Mathf.Sin(rad) + dir.y * Mathf.Cos(rad)
        ).normalized;
        float speed = Random.Range(speedMin, speedMax);

        // 인스턴스
        var star = (pool.Count > 0) ? pool.Dequeue() : CreateOne();
        star.gameObject.SetActive(true);

        // 스프라이트 랜덤 지정
        if (randomSpriteEverySpawn && starSprites != null && starSprites.Length > 0)
        {
            int idx = Random.Range(0, starSprites.Length);
            star.SetSprite(starSprites[idx]);   // <- ShootingStar.cs에 추가한 세터 사용
        }

        // 달 패널 아래 레이어를 유지하기 위해, 부모/순서 정리(에지 케이스 방지)
        if (moonPanel != null)
        {
            if (star.transform.parent != spawnParent) star.transform.SetParent(spawnParent, false);
            int moonIdx = moonPanel.GetSiblingIndex();
            int targetIdx = Mathf.Max(0, moonIdx - 1);
            spawnParent.SetSiblingIndex(targetIdx); // 혹시 다른 코드가 순서를 바꿨을 경우 재보정
        }

        star.transform.SetAsLastSibling(); // 같은 레이어 내에서 제일 위(여전히 달 패널 아래 레이어)

        star.Init(new Vector2(startX, startY), dirJitter, speed);
        active.Add(star);
    }

    void Despawn(ShootingStar star)
    {
        if (active.Contains(star)) active.Remove(star);
        star.gameObject.SetActive(false);
        if (pool.Count < maxPool) pool.Enqueue(star);
        else Destroy(star.gameObject);
    }

    public void StopSpawning(bool hideActiveStars = true)
    {
        isSpawning = false;

        if (!hideActiveStars)
            return;

        // 현재 화면에 날아가고 있는 별똥별도 모두 회수
        List<ShootingStar> starsToHide =
            new List<ShootingStar>(active);

        foreach (ShootingStar star in starsToHide)
        {
            if (star != null)
                Despawn(star);
        }
    }
}
