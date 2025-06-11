using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint; 
    public SeatManager seatManager;
    public List<Route> routesPerSeat;

    public float spawnInterval = 5f;
    private float timer;

    [SerializeField] private float questCustomerChance = 0.2f;
    [SerializeField] private GameObject questCustomerPrefab;

    void Update()
    {
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
            GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
            seatManager.OccupySeat(seatIndex);

            Transform[] path = routesPerSeat[seatIndex].waypoints;
            Customer customerScript = customer.GetComponent<Customer>();
            customerScript.Initialize(path);
            customerScript.SetSeatInfo(seatIndex, seatManager);
        }
    }
}
