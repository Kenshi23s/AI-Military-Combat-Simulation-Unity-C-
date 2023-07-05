using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Red,
    Green
}
public interface InitializeUnit
{
    void InitializeUnit(Team newTeam);
}

public class TeamsManager : MonoSingleton<TeamsManager>
{
    protected override void SingletonAwake()
    {
       
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
