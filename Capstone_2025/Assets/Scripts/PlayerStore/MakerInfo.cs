using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        // 1. 진행바 프리팹 인스턴스 생성 및 위치 지정
        RectTransform progressBar = Instantiate(progressBarPrefab, ProgressworldCanvasParent);
        Vector3 worldPos = transform.position + new Vector3(0f, 1.2f, 0f);
        progressBar.position = worldPos;

        // 2. Fill 오브젝트 참조 및 초기화
        Transform fill = progressBar.transform.Find("Fill");
        if (fill == null)
        {
            Debug.LogError("진행바 프리팹에 'Fill' 오브젝트가 없습니다!");
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

        // 4. 진행바 파괴
        Destroy(progressBar.gameObject);

        // 5. 결과물 생성 및 스폰
        GameObject result = Instantiate(resultItemPrefab, worldPos, Quaternion.identity);
        SpriteRenderer sr = result.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sprite = resultSprite;
        else
            Debug.LogError("결과 프리팹에 SpriteRenderer가 없습니다!");

        currentResultObject = result;

        Debug.Log($"[제작기] 결과물 {resultSprite.name} 생성");
    }

}
