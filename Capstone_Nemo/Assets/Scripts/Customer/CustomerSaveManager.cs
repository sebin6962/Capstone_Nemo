using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSaveManager : MonoBehaviour
{
    public static CustomerSaveManager Instance;

    public List<CustomerSave> save = new List<CustomerSave>();

    private float spawnInterval;
    private int maxSeats;
    private float spawnTimer;
    private int prefabCount;

    private bool simulateWhileStoreClosed = false;

    private float[] prefabOrderTimes;

    private bool ignoreSaveFromScene = false;
    private string currentServerName = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentServerName =
            PlayerPrefs.GetString(
                "SelectedSave",
                ""
            );
    }

    public void SwitchToServer(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
        {
            Debug.LogError(
                "[CustomerSaveManager] 변경할 슬롯 이름이 비어 있습니다."
            );

            return;
        }

        // 현재 슬롯과 같으면 유지
        if (currentServerName == serverName)
        {
            return;
        }

        Debug.Log(
            "[CustomerSaveManager] 손님 데이터 슬롯 전환: " +
            $"{currentServerName} → {serverName}"
        );

        currentServerName = serverName;

        // 이전 슬롯의 손님이 새 슬롯에 넘어가지 않도록 초기화
        save.Clear();

        simulateWhileStoreClosed = false;
        spawnTimer = 0f;
        ignoreSaveFromScene = false;
    }

    public void ConfigureFromSpawner(CustomerSpawner spawner)
    {
        string selectedServer =
        PlayerPrefs.GetString(
            "SelectedSave",
            ""
        );

        // SaveSelect 씬에 CustomerSaveManager가 없었던 경우에도
        // 상점 진입 시 현재 슬롯과 동기화
        if (!string.IsNullOrWhiteSpace(selectedServer) &&
            currentServerName != selectedServer)
        {
            SwitchToServer(selectedServer);
        }

        ignoreSaveFromScene = false;

        spawnInterval = spawner.spawnInterval;
        maxSeats = spawner.routesPerSeat.Count;
        prefabCount = spawner.customerPrefab.Length;

        prefabOrderTimes = new float[prefabCount];
        for (int i = 0; i < prefabCount; i++)
        {
            var customer = spawner.customerPrefab[i].GetComponent<Customer>();
            if (customer != null)
                prefabOrderTimes[i] = customer.OrderTimeLimit;
            else
                prefabOrderTimes[i] = 20f;
        }
    }

    private void Update()
    {
        if (!simulateWhileStoreClosed)
            return;

        float dt = Time.deltaTime;

        //타이머 감소, 타이머 종료
        for (int i = save.Count - 1; i >= 0; i--)
        {
            var s = save[i];

            if (s.state == CustomerState.Waiting || s.state == CustomerState.Ordering)
            {
                s.remainingTime -= dt;

                if (s.remainingTime <= 0f)
                {
                    s.state = CustomerState.Displeased;
                    save.RemoveAt(i);
                }
            }
        }

        //새 손님 스폰
     /*   spawnTimer += dt;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnVirtualCustomer();
        }*/
    }

    private void TrySpawnVirtualCustomer()
    {
        int usedCount = save.Count;
        if (usedCount >= maxSeats)
            return;

        int seatIndex = GetFreeSeatIndex();
        if (seatIndex == -1)
            return;

        int prefabIndex = Random.Range(0, prefabCount);

        float limit = 20f;
        if (prefabOrderTimes != null &&
            prefabIndex >= 0 && prefabIndex < prefabOrderTimes.Length)
        {
            limit = prefabOrderTimes[prefabIndex];
        }

        var data = new CustomerSave
        {
            seatIndex = seatIndex,
            state = CustomerState.Ordering,
            isTutorialCustomer = false,
            tutorialDagwaId = null,
            orderedDagwa = OrderManager.Instance.GetRandomDagwaList(),
            orderTimeLimit = limit,
            remainingTime = limit,
            currentWaypointIndex = 0,
            position = Vector3.zero,
            prefabIndex = prefabIndex,
            hasScenePosition = false
        };

        save.Add(data);
    }

    private int GetFreeSeatIndex()
    {
        bool[] used = new bool[maxSeats];

        foreach (var s in save)
        {
            if (s.seatIndex >= 0 && s.seatIndex < maxSeats)
                used[s.seatIndex] = true;
        }

        for (int i = 0; i < maxSeats; i++)
        {
            if (!used[i]) return i;
        }
        return -1;
    }

    public void SaveFromScene()
    {
        if (ignoreSaveFromScene)
        {
            return;
        }

        string selectedServer =
            PlayerPrefs.GetString(
                "SelectedSave",
                ""
            );

        // 슬롯 전환 중 이전 씬의 손님을
        // 새 슬롯 데이터로 저장하는 것을 방지
        if (string.IsNullOrWhiteSpace(selectedServer) ||
            currentServerName != selectedServer)
        {
            Debug.LogWarning(
                "[CustomerSaveManager] 슬롯이 일치하지 않아 " +
                "손님 데이터 수집을 건너뜁니다. " +
                $"현재={currentServerName}, " +
                $"선택={selectedServer}"
            );

            return;
        }

        save.Clear();

        var customers =
            FindObjectsOfType<Customer>();

        Debug.Log(
            "[CustomerSaveManager] SaveFromScene 호출, " +
            $"발견된 Customer 수={customers.Length}"
        );

        foreach (var customer in customers)
        {
            save.Add(customer.ToSave());
        }

        Debug.Log(
            "[CustomerSaveManager] 저장 완료, " +
            $"save.Count={save.Count}"
        );

        simulateWhileStoreClosed = true;
        spawnTimer = 0f;
    }

    public void ClearForNewDay()
    {
        Debug.Log("[CustomerSaveManager] 손님 저장 데이터 초기화(Day)");

        ignoreSaveFromScene = true;

        save.Clear();

        simulateWhileStoreClosed = false;
        spawnTimer = 0f;
    }

    public void ClearForExit()
    {
        Debug.Log("[CustomerSaveManager] 손님 저장 데이터 초기화(Exit)");

        ignoreSaveFromScene = true;

        save.Clear();

        simulateWhileStoreClosed = false;
        spawnTimer = 0f;
    }

    public void RestoreToScene(CustomerSpawner spawner)
    {
        simulateWhileStoreClosed = false;

        //기존 Customer 정리
        var oldCustomers = FindObjectsOfType<Customer>();
        foreach (var c in oldCustomers)
        {
            Destroy(c.gameObject);
        }

        //저장된 데이터 스폰
        foreach (var data in save)
        {
            spawner.SpawnFromSave(data);
        }
    }
}
