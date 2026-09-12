using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class TMPCharacterExtractor
{
    // 결과 파일 저장 위치
    private const string OutputPath = "Assets/TMP_KoreanCharacters.txt";

    // 검사할 파일 확장자
    private static readonly HashSet<string> TargetExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".json",
            ".txt",
            ".csv",

            // Unity 데이터
            ".unity",
            ".prefab",
            ".asset",

            // 코드 안에 직접 작성한 UI 문구 등을 찾기 위해 포함
            ".cs",

            // UI Toolkit을 사용하고 있다면 포함
            ".uxml",
            ".uss"
        };

    [MenuItem("Tools/TMP/한글 문자 파일 생성")]
    public static void GenerateCharacterFile()
    {
        HashSet<char> characters = new HashSet<char>();

        string assetsPath = Application.dataPath;

        string[] files = Directory.GetFiles(
            assetsPath,
            "*.*",
            SearchOption.AllDirectories
        );

        int scannedFileCount = 0;

        try
        {
            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];

                // 우리가 생성하는 결과 파일은 다시 검사하지 않음
                if (NormalizePath(filePath)
                    .EndsWith("Assets/TMP_KoreanCharacters.txt",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string extension = Path.GetExtension(filePath);

                if (!TargetExtensions.Contains(extension))
                    continue;

                EditorUtility.DisplayProgressBar(
                    "TMP 문자 추출",
                    $"검사 중...\n{Path.GetFileName(filePath)}",
                    (float)i / files.Length
                );

                try
                {
                    string text = File.ReadAllText(filePath);

                    ExtractCharacters(text, characters);

                    scannedFileCount++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[TMP Character Extractor] 읽기 실패: {filePath}\n{e.Message}"
                    );
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 기본적으로 반드시 포함할 문자
        AddDefaultCharacters(characters);

        // 보기 좋게 정렬
        List<char> sortedCharacters = characters
            .OrderBy(c => c)
            .ToList();

        string result = new string(sortedCharacters.ToArray());

        string fullOutputPath = Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            OutputPath
        );

        File.WriteAllText(
            fullOutputPath,
            result,
            new UTF8Encoding(false)
        );

        AssetDatabase.Refresh();

        Debug.Log(
            $"<color=#70D6FF>[TMP 문자 추출 완료]</color>\n" +
            $"검사한 파일: {scannedFileCount}개\n" +
            $"추출한 고유 문자: {characters.Count}개\n" +
            $"저장 위치: {OutputPath}"
        );

        EditorUtility.DisplayDialog(
            "TMP 문자 추출 완료",
            $"문자 파일 생성 완료!\n\n" +
            $"검사 파일: {scannedFileCount}개\n" +
            $"고유 문자: {characters.Count}개\n\n" +
            $"{OutputPath}",
            "확인"
        );
    }

    private static void ExtractCharacters(
        string text,
        HashSet<char> characters)
    {
        foreach (char c in text)
        {
            // 제어 문자 제외
            if (char.IsControl(c))
                continue;

            // 한글 완성형
            if (c >= '\uAC00' && c <= '\uD7A3')
            {
                characters.Add(c);
                continue;
            }

            // 한글 호환 자모
            if (c >= '\u3130' && c <= '\u318F')
            {
                characters.Add(c);
                continue;
            }

            // 한글 자모
            if (c >= '\u1100' && c <= '\u11FF')
            {
                characters.Add(c);
                continue;
            }

            // 영어
            if ((c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z'))
            {
                characters.Add(c);
                continue;
            }

            // 숫자
            if (c >= '0' && c <= '9')
            {
                characters.Add(c);
                continue;
            }

            // 실제 파일에 등장한 문장부호 / 기호
            if (char.IsPunctuation(c) ||
                char.IsSymbol(c))
            {
                characters.Add(c);
            }
        }
    }

    private static void AddDefaultCharacters(
        HashSet<char> characters)
    {
        // 공백
        characters.Add(' ');

        // 영문
        for (char c = 'A'; c <= 'Z'; c++)
            characters.Add(c);

        for (char c = 'a'; c <= 'z'; c++)
            characters.Add(c);

        // 숫자
        for (char c = '0'; c <= '9'; c++)
            characters.Add(c);

        // 게임 UI에서 자주 쓰는 기본 기호
        const string commonSymbols =
            "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~" +
            "…·‘’“”「」『』〈〉《》" +
            "℃★☆♥♡→←↑↓";

        foreach (char c in commonSymbols)
            characters.Add(c);
    }

    private static string NormalizePath(string path)
    {
        return path.Replace("\\", "/");
    }
}
