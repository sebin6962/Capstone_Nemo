using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateReceiver : MonoBehaviour
{
   private bool hasPlacedThisPress = false; // 누르고 있는 동안 중복 방지용

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("HeldDagwa"))
        {
            // E 키를 누르고 있고 아직 실행 안 했으면 수행
            if (Input.GetKey(KeyCode.E) && !hasPlacedThisPress)
            {
                Debug.Log("다과가 접시에 놓였습니다!");

                HeldItemManager.Instance.HideHeldItem();

                PlateCheck plate = GetComponent<PlateCheck>();
                if (plate != null)
                {
                    other.transform.position = transform.position + new Vector3(0, 0.3f, 0);
                    other.transform.SetParent(plate.transform);
                    other.tag = "Dagwa";

                    DagwaItem dagwa = other.GetComponent<DagwaItem>();
                    if (dagwa != null)
                    {
                        plate.SendDagwaToCustomer(dagwa.dagwaName);
                    }
                }

                hasPlacedThisPress = true; // 다시 누르지 않도록 설정
            }

            // E 키에서 손 뗐을 때 다시 허용
            if (!Input.GetKey(KeyCode.E))
            {
                hasPlacedThisPress = false;
            }
        }
    }
}
