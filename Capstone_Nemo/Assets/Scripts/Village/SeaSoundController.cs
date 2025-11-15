using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaSoundController : MonoBehaviour
{
    [Header("참조")]
    public Transform player;     // 플레이어 Transform
    public Transform seaPoint;   // 바다 기준 위치(보통 이 오브젝트 자신)

    [Header("세기 설정")]
    public float maxDistance = 15f;   // 이 거리 밖에서는 안 들림
    public float maxVolume = 1f;      // 가장 가까울 때 볼륨
    public float fadeSpeed = 5f;      // 볼륨 변화 속도 (클수록 더 빠르게)

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (seaPoint == null)
            seaPoint = transform;
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.volume = 0f;
        }
    }

    private void Update()
    {
        if (player == null || seaPoint == null || audioSource == null)
            return;

        // 플레이어와 바다 기준점 사이 거리
        float dist = Vector3.Distance(player.position, seaPoint.position);

        // targetVolume 계산: 가까울수록 1, 멀수록 0
        float targetVolume = 0f;
        if (dist < maxDistance)
        {
            // dist=0일 때 1, dist=maxDistance일 때 0
            float t = 1f - (dist / maxDistance);
            targetVolume = Mathf.Clamp01(t) * maxVolume;
        }

        // 서서히 볼륨 조정
        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * fadeSpeed);

        // 볼륨이 충분히 크면 재생, 거의 0이면 Stop
        if (audioSource.volume > 0.01f)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }
}
