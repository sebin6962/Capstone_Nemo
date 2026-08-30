using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class GameLanguageManager : MonoBehaviour
{
    private const string LanguageSaveKey = "SelectedLanguage";

    private IEnumerator Start()
    {
        // Localization 초기화가 완료될 때까지 기다림
        yield return LocalizationSettings.InitializationOperation;

        // 이전에 사용자가 선택한 언어 불러오기
        if (PlayerPrefs.HasKey(LanguageSaveKey))
        {
            string savedLanguage =
                PlayerPrefs.GetString(LanguageSaveKey);

            ApplyLanguage(savedLanguage, false);
        }
    }

    public void SetKorean()
    {
        ApplyLanguage("ko", true);
    }

    public void SetEnglish()
    {
        ApplyLanguage("en", true);
    }

    private void ApplyLanguage(string languageCode, bool save)
    {
        Locale locale =
            LocalizationSettings.AvailableLocales.GetLocale(
                new LocaleIdentifier(languageCode)
            );

        if (locale == null)
        {
            Debug.LogWarning(
                $"지원하지 않는 언어입니다: {languageCode}"
            );

            return;
        }

        LocalizationSettings.SelectedLocale = locale;

        if (save)
        {
            PlayerPrefs.SetString(
                LanguageSaveKey,
                languageCode
            );

            PlayerPrefs.Save();
        }

        Debug.Log($"게임 언어 변경: {languageCode}");
    }
}