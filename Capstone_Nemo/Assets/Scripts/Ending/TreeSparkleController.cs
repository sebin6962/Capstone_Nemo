using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeSparkleController : MonoBehaviour
{
    [Header("한 번 반짝이는 파티클 프리팹")]
    public ParticleSystem sparklePrefab;

    [Header("반짝일 영역 크기 (나무 기준)")]
    public Vector2 areaSize = new Vector2(2f, 3f);

    [Header("반짝이 생성 간격(초)")]
    public float spawnIntervalMin = 0.1f;
    public float spawnIntervalMax = 0.5f;

    [Header("카메라에 보일 때만 반짝이게 할지 여부")]
    public bool onlyWhenVisible = true;

    private SpriteRenderer spriteRenderer;
    private float timer = 0f;
    private float nextInterval = 0.2f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ResetTimer();
    }

    void ResetTimer()
    {
        nextInterval = Random.Range(spawnIntervalMin, spawnIntervalMax);
        timer = 0f;
    }

    void Update()
    {
        // 카메라에 안 보일 때는 안 돌도록
        if (onlyWhenVisible && spriteRenderer != null && !spriteRenderer.isVisible)
            return;

        if (sparklePrefab == null)
            return;

        timer += Time.deltaTime;
        if (timer >= nextInterval)
        {
            SpawnSparkle();
            ResetTimer();
        }
    }

    void SpawnSparkle()
    {
        // 나무를 기준으로 지정한 사각형 안에서 랜덤 위치
        float halfX = areaSize.x * 0.5f;
        float halfY = areaSize.y * 0.5f;

        Vector2 randomOffset = new Vector2(
            Random.Range(-halfX, halfX),
            Random.Range(-halfY, halfY)
        );

        Vector3 spawnPos = transform.position + (Vector3)randomOffset;

        // 나무의 자식으로 생성하면 같이 움직이고 정리도 편함
        ParticleSystem ps = Instantiate(sparklePrefab, spawnPos, Quaternion.identity, transform);
        ps.Play();

        // 파티클이 끝나면 자동 삭제
        var main = ps.main;
        float life = main.duration + main.startLifetimeMultiplier;
        Destroy(ps.gameObject, life);
    }

    // 편집기에서 영역 보이게
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position,
            new Vector3(areaSize.x, areaSize.y, 0f));
    }
}
