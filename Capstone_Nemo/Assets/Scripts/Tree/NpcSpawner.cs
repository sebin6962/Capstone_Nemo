using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NpcSpawner : MonoBehaviour
{
    public GameObject[] npcObjects;

    public ParticleSystem[] treeParticles;

    void Start()
    {
        string serverName = PlayerPrefs.GetString("SelectedSave", "");
        bool hasSeenEnding = false;

        if (!string.IsNullOrEmpty(serverName))
        {
            string path = Path.Combine(Application.persistentDataPath, $"ending_{serverName}.json");
            if (File.Exists(path))
            {
                var data = JsonUtility.FromJson<EndingData>(File.ReadAllText(path));
                hasSeenEnding = (data != null && data.hasSeenEnding);
            }
        }

        foreach (var npc in npcObjects)
        {
            if (npc == null) continue;
            npc.SetActive(hasSeenEnding); // 엔딩 본 세이브에서만 등장
        }

        if (treeParticles != null)
        {
            foreach (var ps in treeParticles)
            {
                if (ps == null) continue;

                // 게임오브젝트 활성/비활성
                ps.gameObject.SetActive(hasSeenEnding);

                if (hasSeenEnding)
                {
                    ps.Clear();
                    ps.Play();
                }
                else
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }
}

