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
   
    AI_Movement _infantry_AI;
    FOVAgent _fov;
    Fireteam myFireteam;
    public EventFSM<INFANTRY_STATES> infantry_FSM;

    public Entity actualTarget;
    protected override void EntityAwake()
    {
        _infantry_AI = GetComponent<AI_Movement>();
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
            _infantry_AI.SetDestination(myFireteam.newPos);
            StartCoroutine(LookForTargets());
        };

        state.OnExit += (x) => 
        { 
            StopCoroutine(LookForTargets());
            _infantry_AI.CancelMovement();
        };

        return state;
    }

    #region Metodos Utiles
    IEnumerator LookForTargets()
    {
        while (true)
        {
            var z = GetEntitiesAround()                               
           .Where(x => x.myTeam != myTeam)
           .Where(x => _fov.IN_FOV(x.transform.position));

            if (z.Any()) infantry_FSM.SendInput(INFANTRY_STATES.FireAtWill);

            for (int i = 0; i < 30; i++)
                yield return null;
        }
    }


    IEnumerable<Entity> GetEntitiesAround()
    {
        var z = gridEntity.GetEntitiesInRange(_fov.viewRadius)
         .Where(x => x != this)
         .OfType<Entity>()
         .Where(x => x.GetType() != typeof(Civilian));



        return z;
    }

    IEnumerator SetTarget()
    {
        while (true)
        {
           actualTarget = GetEntitiesAround().Minimum(GetWeakestAndNearest);
            if (actualTarget)
            {

            }
        }
    }
    #endregion

    float GetWeakestAndNearest(Entity entity)
    {
        float result=0;
        result += Vector3.Distance(transform.position, entity.transform.position);
        result += entity.health.life;
        return result;
    }

    State<INFANTRY_STATES> FireAtWill()
    {
        State<INFANTRY_STATES> state = new State<INFANTRY_STATES>("FireAtWill");

        state.OnEnter += (x) => 
        { 

        };

        return state;
    }

    public void MoveTowardsTransition()
    {
        infantry_FSM.SendInput(INFANTRY_STATES.MoveTowards);
    }

    public void FollowLeaderTransition()
    {
        infantry_FSM.SendInput(INFANTRY_STATES.FollowLeader);
    }
}
