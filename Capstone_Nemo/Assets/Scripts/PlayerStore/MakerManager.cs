using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MakerManager : MonoBehaviour
{
    string CurrentServer => PlayerPrefs.GetString("SelectedSave", "");

    string MakerSavePath
        => string.IsNullOrEmpty(CurrentServer)
           ? null
           : Path.Combine(Application.persistentDataPath, $"maker_{CurrentServer}.json");

    void Start()
    {
        LoadMakerState();
    }

    void OnDisable()
    {
        SaveMakerState();
    }

    void OnApplicationQuit()
    {
        SaveMakerState();
    }

    public void SaveMakerState()
    {
        if (string.IsNullOrEmpty(MakerSavePath)) return;

        // 씬에 MakerInfo가 하나도 없으면 저장 안 함 (기존 파일 보존)
        var makersInScene = FindObjectsOfType<MakerInfo>();
        if (makersInScene.Length == 0)
        {
            Debug.Log("[Maker] 씬에 MakerInfo 없음 → 기존 maker json 덮어쓰기 생략");
            return;
        }

        var data = new MakerSaveData();

        foreach (var maker in makersInScene)
        {
            // 아무것도 없는 제작기는 저장 안 함
            bool hasInput = maker.inputItemNames.Count > 0;
            bool hasResult = maker.currentResultObject != null;
            bool producing = maker.isProducing;

            if (!hasInput && !hasResult && !producing)
                continue;

            var m = new MakerSlotSave();
            m.makerId = maker.makerId;
            m.inputItemNames = new List<string>(maker.inputItemNames);
            m.isProducing = maker.isProducing;
            m.resultItemName = maker.resultItemName;
            m.craftEndUtcSeconds = maker.craftEndUtcSeconds;

            data.makers.Add(m);
        }

        File.WriteAllText(MakerSavePath, JsonUtility.ToJson(data, true));
        Debug.Log($"[Maker] Saved {data.makers.Count} makers → {MakerSavePath}");
    }

    public void LoadMakerState()
    {
        if (string.IsNullOrEmpty(MakerSavePath)) return;
        if (!File.Exists(MakerSavePath)) return;

        var json = File.ReadAllText(MakerSavePath);
        var data = JsonUtility.FromJson<MakerSaveData>(json);
        if (data == null) return;

        double nowUtc = (System.DateTime.UtcNow - System.DateTime.UnixEpoch).TotalSeconds;

        // 씬에 있는 MakerInfo들을 makerId 기준으로 매핑
        var makersInScene = FindObjectsOfType<MakerInfo>();
        var map = new Dictionary<string, MakerInfo>();
        foreach (var mi in makersInScene)
            if (!string.IsNullOrEmpty(mi.makerId))
                map[mi.makerId] = mi;

        foreach (var m in data.makers)
        {
            if (!map.TryGetValue(m.makerId, out var maker)) continue;

            // 1) 입력 재료 복원
            maker.inputItemNames = new List<string>(m.inputItemNames);
            maker.inputItemSprites.Clear();

            // 아이템 이름 → 스프라이트 재로딩(프로젝트에서 쓰는 방식에 맞춰서 수정)
            foreach (var itemName in maker.inputItemNames)
            {
                Sprite sp = Resources.Load<Sprite>($"Sprites/Ingredients/{itemName}");
                maker.inputItemSprites.Add(sp);
            }

            // 슬롯 UI 갱신
            maker.EnsureSlotUIInstance();

            if (maker.slotUIManager != null)
            {
                // 저장된 재료가 하나라도 있으면 UI를 켜서 보여줌
                if (maker.inputItemSprites.Count > 0)
                {
                    maker.slotUIManager.transform.position =
                        maker.transform.position + new Vector3(0, 1.0f, 0);

                    maker.slotUIManager.gameObject.SetActive(true);
                    maker.slotUIManager.UpdateSlots(maker.inputItemSprites);
                }
                else
                {
                    // 재료가 없으면 그냥 비워두고 끔
                    maker.slotUIManager.ClearSlots();
                    maker.slotUIManager.gameObject.SetActive(false);
                }
            }

            // 2) 제작 진행 복원
            maker.isProducing = m.isProducing;
            maker.resultItemName = m.resultItemName;
            maker.craftEndUtcSeconds = m.craftEndUtcSeconds;

            if (m.isProducing && !string.IsNullOrEmpty(m.resultItemName))
            {
                Sprite resultSprite = Resources.Load<Sprite>($"Sprites/Ingredients/{m.resultItemName}");

                double remain = m.craftEndUtcSeconds - nowUtc;
                float remainF = Mathf.Max(0.01f, (float)remain);

                if (remain <= 0)
                {
                    // 이미 제작 끝나 있을 경우 → 거의 0초짜리로 돌려서 바로 완성
                    maker.StartCraft(resultSprite, 0.01f);
                }
                else
                {
                    // 남은 시간만큼만 진행바 재생
                    maker.StartCraft(resultSprite, remainF);
                }
            }
            // 진행은 끝났지만, 제작대 위에 결과물이 남아 있는 경우
            else if (!m.isProducing && !string.IsNullOrEmpty(m.resultItemName))
            {
                // 이미 currentResultObject가 없다면 새로 생성
                if (maker.currentResultObject == null)
                {
                    Sprite resultSprite = Resources.Load<Sprite>($"Sprites/Ingredients/{m.resultItemName}");
                    if (resultSprite != null && maker.resultItemPrefab != null)
                    {
                        Vector3 resultPos = maker.transform.position + new Vector3(0f, 1.2f, 0f);
                        GameObject resultObj = Instantiate(maker.resultItemPrefab, resultPos, Quaternion.identity);

                        var sr = resultObj.GetComponent<SpriteRenderer>();
                        if (sr != null)
                            sr.sprite = resultSprite;

                        maker.currentResultObject = resultObj;
                        Debug.Log($"[Maker] 저장된 결과물 복원: makerId={m.makerId}, item={m.resultItemName}");
                        
                        //이펙트도 켜줌
                        maker.SpawnCompleteEffect();
                    }
                    else
                    {
                        Debug.LogWarning($"[Maker] 결과 스프라이트 로드 실패: {m.resultItemName}");
                    }
                }
            }
        }

        Debug.Log($"[Maker] Loaded {data.makers.Count} makers");
    }
}

[System.Serializable]
public class MakerSlotSave
{
    public string makerId;

    // 슬롯에 들어간 재료들 (아이템 이름)
    public List<string> inputItemNames = new List<string>();

    // 제작 진행 상태
    public bool isProducing;
    public string resultItemName;
    public double craftEndUtcSeconds;   // 제작이 끝나는 절대 시간(초)
}

[System.Serializable]
public class MakerSaveData
{
    public List<MakerSlotSave> makers = new List<MakerSlotSave>();
}

