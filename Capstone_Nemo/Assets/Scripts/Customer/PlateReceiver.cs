using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateReceiver : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HeldDagwa"))
        {
            Debug.Log("�ٰ��� ���ÿ� �������ϴ�!");

            // ��� �ִ� ���� ����
            HeldItemManager.Instance.HideHeldItem();

            // �ٰ� ��ġ ����
            other.transform.position = transform.position + new Vector3(0, 0.3f, 0);
            other.transform.SetParent(transform); // ���ÿ� ����

            // ���ð� �մ԰� �����Ǿ� �ִٸ� ����
            PlateCheck plate = GetComponent<PlateCheck>();
            if (plate != null)
            {
                DagwaItem dagwa = other.GetComponent<DagwaItem>();
                if (dagwa != null)
                {
                    plate.SendDagwaToCustomer(dagwa.dagwaName);
                }
            }
        }
    }
}
