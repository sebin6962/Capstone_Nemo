using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerLevelUI : MonoBehaviour
{
    public TextMeshProUGUI levelText;
    public Slider expBar;

    void Update()
    {
        levelText.text = $"Lv {PlayerLevelManager.Instance.Level}";
        expBar.value = (float)PlayerLevelManager.Instance.Exp / PlayerLevelManager.Instance.ExpToNextLevel;
    }

    public string GetExpTooltipText()
    {
        if (PlayerLevelManager.Instance == null)
            return "";

        int currentExp = PlayerLevelManager.Instance.Exp;
        int nextExp = PlayerLevelManager.Instance.ExpToNextLevel;

        return $"{currentExp} / {nextExp}";
    }
}
