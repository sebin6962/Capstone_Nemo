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
            // ���� ��ġ���� z���� 0����
            Vector3 spawnPos = spawnPoint.transform.position;
            spawnPos.z = 0f; // z���� 0���� ����
            transform.position = spawnPos;

            Debug.Log($"[Spawner] entranceID: {entrance}");

            // ���� ����
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
