using IA2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Infantry : Entity,InitializeUnit
{
    public Team myTeam;

   
        
    public void InitializeUnit(Team newTeam)
    {
        myTeam = newTeam;
    }



    protected override void EntityAwake()
    {
       
    }

    
   
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
