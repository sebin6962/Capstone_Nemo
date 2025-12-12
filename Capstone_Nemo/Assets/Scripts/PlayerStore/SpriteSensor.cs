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
    private PlayerInteract _playerInteract;

    private bool usingCentral = false;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInParent<SpriteRenderer>();

        SetOutline(false);
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        spriteRenderer = GetComponentInParent<SpriteRenderer>();
    }

    public void SetOutline(bool isOn)
    {
        /* if (spriteRenderer != null)
             spriteRenderer.enabled = on;*/

        if (spriteRenderer == null)
            return;

        if (!enabled)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = isOn;
    }

    public Vector3 GetTargetPosition()
    {
        return spriteRenderer != null ? spriteRenderer.transform.position : transform.position;
    }

    bool IsPlayer(Collider2D c)
    {
        if (playerLayer.value != 0)
        {
            if (((1 << c.gameObject.layer) & playerLayer.value) != 0)
                return true;
        }
        return c.CompareTag("Player");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (_inside.Add(other))
        {
            //spriteRenderer.enabled = true;
            _playerInteract =
                   other.GetComponent<PlayerInteract>() ??
                   other.GetComponentInParent<PlayerInteract>() ??
                   FindFirstObjectByType<PlayerInteract>();

        }
        if (_playerInteract != null)
        {
            usingCentral = true;
            SetOutline(false);
            _playerInteract.RegisterSensor(this);
        }
        else
        {
            usingCentral = false;
            SetOutline(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        if (_inside.Remove(other) && _inside.Count == 0)
        {
            if (usingCentral && _playerInteract != null)
            {
                _playerInteract.UnregisterSensor(this);
            }
            else
            {
                SetOutline(false);
            }
        }
    }

    void OnDisable()
    {
        if (_inside.Count > 0)
        {
            _inside.Clear();
            //spriteRenderer.enabled = false;
            if (_playerInteract != null)
                _playerInteract.UnregisterSensor(this);
        }
        SetOutline(false);
    }
}
