using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class QuestOrderUI : MonoBehaviour
{
    [SerializeField] private Image dagwaImage;
    [SerializeField] private Image heartImage;
    [SerializeField] private Image ximage;
    [SerializeField] private Slider timerSlider;

    private bool isQuestAccepted = false;
    private string currentDagwaName;

    /// <summary>
    /// 퀘스트 수락 전에 초기화 (UI 모두 숨김)
    /// </summary>
    public void Initialize()
    {
        dagwaImage.gameObject.SetActive(false);
        heartImage.gameObject.SetActive(false);
        ximage.gameObject.SetActive(false);
        timerSlider.gameObject.SetActive(false);
        isQuestAccepted = false;
    }

    /// <summary>
    /// 퀘스트 수락 후 실제 주문 시작
    /// </summary>
    /// <param name="dagwaName">원하는 다과 이름</param>
    public void AcceptQuest(string dagwaName, Sprite dagwaSprite)
    {
        if (dagwaImage != null && dagwaSprite != null)
        {
            dagwaImage.sprite = dagwaSprite;
            dagwaImage.gameObject.SetActive(true);
        }

        if (timerSlider != null)
        {
            timerSlider.gameObject.SetActive(true);
        }
        isQuestAccepted = true;
        Debug.Log($"퀘스트 수락: {dagwaName}");
    }
    public void ShowTimerUI(bool show)
    {
        if (timerSlider != null)
        {
            timerSlider.gameObject.SetActive(show);
        }
    }
    /// <summary>
    /// 수락된 경우에만 타이머 갱신
    /// </summary>
    public void UpdateTimer(float ratio)
    {
        if (isQuestAccepted && timerSlider != null)
        {
            timerSlider.value = Mathf.Clamp01(ratio);
        }
    }

    /// <summary>
    /// 결과 UI 표시
    /// </summary>
    public void ShowResult(bool isCorrect)
    {
        heartImage.gameObject.SetActive(isCorrect);
        ximage.gameObject.SetActive(!isCorrect);
        timerSlider.gameObject.SetActive(false);
    }

    /// <summary>
    /// 수락 여부 확인
    /// </summary>
    public bool IsQuestAccepted()
    {
        return isQuestAccepted;
    }
}
