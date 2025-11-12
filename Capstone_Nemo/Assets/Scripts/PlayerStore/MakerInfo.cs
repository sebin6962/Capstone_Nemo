using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using System.Linq;

public class MakerInfo : MonoBehaviour
{
    public string makerId;
    public GameObject currentResultObject; // 결과 오브젝트 추적용
    public List<string> inputItemNames = new List<string>(4);
    public List<Sprite> inputItemSprites = new List<Sprite>(4);

    [Header("슬롯 UI 자동생성 관련")]
    public GameObject slotUIManagerPrefab;      // MakerSlotUI 프리팹
    public MakerSlotUIManager slotUIManager;    // 동적 생성 후 연결됨
    public Transform worldCanvasParent;         // 월드캔버스(씬에 하나만, Inspector에서 연결)

    [Header("제작 진행 관련")]
    public RectTransform progressBarPrefab; // 진행바 프리팹
    public GameObject resultItemPrefab;     // 결과물 프리팹(스프라이트 렌더러 필요)
    public Transform ProgressworldCanvasParent;     // 월드 캔버스(진행바용)

    [Header("Lock Visual")]
    public Color unlockedColor = Color.white;
    public Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    private bool _isLocked;

    //이펙트
    public GameObject craftCompleteEffect;
    private GameObject activeEffect;

    [Header("진행 저장용")]
    public bool isProducing;
    public string resultItemName;           // 결과 아이템 이름 (스프라이트 이름)
    public double craftEndUtcSeconds;       // 제작 종료 시각(초)

    // --- 여기부터 씬 전환 시 진행 상태 관련 ---
    public void StartCraft(Sprite resultSprite, float duration)
    {
        // 진행 상태 기록
        isProducing = true;
        resultItemName = resultSprite.name;

        double nowUtc = (System.DateTime.UtcNow - System.DateTime.UnixEpoch).TotalSeconds;
        craftEndUtcSeconds = nowUtc + duration;

        // 실제 진행바 코루틴 시작
        StartCoroutine(ShowProgressAndSpawnItem(resultSprite, duration));
    }

    // 제작이 정상적으로 끝났을 때 호출
    public void OnCraftFinished()
    {
        isProducing = false;
        craftEndUtcSeconds = 0;
        inputItemNames.Clear();
        inputItemSprites.Clear();

        // 슬롯 UI 비우기
        if (slotUIManager != null)
            slotUIManager.ClearSlots();

        var makerMgr = FindObjectOfType<MakerManager>();
        if (makerMgr != null)
            makerMgr.SaveMakerState();
    }


    // 슬롯UI가 없으면 동적으로 생성, 이미 있으면 그대로 사용
    public void EnsureSlotUIInstance()
    {
        if (slotUIManager == null)
        {
            GameObject slotUIObj = Instantiate(slotUIManagerPrefab, transform.position + new Vector3(0, 1.0f, 0), Quaternion.identity, worldCanvasParent);
            slotUIManager = slotUIObj.GetComponent<MakerSlotUIManager>();
            slotUIManager.gameObject.SetActive(false);
        }
    }

    public void ActivateSlotUI()
    {
        EnsureSlotUIInstance();
        slotUIManager.transform.position = transform.position + new Vector3(0, 1.0f, 0); // y값 조정
        slotUIManager.gameObject.SetActive(true);
    }

    public void DeactivateSlotUI()
    {
        if (slotUIManager != null && slotUIManager.gameObject.activeSelf)
        {
            slotUIManager.gameObject.SetActive(false);
        }
    }

    public void ClearAllSlots()
    {
        inputItemNames.Clear();
        inputItemSprites.Clear();
        if (slotUIManager != null)
        {
            slotUIManager.ClearSlots();
            slotUIManager.gameObject.SetActive(false); // UI도 비활성화
        }
    }

    /// <summary>
    /// 제작 완료시 결과물 생성까지 모두 담당하는 코루틴 (진행바-완성물)
    /// </summary>
    public IEnumerator ShowProgressAndSpawnItem(Sprite resultSprite, float duration = 3f)
    {
        // SFX 시작
        SFXManager.Instance.PlayMakerProgressSFX(makerId);

        // 1. 진행바 프리팹 인스턴스 생성 및 위치 지정
        RectTransform progressBar = Instantiate(progressBarPrefab, ProgressworldCanvasParent);
        Vector3 worldPos = transform.position + new Vector3(0f, 1.2f, 0f);
        progressBar.position = worldPos;

        // 2. Fill 오브젝트 참조 및 초기화
        Transform fill = progressBar.transform.Find("Fill");
        if (fill == null)
        {
            Debug.LogError("진행바 프리팹에 'Fill' 오브젝트가 없습니다!");
            SFXManager.Instance.StopMakerProgressSFX(); // 에러 시에도 멈추기
            yield break;
        }
        Image fillImage = fill.GetComponent<Image>();
        fillImage.fillAmount = 0f;

        // 3. duration만큼 진행바 채우기
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fillImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        // 진행바 끝났을 때 SFX 멈춤
        SFXManager.Instance.StopMakerProgressSFX();

        // 4. 진행바 파괴
        Destroy(progressBar.gameObject);

        //이펙트재생추가
        //if (craftCompleteEffect != null)
        //{
        //    if (activeEffect != null) Destroy(activeEffect);

        //    Vector3 effectPos = worldPos + new Vector3(0f, -1f, 0f);

        //    activeEffect = Instantiate(craftCompleteEffect, effectPos, Quaternion.identity);

        //    var ps = activeEffect.GetComponentInChildren<ParticleSystem>();
        //    if (ps != null)
        //        ps.Play();
        //}
        SpawnCompleteEffect();

        // 5. 결과물 생성 및 스폰
        GameObject result = Instantiate(resultItemPrefab, worldPos, Quaternion.identity);
        SpriteRenderer sr = result.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sprite = resultSprite;
        else
            Debug.LogError("결과 프리팹에 SpriteRenderer가 없습니다!");

        currentResultObject = result;

        Debug.Log($"[제작기] 결과물 {resultSprite.name} 생성");

        OnCraftFinished();
    }

    // 잠금 상태 조회 (PlayerInteract에서 참고)
    public bool IsLocked() => _isLocked;

    // 잠금/해제 시 비주얼 + 상호작용 동기화
    public void ApplyLockState(bool locked)
    {
        _isLocked = locked;

        // 1) 월드 오브젝트 색상(스프라이트) 틴트
        var srs = GetComponentsInChildren<SpriteRenderer>(includeInactive: true);
        foreach (var sr in srs)
            sr.color = locked ? lockedColor : unlockedColor;

        // 2) UI 이미지도 회색 처리(있다면)
        //var imgs = GetComponentsInChildren<Image>(includeInactive: true);
        //foreach (var img in imgs)
        //    img.color = locked ? lockedColor : unlockedColor;

        // 3) 상호작용 차단: 콜라이더 비활성
        var cols = GetComponentsInChildren<Collider2D>(includeInactive: true);
        foreach (var c in cols)
        {
            if (!c.isTrigger) continue;   // 비트리거는 손대지 않음
            c.enabled = !locked;          // 트리거만 잠금/해제
        }

        // 4) 슬롯 UI/진행바 등 표시 요소는 잠그면 감추기
        if (locked && slotUIManager != null)
            slotUIManager.ClearSlots();
    }


    public void KillActiveEffect(float delay = 0f)
    {
        if (activeEffect == null) return;

        var ps = activeEffect.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (delay > 0f) Destroy(activeEffect, delay);
        else Destroy(activeEffect);

        activeEffect = null;
    }

    public void SpawnCompleteEffect()
    {
        if (craftCompleteEffect == null) return;

        // 이전 이펙트가 남아 있으면 제거
        if (activeEffect != null)
            Destroy(activeEffect);

        // 진행바와 같은 기준 위치에서 살짝 아래쪽
        Vector3 worldPos = transform.position + new Vector3(0f, 1.2f, 0f);
        Vector3 effectPos = worldPos + new Vector3(0f, -1f, 0f);

        activeEffect = Instantiate(craftCompleteEffect, effectPos, Quaternion.identity);

        var ps = activeEffect.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
            ps.Play();
    }
}


