using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveInfo
{
    public string serverName;
    public string created;
    public string lastPlayed;
}

[Serializable]
public class Profile
{
    public string username;
    public List<SaveInfo> saves = new List<SaveInfo>();
}

[Serializable]
public class SaveData
{
    // 세이브 파일을 구분하는 마을 이름
    public string serverName;

    // 해당 세이브에서 사용하는 캐릭터 이름
    public string playerName;

    // ① 별빛(재화)
    public int starlight;

    // ② 날짜
    public int day;

    // ③ 경험치/레벨
    public int exp;
    public int level;

    // ④ 나무 해금 현황
    public int currentUnlockedTreeLevel;

    // ⑤ 아이템 재고
    public List<StorageEntry> storageItems;

    public float playerPosX;
    public float playerPosY;
    public float moveDirX;
    public float moveDirY;

    public List<string> acceptedQuestIds;

    public string dailyQuestRealDate;
    public List<string> dailyQuestIds;

    // 새 게임 생성 시 기본값
    public SaveData()
    {
        serverName = "";
        playerName = "";

        starlight = 0;
        day = 1;
        exp = 0;
        level = 1;
        currentUnlockedTreeLevel = 0;

        storageItems = new List<StorageEntry>();
        acceptedQuestIds = new List<string>();

        dailyQuestRealDate = "";
        dailyQuestIds = new List<string>();

        storageItems.Add(new StorageEntry
        {
            name = "Mepssalgaru",
            amount = 50
        });

        // 위치/방향 기본값
        playerPosX = 0f;
        playerPosY = 0f;
        moveDirX = 0f;
        moveDirY = 1f;
    }
}

