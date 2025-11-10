using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootstepSceneSetting : MonoBehaviour
{
    public AudioClip sceneFootstepClip;  // 이 씬에서 사용할 발소리 (잔디 또는 나무)

    void Start()
    {
        if (SFXManager.Instance != null && sceneFootstepClip != null)
        {
            SFXManager.Instance.SetPlayerWalkClip(sceneFootstepClip);
        }
    }
}
