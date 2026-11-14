using IA2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Entity that runs to the nearest Bunker and hides inside.
/// The civilian prays when it can't enter a Bunker.
/// </summary>
[SelectionBase]
[RequireComponent(typeof(NewAIMovement))]
public class Civilian : Entity
{
    /// <summary>States of the FSM.</summary>
    public enum CivilianStates
    {
        Idle,
        Pray,
        LookForRefugee,
        Die
    }
    [SerializeField]private Animator anim;
    private NewAIMovement civilian_AI;
    private Bunker nearestBunker;

    private EventFSM<CivilianStates> civilianFSM;

    protected override void Awake()
    {
        base.Awake();

        civilian_AI = GetComponent<NewAIMovement>();

        Health.OnKilled += () => civilianFSM?.SendInput(CivilianStates.Die);
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
            civilian_AI.SetDestination(nearestBunker.transform.position, TryEnterBunker);

            DebugEntity.Log("Corro hacia el refugio");
        };

        run.OnExit += (x) =>
        {
            anim.SetBool("Running", false);
        };
    
        return run;
    }

    private State<CivilianStates> Pray()
    {
        State<CivilianStates> pray = new State<CivilianStates>("Pray");

        pray.OnEnter += (x) => { DebugEntity.Log("A rezar C:"); };

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

    private void TryEnterBunker()
    {
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
