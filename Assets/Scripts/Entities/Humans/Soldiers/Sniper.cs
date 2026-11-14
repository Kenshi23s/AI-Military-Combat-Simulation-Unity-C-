using IA2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Static Soldier that aims at the furthest enemy in its FOVAgent and shoots after a focus time.
/// A LineRenderer shows the laser while the sniper focuses.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class Sniper : Soldier
{
    /// <summary>States of the FSM.</summary>
    public enum SNIPER_STATES
    {
        LOOK_FOR_TARGETS,
        AIM,
        FOCUS_N_SHOOT,
        DIE
    }

    /// <summary>Raised when LookForEnemiesCoroutine finds a target.</summary>
    public event Action OnEnemyFound = delegate { };

    private EventFSM<SNIPER_STATES> _fsm;

    private LineRenderer _laser;

    // Running search of the LOOK_FOR_TARGETS state.
    private Coroutine _lookForEnemies;

    /// <summary>The enemy the sniper aims at.</summary>
    public Soldier target { get; private set; }

    [SerializeField] private int FramesBetweenEnemySearch = 4;
    [SerializeField] private float _aimSpeed;
    private float _currentAimLerp, _currentFocusTime;

    [SerializeField, Header("Shoot State")] private float _addPerTimesFocused;
    [SerializeField] private float _requiredFocusTime, maxShootsInRow = 1;
    private int timesFocused = 1;

    protected override void Awake()
    {
        base.Awake();
        _laser = GetComponent<LineRenderer>(); _laser.enabled = false;
        ShootComponent.onHit += _ => TotalDamageDealt += ShootComponent.BulletDamage;
        Health.OnKilled += () => _fsm?.SendInput(SNIPER_STATES.DIE);
      
    }
    private void Start()
    {
        CreateFSM();
        var tm = TeamsManager.instance;
        var sprite = tm.GetSprite(typeof(Sniper)); tm.AddToTeam(Team, this, sprite);

    }


    #region FSM SET AND STATES
    private void CreateFSM()
    {
        var lookEnemies = LookForEnemies();
        var aimEnemy = AimAtEnemy();
        var focus_n_shoot = ShootAtEnemy();
        var die = Die();

        StateConfigurer.Create(lookEnemies)
            .SetTransition(SNIPER_STATES.AIM, aimEnemy)
            .SetTransition(SNIPER_STATES.DIE, die)
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
    private State<SNIPER_STATES> LookForEnemies()
    {
        var state = new State<SNIPER_STATES>("Look For Enemies");

        Action onFound = () =>
        {
            _fsm.SendInput(SNIPER_STATES.AIM);
        };

        state.OnEnter += (x) =>
        {
            Anim.SetBool("Shooting", false);
            OnEnemyFound += onFound;
            _lookForEnemies = StartCoroutine(LookForEnemiesCoroutine());
        };

        state.OnExit += (x) =>
        {
            OnEnemyFound -= onFound;

            if (_lookForEnemies != null)
            {
                StopCoroutine(_lookForEnemies);
                _lookForEnemies = null;
            }
        };

        return state;
    }

    //obtiene el soldado mas lejano
    private Soldier GetFurthestEnemy()
    {
        return _gridEntity.GetEntitiesInRange(_fovAgent.ViewRadius)
            .OfType<Soldier>()
            .Where(x => x.Team != Team && x.Team != MilitaryTeam.None)
            .Where(x => x.Health.IsAlive)
            // IN_FOV va ultimo porque hace un raycast.
            .Where(x => _fovAgent.IN_FOV(x.transform.position))
            .Maximum(x => Vector3.SqrMagnitude(x.transform.position - transform.position));
    }

    private IEnumerator LookForEnemiesCoroutine()
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
    private State<SNIPER_STATES> AimAtEnemy()
    {
        var state = new State<SNIPER_STATES>("Aim At Enemy");

        state.OnEnter += (x) =>
        {
            if (target == null)
            {
                _laser.enabled = false;
                DebugEntity.Log("El Target es null, paso a buscar otro enemigo");
                _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);
                return;
            }
            Anim.SetBool("Shooting", true);
            _currentAimLerp = 0;
        };

        state.OnUpdate += () =>
        {
            if (!target.Health.IsAlive || !_fovAgent.IN_FOV(target.transform.position))
            {
                DebugEntity.Log("El objetivo murio o ya no lo veo, cambio a LOOK_FOR_TARGETS");
                _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);
                return;
            }

            Vector3 dir = target.AimPoint - transform.position;

            if (_currentAimLerp < 1)
            {
                DebugEntity.Log("Apunto al Objetivo");

                _currentAimLerp += Time.deltaTime * _aimSpeed;

                transform.rotation = Quaternion.Lerp(transform.rotation, GetTargetRotation(dir), _currentAimLerp);

              
                return;
            }

            DebugEntity.Log("Tengo al enemigo en la mira!");

            _currentAimLerp = 1;

            transform.forward = dir.normalized;

            _fsm.SendInput(SNIPER_STATES.FOCUS_N_SHOOT);
        };

        return state;
    }


    private State<SNIPER_STATES> ShootAtEnemy()
    {
        var state = new State<SNIPER_STATES>("Focus N Shoot");

        state.OnEnter += (x) =>
        {
            if (target == null)
            {
                _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);
                return;
            }

            _currentFocusTime = 0;
            timesFocused = 1;
            UpdateLaser();
        };

        state.OnUpdate += () =>
        {
            // si no lo veo o ya no esta vivo, paso a buscar otro objetivo
            if (!target.Health.IsAlive || !_fovAgent.IN_FOV(target.transform.position))
            {
                _fsm.SendInput(SNIPER_STATES.LOOK_FOR_TARGETS);
                return;
            }

            Vector3 dir = target.AimPoint - transform.position;
         
            transform.rotation = GetTargetRotation(dir + Vector3.zero);

            //tiempo entre frames * incremento en la velocidad que se puede concentrar * las veces que se concentro
            //(queria darle como un toque unico al sniper con esto, quedo medio raro?)
            _currentFocusTime += Time.deltaTime * (_addPerTimesFocused * timesFocused);

            //se "concentra" para pegarle al objetivo
            if (_currentFocusTime >= _requiredFocusTime)
            {
                _currentFocusTime = 0;
                ShootComponent.Shoot(_shootPos, target.AimPoint - _shootPos.position);
                timesFocused++;
                DebugEntity.Log("Disparo al target");

                //si disparo x tiros tiene que volver a apuntar
                if (timesFocused > maxShootsInRow) 
                {
                    DebugEntity.Log("Tengo que volver a apuntar :C");
                    _fsm.SendInput(SNIPER_STATES.AIM);

                    // OnExit ya apago el laser.
                    return;
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

    private State<SNIPER_STATES> Die()
    {
        State<SNIPER_STATES> state = new State<SNIPER_STATES>("Die");
        state.OnEnter += (x) =>
        {
            StopAllCoroutines();
            Anim.SetBool("Shooting", false);
            Anim.SetBool("Die",true);
            _laser.enabled = false;
            DebugEntity.Log("Die");
        };
        return state;
    }
    #endregion

    private void UpdateLaser()
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

    private Quaternion GetTargetRotation(Vector3 dir)
    {
        Vector3 gunForward = _shootPos.forward; gunForward.y = 0;

        dir.y = 0;

        Quaternion fromToRotation = Quaternion.FromToRotation(gunForward.normalized, dir.normalized);

        Quaternion targetRotation = transform.rotation * fromToRotation;

        return targetRotation;
    }

    /// <summary>Sets the Team.</summary>
    public void InitializeUnit(MilitaryTeam newTeam)
    {
        Team = newTeam;
    }

   

    private void Update()
    {
        _fsm?.Update();
    }
}
