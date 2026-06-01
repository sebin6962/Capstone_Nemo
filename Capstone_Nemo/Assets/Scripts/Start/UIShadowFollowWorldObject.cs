using UnityEngine;
using UnityEngine.UI;

public class UIShadowFollowWorldObject : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform targetObject;

    [Header("Camera")]
    [SerializeField] private Camera worldCamera;

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform groundRect;

    [Header("Offset")]
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, -20f);

    private RectTransform shadowRect;
    private Image shadowImage;

    private void Awake()
    {
        shadowRect = GetComponent<RectTransform>();
        shadowImage = GetComponent<Image>();
    }

    private void Start()
    {
        TryFindReferences();
    }

    private void OnEnable()
    {
        TryFindReferences();

        if (shadowImage != null)
            shadowImage.enabled = true;
    }

    private void LateUpdate()
    {
        TryFindReferences();

        if (targetObject == null || canvas == null || groundRect == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                $"[UIShadowFollowWorldObject] 참조 누락 - " +
                $"targetObject:{targetObject}, canvas:{canvas}, groundRect:{groundRect}",
                this
            );
#endif
            return;
        }

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        Vector2 screenPoint;

        // 월드 오브젝트일 경우
        if (worldCamera != null)
        {
            screenPoint = worldCamera.WorldToScreenPoint(targetObject.position);
        }
        // UI 오브젝트이거나 Overlay Canvas일 경우
        else
        {
            screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, targetObject.position);
        }

        screenPoint += screenOffset;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            groundRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint
        );

        shadowRect.anchoredPosition = localPoint;

        if (shadowImage != null && !shadowImage.enabled)
            shadowImage.enabled = true;
    }

    private void TryFindReferences()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (groundRect == null && transform.parent != null)
            groundRect = transform.parent.GetComponent<RectTransform>();

        if (worldCamera == null)
            worldCamera = Camera.main;
    }
}
