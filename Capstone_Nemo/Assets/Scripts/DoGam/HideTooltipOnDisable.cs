using UnityEngine;

public class HideTooltipOnDisable : MonoBehaviour
{
    [Header("패널이 꺼질 때 함께 숨길 툴팁 오브젝트들")]
    [SerializeField] private GameObject[] tooltipObjects;

    private void OnDisable()
    {
        HideAll();
    }

    public void HideAll()
    {
        if (tooltipObjects == null) return;

        for (int i = 0; i < tooltipObjects.Length; i++)
        {
            if (tooltipObjects[i] != null)
                tooltipObjects[i].SetActive(false);
        }
    }
}
