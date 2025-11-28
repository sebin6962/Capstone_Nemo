using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MillNpc : MonoBehaviour
{
    public GameObject MillPanel;
    public NpcTrigger trigger;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && trigger.isPlayerNearNpc && !IsMillOpen())
        {
            Debug.Log("E키 눌림 - 방앗간 토글 시도");
            OpenMill();

            //Mill 튜토리얼 진행 트리거 1
            if (MillTutorialManager.Instance && MillTutorialManager.Instance.IsCurrentStep(MillTutorialStep.TalkToNpc))
            {
                MillTutorialManager.Instance.GoToNextMillStep();
            }
        }
    }

    private void OpenMill()
    {
        MillPanel.SetActive(true);

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayBbyongSFX();
        }
    }

    public bool IsMillOpen()
    {
        return MillPanel.activeSelf;
    }
}
