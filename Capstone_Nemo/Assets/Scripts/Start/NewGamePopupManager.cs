using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.IO;

public class NewGamePopupManager : MonoBehaviour
{
    public TMP_InputField inputServerName;
    public Button btnCreate, btnCancel;

    [Header("이름 길이 초과 시 팝업 패널")]
    public GameObject nameTooLongPanel; 
    public CanvasGroup nameTooLongGroup; 
    private Coroutine nameTooLongCo = null;

    private bool suppressWarnings = false;

    void OnEnable()
    {
        suppressWarnings = false;

        // 경고 패널 초기 상태
        if (nameTooLongPanel) nameTooLongPanel.SetActive(false);
        if (nameTooLongGroup) nameTooLongGroup.alpha = 0f;

        if (inputServerName != null)
        {
            inputServerName.characterLimit = 6;

            // 입력 초기화 (문자/캐럿/선택 모두 리셋)
            inputServerName.text = "";
            inputServerName.caretPosition = 0;
            inputServerName.selectionStringAnchorPosition = 0;
            inputServerName.selectionStringFocusPosition = 0;
            inputServerName.DeactivateInputField();

            // Validate 핸들러 재연결
            inputServerName.onValidateInput -= ValidateNameChar;
            inputServerName.onValidateInput += ValidateNameChar;

        }

        // 경고 패널 리셋
        if (nameTooLongPanel) nameTooLongPanel.SetActive(false);
        if (nameTooLongGroup) nameTooLongGroup.alpha = 0f;
    }

    void OnDisable()
    {
        // 혹시 남은 것들 철거 (이중 안전망)
        if (inputServerName != null)
            inputServerName.onValidateInput -= ValidateNameChar;

        if (nameTooLongCo != null) { StopCoroutine(nameTooLongCo); nameTooLongCo = null; }
        if (nameTooLongGroup) nameTooLongGroup.alpha = 0f;
        if (nameTooLongPanel) nameTooLongPanel.SetActive(false);

        suppressWarnings = false;
    }

    void Start()
    {
        // 6글자 제한
        if (inputServerName != null)
        {
            inputServerName.characterLimit = 6;
            inputServerName.onValidateInput += ValidateNameChar;
        }

        btnCancel.onClick.AddListener(() =>
        {
            suppressWarnings = true;

            // 2) Validate 해제 (이후 동작 중 onValidateInput이 끼어들지 못하게)
            if (inputServerName != null)
                inputServerName.onValidateInput -= ValidateNameChar;

            // 3) 경고 연출 강제 종료
            if (nameTooLongCo != null) { StopCoroutine(nameTooLongCo); nameTooLongCo = null; }
            if (nameTooLongGroup) nameTooLongGroup.alpha = 0f;
            if (nameTooLongPanel) nameTooLongPanel.SetActive(false);

            // 4) 입력 완전 초기화 + 포커스 해제
            if (inputServerName != null)
            {
                inputServerName.DeactivateInputField();
                inputServerName.SetTextWithoutNotify("");
                inputServerName.caretPosition = 0;
                inputServerName.selectionStringAnchorPosition = 0;
                inputServerName.selectionStringFocusPosition = 0;
            }

            // 5) 패널 닫기
            gameObject.SetActive(false);
        });

        btnCreate.onClick.AddListener(() =>
        {
            if (SFXManager.Instance != null)
                SFXManager.Instance.PlayBtnClickSFX();

            string serverName = inputServerName.text.Trim();
            if (string.IsNullOrEmpty(serverName)) return;

            if (serverName.Length > 6)
            {
                inputServerName.text = serverName.Substring(0, 6);
                //ShowNameTooLong();
                return;
            }

            string profilePath = Application.persistentDataPath + "/profile_myuser.json";
            Profile profile = File.Exists(profilePath) ?
                JsonUtility.FromJson<Profile>(File.ReadAllText(profilePath)) :
                new Profile { username = "myuser" };

            if (profile.saves.Exists(x => x.serverName == serverName)) return;

            profile.saves.Add(new SaveInfo
            {
                serverName = serverName,
                created = DateTime.Now.ToString("s"),
                lastPlayed = DateTime.Now.ToString("s")
            });
            File.WriteAllText(profilePath, JsonUtility.ToJson(profile, true));

            File.WriteAllText(Application.persistentDataPath + $"/save_myuser_{serverName}.json", JsonUtility.ToJson(new SaveData { serverName = serverName }, true));
            File.WriteAllText(Application.persistentDataPath + $"/playerStarData_{serverName}.json", "{\"starlight\":0}");
            File.WriteAllText(Application.persistentDataPath + $"/player_level_data_{serverName}.json", "{\"Level\":1,\"Exp\":0}");
            File.WriteAllText(Application.persistentDataPath + $"/dayData_{serverName}.json", "{\"day\":1,\"hour\":9,\"minute\":0}");

            PlayerPrefs.SetString("SelectedSave", serverName);
            FadeManager.Instance.FadeToScene("CutScene");
        });
    }

    System.Collections.IEnumerator ResetSuppressNextFrame()
    {
        yield return null;
        suppressWarnings = false;
    }

    private char ValidateNameChar(string text, int charIndex, char addedChar)
    {
        // 제어키(Backspace 등)는 허용
        if (char.IsControl(addedChar))
            return addedChar;

        // 닫기/전환 중이면 차단(팝업 X)
        if (suppressWarnings)
            return '\0';

        // 포커스가 아니면 그냥 통과
        if (inputServerName == null || !inputServerName.isFocused)
            return addedChar;

        // "맨 뒤에 붙이기"인지 확인 (버그 포인트: 이 체크가 빠지면 닫기 시에도 경고 가능)
        bool appendingAtEnd = (text != null) && (charIndex >= (text.Length));

        // 이미 6글자이고, 맨 뒤에 새 글자를 붙이려는 "실제 입력 시도"에서만 경고
        if (appendingAtEnd && text != null && text.Length >= 6)
        {
            ShowNameTooLong();
            return '\0';    // 추가 차단
        }

        return addedChar;   // 정상 입력
    }
 
    void ShowNameTooLong()
    {
        if (suppressWarnings || !isActiveAndEnabled) return;
        if (nameTooLongCo != null) StopCoroutine(nameTooLongCo);
        nameTooLongCo = StartCoroutine(NameTooLongRoutine());
    }

    System.Collections.IEnumerator NameTooLongRoutine()
    {
        if (nameTooLongPanel == null || nameTooLongGroup == null) yield break;

        nameTooLongPanel.SetActive(true);
        float duration = 0.5f;
        float elapsed = 0f;

        // Fade In
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            nameTooLongGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        nameTooLongGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(1f);

        // Fade Out
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            nameTooLongGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        nameTooLongGroup.alpha = 0f;

        nameTooLongPanel.SetActive(false);
        nameTooLongCo = null;
    }


}