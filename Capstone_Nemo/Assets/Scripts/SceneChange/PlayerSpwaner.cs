using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpwaner : MonoBehaviour
{
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        string entrance = SceneTransitionInfo.Instance.entranceID;
        GameObject spawnPoint = GameObject.Find(entrance);

        if (spawnPoint != null)
        {
            //transform.position = spawnPoint.transform.position;
            // 기존 위치에서 z값만 0으로
            Vector3 spawnPos = spawnPoint.transform.position;
            spawnPos.z = 0f; // z값을 0으로 고정
            transform.position = spawnPos;

            Debug.Log($"[Spawner] entranceID: {entrance}");

            // 방향 지정
            var animator = GetComponent<Animator>();
            if (entrance == "FromVillage(Store)") LookUp();
            else if (entrance == "FromPlayerStore") LookDown();
        }
    }

    public void LookDown()
    {
        animator.SetFloat("MoveX", 0);
        animator.SetFloat("MoveY", -1);
        animator.SetBool("IsWalking", false);

        var pm = GetComponent<PlayerManager>();
        if (pm != null)
            pm.lastMoveDir = Vector2.down;
    }
    public void LookUp()
    {
        animator.SetFloat("MoveX", 0);
        animator.SetFloat("MoveY", 1);
        animator.SetBool("IsWalking", false);

        var pm = GetComponent<PlayerManager>();
        if (pm != null)
            pm.lastMoveDir = Vector2.up;
    }

}
