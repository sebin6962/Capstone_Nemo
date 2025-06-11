using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayerInteract : MonoBehaviour
{
    private GameObject heldObject;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldObject == null)
            {
                TryPickup();
            }
            else
            {
                PutDown();
            }
        }
    }

    void TryPickup()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Dagwa"))
            {
                heldObject = hit.gameObject;
                heldObject.tag = "HeldDagwa";
                heldObject.transform.SetParent(transform);
                heldObject.transform.localPosition = new Vector3(0, 1f, 0);
                Debug.Log("다과 들기: " + heldObject.name);
                return;
            }
        }
    }

    void PutDown()
    {
        heldObject.transform.SetParent(null);
        heldObject.tag = "Dagwa";
        Debug.Log("다과 내려놓음");
        heldObject = null;
    }
}
