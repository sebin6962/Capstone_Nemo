using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PlayerSceneTrigger : MonoBehaviour
{
    private string targetScene = "";
    private bool isInTrigger = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerStore"))
        {
            targetScene = "PlayerStoreScene"; // 필요 시 이름 커스터마이징
            isInTrigger = true;
        }

        if (other.CompareTag("TreeEntry"))
        {
            targetScene = "TreeScene"; // 필요 시 이름 커스터마이징
            isInTrigger = true;
        }

        if (other.CompareTag("Market"))
        {
            targetScene = "MarketScene"; // 이동할 씬 이름
            isInTrigger = true;
        }

        if (other.CompareTag("MillStore"))
        {
            targetScene = "MillScene"; // 이동할 씬 이름
            isInTrigger = true;
        }

        if (other.CompareTag("Village"))
        {
            targetScene = "VillageScene"; // 이동할 씬 이름
            isInTrigger = true;
        }

    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerStore"))
        {
            targetScene = "";
            isInTrigger = false;
        }

        if (other.CompareTag("TreeEntry"))
        {
            targetScene = "";
            isInTrigger = false;
        }

        if (other.CompareTag("Market"))
        {
            targetScene = "";
            isInTrigger = false;
        }

        if (other.CompareTag("MillStore"))
        {
            targetScene = "";
            isInTrigger = false;
        }

        if (other.CompareTag("Village"))
        {
            
            targetScene = "";
            isInTrigger = false;
        }
    }

    private void Update()
    {
        if (isInTrigger && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S)))
        {
            Debug.Log("상호작용: W키 누름 → 씬 전환 중");
            FadeManager.Instance.FadeToScene(targetScene, 0.5f); // 0.5초 후 씬 전환
        }
    }
}
