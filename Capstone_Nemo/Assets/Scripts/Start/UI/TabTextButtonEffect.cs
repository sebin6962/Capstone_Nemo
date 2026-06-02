using UnityEngine;
using UnityEngine.EventSystems;

public class TabTextButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private SaveSelectTabManager manager;
    private string tabName;

    public void Init(SaveSelectTabManager manager, string tabName)
    {
        this.manager = manager;
        this.tabName = tabName;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnTabPointerEnter(tabName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnTabPointerExit(tabName);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnTabPointerDown(tabName);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (manager != null)
            manager.OnTabPointerUp(tabName);
    }
}
