using UnityEngine;
using UnityEngine.UI;

public class ToggleSprite : MonoBehaviour
{
    [SerializeField] private Image targetGraphic; 
    [SerializeField] private Sprite onSprite;     
    [SerializeField] private Sprite offSprite;   

    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(UpdateVisual);
        UpdateVisual(toggle.isOn);
    }

    private void UpdateVisual(bool isOn)
    {
        if (targetGraphic == null) return;
        targetGraphic.sprite = isOn ? onSprite : offSprite;
    }
}