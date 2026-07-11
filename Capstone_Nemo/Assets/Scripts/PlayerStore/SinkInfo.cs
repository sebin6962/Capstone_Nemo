using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SinkInfo : MonoBehaviour
{
    [Header("진행바/캔버스")]
    public RectTransform progressBarPrefab;
    public Transform worldCanvasParent;

    [Header("물 아이템")]
    public Sprite waterSprite;
    public string waterItemName = "water";
    public GameObject waterResultPrefab;

    [Header("연출")]
    public float fillDuration = 1.5f;
    public Vector3 barOffset = new Vector3(0f, 1.2f, 0f);
    public Vector3 resultOffset = new Vector3(0f, 1.2f, 0f);

    private bool isRunning = false;
    [HideInInspector] public GameObject currentWaterObject;

    public bool IsRunning => isRunning;
    public bool HasWaterResult => currentWaterObject != null;

    // 싱크대 비주얼 이벤트
    public event System.Action SinkVisualStarted;
    public event System.Action SinkVisualEnded;

    private void SetSinkVisuals(bool active)
    {
        if (active)
            SinkVisualStarted?.Invoke();
        else
            SinkVisualEnded?.Invoke();
    }

    public IEnumerator FillAndGiveWater()
    {
        if (isRunning) yield break;

        isRunning = true;

        // 싱크대 스프라이트 애니메이션 시작
        SetSinkVisuals(true);

        SFXManager.Instance.PlayMakerProgressSFX("Sink");

        RectTransform progressBar = Instantiate(progressBarPrefab, worldCanvasParent);
        Vector3 worldPos = transform.position + barOffset;
        progressBar.position = worldPos;

        Transform fill = progressBar.transform.Find("Fill");
        if (fill == null)
        {
            Debug.LogError("[SinkInfo] 진행바 프리팹에 'Fill' 오브젝트가 없습니다!");
            SFXManager.Instance.StopMakerProgressSFX("Sink");

            if (progressBar != null)
                Destroy(progressBar.gameObject);

            // 싱크대 스프라이트 애니메이션 종료
            SetSinkVisuals(false);

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

        SFXManager.Instance.StopMakerProgressSFX("Sink");

        if (progressBar != null)
            Destroy(progressBar.gameObject);

        // 싱크대 스프라이트 애니메이션 종료
        SetSinkVisuals(false);

        if (currentWaterObject != null)
        {
            Destroy(currentWaterObject);
            currentWaterObject = null;
        }

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

    private void OnDisable()
    {
        SetSinkVisuals(false);
    }
}

