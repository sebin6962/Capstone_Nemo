using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint; 
    public SeatManager seatManager;
    public List<Route> routesPerSeat;

    public float spawnInterval;
    private float timer;

    [SerializeField] private float questCustomerChance = 0.2f;
    [SerializeField] private GameObject questCustomerPrefab;

    [SerializeField] private bool tutorialMode = false;

    void Awake()
    {
        Debug.Log($"[CustomerSpawner] Awake name={name}, tutorialMode={tutorialMode}");
    }

    void Update()
    {
        if (tutorialMode)
            return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            TrySpawnCustomer();
        }
    }

    void TrySpawnCustomer()
    {
        int seatIndex = seatManager.GetAvailableSeatIndex();
        if (seatIndex >= 0 && seatIndex < routesPerSeat.Count)
        {
            GameObject prefab = (Random.value < questCustomerChance) ? questCustomerPrefab : customerPrefab;
            GameObject customer = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            seatManager.OccupySeat(seatIndex);

            Transform[] path = routesPerSeat[seatIndex].waypoints;
            Customer customerScript = customer.GetComponent<Customer>();
            customerScript.Initialize(path);
            customerScript.SetSeatInfo(seatIndex, seatManager);
        }
    }

    public void SpawnTutorialCustomer(string tutorialDagwaId, float delay = 0f)
    {
        tutorialMode = true;  // 자동 스폰 막기
        StartCoroutine(SpawnCustomerDelay(tutorialDagwaId, delay));
    }

    private IEnumerator SpawnCustomerDelay(string tutorialDawgaId, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        int seatIndex = seatManager.GetAvailableSeatIndex();

        seatManager.OccupySeat(seatIndex);

        GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);

        Transform[] path = routesPerSeat[seatIndex].waypoints;
        Customer customerScript = customer.GetComponent<Customer>();

        customerScript.Initialize(path);
        customerScript.SetSeatInfo(seatIndex, seatManager);
        customerScript.SetTutorialCustomer("baekseolgi_finish");
    }

    public void EndTutorial()
    {
        tutorialMode = false;
        timer = 0f;
    }
}
