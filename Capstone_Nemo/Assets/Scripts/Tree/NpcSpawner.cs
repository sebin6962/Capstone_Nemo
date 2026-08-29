using UnityEngine;

public class NpcSpawner : MonoBehaviour
{
    public GameObject[] npcObjects;

    public ParticleSystem[] treeParticles;

    private void Start()
    {
        bool hasSeenEnding =
            LoadEndingState();

        ApplyEndingState(hasSeenEnding);
    }

    private bool LoadEndingState()
    {
        string serverName =
            PlayerPrefs.GetString(
                "SelectedSave",
                ""
            );

        if (string.IsNullOrWhiteSpace(serverName))
        {
            Debug.LogWarning(
                "[NpcSpawner] 선택된 세이브가 없습니다."
            );

            return false;
        }

        if (!SaveService.EnsureLoaded(serverName))
        {
            Debug.LogError(
                "[NpcSpawner] 통합 세이브를 " +
                $"불러올 수 없습니다: {serverName}"
            );

            return false;
        }

        EndingData endingData =
            SaveService.CurrentData.endingData;

        return endingData != null &&
               endingData.hasSeenEnding;
    }

    private void ApplyEndingState(
        bool hasSeenEnding
    )
    {
        if (npcObjects != null)
        {
            foreach (GameObject npc in npcObjects)
            {
                if (npc == null)
                {
                    continue;
                }

                npc.SetActive(hasSeenEnding);
            }
        }

        if (treeParticles == null)
        {
            return;
        }

        foreach (ParticleSystem particle in treeParticles)
        {
            if (particle == null)
            {
                continue;
            }

            particle.gameObject.SetActive(
                hasSeenEnding
            );

            if (hasSeenEnding)
            {
                particle.Clear();
                particle.Play();
            }
            else
            {
                particle.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear
                );
            }
        }
    }
}