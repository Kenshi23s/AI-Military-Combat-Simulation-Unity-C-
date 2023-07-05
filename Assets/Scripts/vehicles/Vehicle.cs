using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IA2;

public struct Seat
{
    public bool Available;
    public Infantry passenger;
    public Transform seatPos;
}

[RequireComponent(typeof(LifeComponent))]
[RequireComponent(typeof(DebugableObject))]
public abstract class Vehicle : MonoBehaviour
{
    public Team myTeam;
    public List<Seat> passengers = new List<Seat>();

    public void Initialize(Team newTeam)
    {
        myTeam = newTeam;

    }
}
