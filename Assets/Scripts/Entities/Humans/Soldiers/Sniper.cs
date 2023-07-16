using IA2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ShootComponent))]
[RequireComponent(typeof(FOVAgent))]
[RequireComponent(typeof(LineRenderer))]
public class Sniper : Soldier
{
    public enum SNIPER_STATES
    {

        LOOK_FOR_TARGETS,
        AIM,
        SHOOT,
        DIE
    }

    public event Action OnEnemyFound = delegate { };

    EventFSM<SNIPER_STATES> _fsm;

    ShootComponent _shootComponent;
    [SerializeField] Transform _shootPos;
    FOVAgent _fovAgent;
    LineRenderer _laser;
    public Soldier target { get; private set; }

    [SerializeField] int FramesBetweenSearch = 4, maxShootsInRow = 1;
    [SerializeField] float _requiredFocusTime, _aimSpeed;
    float _currentAimLerp, _currentFocusTime;
    int timesFocused = 1;
    [SerializeField] float _addPerTimesFocused;

    protected override void EntityAwake()
    {
        base.EntityAwake();
        _shootComponent = GetComponent<ShootComponent>();
        _fovAgent = GetComponent<FOVAgent>();
        _laser = GetComponent<LineRenderer>();
        _laser.enabled = false;

    }

    void CreateFSM()
    {
        var lookEnemies = LookForEnemies();
        var aimEnemy = AimAtEnemy();
        var shootAtEnemy = ShootAtEnemy();
        var die = Die();

        StateConfigurer.Create(lookEnemies)
            .SetTransition(SNIPER_STATES.AIM, aimEnemy)
            .Done();

        StateConfigurer.Create(aimEnemy)
           .SetTransition(SNIPER_STATES.LOOK_FOR_TARGETS, lookEnemies)
           .SetTransition(SNIPER_STATES.SHOOT, shootAtEnemy)
           .SetTransition(SNIPER_STATES.DIE, die)
           .Done();

        StateConfigurer.Create(die)
            .Done();
    }


    State<SNIPER_STATES> LookForEnemies()
    {
        var state = new State<SNIPER_STATES>("Look For Enemies");
        Action _onFound = () =>
        {
            _fsm.SendInput(SNIPER_STATES.AIM);
        };

        state.OnEnter += (x) =>
        {
            OnEnemyFound += _onFound;
            StartCoroutine(LookForEnemiesCoroutine());

        };

        state.OnExit += (x) =>
        {
            OnEnemyFound -= _onFound;         
        };

        return state;
    }

    Soldier GetFurthestEnemy()
    {
        return _gridEntity.GetEntitiesInRange(_fovAgent.viewRadius)
            .OfType<Soldier>()
            .Where(x => x.Team != Team && x.Team != MilitaryTeam.None)
            .Where(x => _fovAgent.IN_FOV(x.transform.position))
            .Maximum(x => Vector3.SqrMagnitude(x.transform.position - transform.position));
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
            _currentAimLerp = 0;

        };

        state.OnUpdate += () =>
        {
            if (!_fovAgent.IN_FOV(target.transform.position) || !target.Health.isAlive) _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);

            Vector3 dir = target.transform.position - transform.position;
            if (_currentAimLerp < 1)
            {
                _currentAimLerp += Time.deltaTime * _aimSpeed;
                Vector3 aux = Vector3.Slerp(transform.forward, dir.normalized, _currentAimLerp);
                transform.forward = new Vector3(aux.x, transform.forward.y, aux.z);
                return;
            }


            _currentAimLerp = 1;
            transform.forward = dir.normalized;
            _currentFocusTime += Time.deltaTime;
            if (_currentFocusTime >= _requiredFocusTime)
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
            _currentFocusTime = 0;
            timesFocused = 1;

        };

        state.OnUpdate += () =>
        {
            if (!_fovAgent.IN_FOV(target.transform.position) || !target.Health.isAlive) _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);

            Vector3 dir = target.transform.position - transform.position;
         
            transform.forward = dir.normalized;

            _currentFocusTime += Time.deltaTime * (_addPerTimesFocused * timesFocused);

            if (_currentFocusTime >= _requiredFocusTime)
            {
                _currentFocusTime = 0;
                _shootComponent.Shoot(_shootPos);
                timesFocused++;

                if (timesFocused > maxShootsInRow) _fsm.SendInput(SNIPER_STATES.AIM);
            }
        };

        state.OnExit += (x) =>
        {
            _currentFocusTime = 0;
            timesFocused = 0;
        };

        return state;
    }

    State<SNIPER_STATES> Die()
    {
        State<SNIPER_STATES> state = new State<SNIPER_STATES>("Die");

        state.OnEnter += (x) =>
        {
            DebugEntity.Log("Die");
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

    public void InitializeUnit(MilitaryTeam newTeam)
    {
        Team = newTeam;
    }
}
