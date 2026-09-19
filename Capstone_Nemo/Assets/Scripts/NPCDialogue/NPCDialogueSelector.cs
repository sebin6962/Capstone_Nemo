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

        // ƒ´≈◊∞Ì∏Æ ±∏¡∂∞° æ¯¿∏∏È ±∏πˆ¿¸ fallback
        if (!hasCategoryData)
        {
            if (!npcProgress.hasMetNpc && !string.IsNullOrEmpty(npcData.firstInteractionStartNodeId))
            {
                npcProgress.hasMetNpc = true;
                return npcData.firstInteractionStartNodeId;
            }

            return GetLegacyStartNodeId(npcData);
        }

        // ∞Ëºˆ≥™π´ ¥‹∞Ë∫∞ ¥Î»≠∏¶ ªÁøÎ«œ¥¬ NPC¥¬ √π ¿ŒªÁ ¿Ã»ƒ
        // «ˆ¿Á ¥‹∞Ëø° µÓ∑œµ» ¥Î»≠ ºº∆Æ∏∏ º¯»Ø º±≈√«—¥Ÿ.
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

        // categoryId∞° æ¯¿∏∏È "NPCøÕ¿« ¿œπ› ªÛ»£¿€øÎ Ω√¿€"
        if (string.IsNullOrEmpty(categoryId))
        {
            // √π ªÛ»£¿€øÎ¿Ã∏È NPC ¿¸øÎ √π ¥Î»≠
            if (!npcProgress.hasMetNpc)
            {
                npcProgress.hasMetNpc = true;

                if (!string.IsNullOrEmpty(npcData.firstInteractionStartNodeId))
                    return npcData.firstInteractionStartNodeId;

                return GetLegacyStartNodeId(npcData);
            }

            // √π ªÛ»£¿€øÎ¿Ã æ∆¥œ∏È ∑£¥˝ ¿ŒªÒ∏ª
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

        // ∆Ø¡§ ƒ´≈◊∞Ì∏Æ∏¶ ¡˜¡¢ ø©¥¬ ∞ÊøÏ: π´¡∂∞« ±◊ ƒ´≈◊∞Ì∏Æ æ»ø°º≠ ∑£¥˝
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
        {
            if (npcData.useTreeLevelDialogue)
            {
                int currentTreeLevel =
                    TreeLevelUnlocker
                        .GetSavedCurrentLevel();

                string levelNodeId =
                    GetTreeLevelStartNodeId(
                        npcData,
                        npcProgress,
                        currentTreeLevel
                    );

                if (!string.IsNullOrEmpty(levelNodeId))
                {
                    Debug.Log(
                        "[NPCDialogueSelector] ƒ´≈◊∞Ì∏Æø° " +
                        "¡§»Æ»˜ ¿œƒ°«œ¥¬ ºº∆Æ∞° æ¯æÓ " +
                        "«ˆ¿Á ¥‹∞Ë ¥Î»≠∏¶ ªÁøÎ«’¥œ¥Ÿ. " +
                        $"NPC={npcData.npcId}, " +
                        $"category={categoryId}, " +
                        $"treeLevel={currentTreeLevel}, " +
                        $"node={levelNodeId}"
                    );

                    return levelNodeId;
                }
            }

            Debug.LogWarning(
                "[NPCDialogueSelector] ªÁøÎ«“ ¥Î»≠ ºº∆Æ∞° æ¯Ω¿¥œ¥Ÿ. " +
                $"NPC={npcData.npcId}, " +
                $"category={categoryId}, " +
                $"treeLevel=" +
                TreeLevelUnlocker.GetSavedCurrentLevel()
            );

            return GetLegacyStartNodeId(npcData);
        }

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
        if (npcData == null ||
            npcData.dialogueSets == null ||
            npcData.dialogueSets.Count == 0)
        {
            return null;
        }

        // 1) ?¥Ï†Ñ ?®Í≥Ñ???ÑÏßÅ Î≥¥Ï? ?äÏ? ?Ä?îÍ? ?àÏúºÎ©?
        //    Í∞Ä???§Îûò???®Í≥ÑÎ∂Ä???∞ÏÑ† Ï≤òÎ¶¨?úÎã§.
        List<int> previousLevels = new List<int>();

        for (int i = 0; i < npcData.dialogueSets.Count; i++)
        {
            NPCDialogueSetData set = npcData.dialogueSets[i];

            if (set == null ||
                !set.useTreeLevel ||
                set.treeLevel >= treeLevel)
            {
                continue;
            }

            if (!previousLevels.Contains(set.treeLevel))
                previousLevels.Add(set.treeLevel);
        }

        previousLevels.Sort();

        for (int i = 0; i < previousLevels.Count; i++)
        {
            int previousLevel = previousLevels[i];

            List<NPCDialogueSetData> previousLevelSets =
                GetSetsForTreeLevel(npcData, previousLevel);

            NPCDialogueCategoryProgressData previousProgress =
                GetOrCreateCategoryProgress(
                    npcProgress,
                    $"tree_level_{previousLevel}");

            List<NPCDialogueSetData> unfinishedPrevious =
                GetUnseenSets(previousLevelSets, previousProgress);

            if (unfinishedPrevious.Count > 0)
            {
                return SelectAndMarkTreeLevelSet(
                    unfinishedPrevious,
                    previousProgress,
                    npcProgress);
            }
        }

        // 2) ?ÑÏû¨ ?®Í≥Ñ?êÏÑú???§ÌÜ†Î¶?Í¥Ä???Ä??> ?ºÏÉÅ ?Ä???úÏÑúÎ°??∞ÏÑ†?úÎã§.
        List<NPCDialogueSetData> currentLevelSets =
            GetSetsForTreeLevel(npcData, treeLevel);

        if (currentLevelSets.Count > 0)
        {
            NPCDialogueCategoryProgressData currentProgress =
                GetOrCreateCategoryProgress(
                    npcProgress,
                    $"tree_level_{treeLevel}");

            List<NPCDialogueSetData> unseenCurrent =
                GetUnseenSets(currentLevelSets, currentProgress);

            // ?ÑÏû¨ ?®Í≥Ñ ?Ä?îÎ? ?ÑÎ? Î≥??§Ïóê??Í∏∞Ï°¥Ï≤òÎüº ?§Ïãú ?úÌôò?úÎã§.
            if (unseenCurrent.Count == 0)
            {
                currentProgress.seenSetIds.Clear();
                unseenCurrent.AddRange(currentLevelSets);
            }

            return SelectAndMarkTreeLevelSet(
                unseenCurrent,
                currentProgress,
                npcProgress);
        }

        // 3) ?ÑÏû¨ ?®Í≥Ñ???±Î°ù???Ä?îÍ? ?ÑÏòà ?ÜÎäî NPC??
        //    ?¥Ï†Ñ ?®Í≥Ñ ÎØ∏ÏôÑÎ£åÎ∂Ñ??Î™®Îëê Ï≤òÎ¶¨????Í∞Ä??ÏµúÍ∑º ?®Í≥Ñ ?Ä?îÎ? ?úÌôò?úÎã§.
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

        if (latestRegisteredLevel == int.MinValue)
            return null;

        List<NPCDialogueSetData> latestSets =
            GetSetsForTreeLevel(npcData, latestRegisteredLevel);

        if (latestSets.Count == 0)
            return null;

        NPCDialogueCategoryProgressData latestProgress =
            GetOrCreateCategoryProgress(
                npcProgress,
                $"tree_level_{latestRegisteredLevel}");

        latestProgress.seenSetIds.Clear();

        return SelectAndMarkTreeLevelSet(
            latestSets,
            latestProgress,
            npcProgress);
    }

    private static List<NPCDialogueSetData> GetSetsForTreeLevel(
        NPCDialogueData npcData,
        int treeLevel)
    {
        List<NPCDialogueSetData> result =
            new List<NPCDialogueSetData>();

        if (npcData == null || npcData.dialogueSets == null)
            return result;

        for (int i = 0; i < npcData.dialogueSets.Count; i++)
        {
            NPCDialogueSetData set = npcData.dialogueSets[i];

            if (set != null &&
                set.useTreeLevel &&
                set.treeLevel == treeLevel)
            {
                result.Add(set);
            }
        }

        return result;
    }

    private static List<NPCDialogueSetData> GetUnseenSets(
        List<NPCDialogueSetData> source,
        NPCDialogueCategoryProgressData progress)
    {
        List<NPCDialogueSetData> result =
            new List<NPCDialogueSetData>();

        if (source == null || progress == null)
            return result;

        if (progress.seenSetIds == null)
            progress.seenSetIds = new List<string>();

        for (int i = 0; i < source.Count; i++)
        {
            NPCDialogueSetData set = source[i];

            if (set != null &&
                !progress.seenSetIds.Contains(set.setId))
            {
                result.Add(set);
            }
        }

        return result;
    }

    private static string SelectAndMarkTreeLevelSet(
        List<NPCDialogueSetData> candidates,
        NPCDialogueCategoryProgressData levelProgress,
        NPCDialogueNpcProgressData npcProgress)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        List<NPCDialogueSetData> storyPool =
            new List<NPCDialogueSetData>();

        List<NPCDialogueSetData> dailyPool =
            new List<NPCDialogueSetData>();

        for (int i = 0; i < candidates.Count; i++)
        {
            NPCDialogueSetData set = candidates[i];

            if (IsStoryRelatedSet(set))
                storyPool.Add(set);
            else
                dailyPool.Add(set);
        }

        List<NPCDialogueSetData> selectedPool =
            storyPool.Count > 0
                ? storyPool
                : dailyPool;

        if (selectedPool.Count == 0)
            return null;

        NPCDialogueSetData selectedSet =
            selectedPool[Random.Range(0, selectedPool.Count)];

        if (levelProgress.seenSetIds == null)
            levelProgress.seenSetIds = new List<string>();

        if (!levelProgress.seenSetIds.Contains(selectedSet.setId))
            levelProgress.seenSetIds.Add(selectedSet.setId);

        npcProgress.hasMetNpc = true;
        return selectedSet.startNodeId;
    }

    private static bool IsStoryRelatedSet(
        NPCDialogueSetData set)
    {
        if (set == null)
            return false;

        // ?ÑÏû¨ ?∞Ïù¥?∞Ïóê Î≥ÑÎèÑ isStory ?åÎûòÍ∑∏Í? ?ÜÏñ¥??
        // daily_talk ?¥Ïô∏ Ïπ¥ÌÖåÍ≥†Î¶¨???§ÌÜ†Î¶?Í¥ÄÍ≥?Í≥ºÍ±∞ ?Ä?îÎ°ú ?∞ÏÑ† Ï≤òÎ¶¨?òÍ≥†,
        // daily_talk ?àÏóê ?§Ïñ¥?àÎäî *_story_* ?∏Ìä∏???§ÌÜ†Î¶??Ä?îÎ°ú Î≥∏Îã§.
        if (!string.IsNullOrEmpty(set.categoryId) &&
            set.categoryId != "daily_talk")
        {
            return true;
        }

        return !string.IsNullOrEmpty(set.setId) &&
               set.setId.Contains("_story_");
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