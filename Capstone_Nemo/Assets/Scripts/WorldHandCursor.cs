using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WorldHandCursor : MonoBehaviour
{
    private bool isRegistered;

    private void OnMouseEnter()
    {
        RegisterHandCursor();
    }

    private void OnMouseExit()
    {
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
