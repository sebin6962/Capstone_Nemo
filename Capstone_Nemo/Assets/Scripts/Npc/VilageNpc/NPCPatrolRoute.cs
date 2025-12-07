using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCPatrolRoute : MonoBehaviour
{
    [Header("웨이포인트 설정")]
    public Transform[] waypoints;
    public float moveSpeed = 2f;
    public float stopDistance = 0.05f;   // 이 거리 안으로 들어오면 도착으로 처리
    public float waitTimeAtPoint = 1f;   // 각 포인트에서 잠깐 쉬는 시간
    public bool loop = true;             // 끝까지 가면 다시 처음으로

    [Header("애니메이션")]
    public Animator animator;            // NPC의 Animator
    // Animator에 isMoving(bool), moveX(float), moveY(float) 파라미터가 있다고 가정

    private int currentIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private bool isActive = true;        // 스케줄에 따라 켜고 끄기용

    // 대각선 제거를 위한 축 이동 상태
    private enum MoveAxis { Horizontal, Vertical }
    private MoveAxis currentAxis = MoveAxis.Horizontal;
    private Vector3 currentTarget;       // 현재 이동 중인 목표 위치

    private void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            // 첫 번째 웨이포인트로 가는 경로 준비
            PrepareMoveToCurrentWaypoint();
        }
    }

    private void Update()
    {
        if (!isActive)
        {
            // 비활성화 상태에서는 가만히 있는 애니메이션
            UpdateAnimation(Vector3.zero);
            return;
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            UpdateAnimation(Vector3.zero);
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            // 기다리는 동안은 Idle
            UpdateAnimation(Vector3.zero);

            if (waitTimer <= 0f)
            {
                isWaiting = false;

                // 다음 웨이포인트가 유효하면 그쪽으로 이동 준비
                if (currentIndex < waypoints.Length)
                {
                    PrepareMoveToCurrentWaypoint();
                }
            }
            return;
        }

        // 실제 이동 로직
        Vector3 moveDir = MoveAlongAxis();  // 수평 또는 수직으로만 이동
        UpdateAnimation(moveDir);
    }

    /// <summary>
    /// 현재 currentIndex 웨이포인트로 가기 위한 축/타겟 설정
    /// </summary>
    private void PrepareMoveToCurrentWaypoint()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (currentIndex >= waypoints.Length) return;

        currentTarget = waypoints[currentIndex].position;

        // 어느 축을 먼저 갈지 결정 (여기서는 X축 먼저, 필요하면 Y축 먼저로 바꿔도 됨)
        float dx = Mathf.Abs(currentTarget.x - transform.position.x);
        float dy = Mathf.Abs(currentTarget.y - transform.position.y);

        currentAxis = dx >= dy ? MoveAxis.Horizontal : MoveAxis.Vertical;
    }

    /// <summary>
    /// 수평 / 수직 중 한 축으로만 이동시키고, 이동 방향 벡터를 반환
    /// </summary>
    private Vector3 MoveAlongAxis()
    {
        Vector3 pos = transform.position;
        Vector3 moveDir = Vector3.zero;

        float dx = currentTarget.x - pos.x;
        float dy = currentTarget.y - pos.y;

        // 먼저 선택된 축을 기준으로 이동
        if (currentAxis == MoveAxis.Horizontal)
        {
            // 아직 X축이 충분히 안 맞았으면 X축으로만 이동
            if (Mathf.Abs(dx) > stopDistance)
            {
                float step = Mathf.Sign(dx) * moveSpeed * Time.deltaTime;
                if (Mathf.Abs(step) > Mathf.Abs(dx)) step = dx; // 오버슈팅 방지

                pos.x += step;
                moveDir = new Vector3(Mathf.Sign(dx), 0f, 0f);
            }
            else
            {
                // X축이 거의 맞았으면 타겟에 스냅하고, 이제부터는 Y축 이동으로 전환
                pos.x = currentTarget.x;
                currentAxis = MoveAxis.Vertical;
            }
        }

        // 수평 이동이 끝났거나 처음부터 수직을 먼저 선택한 경우
        if (currentAxis == MoveAxis.Vertical)
        {
            dy = currentTarget.y - pos.y;

            if (Mathf.Abs(dy) > stopDistance)
            {
                float step = Mathf.Sign(dy) * moveSpeed * Time.deltaTime;
                if (Mathf.Abs(step) > Mathf.Abs(dy)) step = dy;

                pos.y += step;
                // 수직 이동일 때만 moveDir 설정 (수평 축에서 이미 도착했다면)
                moveDir = new Vector3(0f, Mathf.Sign(dy), 0f);
            }
            else
            {
                // Y축까지 다 도착 → 웨이포인트 도착 처리
                pos.y = currentTarget.y;
                OnArrivedAtWaypoint();
            }
        }

        transform.position = pos;
        return moveDir;
    }

    /// <summary>
    /// 웨이포인트에 도착했을 때 처리 (대기 후 다음 웨이포인트로)
    /// </summary>
    private void OnArrivedAtWaypoint()
    {
        //isWaiting = true;
        //waitTimer = waitTimeAtPoint;

        // 1. 지금 도착한 웨이포인트 정보 가져오기
        Transform wp = waypoints[currentIndex];
        float wait = 0f;

        WayPointInfo info = wp.GetComponent<WayPointInfo>();
        if (info != null && info.stopHere)
        {
            wait = info.waitTime;
        }

        // 2. 기다릴지 말지 결정
        if (wait > 0f)
        {
            isWaiting = true;
            waitTimer = wait;
        }
        else
        {
            isWaiting = false;
        }

        // 3. 다음 웨이포인트 인덱스 이동
        currentIndex++;

        if (currentIndex >= waypoints.Length)
        {
            if (loop && waypoints.Length > 0)
            {
                currentIndex = 0;
            }
            else
            {
                // 루프 안 하면 마지막 포인트에서 멈춤
                isActive = false;
                UpdateAnimation(Vector3.zero);
                return;
            }
        }

        // 4. 기다리지 않는 웨이포인트라면
        //    바로 다음 웨이포인트 타겟
        if (!isWaiting && isActive && waypoints != null && waypoints.Length > 0)
        {
            PrepareMoveToCurrentWaypoint();
        }
    }

    /// <summary>
    /// 애니메이터 파라미터 업데이트 (4방향 걷기)
    /// </summary>
    private void UpdateAnimation(Vector3 moveDir)
    {
        if (animator == null) return;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            animator.SetBool("isMoving", true);
            animator.SetFloat("moveX", moveDir.x);
            animator.SetFloat("moveY", moveDir.y);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }
    }

    // 시간 스케줄에서 켜고/끄기용
    public void SetActive(bool value)
    {
        isActive = value;

        if (!isActive)
        {
            // 비활성화되면 애니메이션도 Idle로
            UpdateAnimation(Vector3.zero);
        }
        else
        {
            // 다시 켤 때 현재 인덱스 기준으로 경로 준비
            if (waypoints != null && waypoints.Length > 0)
            {
                PrepareMoveToCurrentWaypoint();
            }
        }
    }

    // 시간대별로 다른 루트를 쓰고 싶으면, 외부에서 waypoints를 갈아끼워도 됨
    public void SetRoute(Transform[] newWaypoints, bool resetIndex = true)
    {
        waypoints = newWaypoints;

        if (resetIndex)
            currentIndex = 0;

        isActive = true;

        if (waypoints != null && waypoints.Length > 0)
        {
            PrepareMoveToCurrentWaypoint();
        }
    }
}
