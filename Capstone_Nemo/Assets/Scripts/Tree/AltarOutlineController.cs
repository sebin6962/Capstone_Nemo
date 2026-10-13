
using UnityEngine;

// Altar 전용 아웃라인 제어
// 공용 SpriteSensor는 수정하지 않음
[DefaultExecutionOrder(500)]
[RequireComponent(typeof(SpriteSensor))]
public class AltarOutlineController : MonoBehaviour
{
    [Header("Altar 센서")]
    [SerializeField]
    private SpriteSensor spriteSensor;

    [Header("나무 상호작용")]
    [SerializeField]
    private TreeInteract treeInteract;

    private bool wasBlocked;

    private void Awake()
    {
        if (spriteSensor == null)
            spriteSensor = GetComponent<SpriteSensor>();

        if (treeInteract == null)
            treeInteract = TreeInteract.Instance;
    }

    private void LateUpdate()
    {
        if (spriteSensor == null)
            return;

        if (treeInteract == null)
            treeInteract = TreeInteract.Instance;

        // 흔들림, 대기 시간, 엔딩 진행 중에는 아웃라인 숨김
        bool blocked =
            treeInteract != null &&
            !treeInteract.CanShowAltarOutline;

        if (blocked)
        {
            spriteSensor.SetOutline(false);
        }
        else if (wasBlocked)
        {
            // 대기 시간이 끝났을 때
            // 플레이어가 범위 안에 있으면 다시 표시
            spriteSensor.SetOutline(
                spriteSensor.IsPlayerInside
            );
        }

        wasBlocked = blocked;
    }

    private void OnDisable()
    {
        if (spriteSensor != null)
            spriteSensor.SetOutline(false);

        wasBlocked = false;
    }
}
