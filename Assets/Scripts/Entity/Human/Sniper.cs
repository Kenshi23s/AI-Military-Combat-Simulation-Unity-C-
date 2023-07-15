using IA2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
[RequireComponent(typeof(ShootComponent))]
[RequireComponent(typeof(FOVAgent))]
[RequireComponent(typeof(LineRenderer))]

public class Sniper : GridEntity, InitializeUnit
{
    public enum SNIPER_STATES
    {

        LOOK_FOR_TARGETS,
        AIM,
        SHOOT,
        DEAD
    }

    public event Action OnEnemyFound = delegate { };

    EventFSM<SNIPER_STATES> _fsm;

    ShootComponent _shootComponent;
    [SerializeField] Transform _shootPos;
    FOVAgent _fovAgent;
    LineRenderer _laser;
    public GridEntity target { get; private set; }

    [SerializeField] int FramesBetweenSearch;
    [SerializeField] float RequiredFocusTime, AimSpeed;
    float currentAimSlerp,currentFocusTime;

    protected override void EntityAwake()
    {
        base.EntityAwake();
        _shootComponent = GetComponent<ShootComponent>();
        _fovAgent = GetComponent<FOVAgent>();
        _laser = GetComponent<LineRenderer>();
        _laser.enabled = false;

    }

    public override void GridEntityStart()
    {

    }

    void CreateFSM()
    {






    }


    State<SNIPER_STATES> LookForEnemies()
    {
        var state = new State<SNIPER_STATES>("Look For Enemies");
        Action found = () =>
        {
            _fsm.SendInput(SNIPER_STATES.AIM);
        };

        state.OnEnter += (x) =>
        {
            OnEnemyFound += found;
            StartCoroutine(LookForEnemiesCoroutine());

        };

        state.OnExit += (x) =>
        {
            OnEnemyFound -= found;         
        };

        return state;
    }

    GridEntity GetFurthestEnemy()
    {
        return GetEntitiesInRange(_fovAgent.viewRadius)
            .Where(x => x.MyTeam != MyTeam && x.MyTeam != Team.None)
            .NotOfType<GridEntity,Vehicle>()
            .Where(x => _fovAgent.IN_FOV(x.transform.position))
            .Maximum(x => Vector3.Distance(x.transform.position, transform.position));
    }

    IEnumerator LookForEnemiesCoroutine()
    {
        while (true)
        {
            for (int i = 0; i < FramesBetweenSearch; i++) yield return null;
            var aux = GetFurthestEnemy();
            if (aux != null)
            {
                target = aux;
                OnEnemyFound();
                break;
            }
        } 
    }


    State<SNIPER_STATES> AimAtEnemy()
    {
        var state = new State<SNIPER_STATES>("Aim At Enemy");

        state.OnEnter += (x) =>
        {
            if (target == null) _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);
            currentAimSlerp = 0;

        };

        state.OnUpdate += () =>
        {
            if (!_fovAgent.IN_FOV(target.transform.position)) _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);

            Vector3 dir = target.transform.position - transform.position;
            if (currentAimSlerp < 1)
            {
                currentAimSlerp += Time.deltaTime * AimSpeed;
                transform.forward = Vector3.Slerp(transform.forward, dir.normalized, currentAimSlerp);
                return;
            }
           
            
            currentAimSlerp = 1;
            transform.forward = dir.normalized;
            currentFocusTime += Time.deltaTime;
            if (currentFocusTime >= RequiredFocusTime)
            {

            }

            
            


        };

        return state;
    }

    State<SNIPER_STATES> ShootAtEnemy()
    {
        var state = new State<SNIPER_STATES>("Aim At Enemy");

        state.OnEnter += (x) =>
        {
            if (target == null) _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);
            currentAimSlerp = 0;

        };

        state.OnUpdate += () =>
        {
            if (!_fovAgent.IN_FOV(target.transform.position)) _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);

            Vector3 dir = target.transform.position - transform.position;
         
            transform.forward = dir.normalized;
            currentFocusTime += Time.deltaTime;
            if (currentFocusTime >= RequiredFocusTime)
            {

            }





        };

        return state;
    }




    void ActivateLaser()
    {
         if (target == null) return;
         _laser.enabled = true;
         _laser.SetPosition(0,_shootPos.position);
         _laser.SetPosition(1, target.transform.position);
    }

    public void InitializeUnit(Team newTeam)
    {
        MyTeam = newTeam;
    }
}
