using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StatementManager : MonoBehaviour
{
    [SerializeField] private NextDayCutscene cutscene;

    [Header("패널")]
    [SerializeField] private GameObject statementPanel;       // 명세서 패널
    [SerializeField] private LevelUpRevealPanel levelUpPanel; // 레벨업 패널

    [Header("명세서 텍스트")]
    [SerializeField] private TMP_Text txtNormalCount;
    [SerializeField] private TMP_Text txtNormalStars;
    [SerializeField] private TMP_Text txtQuestCount;
    [SerializeField] private TMP_Text txtQuestStars;
    [SerializeField] private TMP_Text txtTotalStars;

    [Header("연출 타이밍")]
    [SerializeField] private float firstDelay = 0.3f;   // 패널 켜진 뒤 첫 줄 나오기까지
    [SerializeField] private float rowDelay = 0.35f;  // 각 줄 사이 간격
    [SerializeField] private float fadeSec = 0.25f;  // 각 줄 페이드인 시간
    [SerializeField] private float totalAnimSec = 0.6f; // 총 별빛 숫자 오르는 시간

    Coroutine _reportRoutine;

    void Awake()
    {
        // 혹시 TimeManager 이벤트 순서가 씬 전환 뒤로 밀렸어도 안전하도록
        UnlockManager.Instance?.ApplyScheduledUnlocksForNewDay();
        
    }

    void Start()
    {
        // 어제 스냅샷
        DayReport rep = StarDataManager.Instance != null
    ? StarDataManager.Instance.GetYesterdayReport()
    : default(DayReport);

        // 1) 처음엔 모두 숨김(알파 0)
        PrimeAlpha(txtNormalCount, 0f);
        PrimeAlpha(txtNormalStars, 0f);
        PrimeAlpha(txtQuestCount, 0f);
        PrimeAlpha(txtQuestStars, 0f);
        PrimeAlpha(txtTotalStars, 0f);

        // 2) 순차 표시 코루틴 시작
        if (_reportRoutine != null) StopCoroutine(_reportRoutine);
        _reportRoutine = StartCoroutine(ShowReportSequence(rep));
    }

    public void OnNextDayButtonClicked()
    {
        PlayerPrefs.SetFloat("SpawnX", -16f);
        PlayerPrefs.SetFloat("SpawnY", 5f);
        PlayerPrefs.SetFloat("SpawnZ", 0f);

        // 다음날 플래그 저장
        PlayerPrefs.SetInt("NextDayFlag", 1);

        var um = UnlockManager.Instance;
        // 씬 전환은 컷신 종료 후에
        //cutscene.onFinished.RemoveAllListeners();
        //cutscene.onFinished.AddListener(() =>
        //{
        //    if (FadeManager.Instance != null)
        //        FadeManager.Instance.FadeToScene("VillageScene", 0.5f);
        //    else
        //        SceneManager.LoadScene("VillageScene");
        //});

        // 전날 레벨업 여부 검사 (다음 날 적용 예약이 있으면 true)
        if (um != null && um.HasLevelUpRevealToShow())  // ok
        {
            int newLevel = um.GetLevelUpRevealLevel();        // 표시용 레벨
            var finishKeys = um.GetLevelUpRevealFinishKeys();   // 표시용 키들

            if (statementPanel != null) statementPanel.SetActive(false);

            if (levelUpPanel != null)
            {
                levelUpPanel.Show(newLevel, finishKeys, () => {
                    um.MarkLevelUpRevealShown();
                    cutscene.Play();
                });
            }
            else
            {
                Debug.LogWarning("[StatementManager] levelUpPanel 미지정 → 바로 컷신 진행");
                cutscene.Play();
            }
        }
        else
        {
            cutscene.Play();
        }
    }

    //FadeManager.Instance.FadeToScene("VillageScene", 0.5f);

    //// 하루 증가 및 저장!
    //if (TimeManager.Instance != null)
    //{
    //    TimeManager.Instance.currentDay++;
    //    TimeManager.Instance.hour = 9;
    //    TimeManager.Instance.minute = 0;
    //    TimeManager.Instance.SaveDayData();
    //}

    private IEnumerator ShowReportSequence(DayReport rep)
    {
        // 0) 첫 대기
        yield return new WaitForSecondsRealtime(firstDelay);

        // 1) 일반 손님 수
        if (txtNormalCount != null)
        {
            if (SFXManager.Instance) SFXManager.Instance.PlayMoneyCountSFX();
            txtNormalCount.text = $"{rep.normalCount:N0}";
            yield return FadeTextIn(txtNormalCount, fadeSec);
        }
        yield return new WaitForSecondsRealtime(rowDelay);

        // 2) 일반 별빛
        if (txtNormalStars != null)
        {
            if (SFXManager.Instance) SFXManager.Instance.PlayMoneyCountSFX();
            txtNormalStars.text = $"{rep.normalStars:N0}";
            yield return FadeTextIn(txtNormalStars, fadeSec);
        }
        yield return new WaitForSecondsRealtime(rowDelay);

        // 3) 특별 손님 수
        if (txtQuestCount != null)
        {
            if (SFXManager.Instance) SFXManager.Instance.PlayMoneyCountSFX();
            txtQuestCount.text = $"{rep.questCount:N0}";
            yield return FadeTextIn(txtQuestCount, fadeSec);
        }
        yield return new WaitForSecondsRealtime(rowDelay);

        // 4) 특별 별빛
        if (txtQuestStars != null)
        {
            if (SFXManager.Instance) SFXManager.Instance.PlayMoneyCountSFX();
            txtQuestStars.text = $"{rep.questStars:N0}";
            yield return FadeTextIn(txtQuestStars, fadeSec);
        }
        yield return new WaitForSecondsRealtime(rowDelay);

        // 5) 총 별빛: 먼저 보이게 만들고, 숫자를 0→총합으로
        if (txtTotalStars != null)
        {
            // 알파만 즉시 1로
            yield return FadeTextIn(txtTotalStars, 0.01f);

            if (SFXManager.Instance) SFXManager.Instance.PlayTotalMoneySFX();

            int from = 0;
            int to = rep.TotalStars;
            float t = 0f;
            while (t < totalAnimSec)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / totalAnimSec);
                int v = (int)Mathf.Lerp(from, to, p);
                txtTotalStars.text = $"{v:N0}";
                yield return null;
            }
            txtTotalStars.text = $"{to:N0}";
        }

        _reportRoutine = null;
    }

    private void PrimeAlpha(TMP_Text t, float a)
    {
        if (t == null) return;
        var c = t.color; c.a = a; t.color = c;
    }

    private IEnumerator FadeTextIn(TMP_Text t, float sec)
    {
        if (t == null) yield break;
        float el = 0f;
        var c = t.color; float start = c.a;
        while (el < sec)
        {
            el += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(el / sec);
            c.a = Mathf.SmoothStep(start, 1f, p);
            t.color = c;
            yield return null;
        }
        c.a = 1f; t.color = c;
    }
}
