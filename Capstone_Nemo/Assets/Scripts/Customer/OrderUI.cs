using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OrderUI : MonoBehaviour
{
    [SerializeField] private Image dagwaImage;
    [SerializeField] private Image heartImage;
    [SerializeField] private Image ximage;
    public Slider timerSlider;


    public void ShowOrder(string dagwaName)
    {
        Debug.Log("ShowOrder 호출됨: " + dagwaName);
        Sprite icon = Resources.Load<Sprite>("Sprites/Ingredients/" + dagwaName);
        
        dagwaImage.sprite = icon;
        dagwaImage.gameObject.SetActive(true);
        heartImage.gameObject.SetActive(false);
        ximage.gameObject.SetActive(false);
        timerSlider.gameObject.SetActive(true);
    }
    public void UpdateTimer(float ratio)
    {
        if (timerSlider != null)
            timerSlider.value = Mathf.Clamp01(ratio);
        /*Debug.Log($"슬라이더 값 갱신: {timerSlider.value}");*/
    }
    public void ShowTimerUI(bool show)
    {
        if (timerSlider != null)
            timerSlider.gameObject.SetActive(show);
    }

    public void ShowResult(bool isCorrect)
    {
        heartImage.gameObject.SetActive(isCorrect);
        ximage.gameObject.SetActive(!isCorrect);
        timerSlider.gameObject.SetActive(false);
    }
}
