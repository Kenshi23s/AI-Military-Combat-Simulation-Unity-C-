using IA2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        FOCUS_N_SHOOT,
        DIE
    }

    public event Action OnEnemyFound = delegate { };

    EventFSM<SNIPER_STATES> _fsm;

    ShootComponent _shootComponent;
 
    FOVAgent _fovAgent;

    LineRenderer _laser;

    public Soldier target { get; private set; }

    [Header("Sniper"),SerializeField] Transform _shootPos;

    [SerializeField] int FramesBetweenEnemySearch = 4;
    [SerializeField] float _aimSpeed;
    float _currentAimLerp, _currentFocusTime;

    [SerializeField,Header("Shoot State")] float _addPerTimesFocused;
    [SerializeField] float _requiredFocusTime, maxShootsInRow = 1;
    int timesFocused = 1;

    [SerializeField] Animator _anim;

    protected override void SoldierAwake()
    {       

        _shootComponent = GetComponent<ShootComponent>();
        _fovAgent = GetComponent<FOVAgent>();
        _laser = GetComponent<LineRenderer>();
        _laser.enabled = false;
      
    }
    private void Start()
    {
        CreateFSM();
        var sprite = TeamsManager.instance.GetSprite(typeof(Sniper));
        TeamsManager.instance.AddToTeam(Team, this, sprite);
    }


    #region FSM SET AND STATES
    void CreateFSM()
    {
        var lookEnemies = LookForEnemies();
        var aimEnemy = AimAtEnemy();
        var focus_n_shoot = ShootAtEnemy();
        var die = Die();

        StateConfigurer.Create(lookEnemies)
            .SetTransition(SNIPER_STATES.AIM, aimEnemy)
            .Done();

        StateConfigurer.Create(aimEnemy)
           .SetTransition(SNIPER_STATES.LOOK_FOR_TARGETS, lookEnemies)
           .SetTransition(SNIPER_STATES.FOCUS_N_SHOOT, focus_n_shoot)
           .SetTransition(SNIPER_STATES.DIE, die)
           .Done();


        StateConfigurer.Create(focus_n_shoot)
           .SetTransition(SNIPER_STATES.AIM, aimEnemy)
           .SetTransition(SNIPER_STATES.LOOK_FOR_TARGETS, lookEnemies)
           .SetTransition(SNIPER_STATES.DIE, die)
           .Done();

        StateConfigurer.Create(die)
            .Done();

        _fsm = new EventFSM<SNIPER_STATES>(lookEnemies);
    }

    //busca a los enemigos cercanos en su fov
    State<SNIPER_STATES> LookForEnemies()
    {
        var state = new State<SNIPER_STATES>("Look For Enemies");

        Action _onFound = () =>
        {
            _fsm.SendInput(SNIPER_STATES.AIM);
        };

        state.OnEnter += (x) =>
        {
            _anim.SetBool("Shooting", false);
            OnEnemyFound += _onFound;
            StartCoroutine(LookForEnemiesCoroutine());

        };

        state.OnExit += (x) =>
        {
            OnEnemyFound -= _onFound;         
        };

        return state;
    }

    //obtiene el soldado mas lejano
    Soldier GetFurthestEnemy()
    {
        return _gridEntity.GetEntitiesInRange(_fovAgent.ViewRadius)
            .OfType<Soldier>()
            .Where(x => x.Team != Team && x.Team != MilitaryTeam.None)
            .Where(x => _fovAgent.IN_FOV(x.transform.position))
            .Where(x => x.Health.isAlive)
            .Maximum(x => Vector3.SqrMagnitude(x.transform.position - transform.position));
    }

    IEnumerator LookForEnemiesCoroutine()
    {
        while (true)
        {
            for (int i = 0; i < FramesBetweenEnemySearch; i++) yield return null;
            var aux = GetFurthestEnemy();
            if (aux == null) continue;            

            target = aux;
            OnEnemyFound();
            break;       
        } 
    }

    //apunta al enemigo encontrado 
    State<SNIPER_STATES> AimAtEnemy()
    {
        var state = new State<SNIPER_STATES>("Aim At Enemy");

        state.OnEnter += (x) =>
        {
            if (target == null) 
            {
                _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);
                DebugEntity.Log("El Target es null, paso a buscar otro enemigo");
               
            }
            _anim.SetBool("Shooting", true);
            _currentAimLerp = 0;
        };

        state.OnUpdate += () =>
        {
            if (!_fovAgent.IN_FOV(target.transform.position) || !target.Health.isAlive) 
            {
                _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);
                DebugEntity.Log("El objetivo murio o ya no lo veo, cambio a LOOK_FOR_TARGETS");
            } 

            Vector3 dir = target.AimPoint - transform.position;
            if (_currentAimLerp < 1)
            {
                DebugEntity.Log("Apunto al Objetivo");
                _currentAimLerp += Time.deltaTime * _aimSpeed;
               
                Vector3 aux = Vector3.Slerp(transform.forward, dir.normalized, _currentAimLerp);
                transform.forward = new Vector3(aux.x, transform.forward.y, aux.z);
                return;
            }
            DebugEntity.Log("Tengo al enemigo en la mira!");
            _currentAimLerp = 1;
            transform.forward = dir.normalized;
            _fsm.SendInput(SNIPER_STATES.FOCUS_N_SHOOT);
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
            UpdateLaser();

        };

        state.OnUpdate += () =>
        {
            // si no lo veo o ya no esta vivo, paso a buscar otro objetivo
            if (!_fovAgent.IN_FOV(target.transform.position) || !target.Health.isAlive) _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);

            Vector3 dir = target.AimPoint - transform.position;
         
            transform.forward = new Vector3(dir.x,0,dir.z).normalized;
            //tiempo entre frames * incremento en la velocidad que se puede concentrar * las veces que se concentro
            //(queria darle como un toque unico al sniper con esto, quedo medio raro?)
            _currentFocusTime += Time.deltaTime * (_addPerTimesFocused * timesFocused);

            //se "concentra" para pegarle al objetivo
            if (_currentFocusTime >= _requiredFocusTime)
            {
                _currentFocusTime = 0;
                _shootComponent.Shoot(_shootPos,dir);
                timesFocused++;
                DebugEntity.Log("Disparo al target");

                //si disparo x tiros tiene que volver a apuntar
                if (timesFocused > maxShootsInRow) 
                {
                    DebugEntity.Log("Tengo que volver a apuntar :C");
                    _fsm.SendInput(SNIPER_STATES.AIM);
                } 
            }
            UpdateLaser();
        };

        state.OnExit += (x) =>
        {
            _currentFocusTime = 0;
            timesFocused = 0;
            _laser.enabled = false;
        };

        return state;
    }

    State<SNIPER_STATES> Die()
    {
        State<SNIPER_STATES> state = new State<SNIPER_STATES>("Die");
        state.OnEnter += (x) =>
        {
            _anim.SetBool("Shooting", false);
            _anim.SetBool("Die",true);
            DebugEntity.Log("Die");
        };
        return state;
    }
    #endregion

    void UpdateLaser()
    {
        if (target == null)
        {
            _laser.enabled = false;
            return;
        }
         _laser.enabled = true;
         _laser.SetPosition(0,_shootPos.position);
         _laser.SetPosition(1, target.AimPoint);
    }

    public void InitializeUnit(MilitaryTeam newTeam)
    {
        Team = newTeam;
    }

   

    private void Update()
    {
        _fsm?.Update();
    }
}
