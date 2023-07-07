using IA2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(AI_Movement))]
[RequireComponent(typeof(FOVAgent))]
public abstract class Infantry : Entity 
{ 


    public enum INFANTRY_STATES
    {
        MoveTowards,
        FollowLeader,
        Die,
        Shoot,
        FireAtWill
    }
   
    AI_Movement _ai_Move;
    FOVAgent _fov;
    Fireteam myFireteam;
    public EventFSM<INFANTRY_STATES> infantry_FSM;
        
    protected override void EntityAwake()
    {
        _ai_Move = GetComponent<AI_Movement>();
        _fov = GetComponent<FOVAgent>();
    }

    void SetFSM()
    {
        var moveTowards = MoveTowards();
    }
   
    State<INFANTRY_STATES> MoveTowards()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("MoveTowards");

        state.OnEnter += (x) =>
        {
            _ai_Move.SetDestination(myFireteam.newPos);
            StartCoroutine(LookForTargets());
        };



        state.OnExit += (x) => 
        { 
            StopCoroutine(LookForTargets());
            _ai_Move.CancelMovement();
        };

        return state;
    }

    IEnumerator LookForTargets()
    {
        while (true)
        {
            var z = gridEntity.GetEntitiesInRange(_fov.viewRadius)
           .Where(x => x != this)
           .OfType<Entity>()
           .Where(x => x.myTeam!=myTeam)
           .Where(x => _fov.IN_FOV(x.transform.position));

            if (z.Any()) infantry_FSM.SendInput(INFANTRY_STATES.FireAtWill);

            for (int i = 0; i < 30; i++)           
                yield return null;           
        }
    }



    public void MoveTowardsTransition()
    {
        infantry_FSM.SendInput(INFANTRY_STATES.MoveTowards);
    }

    public void FollowLeaderTransition()
    {
        infantry_FSM.SendInput(INFANTRY_STATES.FollowLeader);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
