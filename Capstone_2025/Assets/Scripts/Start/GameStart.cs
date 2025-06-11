using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStart : MonoBehaviour
{
    public float waitBeforeCut = 1f;     // 게임 시작 후 컷씬 전까지 대기 (초)
    public float waitBeforeVillage = 2f; // 컷씬 후 마을 전까지 대기 (초)

    public void OnClickStart()
    {
        StartCoroutine(GameStartFlow());
        TimeManager.Instance?.SetTimeFlow(false); // 시간 멈춤
    }

    private IEnumerator GameStartFlow()
    {
        // 1. 게임 시작 버튼 누른 후 잠시 대기
        yield return new WaitForSeconds(waitBeforeCut);

        // 2. cutScene으로 전환 (페이드)
        FadeManager.Instance.FadeToScene("CutScene");
        // Fade 전환은 내부적으로 씬 전환 후 페이드인까지 하므로, 1프레임 대기
        yield return new WaitForSeconds(FadeManager.Instance.fadeDuration + 0.1f);

        // 3. cutScene에서 잠시 대기
        yield return new WaitForSeconds(waitBeforeVillage);

        // 4. VillageScene으로 전환 (페이드)
        // VillageScene 입장 시 시간 흐름 시작을 위해 플래그 저장
        //PlayerPrefs.SetInt("StartTimeOnEnter", 1);
        //FadeManager.Instance.FadeToScene("VillageScene");
        // 역시 페이드 타임 만큼 대기
        yield return new WaitForSeconds(FadeManager.Instance.fadeDuration + 0.1f);
    }
}
