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
    /// ����Ʈ ���� ���� �ʱ�ȭ (UI ��� ����)
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
    /// ����Ʈ ���� �� ���� �ֹ� ����
    /// </summary>
    /// <param name="dagwaName">���ϴ� �ٰ� �̸�</param>
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
        Debug.Log($"����Ʈ ����: {dagwaName}");
    }
    public void ShowTimerUI(bool show)
    {
        if (timerSlider != null)
        {
            timerSlider.gameObject.SetActive(show);
        }
    }
    /// <summary>
    /// ������ ��쿡�� Ÿ�̸� ����
    /// </summary>
    public void UpdateTimer(float ratio)
    {
        if (isQuestAccepted && timerSlider != null)
        {
            timerSlider.value = Mathf.Clamp01(ratio);
        }
    }

    /// <summary>
    /// ��� UI ǥ��
    /// </summary>
    public void ShowResult(bool isCorrect)
    {
        heartImage.gameObject.SetActive(isCorrect);
        ximage.gameObject.SetActive(!isCorrect);
        timerSlider.gameObject.SetActive(false);
    }

    /// <summary>
    /// ���� ���� Ȯ��
    /// </summary>
    public bool IsQuestAccepted()
    {
        return isQuestAccepted;
    }
}
