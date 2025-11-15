using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("이 문을 통해 전환될 씬 이름")]
    public string targetScene;

    [Header("도착 씬의 스폰 지점 이름")]
    public string entranceID;

    private bool isInTrigger = false;

    [SerializeField] private float sfxLead = 0.06f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isInTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isInTrigger = false;
    }

    private void Update()
    {
        if (!isInTrigger) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        // 1 전역 전환 정보
        if (SceneTransitionInfo.Instance != null)
        {
            SceneTransitionInfo.Instance.fromScene = SceneManager.GetActiveScene().name;
            SceneTransitionInfo.Instance.toScene = targetScene;
            SceneTransitionInfo.Instance.entranceID = entranceID;
        }

        // 2 세이프 브리지
        PlayerPrefs.SetString("__fromScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString("__toScene", targetScene ?? "");
        PlayerPrefs.SetString("__entranceID", entranceID ?? "");
        PlayerPrefs.Save();

        // SFX 먼저 재생 → 아주 짧게 대기 → 페이드/전환
        StartCoroutine(PlaySfxThenFade());

        //Debug.Log($"[Portal] to={targetScene}, id={entranceID}");
        //FadeManager.Instance.FadeToScene(targetScene, 0.5f);
        
    }

    private IEnumerator PlaySfxThenFade()
    {

        if (SFXManager.Instance != null)
        {
            // 현재 씬(출발 씬)
            string fromScene = SceneManager.GetActiveScene().name;

            if (targetScene == "TreeScene" || fromScene == "TreeScene")
            {
                SFXManager.Instance.PlayTreeEnterSFX(); 
            }
            else
            {
                SFXManager.Instance.PlayDoorOpenSFX();
            }
        }

        // FadeManager가 timescale을 건드려도 안전하게: 실시간 대기
        if (sfxLead > 0f) yield return new WaitForSecondsRealtime(sfxLead);

        Debug.Log($"[Portal] to={targetScene}, id={entranceID}");
        FadeManager.Instance.FadeToScene(targetScene, 0.5f);
    }
}

