using System.Collections;
using UnityEngine;

public class NpcSpeechBubbleTimer : MonoBehaviour
{
    [SerializeField] private GameObject bubbleRoot; // 말풍선 오브젝트
    [SerializeField] private float showDelay = 2f;  // 씬 진입 후 보여줄 때까지 대기 시간
    [SerializeField] private float visibleTime = 3f; // 보여주는 시간
    [SerializeField] private bool deactivateOnStart = true; // 시작 시 강제로 꺼둘지

    private Coroutine routine;

    private void Start()
    {
        if (bubbleRoot == null)
        {
            Debug.LogWarning("[NpcSpeechBubbleTimer] bubbleRoot가 비어있음");
            return;
        }

        if (deactivateOnStart) bubbleRoot.SetActive(false);

        routine = StartCoroutine(ShowThenHide());
    }

    private IEnumerator ShowThenHide()
    {
        yield return new WaitForSeconds(showDelay);
        bubbleRoot.SetActive(true);
        yield return new WaitForSeconds(visibleTime);
        bubbleRoot.SetActive(false);
        routine = null;
    }

    // 필요하면 외부에서 다시 트리거할 수 있게 공개 메서드도 제공
    public void Trigger(float delay = 0f, float duration = -1f)
    {
        if (bubbleRoot == null) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(TriggerRoutine(delay, duration));
    }

    private IEnumerator TriggerRoutine(float delay, float duration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        bubbleRoot.SetActive(true);
        float t = (duration > 0f) ? duration : visibleTime;
        yield return new WaitForSeconds(t);
        bubbleRoot.SetActive(false);
        routine = null;
    }
}

