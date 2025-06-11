using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StorageAlertManager : MonoBehaviour
{
    public static StorageAlertManager Instance;

    public GameObject alertIcon;  // 알림 아이콘 ("!")
    private bool hasNewItems = false;

    private HashSet<string> unseenItems = new(); // 아직 창고에서 본 적 없는 수확물

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// 수확된 아이템이 창고로 들어간 후, 일정 시간 뒤 알림 표시
    /// </summary>
    public void NotifyNewHarvestedItem(string itemName)
    {
        StartCoroutine(DelayedAlert(itemName));
    }

    private IEnumerator DelayedAlert(string itemName)
    {
        yield return new WaitForSeconds(1.2f);

        unseenItems.Add(itemName);  // 아직 안 본 아이템으로 등록
        hasNewItems = true;

        if (alertIcon != null)
            alertIcon.SetActive(true);
    }

    /// <summary>
    /// 창고를 열었을 때, 모든 미확인 아이템을 "본 것으로" 처리
    /// </summary>
    public void OnStorageOpened()
    {
        hasNewItems = false;
        unseenItems.Clear();

        if (alertIcon != null)
            alertIcon.SetActive(false);
    }

    public bool HasUnseenItems() => hasNewItems;
}
