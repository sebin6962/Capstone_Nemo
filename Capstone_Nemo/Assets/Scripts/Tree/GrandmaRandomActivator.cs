using UnityEngine;

/// <summary>
/// 계수나무 맵에 진입할 때 할머니 NPC의 등장 여부를 한 번 결정합니다.
/// 이 스크립트는 비활성화될 할머니 NPC가 아니라 항상 활성화된 별도 오브젝트에 연결하세요.
/// </summary>
public class GrandmaRandomActivator : MonoBehaviour
{
    [Header("할머니 NPC")]
    [SerializeField] private GameObject grandmaNpcObject;

    [Header("등장 설정")]
    [Range(0f, 1f)]
    [SerializeField] private float appearanceChance = 0.2f;

    [Tooltip("체크하면 등장 확률과 관계없이 할머니를 항상 활성화합니다.")]
    [SerializeField] private bool forceActivateForTesting;

    private void Start()
    {
        ApplyGrandmaActiveState();
    }

    private void ApplyGrandmaActiveState()
    {
        if (grandmaNpcObject == null)
        {
            Debug.LogWarning(
                "[GrandmaRandomActivator] Grandma Npc Object가 연결되지 않았습니다.",
                this
            );
            return;
        }

        bool shouldActivate =
            forceActivateForTesting || Random.value < appearanceChance;

        grandmaNpcObject.SetActive(shouldActivate);

        Debug.Log(
            $"[GrandmaRandomActivator] 할머니 NPC 등장: {shouldActivate} " +
            $"(강제 활성화: {forceActivateForTesting}, 확률: {appearanceChance:P0})",
            this
        );
    }
}
