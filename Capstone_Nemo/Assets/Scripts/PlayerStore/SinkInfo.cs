using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SinkInfo : MonoBehaviour
{
    [Header("진행바/캔버스")]
    public RectTransform progressBarPrefab;   // Maker에서 쓰는 것 재사용 가능 (Fill 이미지 포함)
    public Transform worldCanvasParent;       // 진행바를 붙일 월드 캔버스 (Maker의 ProgressworldCanvasParent와 동일 계열)

    [Header("물 아이템")]
    public Sprite waterSprite;                // 손에 표시할 ‘물’ 스프라이트
    public string waterItemName = "water";    // HeldItemManager에 전달될 아이템 이름

    [Header("연출")]
    public float fillDuration = 1.5f;         // 물 긷는 시간
    public Vector3 barOffset = new Vector3(0f, 1.2f, 0f);

    private bool isRunning = false;

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

        // 아직 빈손이라면 물을 손에 쥐어줌
        if (!HeldItemManager.Instance.IsHoldingItem())
        {
            HeldItemManager.Instance.ShowHeldItem(waterSprite, waterItemName);
            SFXManager.Instance.PlayBbyongSFX();
            Debug.Log("[SinkInfo] 물을 손에 들었습니다.");
        }
        else
        {
            Debug.Log("[SinkInfo] 진행 중에 다른 아이템을 들어서 물 지급을 건너뜀");
        }

        isRunning = false;
    }
}

