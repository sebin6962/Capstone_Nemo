using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcAreaSpeechBubble : MonoBehaviour
{
    [Header("말풍선 오브젝트 (NPC당 2개 이상 가능)")]
    public GameObject[] bubbleObjects;

    [Header("설정")]
    public float visibleTime = 5f; // 말풍선 보여줄 시간

    private bool isPlaying = false; // 말풍선이 현재 떠 있는지 여부

    private void Start()
    {
        HideAllBubbles();
    }

    private void HideAllBubbles()
    {
        if (bubbleObjects == null) return;

        foreach (var bubble in bubbleObjects)
        {
            if (bubble != null)
                bubble.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // 이미 말풍선 재생 중이면 무시
        if (isPlaying) return;

        StartCoroutine(ShowBubbleRoutine());
    }

    private IEnumerator ShowBubbleRoutine()
    {
        isPlaying = true;

        HideAllBubbles();

        if (bubbleObjects != null && bubbleObjects.Length > 0)
        {
            int idx = Random.Range(0, bubbleObjects.Length);
            var chosen = bubbleObjects[idx];
            if (chosen != null)
                chosen.SetActive(true);
        }

        // 지정한 시간동안 유지
        yield return new WaitForSeconds(visibleTime);

        HideAllBubbles();
        isPlaying = false;
    }
}
