using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WorldHandCursor : MonoBehaviour
{
    private Collider2D targetCollider;
    private bool isRegistered;

    private void Awake()
    {
        targetCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (targetCollider == null)
            targetCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (targetCollider == null ||
            !targetCollider.enabled ||
            Camera.main == null)
        {
            UnregisterHandCursor();
            return;
        }

        Vector3 mouseWorldPosition =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        bool isMouseOver =
            targetCollider.OverlapPoint(
                new Vector2(
                    mouseWorldPosition.x,
                    mouseWorldPosition.y
                )
            );

        if (isMouseOver)
            RegisterHandCursor();
        else
            UnregisterHandCursor();
    }

    private void OnDisable()
    {
        UnregisterHandCursor();
    }

    private void OnDestroy()
    {
        UnregisterHandCursor();
    }

    private void RegisterHandCursor()
    {
        if (isRegistered)
            return;

        if (GameCursorManager.Instance == null)
            return;

        isRegistered = true;

        GameCursorManager.Instance.EnterHandCursor(this);
    }

    private void UnregisterHandCursor()
    {
        if (!isRegistered)
            return;

        isRegistered = false;

        if (GameCursorManager.Instance != null)
        {
            GameCursorManager.Instance.ExitHandCursor(this);
        }
    }
}