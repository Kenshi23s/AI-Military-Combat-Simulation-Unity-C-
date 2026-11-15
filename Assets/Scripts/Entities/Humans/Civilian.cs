using IA2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entity that runs to the nearest Bunker and hides inside.
/// The civilian prays when it can't enter a Bunker or can't reach it.
/// </summary>
/// <remarks>The civilian reaches the Bunker when it enters the Bunker trigger or arrives at its position.</remarks>
[SelectionBase]
[RequireComponent(typeof(NewAIMovement))]
public class Civilian : Entity
{
    [SerializeField] private CivilianStats _stats;
    [SerializeField]private Animator anim;
    private NewAIMovement civilian_AI;
    private Bunker nearestBunker;

    private EventFSM<CivilianStates> civilianFSM;

    // true while the civilian runs to nearestBunker. Blocks a second TryEnterBunker.
    private bool _runningToBunker;

    /// <summary>States of the FSM.</summary>
    public enum CivilianStates
    {
        Idle,
        Pray,
        LookForRefugee,
        Die
    }

    protected override void Awake()
    {
        base.Awake();

        civilian_AI = GetComponent<NewAIMovement>();

        Health.OnKilled += () => civilianFSM?.SendInput(CivilianStates.Die);

        BindStats(_stats);
    }

    protected override void ApplyStats(bool refill)
    {
        base.ApplyStats(refill);

        civilian_AI.ApplyStats(_stats.Movement);
    }

    private void Start()
    {
        var run = RunToRefugee();
        var pray = Pray();
        var die = DieState();
        var idle = Idle();
        StateConfigurer.Create(idle)
            .SetTransition(CivilianStates.LookForRefugee, run)
            .SetTransition(CivilianStates.Die, die)
            .Done();

        StateConfigurer.Create(run)
          .SetTransition(CivilianStates.Pray, pray)
          .SetTransition(CivilianStates.Die, die)
          .Done();

        StateConfigurer.Create(pray)
            .SetTransition(CivilianStates.Die, die)
            .Done();
        StateConfigurer.Create(die).Done();

        // Arranca en Idle y pasa a LookForRefugee: asi el OnEnter de run ya tiene civilianFSM asignado.
        civilianFSM = new EventFSM<CivilianStates>(idle);
        civilianFSM.SendInput(CivilianStates.LookForRefugee);

    }

    private State<CivilianStates> Idle()
    {
        State<CivilianStates> Idle = new State<CivilianStates>("Idle");
        return Idle;
    }

    private State<CivilianStates> RunToRefugee()
    {
        State<CivilianStates> run = new State<CivilianStates>("LookForRefugee");

        run.OnEnter += (x) =>
        {
            nearestBunker = GameManager.instance.Bunkers.Minimum(x => Vector3.SqrMagnitude(x.transform.position - transform.position));

            // Sin bunkers en la escena, el civil se queda rezando.
            if (nearestBunker == null)
            {
                civilianFSM.SendInput(CivilianStates.Pray);
                return;
            }

            anim.SetBool("Running", true);
            _runningToBunker = true;
            civilian_AI.SetDestination(nearestBunker.transform.position, TryEnterBunker, onFailed: OnBunkerUnreachable);

            DebugEntity.Log("Corro hacia el refugio");
        };

        run.OnExit += (x) =>
        {
            _runningToBunker = false;
            anim.SetBool("Running", false);
        };
    
        return run;
    }

    private State<CivilianStates> Pray()
    {
        State<CivilianStates> pray = new State<CivilianStates>("Pray");

        pray.OnEnter += (x) =>
        {
            DebugEntity.Log("A rezar C:");
            civilian_AI.CancelMovement();
        };

        pray.OnEnter += (x) => { anim.SetBool("Praying", true); };

        pray.OnExit += (x) => { anim.SetBool("Praying", false); }; 

        return pray;
    }

    private State<CivilianStates> DieState()
    {
        State<CivilianStates> die = new State<CivilianStates>("Die");

        die.OnEnter += (x) =>
        {
            DebugEntity.Log("ha muerto");
            civilian_AI.CancelMovement();
            Destroy(GetComponent<BoxCollider>());
            Destroy(Health);
            anim.SetBool("Die", true);
        };

        return die;
    }

    /// <summary>Enters bunker when the civilian is running to it.</summary>
    public void ReachBunker(Bunker bunker)
    {
        if (!_runningToBunker || bunker != nearestBunker)
            return;

        civilian_AI.CancelMovement();
        TryEnterBunker();
    }

    private void OnBunkerUnreachable()
    {
        if (!_runningToBunker)
            return;

        DebugEntity.Log("No puedo llegar al refugio");
        civilianFSM.SendInput(CivilianStates.Pray);
    }

    private void TryEnterBunker()
    {
        if (!_runningToBunker)
            return;

        _runningToBunker = false;

        if (nearestBunker == null) { civilianFSM.SendInput(CivilianStates.Pray); return; }
        if (!nearestBunker.EnterBunker(this)) { civilianFSM.SendInput(CivilianStates.Pray); return; }

        nearestBunker.onBunkerDestroyed += () =>
        {
            DebugEntity.Log("El bunker en el que estaba fue destruido");
            gameObject.SetActive(true);
            civilianFSM.SendInput(CivilianStates.Pray);
        };
        gameObject.SetActive(false);

        // si el bunker sigue existiendo tratar de entrar
        //sino rezar C:
        // en el final todos estamos solos

    }
}
