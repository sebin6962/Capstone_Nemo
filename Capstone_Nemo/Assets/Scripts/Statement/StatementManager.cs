using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class StatementManager : MonoBehaviour
{
    [SerializeField]
    private NextDayCutscene cutscene;

    [Header("패널")]
    [SerializeField]
    private GameObject statementPanel;

    [SerializeField]
    private LevelUpRevealPanel levelUpPanel;

    [Header("명세서 텍스트")]
    [SerializeField]
    private TMP_Text txtNormalCount;

    [SerializeField]
    private TMP_Text txtTotalStars;

    [Header("판매 다과 슬롯")]
    [SerializeField]
    private StatementDagwaSlot[] soldDagwaSlots;

    [Header("연출 타이밍")]
    [SerializeField]
    private float firstDelay = 0.3f;

    [SerializeField]
    private float rowDelay = 0.35f;

    [SerializeField]
    private float fadeSec = 0.25f;

    [SerializeField]
    private float slotDelay = 0.12f;

    [SerializeField]
    private float slotAnimSec = 0.28f;

    [SerializeField]
    private float totalAnimSec = 0.6f;

    private Coroutine reportRoutine;
    private bool nextDayButtonClicked;

    private void Awake()
    {
        // 이벤트 순서가 씬 전환 뒤로 밀린 경우를 대비
        UnlockManager.Instance?.ApplyScheduledUnlocksForNewDay();
    }

    private void Start()
    {
        DayReport report = StarDataManager.Instance != null
            ? StarDataManager.Instance.GetYesterdayReport()
            : default;

        PrimeAlpha(txtNormalCount, 0f);
        PrimeAlpha(txtTotalStars, 0f);

        HideAllDagwaSlots();

        if (reportRoutine != null)
        {
            StopCoroutine(reportRoutine);
        }

        reportRoutine = StartCoroutine(
            ShowReportSequence(report)
        );
    }

    public void OnNextDayButtonClicked()
    {
        if (nextDayButtonClicked)
        {
            return;
        }

        nextDayButtonClicked = true;

        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayFileSelectSFX();
        }

        PlayerPrefs.SetFloat("SpawnX", -16f);
        PlayerPrefs.SetFloat("SpawnY", 5f);
        PlayerPrefs.SetFloat("SpawnZ", 0f);

        PlayerPrefs.SetInt("NextDayFlag", 1);

        UnlockManager unlockManager = UnlockManager.Instance;

        if (unlockManager != null &&
            unlockManager.HasLevelUpRevealToShow())
        {
            int newLevel =
                unlockManager.GetLevelUpRevealLevel();

            var finishKeys =
                unlockManager.GetLevelUpRevealFinishKeys();

            if (statementPanel != null)
            {
                statementPanel.SetActive(false);
            }

            if (levelUpPanel != null)
            {
                levelUpPanel.Show(
                    newLevel,
                    finishKeys,
                    () =>
                    {
                        unlockManager.MarkLevelUpRevealShown();

                        if (cutscene != null)
                        {
                            cutscene.Play();
                        }
                    }
                );
            }
            else
            {
                Debug.LogWarning(
                    "[StatementManager] Level Up Panel이 연결되지 않아 " +
                    "바로 컷신을 진행합니다."
                );

                if (cutscene != null)
                {
                    cutscene.Play();
                }
            }
        }
        else
        {
            if (cutscene != null)
            {
                cutscene.Play();
            }
        }
    }

    private IEnumerator ShowReportSequence(DayReport report)
    {
        yield return new WaitForSecondsRealtime(firstDelay);

        // 1. 일반 손님 수
        if (txtNormalCount != null)
        {
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayMoneyCountSFX();
            }

            txtNormalCount.text = $"{report.normalCount:N0}";

            yield return FadeTextIn(
                txtNormalCount,
                fadeSec
            );
        }

        yield return new WaitForSecondsRealtime(rowDelay);

        // 3. 판매한 다과 종류
        int soldTypeCount = report.soldDagwaKeys != null
            ? report.soldDagwaKeys.Count
            : 0;

        int slotCount = soldDagwaSlots != null
            ? soldDagwaSlots.Length
            : 0;

        int visibleSlotCount = Mathf.Min(
            soldTypeCount,
            slotCount
        );

        for (int i = 0; i < visibleSlotCount; i++)
        {
            StatementDagwaSlot slot = soldDagwaSlots[i];

            if (slot == null)
            {
                continue;
            }

            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayMoneyCountSFX();
            }

            yield return slot.Show(
                report.soldDagwaKeys[i],
                slotAnimSec
            );

            yield return new WaitForSecondsRealtime(slotDelay);
        }

        if (soldTypeCount > visibleSlotCount)
        {
            Debug.LogWarning(
                $"[StatementManager] 판매 다과 {soldTypeCount}종 중 " +
                $"{visibleSlotCount}종만 표시됩니다. " +
                "명세서 슬롯 수를 늘려주세요."
            );
        }

        yield return new WaitForSecondsRealtime(rowDelay);

        // 4. 총 별빛
        // 일반 손님 별빛과 특별 손님 별빛의 합계
        if (txtTotalStars != null)
        {
            yield return FadeTextIn(
                txtTotalStars,
                0.01f
            );

            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlayTotalMoneySFX();
            }

            int from = 0;
            int to = report.TotalStars;

            float elapsed = 0f;
            float safeDuration = Mathf.Max(
                0.01f,
                totalAnimSec
            );

            while (elapsed < safeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsed / safeDuration
                );

                int currentValue = Mathf.RoundToInt(
                    Mathf.Lerp(from, to, progress)
                );

                txtTotalStars.text =
                    $"{currentValue:N0}";

                yield return null;
            }

            txtTotalStars.text = $"{to:N0}";
        }

        reportRoutine = null;
    }

    private void HideAllDagwaSlots()
    {
        if (soldDagwaSlots == null)
        {
            return;
        }

        foreach (StatementDagwaSlot slot in soldDagwaSlots)
        {
            if (slot != null)
            {
                slot.Hide();
            }
        }
    }

    private void PrimeAlpha(TMP_Text targetText, float alpha)
    {
        if (targetText == null)
        {
            return;
        }

        Color color = targetText.color;
        color.a = alpha;

        targetText.color = color;
    }

    private IEnumerator FadeTextIn(
        TMP_Text targetText,
        float duration
    )
    {
        if (targetText == null)
        {
            yield break;
        }

        float safeDuration = Mathf.Max(
            0.01f,
            duration
        );

        float elapsed = 0f;

        Color color = targetText.color;
        float startAlpha = color.a;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsed / safeDuration
            );

            color.a = Mathf.SmoothStep(
                startAlpha,
                1f,
                progress
            );

            targetText.color = color;

            yield return null;
        }

        color.a = 1f;
        targetText.color = color;
    }
}