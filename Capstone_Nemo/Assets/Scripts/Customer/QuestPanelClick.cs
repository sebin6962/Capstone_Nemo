using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class QuestPanelClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private QuestUIManager questUI;

    public void OnPointerClick(PointerEventData eventData)
    {
        questUI.OnClickNextLine();
        Debug.Log("ÆÐ³Î Å¬¸¯µÊ");
    }
}
