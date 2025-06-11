using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeatManager : MonoBehaviour
{
    public Transform[] seatPositions;
    public bool[] seatOccupied;

    void Awake()
    {
        seatOccupied = new bool[seatPositions.Length];
    }

    public int GetAvailableSeatIndex()
    {
        for (int i = 0; i < seatOccupied.Length; i++)
        {
            if (!seatOccupied[i]) return i;
        }
        return -1;
    }

    public void OccupySeat(int index)
    {
        seatOccupied[index] = true;
    }

    public void VacateSeat(int index)
    {
        seatOccupied[index] = false;
    }
}
