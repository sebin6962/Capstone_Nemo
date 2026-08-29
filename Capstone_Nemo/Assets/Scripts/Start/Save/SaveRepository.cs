using System;
using System.IO;
using UnityEngine;

public static class SaveRepository
{
    [Serializable]
    private class LegacyLevelData
    {
        public int Level;
        public int Exp;
    }

    public static string GetSavePath(string serverName)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"save_myuser_{serverName}.json"
        );
    }

    private static string GetLegacyStarPath(string serverName)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"playerStarData_{serverName}.json"
        );
    }

    private static string GetLegacyLevelPath(string serverName)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"player_level_data_{serverName}.json"
        );
    }

    private static string GetLegacyWorldTimePath(string serverName)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"dayData_{serverName}.json"
        );
    }

    private static string GetLegacyPlaytimePath(
    string serverName
)
    {
        return Path.Combine(
            Application.persistentDataPath,
            $"playtime_{serverName}.json"
        );
    }

    public static bool Exists(string serverName)
    {
        return File.Exists(GetSavePath(serverName));
    }

    public static SaveData Load(string serverName)
    {
        string path = GetSavePath(serverName);

        if (!File.Exists(path))
        {
            Debug.LogWarning(
                $"[SaveRepository] 세이브 파일이 없습니다: {path}"
            );

            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveData saveData = JsonUtility.FromJson<SaveData>(json);

            if (saveData == null)
            {
                Debug.LogError(
                    $"[SaveRepository] JSON 변환에 실패했습니다: {path}"
                );

                return null;
            }

            bool dataChanged = MigrateStarData(
    serverName,
    saveData
);

            if (MigrateLevelData(serverName, saveData))
            {
                dataChanged = true;
            }

            if (MigrateWorldTimeData(serverName, saveData))
            {
                dataChanged = true;
            }

            if (MigratePlaytimeData(serverName, saveData))
            {
                dataChanged = true;
            }

            if (saveData.levelData == null)
            {
                saveData.levelData = new LevelSaveData
                {
                    level = 1,
                    exp = 0
                };

                dataChanged = true;
            }

            if (saveData.starData == null)
            {
                saveData.starData = new StarSaveData();
                dataChanged = true;
            }

            if (saveData.starData == null)
            {
                saveData.starData = new StarSaveData
                {
                    starlight = 0
                };

                dataChanged = true;
            }

            if (saveData.levelData == null)
            {
                saveData.levelData = new LevelSaveData
                {
                    level = 1,
                    exp = 0
                };

                dataChanged = true;
            }

            if (saveData.worldTimeData == null)
            {
                saveData.worldTimeData =
                    new WorldTimeSaveData
                    {
                        day = 1,
                        hour = 9,
                        minute = 0
                    };

                dataChanged = true;
            }

            if (saveData.playtimeData == null)
            {
                saveData.playtimeData =
                    new PlaytimeSaveData
                    {
                        seconds = 0,
                        lastPlayed = ""
                    };

                dataChanged = true;
            }

            // 모든 마이그레이션과 null 보정이 끝난 뒤 한 번만 저장
            if (dataChanged)
            {
                Save(serverName, saveData);
            }

            return saveData;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[SaveRepository] 세이브 불러오기 실패\n" +
                $"경로: {path}\n" +
                $"오류: {exception.Message}"
            );

            return null;
        }
    }

    private static bool MigrateStarData(
        string serverName,
        SaveData saveData
    )
    {
        // 이미 이전이 끝난 세이브라면 다시 읽지 않는다.
        if (saveData.starDataMigrationCompleted)
            return false;

        string legacyPath = GetLegacyStarPath(serverName);

        if (File.Exists(legacyPath))
        {
            try
            {
                string legacyJson = File.ReadAllText(legacyPath);

                StarSaveData legacyData =
                    JsonUtility.FromJson<StarSaveData>(legacyJson);

                if (legacyData == null)
                {
                    Debug.LogError(
                        $"[SaveRepository] 기존 별빛 데이터 변환 실패: " +
                        legacyPath
                    );

                    return false;
                }

                saveData.starData = legacyData;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[SaveRepository] 기존 별빛 데이터 이전 실패\n" +
                    $"경로: {legacyPath}\n" +
                    $"오류: {exception.Message}"
                );

                // 실패했다면 완료 처리하지 않는다.
                return false;
            }
        }
        else
        {
            if (saveData.starData == null)
            {
                saveData.starData = new StarSaveData();
            }

            // 별도 파일이 없는 구형 세이브를 위한 보조 처리
            saveData.starData.starlight = saveData.starlight;
        }

        saveData.starDataMigrationCompleted = true;

        Debug.Log(
            $"[SaveRepository] 별빛 데이터 통합 완료: {serverName}"
        );

        return true;
    }

    public static void Save(
        string serverName,
        SaveData saveData
    )
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            Debug.LogError(
                "[SaveRepository] serverName이 비어 있습니다."
            );

            return;
        }

        if (saveData == null)
        {
            Debug.LogError(
                "[SaveRepository] 저장할 SaveData가 없습니다."
            );

            return;
        }

        try
        {
            saveData.serverName = serverName;

            string path = GetSavePath(serverName);
            string json = JsonUtility.ToJson(saveData, true);

            File.WriteAllText(path, json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[SaveRepository] 세이브 저장 실패\n" +
                $"마을: {serverName}\n" +
                $"오류: {exception.Message}"
            );
        }
    }

    private static bool MigrateLevelData(
    string serverName,
    SaveData saveData
)
    {
        if (saveData.levelDataMigrationCompleted)
            return false;

        string legacyPath = GetLegacyLevelPath(serverName);

        if (File.Exists(legacyPath))
        {
            try
            {
                string legacyJson = File.ReadAllText(legacyPath);

                LegacyLevelData legacyData =
                    JsonUtility.FromJson<LegacyLevelData>(legacyJson);

                if (legacyData == null)
                {
                    Debug.LogError(
                        $"[SaveRepository] 기존 레벨 데이터 변환 실패: " +
                        legacyPath
                    );

                    return false;
                }

                saveData.levelData = new LevelSaveData
                {
                    level = Mathf.Max(1, legacyData.Level),
                    exp = Mathf.Max(0, legacyData.Exp)
                };
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[SaveRepository] 기존 레벨 데이터 이전 실패\n" +
                    $"경로: {legacyPath}\n" +
                    $"오류: {exception.Message}"
                );

                return false;
            }
        }
        else
        {
            // 별도 레벨 파일이 없는 구형 세이브 대응
            saveData.levelData = new LevelSaveData
            {
                level = Mathf.Max(1, saveData.level),
                exp = Mathf.Max(0, saveData.exp)
            };
        }

        saveData.levelDataMigrationCompleted = true;

        Debug.Log(
            $"[SaveRepository] 레벨 데이터 통합 완료: {serverName}"
        );

        return true;
    }

    private static bool MigrateWorldTimeData(
    string serverName,
    SaveData saveData
)
    {
        if (saveData.worldTimeMigrationCompleted)
            return false;

        string legacyPath =
            GetLegacyWorldTimePath(serverName);

        if (File.Exists(legacyPath))
        {
            try
            {
                string legacyJson =
                    File.ReadAllText(legacyPath);

                WorldTimeSaveData legacyData =
                    JsonUtility.FromJson<WorldTimeSaveData>(
                        legacyJson
                    );

                if (legacyData == null)
                {
                    Debug.LogError(
                        "[SaveRepository] 기존 날짜·시간 데이터 " +
                        $"변환 실패: {legacyPath}"
                    );

                    return false;
                }

                saveData.worldTimeData =
                    new WorldTimeSaveData
                    {
                        day = Mathf.Max(1, legacyData.day),
                        hour = Mathf.Clamp(
                            legacyData.hour,
                            0,
                            26
                        ),
                        minute = Mathf.Clamp(
                            legacyData.minute,
                            0,
                            59
                        )
                    };
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[SaveRepository] 기존 날짜·시간 데이터 " +
                    $"이전 실패\n경로: {legacyPath}\n" +
                    $"오류: {exception.Message}"
                );

                // 실패했으므로 완료로 표시하지 않는다.
                return false;
            }
        }
        else
        {
            // dayData 파일이 없는 오래된 세이브 대응
            saveData.worldTimeData =
                new WorldTimeSaveData
                {
                    day = Mathf.Max(1, saveData.day),
                    hour = 9,
                    minute = 0
                };
        }

        saveData.worldTimeMigrationCompleted = true;

        Debug.Log(
            "[SaveRepository] 날짜·시간 데이터 통합 완료: " +
            serverName
        );

        return true;
    }

    private static bool MigratePlaytimeData(
    string serverName,
    SaveData saveData
)
    {
        if (saveData.playtimeMigrationCompleted)
            return false;

        string legacyPath =
            GetLegacyPlaytimePath(serverName);

        if (File.Exists(legacyPath))
        {
            try
            {
                string legacyJson =
                    File.ReadAllText(legacyPath);

                PlaytimeSaveData legacyData =
                    JsonUtility.FromJson<PlaytimeSaveData>(
                        legacyJson
                    );

                if (legacyData == null)
                {
                    Debug.LogError(
                        "[SaveRepository] 기존 플레이 시간 " +
                        $"변환 실패: {legacyPath}"
                    );

                    return false;
                }

                saveData.playtimeData =
                    new PlaytimeSaveData
                    {
                        seconds = Math.Max(
                            0,
                            legacyData.seconds
                        ),
                        lastPlayed =
                            legacyData.lastPlayed ?? ""
                    };
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[SaveRepository] 기존 플레이 시간 " +
                    $"이전 실패\n경로: {legacyPath}\n" +
                    $"오류: {exception.Message}"
                );

                return false;
            }
        }
        else
        {
            saveData.playtimeData =
                new PlaytimeSaveData
                {
                    seconds = 0,
                    lastPlayed = ""
                };
        }

        saveData.playtimeMigrationCompleted = true;

        Debug.Log(
            "[SaveRepository] 플레이 시간 데이터 통합 완료: " +
            serverName
        );

        return true;
    }

    public static void Delete(string serverName)
    {
        string path = GetSavePath(serverName);

        if (!File.Exists(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[SaveRepository] 세이브 삭제 실패\n" +
                $"경로: {path}\n" +
                $"오류: {exception.Message}"
            );
        }
    }
}