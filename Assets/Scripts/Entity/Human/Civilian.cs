using IA2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(NewAIMovement))]
public class Civilian : Entity
{
    Animator anim;
    public enum CivilianStates
    {
        Idle,
        Pray,
        LookForRefugee,
        Die
    }
    NewAIMovement civilian_AI;
    Bunker nearestBunker;

    EventFSM<CivilianStates> civilianFSM;
    protected override void EntityAwake()
    {
        civilian_AI = GetComponent<NewAIMovement>();
       
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

        civilianFSM = new EventFSM<CivilianStates>(run);

    }

    public State<CivilianStates> Idle()
    {
        State<CivilianStates> Idle = new State<CivilianStates>("Idle");
        return Idle;
    }

    public State<CivilianStates> RunToRefugee()
    {
        State<CivilianStates> run = new State<CivilianStates>("LookForRefugee");

        run.OnEnter += (x) =>
        {
            nearestBunker = GameManager.instance.bunkers.Minimum(x => Vector3.Distance(x.transform.position,transform.position));
            civilian_AI.SetDestination(nearestBunker.transform.position);
            civilian_AI.OnDestinationReached += TryEnterBunker;
            _debug.Log("Corro hacia el refugio");
        };


        run.OnExit += (x) => { civilian_AI.OnDestinationReached -= TryEnterBunker; };
        return run;
    }

    public State<CivilianStates> Pray()
    {
        State<CivilianStates> pray = new State<CivilianStates>("Pray");

        pray.OnEnter += (x) => { _debug.Log("A rezar C:"); };
        
       
        //me imagino q aca se harian cosas del animator

        return pray;
    }

    public State<CivilianStates> DieState()
    {
        State<CivilianStates> die = new State<CivilianStates>("Die");

        die.OnEnter += (x) =>
        {
            _debug.Log("ha muerto");
            Destroy(GetComponent<BoxCollider>());
            Destroy(health);
            anim.SetTrigger("Die");

        };

        return die;
    }

    void TryEnterBunker()
    {
        if (nearestBunker == null) { civilianFSM.SendInput(CivilianStates.Pray); return; }
        if (!nearestBunker.EnterBunker(this)) { civilianFSM.SendInput(CivilianStates.Pray); return; }

        nearestBunker.onBunkerDestroyed += () =>
        {
            _debug.Log("El bunker en el que estaba fue destruido");
            gameObject.SetActive(true);
            civilianFSM.SendInput(CivilianStates.Pray);
        };
        gameObject.SetActive(false);

        // si el bunker sigue existiendo tratar de entrar
        //sino rezar C:
        // en el final todos estamos solos

    }



}
