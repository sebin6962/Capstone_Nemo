using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TimeRouteSlot
{
    [Header("이 시간부터 (시)")]
    public int startHour;   // 예: 9
    [Header("이 시간까지 (시, 포함X)")]
    public int endHour;     // 예: 12 → 9:00 ~ 11:59

    [Header("이 시간대에 탈 웨이포인트 루트")]
    public Transform[] waypoints;
}

public class NPCTimeSchedule : MonoBehaviour
{
    public NPCPatrolRoute patrol;          // 같은 오브젝트에 붙은 Patrol 스크립트
    public TimeRouteSlot[] slots;         

    private void Start()
    {
        ApplyScheduleForCurrentTime();
    }

    public void ApplyScheduleForCurrentTime()
    {
        if (TimeManager.Instance == null)
        {
            Debug.LogWarning("TimeManager 없음");
            return;
        }

        int hour = TimeManager.Instance.hour;
        int minute = TimeManager.Instance.minute;
        int nowMinutes = hour * 60 + minute;

        // 1) 현재 시간에 맞는 슬롯 찾기
        TimeRouteSlot slot = FindSlot(nowMinutes);
        if (slot == null || slot.waypoints == null || slot.waypoints.Length == 0)
        {
            patrol.SetActive(false);
            return;
        }

        // 2) 해당 루트를 Patrol에 세팅
        patrol.SetRoute(slot.waypoints, resetIndex: true);

        // 3) 시간대 안에서 얼마나 지났는지(0~1) 계산
        int slotStartMinutes = slot.startHour * 60;
        int slotEndMinutes = slot.endHour * 60;
        int slotDuration = Mathf.Max(1, slotEndMinutes - slotStartMinutes); // 0 방지

        float elapsedInSlot = Mathf.Clamp(nowMinutes - slotStartMinutes, 0, slotDuration);
        float tNorm = elapsedInSlot / slotDuration; // 0~1

        // 4) tNorm을 웨이포인트 진행도에 매핑
        Transform[] wps = slot.waypoints;
        if (wps.Length == 1)
        {
            // 웨이포인트가 1개면 그냥 거기에 세워두기
            transform.position = wps[0].position;
            patrol.ForceSetProgress(0); 
        }
        else
        {
            float lastIndexFloat = (wps.Length - 1);   // 구간 개수 기준
            float routePos = tNorm * lastIndexFloat;   // 0 ~ (N-1)
            int index = Mathf.FloorToInt(routePos);
            float segT = routePos - index;

            if (index >= wps.Length - 1)
            {
                index = wps.Length - 2;
                segT = 1f;
            }

            Vector3 pos = Vector3.Lerp(wps[index].position, wps[index + 1].position, segT);
            transform.position = pos;

            // 다음 프레임부터는 index+1 웨이포인트를 향해 계속 진행
            patrol.ForceSetProgress(index + 1);
        }
    }

    private TimeRouteSlot FindSlot(int nowMinutes)
    {
        foreach (var s in slots)
        {
            int start = s.startHour * 60;
            int end = s.endHour * 60;
            if (nowMinutes >= start && nowMinutes < end)
                return s;
        }
        return null;
    }
}
