using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class VillageEndingTimelineStarter : MonoBehaviour
{
    private const string VillageEndingKey = "PlayVillageEnding";

    [Header("엔딩 타임라인")]
    [SerializeField]
    private PlayableDirector endingDirector;

    [Header("엔딩 중 활성화")]
    [SerializeField]
    private GameObject endingSequenceRoot;

    [Header("엔딩 중 비활성화")]
    [Tooltip("일반 게임 UI, 플레이어, 상호작용 오브젝트 등을 연결")]
    [SerializeField]
    private GameObject[] disableDuringEnding;

    [Header("엔딩 종료")]
    [SerializeField]
    private GameObject endingMessagePanel;

    [SerializeField]
    private bool restoreGameplayAfterEnding = false;

    private bool isPlayingEnding;

    private void Start()
    {
        bool shouldPlayEnding =
            PlayerPrefs.GetInt(VillageEndingKey, 0) == 1;

        if (!shouldPlayEnding)
        {
            if (endingSequenceRoot != null)
                endingSequenceRoot.SetActive(false);

            return;
        }

        // 한 번 사용한 진입 플래그는 즉시 제거
        PlayerPrefs.DeleteKey(VillageEndingKey);
        PlayerPrefs.Save();

        StartCoroutine(StartEndingRoutine());
    }

    private IEnumerator StartEndingRoutine()
    {
        isPlayingEnding = true;

        SetGameplayActive(false);

        if (endingSequenceRoot != null)
            endingSequenceRoot.SetActive(true);

        if (endingMessagePanel != null)
            endingMessagePanel.SetActive(false);

        // 씬 전환 페이드가 완전히 끝날 때까지 기다림
        while (FadeManager.Instance != null &&
               FadeManager.Instance.IsFading)
        {
            yield return null;
        }

        // 씬 오브젝트가 모두 초기화될 시간을 한 프레임 확보
        yield return null;

        if (endingDirector == null)
        {
            Debug.LogError(
                "[VillageEndingTimelineStarter] Ending Director가 연결되지 않았습니다."
            );

            SetGameplayActive(true);
            yield break;
        }

        // Time.timeScale의 영향을 받지 않도록 설정
        endingDirector.timeUpdateMode =
            DirectorUpdateMode.UnscaledGameTime;

        endingDirector.time = 0;
        endingDirector.Evaluate();
        endingDirector.Play();
    }

    // Village Timeline 마지막 Signal에서 호출 가능
    public void FinishVillageEnding()
    {
        if (!isPlayingEnding)
            return;

        isPlayingEnding = false;

        if (endingMessagePanel != null)
            endingMessagePanel.SetActive(true);

        if (restoreGameplayAfterEnding)
        {
            SetGameplayActive(true);

            if (endingSequenceRoot != null)
                endingSequenceRoot.SetActive(false);
        }
    }

    private void SetGameplayActive(bool active)
    {
        if (disableDuringEnding == null)
            return;

        foreach (GameObject target in disableDuringEnding)
        {
            if (target != null)
                target.SetActive(active);
        }
    }
}
