using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SinkInfo : MonoBehaviour
{
    [Header("진행바/캔버스")]
    public RectTransform progressBarPrefab;   // Maker에서 쓰는 것 재사용 가능 (Fill 이미지 포함)
    public Transform worldCanvasParent;       // 진행바를 붙일 월드 캔버스 (Maker의 ProgressworldCanvasParent와 동일 계열)

    [Header("물 아이템")]
    public Sprite waterSprite;                // 결과 오브젝트에 표시할 ‘물’ 스프라이트
    public string waterItemName = "water";    // HeldItemManager에 전달될 아이템 이름
    public GameObject waterResultPrefab;      // 싱크 위에 생성될 물 결과 프리팹(분무기, 물컵 등 스프라이트 렌더러 포함)

    [Header("연출")]
    public float fillDuration = 1.5f;         // 물 긷는 시간
    public Vector3 barOffset = new Vector3(0f, 1.2f, 0f);
    public Vector3 resultOffset = new Vector3(0f, 1.2f, 0f); // 물 결과물 위치 오프셋

    // 진행 중 여부/결과 오브젝트
    private bool isRunning = false;
    [HideInInspector] public GameObject currentWaterObject;

    // PlayerInteract에서 읽기용 프로퍼티
    public bool IsRunning => isRunning;
    public bool HasWaterResult => currentWaterObject != null;

    public IEnumerator FillAndGiveWater()
    {
        if (isRunning) yield break; // 연타 방지
        isRunning = true;

        // (선택) 제작 진행 SFX 재사용
        SFXManager.Instance.PlayMakerProgressSFX("sink");

        // 진행바 생성
        RectTransform progressBar = Instantiate(progressBarPrefab, worldCanvasParent);
        Vector3 worldPos = transform.position + barOffset;
        progressBar.position = worldPos;

        // Fill 이미지 채우기
        Transform fill = progressBar.transform.Find("Fill");
        if (fill == null)
        {
            Debug.LogError("[SinkInfo] 진행바 프리팹에 'Fill' 오브젝트가 없습니다!");
            SFXManager.Instance.StopMakerProgressSFX();
            Destroy(progressBar.gameObject);
            isRunning = false;
            yield break;
        }

        Image fillImage = fill.GetComponent<Image>();
        fillImage.fillAmount = 0f;

        float elapsed = 0f;
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            fillImage.fillAmount = Mathf.Clamp01(elapsed / fillDuration);
            yield return null;
        }

        SFXManager.Instance.StopMakerProgressSFX();
        Destroy(progressBar.gameObject);

        // 이미 결과물이 있으면 하나만 유지
        if (currentWaterObject != null)
        {
            Destroy(currentWaterObject);
            currentWaterObject = null;
        }

        // 싱크 위에 결과 오브젝트 스폰 (제작기 결과랑 같은 느낌)
        Vector3 resultPos = transform.position + resultOffset;
        GameObject resultObj = Instantiate(waterResultPrefab, resultPos, Quaternion.identity);

        SpriteRenderer sr = resultObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = waterSprite;
        }
        else
        {
            Debug.LogError("[SinkInfo] waterResultPrefab에 SpriteRenderer가 없습니다!");
        }

        currentWaterObject = resultObj;
        Debug.Log("[SinkInfo] 싱크 위에 물 결과물을 생성했습니다.");

        isRunning = false;
    }

    /// <summary>
    /// 싱크 위에 생성된 물 결과물을 플레이어 손으로 옮김
    /// (제작기 currentResultObject 줍는 로직과 동일한 패턴)
    /// </summary>
    public void PickupWaterResult()
    {
        if (currentWaterObject == null)
        {
            Debug.Log("[SinkInfo] 줍기 시도했지만 currentWaterObject가 없습니다.");
            return;
        }

        if (HeldItemManager.Instance.IsHoldingItem())
        {
            Debug.Log("[SinkInfo] 이미 다른 아이템을 들고 있어 물을 집을 수 없습니다.");
            return;
        }

        SpriteRenderer sr = currentWaterObject.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            HeldItemManager.Instance.ShowHeldItem(sr.sprite, waterItemName);
            SFXManager.Instance.PlayBbyongSFX();
            Debug.Log("[SinkInfo] 싱크 위에 있던 물을 손에 들었습니다.");
        }
        else
        {
            Debug.LogError("[SinkInfo] currentWaterObject에 SpriteRenderer가 없습니다.");
        }

        Destroy(currentWaterObject);
        currentWaterObject = null;
    }

    public void ClearWaterResult()
    {
        if (currentWaterObject != null)
        {
            Destroy(currentWaterObject);
            currentWaterObject = null;
        }
    }
}

