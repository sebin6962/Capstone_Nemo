using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class LocalizedTMPFontSize : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private TMP_Text targetText;

    [Header("Font Size By Language")]
    [SerializeField, Min(1f)] private float koreanFontSize = 36f;
    [SerializeField, Min(1f)] private float englishFontSize = 30f;

    private Coroutine initializeRoutine;

    private void Reset()
    {
        targetText = GetComponent<TMP_Text>();
    }

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

        if (initializeRoutine != null)
            StopCoroutine(initializeRoutine);

        initializeRoutine = StartCoroutine(ApplyAfterLocalizationInitialized());
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;

        if (initializeRoutine != null)
        {
            StopCoroutine(initializeRoutine);
            initializeRoutine = null;
        }
    }

    private IEnumerator ApplyAfterLocalizationInitialized()
    {
        yield return LocalizationSettings.InitializationOperation;

        ApplyFontSize(LocalizationSettings.SelectedLocale);
        initializeRoutine = null;
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        ApplyFontSize(locale);
    }

    private void ApplyFontSize(Locale locale)
    {
        if (targetText == null)
            return;

        string localeCode = locale != null
            ? locale.Identifier.Code
            : "ko";

        bool isEnglish = localeCode.StartsWith(
            "en",
            System.StringComparison.OrdinalIgnoreCase
        );

        float size = isEnglish
            ? englishFontSize
            : koreanFontSize;

        if (targetText.enableAutoSizing)
        {
            targetText.fontSizeMax = size;

            if (targetText.fontSizeMin > size)
                targetText.fontSizeMin = size;
        }
        else
        {
            targetText.fontSize = size;
        }

        targetText.SetLayoutDirty();
    }
}
