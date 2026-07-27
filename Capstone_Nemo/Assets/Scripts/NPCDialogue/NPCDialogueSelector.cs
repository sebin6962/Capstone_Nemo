using System.Collections.Generic;
using UnityEngine;

public static class NPCDialogueSelector
{
    public static string GetStartNodeId(
        NPCDialogueData npcData,
        NPCDialogueNpcProgressData npcProgress,
        string categoryId = null)
    {
        if (npcData == null)
            return null;

        if (npcProgress == null)
        {
            npcProgress = new NPCDialogueNpcProgressData();
            npcProgress.npcId = npcData.npcId;
        }

        if (npcProgress.categoryProgressList == null)
            npcProgress.categoryProgressList = new List<NPCDialogueCategoryProgressData>();

        bool hasCategoryData =
            npcData.categories != null && npcData.categories.Count > 0 &&
            npcData.dialogueSets != null && npcData.dialogueSets.Count > 0;

        // 카테고리 구조가 없으면 구버전 fallback
        if (!hasCategoryData)
        {
            if (!npcProgress.hasMetNpc && !string.IsNullOrEmpty(npcData.firstInteractionStartNodeId))
            {
                npcProgress.hasMetNpc = true;
                return npcData.firstInteractionStartNodeId;
            }

            return GetLegacyStartNodeId(npcData);
        }

        // 계수나무 단계별 대화를 사용하는 NPC는 첫 인사 이후
        // 현재 단계에 등록된 대화 세트만 순환 선택한다.
        if (npcData.useTreeLevelDialogue && string.IsNullOrEmpty(categoryId))
        {
            if (!npcProgress.hasMetNpc)
            {
                npcProgress.hasMetNpc = true;

                if (!string.IsNullOrEmpty(npcData.firstInteractionStartNodeId))
                    return npcData.firstInteractionStartNodeId;
            }

            int treeLevel = TreeLevelUnlocker.GetSavedCurrentLevel();
            string treeLevelStartNodeId =
                GetTreeLevelStartNodeId(npcData, npcProgress, treeLevel);

            if (!string.IsNullOrEmpty(treeLevelStartNodeId))
                return treeLevelStartNodeId;
        }

        // categoryId가 없으면 "NPC와의 일반 상호작용 시작"
        if (string.IsNullOrEmpty(categoryId))
        {
            // 첫 상호작용이면 NPC 전용 첫 대화
            if (!npcProgress.hasMetNpc)
            {
                npcProgress.hasMetNpc = true;

                if (!string.IsNullOrEmpty(npcData.firstInteractionStartNodeId))
                    return npcData.firstInteractionStartNodeId;

                return GetLegacyStartNodeId(npcData);
            }

            // 첫 상호작용이 아니면 랜덤 인삿말
            if (npcData.randomGreetingNodeIds != null && npcData.randomGreetingNodeIds.Count > 0)
            {
                List<string> validGreetingIds = new List<string>();

                for (int i = 0; i < npcData.randomGreetingNodeIds.Count; i++)
                {
                    string id = npcData.randomGreetingNodeIds[i];
                    if (!string.IsNullOrEmpty(id))
                        validGreetingIds.Add(id);
                }

                if (validGreetingIds.Count > 0)
                {
                    int randomIndex = Random.Range(0, validGreetingIds.Count);
                    return validGreetingIds[randomIndex];
                }
            }

            return GetLegacyStartNodeId(npcData);
        }

        // 특정 카테고리를 직접 여는 경우: 무조건 그 카테고리 안에서 랜덤
        NPCDialogueCategoryData category = npcData.categories.Find(c => c.categoryId == categoryId);
        if (category == null || category.setIds == null || category.setIds.Count == 0)
            return GetLegacyStartNodeId(npcData);

        NPCDialogueCategoryProgressData categoryProgress =
            GetOrCreateCategoryProgress(npcProgress, categoryId);

        List<string> pool = new List<string>();

        for (int i = 0; i < category.setIds.Count; i++)
        {
            string setId = category.setIds[i];

            NPCDialogueSetData candidateSet =
                npcData.dialogueSets.Find(s => s.setId == setId);

            if (npcData.useTreeLevelDialogue &&
                candidateSet != null &&
                candidateSet.useTreeLevel &&
                candidateSet.treeLevel != TreeLevelUnlocker.GetSavedCurrentLevel())
            {
                continue;
            }

            if (!categoryProgress.seenSetIds.Contains(setId))
                pool.Add(setId);
        }

        if (pool.Count == 0)
        {
            categoryProgress.seenSetIds.Clear();

            for (int i = 0; i < category.setIds.Count; i++)
            {
                string setId = category.setIds[i];
                NPCDialogueSetData candidateSet =
                    npcData.dialogueSets.Find(s => s.setId == setId);

                if (npcData.useTreeLevelDialogue &&
                    candidateSet != null &&
                    candidateSet.useTreeLevel &&
                    candidateSet.treeLevel != TreeLevelUnlocker.GetSavedCurrentLevel())
                {
                    continue;
                }

                pool.Add(setId);
            }
        }

        if (pool.Count == 0)
            return GetLegacyStartNodeId(npcData);

        string selectedSetId = pool[Random.Range(0, pool.Count)];

        if (!categoryProgress.seenSetIds.Contains(selectedSetId))
            categoryProgress.seenSetIds.Add(selectedSetId);

        npcProgress.hasMetNpc = true;

        NPCDialogueSetData setData = npcData.dialogueSets.Find(s => s.setId == selectedSetId);
        if (setData != null && !string.IsNullOrEmpty(setData.startNodeId))
            return setData.startNodeId;

        return GetLegacyStartNodeId(npcData);
    }

    private static string GetTreeLevelStartNodeId(
        NPCDialogueData npcData,
        NPCDialogueNpcProgressData npcProgress,
        int treeLevel)
    {
        int resolvedTreeLevel = treeLevel;
        List<NPCDialogueSetData> levelSets = npcData.dialogueSets.FindAll(
            s => s != null && s.useTreeLevel && s.treeLevel == resolvedTreeLevel);

        // 현재 단계 대사가 따로 없는 NPC는 현재 단계보다 낮은 단계 중
        // 가장 최근에 등록된 대화를 유지한다.
        // 예: 대사가 4단계까지인 NPC가 계수나무 5단계에 도달한 경우.
        if (levelSets == null || levelSets.Count == 0)
        {
            int latestRegisteredLevel = int.MinValue;

            for (int i = 0; i < npcData.dialogueSets.Count; i++)
            {
                NPCDialogueSetData set = npcData.dialogueSets[i];

                if (set == null || !set.useTreeLevel)
                    continue;

                if (set.treeLevel <= treeLevel &&
                    set.treeLevel > latestRegisteredLevel)
                {
                    latestRegisteredLevel = set.treeLevel;
                }
            }

            if (latestRegisteredLevel != int.MinValue)
            {
                resolvedTreeLevel = latestRegisteredLevel;
                levelSets = npcData.dialogueSets.FindAll(
                    s => s != null &&
                         s.useTreeLevel &&
                         s.treeLevel == resolvedTreeLevel);
            }
        }

        if (levelSets == null || levelSets.Count == 0)
            return null;

        string progressCategoryId = $"tree_level_{resolvedTreeLevel}";
        NPCDialogueCategoryProgressData levelProgress =
            GetOrCreateCategoryProgress(npcProgress, progressCategoryId);

        List<NPCDialogueSetData> pool = levelSets.FindAll(
            s => !levelProgress.seenSetIds.Contains(s.setId));

        // 현재 단계의 대화를 모두 본 경우 기록을 초기화하고 다시 순환한다.
        if (pool.Count == 0)
        {
            levelProgress.seenSetIds.Clear();
            pool.AddRange(levelSets);
        }

        NPCDialogueSetData selectedSet = pool[Random.Range(0, pool.Count)];

        if (!levelProgress.seenSetIds.Contains(selectedSet.setId))
            levelProgress.seenSetIds.Add(selectedSet.setId);

        npcProgress.hasMetNpc = true;
        return selectedSet.startNodeId;
    }

    private static NPCDialogueCategoryProgressData GetOrCreateCategoryProgress(
        NPCDialogueNpcProgressData npcProgress,
        string categoryId)
    {
        NPCDialogueCategoryProgressData progress =
            npcProgress.categoryProgressList.Find(c => c.categoryId == categoryId);

        if (progress == null)
        {
            progress = new NPCDialogueCategoryProgressData();
            progress.categoryId = categoryId;
            npcProgress.categoryProgressList.Add(progress);
        }

        if (progress.seenSetIds == null)
            progress.seenSetIds = new List<string>();

        return progress;
    }

    private static string GetLegacyStartNodeId(NPCDialogueData npcData)
    {
        if (npcData == null)
            return null;

        if (npcData.randomGreetingNodeIds != null && npcData.randomGreetingNodeIds.Count > 0)
        {
            int randomIndex = Random.Range(0, npcData.randomGreetingNodeIds.Count);
            return npcData.randomGreetingNodeIds[randomIndex];
        }

        return npcData.startNodeId;
    }
}