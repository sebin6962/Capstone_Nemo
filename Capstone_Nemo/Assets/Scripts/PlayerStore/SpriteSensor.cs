using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpriteSensor : MonoBehaviour
{
    public LayerMask playerLayer;
    public SpriteRenderer spriteRenderer;

    [Header("제작대 아웃라인 옵션")]
    [SerializeField] private bool hideWhenMakerProducing = true;
    [SerializeField] private MakerInfo makerInfo;

    private readonly HashSet<Collider2D> _inside = new();
    private PlayerInteract _playerInteract;

    private bool usingCentral = false;

    // 중앙 시스템 또는 직접 감지에서 "켜라/꺼라" 요청한 상태 저장
    private bool requestedOutline = false;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInParent<SpriteRenderer>();

        // 제작대 자식에 SpriteSensor가 붙어 있으면 자동으로 부모 MakerInfo 찾기
        if (makerInfo == null)
            makerInfo = GetComponentInParent<MakerInfo>();

        SetOutline(false);
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        spriteRenderer = GetComponentInParent<SpriteRenderer>();
        makerInfo = GetComponentInParent<MakerInfo>();
    }

    void Update()
    {
        // 제작 중 상태가 중간에 바뀌어도 즉시 반영되게 함
        ApplyOutlineState();
    }

    public void SetOutline(bool isOn)
    {
        requestedOutline = isOn;
        ApplyOutlineState();
    }

    private void ApplyOutlineState()
    {
        if (spriteRenderer == null)
            return;

        if (!enabled)
        {
            spriteRenderer.enabled = false;
            return;
        }

        // 제작대이고, 제작 중이면 아웃라인 강제 OFF
        if (ShouldHideByMakerProducing())
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = requestedOutline;
    }

    private bool ShouldHideByMakerProducing()
    {
        if (!hideWhenMakerProducing)
            return false;

        if (makerInfo == null)
            return false;

        return makerInfo.isProducing;
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
        requestedOutline = false;

        if (_inside.Count > 0)
        {
            _inside.Clear();

            if (_playerInteract != null)
                _playerInteract.UnregisterSensor(this);
        }

        SetOutline(false);
    }
}
