using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class OutLineSensor : MonoBehaviour
{
    public OutLine outline;     
    public LayerMask playerLayer;     

    private readonly HashSet<Collider2D> _inside = new();

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; 
        outline = GetComponentInParent<OutLine>();
    }

    bool IsPlayer(Collider2D c)
    {
        return ((1 << c.gameObject.layer) & playerLayer.value) != 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_inside.Add(other))
        {
            outline?.SetOutLine(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_inside.Remove(other) && _inside.Count == 0)
        {
            outline?.SetOutLine(false);
        }
    }

    void OnDisable()
    {
        if (_inside.Count > 0)
        {
            _inside.Clear();
            outline?.SetOutLine(false);
        }
    }
}
