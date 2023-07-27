using IA2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
    [SerializeField] float rotationSpeed;
    [SerializeField] float _cd_BetweenSearch;

    [SerializeField,Header("Shooting")] 
    Transform[] _shootPos;
    [SerializeField] float _cd_BetweenShots;
    int _pos = 0;
    ShootComponent _ShootHandler;

    EventFSM<TurretStates> _turretFSM;

    float _fixedCanonPos;

    public event Action OnDeathInCombat;

    protected override void EntityAwake()
    {
        Health.OnKilled += OnDeathInCombat;
        _myGridEntity = GetComponent<GridEntity>();
        _ShootHandler = GetComponent<ShootComponent>();
        _fov = GetComponent<FOVAgent>();      
        _fixedCanonPos = _pivotCanon.transform.localRotation.eulerAngles.y;
    }

    private void Start()
    {
        SetFSM();
        var sprite = TeamsManager.instance.GetSprite(typeof(Turret));
        TeamsManager.instance.AddToTeam(Team,this, sprite);
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

    void LateUpdate()
    {
        //var fixPos = new Vector3(_pivotCanon.localRotation.eulerAngles.x,fixedCanonPos,_pivotCanon.localRotation.y);
        //_pivotCanon.localRotation = Quaternion.LookRotation(fixPos); 
    }

    State<TurretStates> Rest_State()
    {
        var state = new State<TurretStates>("Rest");

        state.OnEnter += (x) => DebugEntity.Log("Entro en Rest State");


        state.OnUpdate += () =>
        {
            DebugEntity.Log("Descansando");
            var Acanon = AlignCanon(Vector3.zero);         
            var Abase = AlignBase(Vector3.zero);
           
          
            if (!Abase || !Acanon) return;

            DebugEntity.Log("Ya descanse, vuelvo a search");
            _pivotTurret.forward = Vector3.zero;
            _pivotCanon.forward  = Vector3.zero;
            _turretFSM.SendInput(TurretStates.SEARCH_TARGETS);         
        };




        return state;
    }


    State<TurretStates> SearchTargets_State()
    {
        var state = new State<TurretStates>("SEARCH");

        state.OnEnter += (x) => DebugEntity.Log("Entro en "+ state.Name);

        state.OnExit += (x) => DebugEntity.Log("Salgo de  " + state.Name);

        state.OnEnter += (x) => StartCoroutine(LookForHostilePlane());

        state.OnExit  += (x) => StopCoroutine(LookForHostilePlane());

        return state;
    }

    State<TurretStates> Align_State()
    {
        var state = new State<TurretStates>("ALIGN");

        state.OnEnter += (x) => DebugEntity.Log("Entro en " + state.Name);

        state.OnExit += (x) => DebugEntity.Log("Salgo de  " + state.Name);

        state.OnUpdate += () =>
        {
            if (Target == null) 
            {
                _turretFSM.SendInput(TurretStates.REST);
                return;
            } 
           
            if (_fov.IN_FOV(Target.AimPoint) && Target.Health.IsAlive) return;       

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

        state.OnEnter += (x) => DebugEntity.Log("Entro en " + state.Name);
        state.OnExit += (x) => DebugEntity.Log("Salgo de  " + state.Name);

        state.OnEnter += (x) => StartCoroutine(ShootBullets());

        state.OnUpdate += () =>
        {
            if (_fov.IN_FOV(Target.AimPoint) && Target.Health.IsAlive) return;
            
                Target = null;
                _turretFSM.SendInput(TurretStates.REST);         
        };

        state.OnUpdate += () =>
        {
            if (Target == null) 
            {
                DebugEntity.Log("perdi al target de vista,paso a rest");
                return;
            } 

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
        if (_pos > _shootPos.Length - 1) _pos = 0;
       
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
            DebugEntity.Log("Shoot");

            yield return wait;
        }
    }

    IEnumerator LookForHostilePlane() 
    {
        var wait = new WaitForSeconds(_cd_BetweenSearch);
        while (true) 
        {

            Target = _myGridEntity.GetEntitiesInRange(_fov.ViewRadius)
                .OfType<Plane>()
                .Where(x => _fov.IN_FOV(x.transform.position))
                .Minimum(x => Vector3.Distance(x.transform.position,transform.position));

            if (Target != null)
            {
                _turretFSM.SendInput(TurretStates.ALIGN);
                DebugEntity.Log("Veo un enemigo, alineo");
                break;
            }

            yield return wait;
        }
    }

    bool AlignBase(Vector3 dir)
    {
        var desiredLookRotation = new Vector3(dir.x ,0 , dir.z);
        Quaternion target = Quaternion.LookRotation(desiredLookRotation);
        _pivotTurret.rotation = Quaternion.Lerp(_pivotTurret.rotation, target, Time.deltaTime * rotationSpeed);

      
        return _baseMinAngle > Quaternion.Angle(_pivotTurret.rotation, target);

    }

    bool AlignCanon(Vector3 dir)
    {     
        var desiredLookRotation = dir;
        Quaternion target = Quaternion.LookRotation(desiredLookRotation);
        _pivotCanon.rotation = Quaternion.Lerp(_pivotCanon.rotation, target, Time.deltaTime * rotationSpeed);
        

        return _canonMinAngle > Quaternion.Angle(_pivotCanon.rotation, target);

    }

   

    private void Update()
    {
        _turretFSM?.Update();
    }

    private void OnValidate()
    {
        _fixedCanonPos = _pivotCanon.transform.localRotation.eulerAngles.y;
       
    }
    private void OnDrawGizmos()
    {
        if (Target == null) return;
        Debug.Log("Gizmo Draw");
        Gizmos.color = new Color(255, 165, 0)/255  + new Color(0, 0, 0, 1);
        Gizmos.DrawLine(transform.position, Target.transform.position);
        Gizmos.DrawWireSphere(Target.transform.position,2f);
        
    }
}
