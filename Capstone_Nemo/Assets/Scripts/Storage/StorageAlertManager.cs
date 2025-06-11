using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StorageAlertManager : MonoBehaviour
{
    public static StorageAlertManager Instance;

    public GameObject alertIcon;  // �˸� ������ ("!")
    private bool hasNewItems = false;

    private HashSet<string> unseenItems = new(); // ���� â���� �� �� ���� ��Ȯ��

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// ��Ȯ�� �������� â��� �� ��, ���� �ð� �� �˸� ǥ��
    /// </summary>
    public void NotifyNewHarvestedItem(string itemName)
    {
        StartCoroutine(DelayedAlert(itemName));
    }

    private IEnumerator DelayedAlert(string itemName)
    {
        yield return new WaitForSeconds(1.2f);

        unseenItems.Add(itemName);  // ���� �� �� ���������� ���
        hasNewItems = true;

        if (alertIcon != null)
            alertIcon.SetActive(true);
    }

    /// <summary>
    /// â�� ������ ��, ��� ��Ȯ�� �������� "�� ������" ó��
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
