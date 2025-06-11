using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpwaner : MonoBehaviour
{
    void Start()
    {
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

            // ���� ���� (������)
            //var animator = GetComponent<PlayerAnimator>();
            //if (entrance == "FromVillage") animator?.LookDown();
            //else if (entrance == "FromStore") animator?.LookUp();
        }
    }
}
