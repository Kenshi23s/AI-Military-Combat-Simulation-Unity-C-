using IA2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(AI_Movement))]
public class Civilian : Entity
{
    public enum CivilianStates
    {
        Idle,
        Pray,
        LookForRefugee,
        Die
    }
    AI_Movement civilian_AI;
    Bunker nearestBunker;

    EventFSM<CivilianStates> civilianFSM;
    protected override void EntityAwake()
    {
        civilian_AI = GetComponent<AI_Movement>();
    }
    public State<CivilianStates> RunToRefugee()
    {
        State<CivilianStates> run = new State<CivilianStates>("LookForRefugee");

        run.OnEnter += (x) =>
        {
            nearestBunker = GameManager.instance.bunkers.Minimum(x=>Vector3.Distance(x.transform.position,transform.position));
            civilian_AI.SetDestination(nearestBunker.transform.position);
           // si el bunker sigue existiendo tratar de entrar
           //sino rezar C:
           // en el final todos estamos solos
        };


        return run;
    }




 
}
