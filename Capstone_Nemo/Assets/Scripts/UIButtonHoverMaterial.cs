using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
    private Material _runtimeHoverMaterial;
    private bool _isHovering;

    private void Awake()
    {
        _button = GetComponent<Button>();
        ResolveTargetGraphic();

        if (targetGraphic != null)
        {
            _originalMaterial = targetGraphic.material;
        }

        // 공유 Material을 직접 쓰지 않고 버튼 전용 인스턴스로 사용
        if (hoverMaterial != null)
        {
            _runtimeHoverMaterial = Instantiate(hoverMaterial);
        }
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;

        // 씬 전환/재활성화 시 hover 상태가 남아있지 않도록 복구
        RestoreMaterial();
    }

    private void OnDisable()
    {
        RestoreMaterial();
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        RestoreMaterial();

        if (_runtimeHoverMaterial != null)
        {
            Destroy(_runtimeHoverMaterial);
        }
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        RestoreMaterial();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ResolveTargetGraphic();

        if (targetGraphic == null || hoverMaterial == null)
            return;

        if (ignoreWhenNotInteractable && _button != null && !_button.interactable)
        {
            RestoreMaterial();
            return;
        }

        _isHovering = true;
        targetGraphic.material = _runtimeHoverMaterial != null ? _runtimeHoverMaterial : hoverMaterial;
        targetGraphic.SetMaterialDirty();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        RestoreMaterial();
    }

    private void RestoreMaterial()
    {
        if (targetGraphic == null)
            return;

        _isHovering = false;
        targetGraphic.material = _originalMaterial;
        targetGraphic.SetMaterialDirty();
    }

    private void ResolveTargetGraphic()
    {
        if (targetGraphic != null)
            return;

        if (_button == null)
            _button = GetComponent<Button>();

        if (_button != null && _button.targetGraphic != null)
            targetGraphic = _button.targetGraphic;

        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();
    }
}
