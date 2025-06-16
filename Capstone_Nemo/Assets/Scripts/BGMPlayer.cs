using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    private static BGMPlayer instance;

    void Awake()
    {
        // 이미 존재하는 BGMPlayer가 있으면 새로 생성된 건 파괴
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴되지 않도록 함
    }
    //BGM 교체 함수(예: 특정 씬에서 음악 바꾸기)도 이후에 추가
}
