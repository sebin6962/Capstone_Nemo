using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestCustomer : Customer
/*{
    [SerializeField] public GameObject questBubble;
    [SerializeField] private Sprite portraitImage;
    [SerializeField] private string[] questLines;
    [SerializeField] private QuestUIManager ui;

    void Start()
    {
        questBubble.SetActive(true);
        ui = FindObjectOfType<QuestUIManager>();
    }

    public void OnBubbleClicked()
    {
        questBubble.SetActive(false);
        ui.StartQuestDialogue(questLines, portraitImage, this);
    }

    public void AcceptQuest()
    {
        Debug.Log("����Ʈ ������. Ÿ�̸� ����");
        StartOrdering();
    }

}
*/
{
    [SerializeField] private GameObject questBubble;
    [SerializeField] private QuestUIManager questUI;
    [SerializeField] private QuestOrderUI questOrderUI;

    [SerializeField] private string[] questLines;
    [SerializeField] private Sprite portraitImage;
    [SerializeField] private string questDagwaName;
    [SerializeField] private Sprite questDagwaSprite;

    private bool isQuestAccepted = false;
    private float questTimeLimit = 30f;

    public override void Serve(string givenDagwa)
    {
        // �ٰ� ���� ȿ���� ���
        SFXManager.Instance.PlayPlateSoundSFX();

        if (!isQuestAccepted || state != CustomerState.Ordering || isServed) return;

        isTimerRunning = false;
        isServed = true;

        if (givenDagwa == questDagwaName)
        {
            state = CustomerState.Served;
            questOrderUI.ShowResult(true);
            questOrderUI.ShowTimerUI(false);
            Invoke(nameof(RemoveDagwaOnPlate), 2f);

            // ���� ȿ����
            SFXManager.Instance.PlayCorrectSFX();
        }
        else
        {
            state = CustomerState.Displeased;
            remainingTime -= 3f;
            questOrderUI.ShowResult(false);
            questOrderUI.ShowTimerUI(false);
            isServed = false;
            isTimerRunning = true;
            // ���� ȿ����
            SFXManager.Instance.PlayWrongSFX();

            return;
        }

        Invoke(nameof(Leave), 4f);
    }

    new void Update()
    {
        if (isQuestAccepted && isTimerRunning)
        {
            remainingTime -= Time.deltaTime;
            float ratio = remainingTime / questTimeLimit;
            questOrderUI.UpdateTimer(ratio);

            if (remainingTime <= 0f)
            {
                HandleTimeOver();
            }
        }

        base.Update();
    }

    protected override void StartOrdering()
    {
        questBubble.SetActive(true);
    }

    public void OnBubbleClicked()
    {
        questBubble.SetActive(false);
        SFXManager.Instance.PlayBbyongSFX();
        questUI.StartQuestDialogue(questLines, portraitImage, this);
    }

    public void AcceptQuest()
    {
        Debug.Log("����Ʈ ������");
        isQuestAccepted = true;
        state = CustomerState.Ordering;

        remainingTime = questTimeLimit;
        isTimerRunning = true;

        questOrderUI.AcceptQuest(questDagwaName, questDagwaSprite);
    }

    protected override void HandleTimeOver()
    {
        isTimerRunning = false;
        isServed = true;
        state = CustomerState.Displeased;

        questOrderUI.ShowResult(false);
        questOrderUI.ShowTimerUI(false);
        // ���� ȿ���� �߰�
        SFXManager.Instance.PlayWrongSFX();
        Invoke(nameof(Leave), 4f);
    }
}
