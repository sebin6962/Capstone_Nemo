using System.Collections;
using UnityEngine;

// 이 컴포넌트는 비활성화되지 않는 Player (또는 활성 오브젝트)에 부착하세요.
// bubbleVisual Sprite의 Pivot을 말풍선 꼬리 끝으로 설정하세요.
public class AltarSpeechBubble : MonoBehaviour
{
    [Header("플레이어 및 말풍선")]
    [SerializeField] private PlayerManager player;
    [SerializeField] private Transform bubbleVisual;

    [Header("애니메이션 길이 (초)")]
    [SerializeField, Min(0f)] private float appearDuration = 0.25f;
    [SerializeField, Min(0f)] private float holdDuration = 1.5f;
    [SerializeField, Min(0f)] private float disappearDuration = 0.2f;

    private Vector3 expandedScale;
    private bool animationRunning;
    private bool lockedByBubble;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<PlayerManager>();
        if (bubbleVisual != null)
        {
            expandedScale = bubbleVisual.localScale;
            bubbleVisual.gameObject.SetActive(false);
        }
    }

    // TreeInteract에서 대기할 수 있는 코루틴입니다.
    public IEnumerator PlayRoutine()
    {
        if (bubbleVisual == null)
        {
            Debug.LogWarning("[AltarSpeechBubble] Bubble Visual을 연결하세요.", this);
            yield break;
        }

        if (animationRunning) yield break;
        animationRunning = true;

        // 다른 시스템이 이미 잠가 두었다면 끝나도 잠금을 풀지 않습니다.
        if (player != null && !player.IsActionLocked)
        {
            player.SetActionLocked(true);
            lockedByBubble = true;
        }

        bubbleVisual.gameObject.SetActive(true);
        yield return AnimateScale(Vector3.zero, expandedScale, appearDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return AnimateScale(expandedScale, Vector3.zero, disappearDuration);

        Finish();
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
    {
        bubbleVisual.localScale = from;
        if (duration <= 0f)
        {
            bubbleVisual.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            bubbleVisual.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }
        bubbleVisual.localScale = to;
    }

    public void Cancel() => Finish();

    private void Finish()
    {
        if (bubbleVisual != null)
        {
            bubbleVisual.localScale = expandedScale;
            bubbleVisual.gameObject.SetActive(false);
        }
        if (lockedByBubble && player != null)
            player.SetActionLocked(false);
        lockedByBubble = false;
        animationRunning = false;
    }

    private void OnDisable()
    {
        if (animationRunning) Finish();
    }
}
