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
            // 기존 위치에서 z값만 0으로
            Vector3 spawnPos = spawnPoint.transform.position;
            spawnPos.z = 0f; // z값을 0으로 고정
            transform.position = spawnPos;

            Debug.Log($"[Spawner] entranceID: {entrance}");

            // 방향 예시 (선택적)
            //var animator = GetComponent<PlayerAnimator>();
            //if (entrance == "FromVillage") animator?.LookDown();
            //else if (entrance == "FromStore") animator?.LookUp();
        }
    }
}
