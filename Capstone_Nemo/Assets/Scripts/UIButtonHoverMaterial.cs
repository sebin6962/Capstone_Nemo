using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class UIButtonHoverMaterial : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Target")]
    [SerializeField] private Graphic targetGraphic;

    [Header("Hover Material")]
    [SerializeField] private Material hoverMaterial;

    [Header("Option")]
    [SerializeField] private bool ignoreWhenNotInteractable = true;

    private Button _button;
    private Material _originalMaterial;

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (targetGraphic == null)
        {
            if (_button != null && _button.targetGraphic != null)
                targetGraphic = _button.targetGraphic as Graphic;

            if (targetGraphic == null)
                targetGraphic = GetComponent<Graphic>();
        }

        if (targetGraphic != null)
            _originalMaterial = targetGraphic.material;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetGraphic == null || hoverMaterial == null)
            return;

        if (ignoreWhenNotInteractable && _button != null && !_button.interactable)
            return;

        targetGraphic.material = hoverMaterial;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreMaterial();
    }

    private void OnDisable()
    {
        RestoreMaterial();
    }

    private void OnDestroy()
    {
        RestoreMaterial();
    }

    private void RestoreMaterial()
    {
        if (targetGraphic == null)
            return;

        targetGraphic.material = _originalMaterial;
    }
}
