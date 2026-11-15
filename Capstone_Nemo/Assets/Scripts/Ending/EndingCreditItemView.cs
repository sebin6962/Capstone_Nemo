using TMPro;
using UnityEngine;

/// <summary>
/// Attach this to the credit entry prefab root. Assign its two child TMP labels.
/// </summary>
public class EndingCreditItemView : MonoBehaviour
{
    [Header("Credit entry children")]
    [SerializeField] private TextMeshProUGUI roleText;
    [SerializeField] private TextMeshProUGUI nameText;

    public TextMeshProUGUI RoleText => roleText;
    public TextMeshProUGUI NameText => nameText;
    public bool HasRequiredTexts => roleText != null && nameText != null;

    public void SetText(string role, string personName)
    {
        if (!HasRequiredTexts) return;
        roleText.text = role ?? string.Empty;
        nameText.text = personName ?? string.Empty;
        roleText.raycastTarget = false;
        nameText.raycastTarget = false;
    }
}
