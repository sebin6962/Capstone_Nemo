using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KetSettingUI : MonoBehaviour
{
    [SerializeField] private KeyAction action;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text noticeText;

    bool isWaitingForInput = false;

    public void StartKeyBind()
    {
        isWaitingForInput = true;
        noticeText.text = "키를 입력하세요";
        buttonText.text = "";

    }

    void Update()
    {
        if (!isWaitingForInput)
            return;

        foreach (KeyCode code in (KeyCode[])Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(code))
            {
                KeySetting.Instance.SetKey(action, code);
                KeySetting.Instance.SaveKeyBindings();
                buttonText.text = code.ToString();
                noticeText.text = "";
                isWaitingForInput = false;
                break;
            }
        }
    }

    void Start()
    {
        KeyCode current = KeySetting.Instance.GetKey(action);
        buttonText.text = current.ToString();
    }
}
