using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHandCursor : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Button이 비활성화된 경우 손 커서를 표시하지 않음")]
    [SerializeField] private bool onlyWhenInteractable = true;

    private Selectable selectable;

    private bool isPointerInside;
    private bool isRegistered;

    private void Awake()
    {
        // Button, Toggle, Slider 등 Selectable 컴포넌트 탐색
        selectable = GetComponentInParent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        RefreshCursorRequest();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        RefreshCursorRequest();
    }

    private void Update()
    {
        if (!isPointerInside)
            return;

        // 마우스를 올린 상태에서 버튼의 interactable 값이
        // 변경되는 경우에도 바로 반영
        RefreshCursorRequest();
    }

    private void RefreshCursorRequest()
    {
        bool shouldUseHandCursor =
            isPointerInside && CanInteract();

        if (shouldUseHandCursor)
            RegisterHandCursor();
        else
            UnregisterHandCursor();
    }

    private bool CanInteract()
    {
        if (!onlyWhenInteractable)
            return true;

        // Button 등의 Selectable이 없는 일반 Image라면
        // 상호작용 가능한 UI로 취급
        if (selectable == null)
            return true;

        return selectable.IsInteractable();
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

    private void OnDisable()
    {
        isPointerInside = false;
        UnregisterHandCursor();
    }

    private void OnDestroy()
    {
        UnregisterHandCursor();
    }
}