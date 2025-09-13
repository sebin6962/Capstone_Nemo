using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpriteSensor : MonoBehaviour
{
   /* public Sprite sprite;*/
    public LayerMask playerLayer;
    public SpriteRenderer spriteRenderer;

    private readonly HashSet<Collider2D> _inside = new();

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        spriteRenderer = GetComponentInParent<SpriteRenderer>();
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
            spriteRenderer.enabled = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_inside.Remove(other) && _inside.Count == 0)
        {
            spriteRenderer.enabled = false;
        }
    }

    void OnDisable()
    {
        if (_inside.Count > 0)
        {
            _inside.Clear();
            spriteRenderer.enabled = false;
        }
    }
}
