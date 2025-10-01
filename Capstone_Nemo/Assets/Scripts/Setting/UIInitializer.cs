using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIInitializer : MonoBehaviour
{
    [SerializeField] private CanvasScaler canvasScaler;

    void Start()
    {
        if (SettingsManager.Instance != null)
        {
            ApplySettings(SettingsManager.Instance.UIScale);
        }
    }

    public void ApplySettings()
    {
        float userScale = Mathf.Clamp(SettingsManager.Instance.UIScale, 0.75f, 1.15f);
        ApplySettings(userScale);
    }

    public void ApplySettings(float userScale)
    {
        userScale = Mathf.Clamp(SettingsManager.Instance.UIScale, 0.75f, 1.15f);

        Vector2 baseResolution = new Vector2(1920, 1080);
        canvasScaler.referenceResolution = baseResolution / userScale;
        Debug.Log($"[UIInitializer] Àû¿ëµÈ scaleFactor: {canvasScaler.scaleFactor}");
    }
}
