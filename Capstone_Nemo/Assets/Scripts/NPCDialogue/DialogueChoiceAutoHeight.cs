using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class DialogueChoiceAutoHeight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private TextMeshProUGUI choiceText;

    [Header("Button Size")]
    [Tooltip("한 줄일 때 버튼의 최소 높이")]
    [SerializeField] private float minHeight = 50f;

    [Tooltip("텍스트 위쪽 여백")]
    [SerializeField] private float paddingTop = 12f;

    [Tooltip("텍스트 아래쪽 여백")]
    [SerializeField] private float paddingBottom = 12f;

    [Header("Options")]
    [Tooltip("텍스트가 변경될 때마다 자동으로 다시 계산")]
    [SerializeField] private bool updateEveryFrame = false;

    private string lastText;

    private void Awake()
    {
        if (buttonRect == null)
            buttonRect = GetComponent<RectTransform>();

        if (choiceText == null)
            choiceText = GetComponentInChildren<TextMeshProUGUI>();

        RefreshHeight();
    }

    private void Start()
    {
        RefreshHeight();
    }

    private void OnEnable()
    {
        RefreshHeight();
    }

    private void Update()
    {
        if (choiceText == null)
            return;

        if (updateEveryFrame || lastText != choiceText.text)
        {
            RefreshHeight();
        }
    }

    public void RefreshHeight()
    {
        if (buttonRect == null || choiceText == null)
            return;

        lastText = choiceText.text;

        // TMP가 현재 RectTransform 폭을 기준으로 줄바꿈을 계산하도록 갱신
        choiceText.ForceMeshUpdate();

        float textWidth = choiceText.rectTransform.rect.width;

        if (textWidth <= 0)
            textWidth = buttonRect.rect.width;

        // 현재 버튼 폭에서 필요한 텍스트 높이 계산
        Vector2 preferredSize =
            choiceText.GetPreferredValues(choiceText.text, textWidth, Mathf.Infinity);

        float targetHeight =
            preferredSize.y +
            paddingTop +
            paddingBottom;

        targetHeight = Mathf.Max(minHeight, targetHeight);

        buttonRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            targetHeight
        );

        // 레이아웃 그룹을 사용 중이라면 즉시 반영
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRect);
    }
}