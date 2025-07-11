using System;
using System.Collections;
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
    public string serverName;
    public string playerName;

    // ① 별빛(재화)
    public int starlight;

    // ② 날짜
    public int day;

    // ③ 경험치/레벨 (처음엔 0, 레벨업 구조와 분리 가능)
    public int exp;
    public int level;

    // ④ 나무 해금 현황
    public int currentUnlockedTreeLevel;  // (TreeUnlockData.currentUnlockedLevel)

    // ⑤ 아이템 재고 (이름, 수량)
    public List<StorageEntry> storageItems;

    public float playerPosX;
    public float playerPosY;
    public float moveDirX;
    public float moveDirY;

    // 생성자: 새 게임 생성시 0으로 초기화
    public SaveData()
    {
        starlight = 0;
        day = 1;                // 날짜 1일차부터 시작
        exp = 0;
        level = 1;
        currentUnlockedTreeLevel = 0;
        storageItems = new List<StorageEntry>();
        // 위치/방향 기본값(0)
        playerPosX = 0f; playerPosY = 0f;
        moveDirX = 0f; moveDirY = 1f;
    }
}

