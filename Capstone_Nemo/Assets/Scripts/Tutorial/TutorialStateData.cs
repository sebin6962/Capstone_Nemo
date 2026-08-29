using System;
using UnityEngine;

[Serializable]
public class TutorialStateData
{
    public bool tutorialDone = false;
}

public static class TutorialState
{
    public static TutorialStateData Load(
        string serverName
    )
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            return new TutorialStateData();
        }

        // 아직 실제 세이브가 만들어지지 않은 화면에서
        // 기본 튜토리얼 상태를 요청할 수도 있음
        if (!SaveRepository.Exists(serverName))
        {
            return new TutorialStateData();
        }

        if (!SaveService.EnsureLoaded(serverName))
        {
            Debug.LogError(
                "[TutorialState] 통합 세이브를 " +
                $"불러올 수 없습니다: {serverName}"
            );

            return new TutorialStateData();
        }

        TutorialStateData tutorialData =
            SaveService.CurrentData.tutorialData;

        if (tutorialData == null)
        {
            tutorialData =
                new TutorialStateData
                {
                    tutorialDone = false
                };

            SaveService.CurrentData.tutorialData =
                tutorialData;
        }

        return tutorialData;
    }

    public static void Save(
        string serverName,
        TutorialStateData tutorialData
    )
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            Debug.LogError(
                "[TutorialState] serverName이 비어 있어 " +
                "저장할 수 없습니다."
            );

            return;
        }

        if (!SaveService.EnsureLoaded(serverName))
        {
            Debug.LogError(
                "[TutorialState] 통합 세이브를 " +
                $"준비할 수 없습니다: {serverName}"
            );

            return;
        }

        SaveService.CurrentData.tutorialData =
            tutorialData ??
            new TutorialStateData();

        SaveService.CurrentData
            .tutorialMigrationCompleted = true;

        SaveService.SaveCurrent();
    }
}