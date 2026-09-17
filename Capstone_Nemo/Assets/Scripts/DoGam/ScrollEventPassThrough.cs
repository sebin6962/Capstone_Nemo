using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollEventPassThrough : MonoBehaviour, IScrollHandler
{
    private ScrollRect parentScrollRect;

    private void Awake()
    {
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (parentScrollRect != null)
        {
            parentScrollRect.OnScroll(eventData);
        }
    }
}