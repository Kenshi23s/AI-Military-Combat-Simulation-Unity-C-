using IA2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(GridEntity))]
[RequireComponent(typeof(FOVAgent))]
[RequireComponent(typeof(ShootComponent))]
public class Turret : Entity, IMilitary
{
    public enum TurretStates
    {
        REST,
        SEARCH_TARGETS,
        ALIGN,
        SHOOT
    }
    [field: SerializeField,Header("Turret")] public MilitaryTeam Team { get; private set; }

    public Plane Target { get; private set; }

    [SerializeField] Transform _pivotTurret,_pivotCanon;
    [SerializeField] float _baseMinAngle, _canonMinAngle;

    GridEntity _myGridEntity;
    FOVAgent _fov;
 
    [SerializeField] float _cd_BetweenSearch;

    [SerializeField,Header("Shooting")] 
    Transform[] _shootPos;
    [SerializeField] float _cd_BetweenShots;
    int _pos = 0;
    ShootComponent _ShootHandler;

    EventFSM<TurretStates> _turretFSM;



    protected override void EntityAwake()
    {
        _myGridEntity = GetComponent<GridEntity>();
        _ShootHandler = GetComponent<ShootComponent>();
        _fov = GetComponent<FOVAgent>();
  
        if (Physics.Raycast(transform.position,Vector3.down,out var hit,5f))
        {
            transform.position = hit.point;
            transform.up = hit.normal;
        }
        
    }

    private void Start()
    {
        SetFSM();
        TeamsManager.instance.AddToTeam(Team,this, default);
    }
    void SetFSM()
    {

        var restState = Rest_State();
        var searchTargets = SearchTargets_State();
        var AlignState = Align_State();
        var ShootState = Shoot_State();


        StateConfigurer.Create(restState)
            .SetTransition(TurretStates.SEARCH_TARGETS, searchTargets)
            .Done();

        StateConfigurer.Create(searchTargets)
            .SetTransition(TurretStates.REST, restState)
            .SetTransition(TurretStates.ALIGN,AlignState)
            .Done();

        StateConfigurer.Create(AlignState)
           .SetTransition(TurretStates.SEARCH_TARGETS, searchTargets)
           .SetTransition(TurretStates.SHOOT, ShootState)
           .Done();

        StateConfigurer.Create(ShootState)
          .SetTransition(TurretStates.REST, restState)
          .Done();


        _turretFSM = new EventFSM<TurretStates>(restState);
    }



    State<TurretStates> Rest_State()
    {
        var state = new State<TurretStates>("Rest");

        state.OnUpdate += () =>
        {
            if (!AlignBase(Vector3.zero) || !AlignCanon(Vector3.zero)) return;

            _pivotTurret.forward = Vector3.zero;
            _pivotCanon.forward  = Vector3.zero;
            _turretFSM.SendInput(TurretStates.SEARCH_TARGETS);         
        };


        return state;
    }


    State<TurretStates> SearchTargets_State()
    {
        var state = new State<TurretStates>("SEARCH");

        state.OnEnter += (x) => StartCoroutine(LookForHostilePlane());

        state.OnExit  += (x) => StopCoroutine(LookForHostilePlane());

        return state;
    }

    State<TurretStates> Align_State()
    {
        var state = new State<TurretStates>("ALIGN");

        state.OnUpdate += () =>
        {
            if (_fov.IN_FOV(Target.AimPoint) && Target.Health.isAlive) return;       

               Target = null;
               _turretFSM.SendInput(TurretStates.REST);         
        };

        state.OnUpdate += () =>
        {
            if (Target == null) return;
            
            Vector3 dir = Target.AimPoint - transform.position;

            if (AlignBase(dir) && AlignCanon(dir))          
               _turretFSM.SendInput(TurretStates.SHOOT);
        };

        return state;
    }

    State<TurretStates> Shoot_State()
    {
        var state = new State<TurretStates>("SHOOT");

        state.OnEnter += (x) => StartCoroutine(ShootBullets());

        state.OnUpdate += () =>
        {
            if (_fov.IN_FOV(Target.AimPoint) && Target.Health.isAlive) return;
            
                Target = null;
                _turretFSM.SendInput(TurretStates.ALIGN);         
        };

        state.OnUpdate += () =>
        {
            if (Target == null) return;

            Vector3 dir = Target.AimPoint - transform.position;

            AlignCanon(dir); AlignBase(dir);
            
            _turretFSM.SendInput(TurretStates.SHOOT);
        };

        state.OnExit += (x) => StopCoroutine(ShootBullets());
        
        return state;
    }

    Transform GetShootPos()
    {
        _pos++;
        if (_pos > _shootPos.Length) _pos = 0;

        return _shootPos[_pos];
    }

    IEnumerator ShootBullets()
    {
        var wait = new WaitForSeconds(_cd_BetweenShots);
        while (_turretFSM.CurrentKey == TurretStates.SHOOT)
        {
            var shootPos = GetShootPos();

            Vector3 dir = Target.AimPoint - shootPos.position;

            _ShootHandler.Shoot(shootPos,dir);

            yield return wait;
        }
    }

    IEnumerator LookForHostilePlane() 
    {
        var wait = new WaitForSeconds(_cd_BetweenSearch);
        while (true) 
        {

            Target = _myGridEntity.GetEntitiesInRange(_pos)
                .OfType<Plane>()
                .Where(x => _fov.IN_FOV(x.transform.position))
                .Minimum(x => Vector3.Distance(x.transform.position,transform.position));
            if (Target!=null)
            {
                _turretFSM.SendInput(TurretStates.ALIGN);
                break;
            }

            yield return wait;
        }
    }

    bool AlignBase(Vector3 dir)
    {
        var dirXZ = new Vector3(dir.x, 0, dir.z).normalized;
        _pivotTurret.forward += dirXZ * Time.deltaTime;

        return _canonMinAngle < Vector3.Angle(transform.forward,dir);

    }

    bool AlignCanon(Vector3 dir)
    {
        var dirY = new Vector3(0,dir.y,0).normalized;
        _pivotCanon.forward += dirY * Time.deltaTime;

        return _canonMinAngle < Vector3.Angle(transform.forward, dirY);

    }


    private void Update()
    {
        _turretFSM?.Update();
    }

}
