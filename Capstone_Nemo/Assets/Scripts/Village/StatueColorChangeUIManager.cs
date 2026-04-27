using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatueColorChangeUIManager : MonoBehaviour
{
    public static StatueColorChangeUIManager Instance;

    [Header("Panel")]
    public GameObject panelRoot;
    public TMP_Text messageText;

    [Header("Buttons")]
    public Button btnConfirm;
    public Button btnCancel;
    public Button btnResetDefault;

    [Header("Cost")]
    public int changeCost = 100;

    [Header("Color Roulette")]
    public float colorChangeInterval = 0.08f;
    public int minChangeCount = 18;
    public int maxChangeCount = 30;

    private bool _isChanging = false;

    [Header("Soft Color Setting")]
    [Range(0f, 1f)]
    public float fixedSaturation = 0.5f;   // 낮을수록 흰색이 많이 섞임

    [Range(0f, 1f)]
    public float fixedValue = 1f;          // 밝기. 1이면 가장 밝음

    private readonly float[] _rainbowHues =
    {
    0f,        // 빨강
    0.08f,     // 주황
    0.16f,     // 노랑
    0.33f,     // 초록
    0.58f,     // 파랑
    0.68f,     // 남색
    0.78f      // 보라
};

    private Color GetSoftRainbowColor(int index)
    {
        float hue = _rainbowHues[index % _rainbowHues.Length];

        Color color = Color.HSVToRGB(hue, fixedSaturation, fixedValue);
        color.a = 1f;

        return color;
    }

    void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool IsOpen()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    public void Open()
    {
        if (_isChanging) return;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        //if (messageText != null)
        //    messageText.text = $"{changeCost}별빛으로 색상을 변경하시겠습니까?";

        RefreshButtonState();

        if (btnConfirm != null)
        {
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(OnConfirm);
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(Close);
        }

        if (btnResetDefault != null)
        {
            btnResetDefault.onClick.RemoveAllListeners();
            btnResetDefault.onClick.AddListener(OnResetDefault);
        }
    }

    public void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void RefreshButtonState()
    {
        var star = StarDataManager.Instance;

        bool hasEnoughStarlight =
            star != null &&
            star.playerData != null &&
            star.playerData.starlight >= changeCost;

        if (btnConfirm != null)
            btnConfirm.interactable = hasEnoughStarlight && !_isChanging;
    }

    private void OnConfirm()
    {
        if (_isChanging) return;

        var star = StarDataManager.Instance;
        var skin = PlayerSkinManager.Instance;

        if (star == null || skin == null) return;

        if (star.playerData.starlight < changeCost)
        {
            RefreshButtonState();
            return;
        }

        // 별빛 차감
        star.SpendStarlight(changeCost);

        // 패널 닫기
        Close();

        // 색상 변경 연출 시작
        StartCoroutine(ChangeColorRoutine());
    }

    private void OnResetDefault()
    {
        if (_isChanging) return;

        if (PlayerSkinManager.Instance != null)
        {
            PlayerSkinManager.Instance.ResetPlayerColor(save: true);
        }

        Close();
    }

    private IEnumerator ChangeColorRoutine()
    {
        _isChanging = true;

        int changeCount = Random.Range(minChangeCount, maxChangeCount + 1);
        int finalIndex = Random.Range(0, _rainbowHues.Length);
        Color finalColor = GetSoftRainbowColor(finalIndex);

        for (int i = 0; i < changeCount; i++)
        {
            Color currentColor = GetSoftRainbowColor(i);

            if (PlayerSkinManager.Instance != null)
                PlayerSkinManager.Instance.SetPlayerColor(currentColor, save: false);

            yield return new WaitForSeconds(colorChangeInterval);
        }

        if (PlayerSkinManager.Instance != null)
            PlayerSkinManager.Instance.SetPlayerColor(finalColor, save: true);

        _isChanging = false;
    }
}