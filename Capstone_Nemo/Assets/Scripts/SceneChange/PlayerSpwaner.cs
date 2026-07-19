using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpwaner : MonoBehaviour
{
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(SafeSpawnFallback());
    }

    private IEnumerator SafeSpawnFallback()
    {
        // VSD가 있으면 우선권을 줌: 몇 프레임 기다려 본다
        bool hasDirector = (FindObjectOfType<VillageSpawnDirector>() != null);
        if (hasDirector)
        {
            // VSD가 entranceID를 소비할 시간을 준다
            int frames = 0;
            while (frames < 10) // 약 10프레임 대기
            {
                if (SceneTransitionInfo.Instance != null &&
                    string.IsNullOrEmpty(SceneTransitionInfo.Instance.entranceID))
                    yield break; // VSD가 이미 처리함

                frames++;
                yield return null;
            }
        }

        // 여기까지 왔다는 건 VSD가 없거나 또는 아직 처리를 못 했다는 뜻
        var info = SceneTransitionInfo.Instance;
        if (info == null || string.IsNullOrEmpty(info.entranceID)) yield break;

        string entrance = info.entranceID;
        GameObject spawnPoint = GameObject.Find(entrance);

        if (spawnPoint != null)
        {
            Vector3 spawnPos = spawnPoint.transform.position;
            spawnPos.z = 0f;
            transform.position = spawnPos;

            Debug.Log($"[Spawner-Fallback] entranceID: {entrance}");

            var playerManager = GetComponent<PlayerManager>();

            if (playerManager != null)
            {
                playerManager.SetFacing(PlayerManager.InitialFacing.Up);
            }
            // 방향 지정
            //if (entrance == "FromVillage(Store)") LookUp();
            //else if (entrance == "FromPlayerStore") LookDown();

            // 내가 처리했으니 소모
            info.entranceID = null;
        }
    }

    //public void LookDown()
    //{
    //    animator.SetFloat("MoveX", 0);
    //    animator.SetFloat("MoveY", -1);
    //    animator.SetBool("IsWalking", false);

    //    var pm = GetComponent<PlayerManager>();
    //    if (pm != null)
    //    {
    //        pm.lastMoveDir = Vector2.down;
    //        Debug.Log("Spawner LookDown: lastMoveDir=" + pm.lastMoveDir + ", Animator MoveY=" + animator.GetFloat("MoveY"));
    //    }
           

    //}
    //public void LookUp()
    //{
    //    animator.SetFloat("MoveX", 0);
    //    animator.SetFloat("MoveY", 1);
    //    animator.SetBool("IsWalking", false);

    //    var pm = GetComponent<PlayerManager>();
    //    if (pm != null)
    //    {
    //        pm.lastMoveDir = Vector2.up;
    //        Debug.Log("Spawner LookUp: lastMoveDir=" + pm.lastMoveDir + ", Animator MoveY=" + animator.GetFloat("MoveY"));
    //    }
    //}

}
