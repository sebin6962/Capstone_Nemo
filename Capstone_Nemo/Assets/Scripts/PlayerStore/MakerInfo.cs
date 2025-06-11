using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MakerInfo : MonoBehaviour
{
    public string makerId;
    public GameObject currentResultObject; // ��� ������Ʈ ������
    public List<string> inputItemNames = new List<string>(4);
    public List<Sprite> inputItemSprites = new List<Sprite>(4);

    [Header("���� UI �ڵ����� ����")]
    public GameObject slotUIManagerPrefab;      // MakerSlotUI ������
    public MakerSlotUIManager slotUIManager;    // ���� ���� �� �����
    public Transform worldCanvasParent;         // ����ĵ����(���� �ϳ���, Inspector���� ����)

    [Header("���� ���� ����")]
    public RectTransform progressBarPrefab; // ����� ������
    public GameObject resultItemPrefab;     // ����� ������(��������Ʈ ������ �ʿ�)
    public Transform ProgressworldCanvasParent;     // ���� ĵ����(����ٿ�)

    // ����UI�� ������ �������� ����, �̹� ������ �״�� ���
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
        slotUIManager.transform.position = transform.position + new Vector3(0, 1.0f, 0); // y�� ����
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
            slotUIManager.gameObject.SetActive(false); // UI�� ��Ȱ��ȭ
        }
    }

    /// <summary>
    /// ���� �Ϸ�� ����� �������� ��� ����ϴ� �ڷ�ƾ (�����-�ϼ���)
    /// </summary>
    public IEnumerator ShowProgressAndSpawnItem(Sprite resultSprite, float duration = 3f)
    {
        // 1. ����� ������ �ν��Ͻ� ���� �� ��ġ ����
        RectTransform progressBar = Instantiate(progressBarPrefab, ProgressworldCanvasParent);
        Vector3 worldPos = transform.position + new Vector3(0f, 1.2f, 0f);
        progressBar.position = worldPos;

        // 2. Fill ������Ʈ ���� �� �ʱ�ȭ
        Transform fill = progressBar.transform.Find("Fill");
        if (fill == null)
        {
            Debug.LogError("����� �����տ� 'Fill' ������Ʈ�� �����ϴ�!");
            yield break;
        }
        Image fillImage = fill.GetComponent<Image>();
        fillImage.fillAmount = 0f;

        // 3. duration��ŭ ����� ä���
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fillImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        // 4. ����� �ı�
        Destroy(progressBar.gameObject);

        // 5. ����� ���� �� ����
        GameObject result = Instantiate(resultItemPrefab, worldPos, Quaternion.identity);
        SpriteRenderer sr = result.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sprite = resultSprite;
        else
            Debug.LogError("��� �����տ� SpriteRenderer�� �����ϴ�!");

        currentResultObject = result;

        Debug.Log($"[���۱�] ����� {resultSprite.name} ����");
    }

}
