using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;   // Path
using Newtonsoft.Json; // OrderData.cs 구조로 파싱

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance;

    // 결과 키(완성 스프라이트명) 목록
    private List<string> dagwaList = new();

    // 완성키 -> 제작기 매핑(이 레시피는 어떤 제작기로 만드는지)
    private Dictionary<string, string> finishKeyToMaker = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildCandidatesFromRecipeJson();   // 주문 후보/매핑 준비
        }
        else Destroy(gameObject);
    }

    // CraftingRecipe.json을 읽어 완성 레시피만 후보로 만들기
    private void BuildCandidatesFromRecipeJson()
    {
        dagwaList.Clear();
        finishKeyToMaker.Clear();

        // CraftingRecipe.json 을 쓰는 구조라면
        var txt = Resources.Load<TextAsset>("Data/CraftingRecipe");
        if (txt == null)
        {
            Debug.LogError("[Order] Data/CraftingRecipe.json 없음");
            return;
        }

        var list = JsonConvert.DeserializeObject<RecipeList>(txt.text);
        if (list?.recipes == null) return;

        foreach (var r in list.recipes)
        {
            if (string.IsNullOrEmpty(r.resultSprite)) continue;

            //네이밍이 *_finish 인 것만 사용
            var finishKey = Path.GetFileNameWithoutExtension(r.resultSprite);
            if (!finishKey.EndsWith("_finish")) continue;

            finishKey = finishKey.Trim();

            // 후보 추가
            if (!dagwaList.Contains(finishKey))
                dagwaList.Add(finishKey);

            // 제작기 매핑
            var makerId = (r.makerId ?? "").Trim();
            finishKeyToMaker[finishKey] = makerId;
        }

        // 중복/대소문자 차이 정리
        dagwaList = dagwaList
            .Select(k => k.Trim())
            .Distinct()
            .ToList();

        Debug.Log($"[Order] 완성 후보 {dagwaList.Count}개 준비");
    }

    public string GetRandomDagwaList()
    {
        if (dagwaList.Count == 0)
        {
            Debug.LogError("다과 리스트 비어있음");
            return null;
        }

        // 1. 현재 해금 상태에서 실제 만들 수 있는 후보만 필터
        var unlocked = new List<string>();
        foreach (var key in dagwaList)
        {
            // 레시피 해금
            bool recipeOk = UnlockManager.Instance == null || UnlockManager.Instance.IsRecipeUnlocked(key);

            // 제작기 해금(매핑 있으면 체크, 없으면 통과)
            bool makerOk = true;
            if (finishKeyToMaker.TryGetValue(key, out var makerId) && !string.IsNullOrEmpty(makerId))
            {
                makerOk = UnlockManager.Instance == null || UnlockManager.Instance.IsMakerUnlocked(makerId);
            }

            if (recipeOk && makerOk)
                unlocked.Add(key);
        }

        // 2. 비정상 상황(아무것도 없으면) 대비: 레시피 해금만 통과한 후보로 백업
        if (unlocked.Count == 0)
        {
            foreach (var key in dagwaList)
                if (UnlockManager.Instance == null || UnlockManager.Instance.IsRecipeUnlocked(key))
                    unlocked.Add(key);
        }

        // 3. 그래도 없으면 원본에서 선택(초기 테스트/데이터 꼬임 방어)
        if (unlocked.Count == 0)
        {
            Debug.LogWarning("[Order] 해금된 주문 후보가 없어 원본에서 선택");
            return dagwaList[Random.Range(0, dagwaList.Count)];
        }

        //4. 최종 랜덤 선택
        int index = Random.Range(0, unlocked.Count);
        return unlocked[index];
    }

    // (디버그용) 현재 주문 가능한 후보 리스트 보기
    public List<string> GetUnlockedOrderCandidates()
    {
        var list = new List<string>();
        foreach (var key in dagwaList)
        {
            bool recipeOk = UnlockManager.Instance == null || UnlockManager.Instance.IsRecipeUnlocked(key);
            bool makerOk = !finishKeyToMaker.TryGetValue(key, out var makerId) ||
                           UnlockManager.Instance == null || UnlockManager.Instance.IsMakerUnlocked(makerId);
            if (recipeOk && makerOk) list.Add(key);
        }
        return list;
    }
}
