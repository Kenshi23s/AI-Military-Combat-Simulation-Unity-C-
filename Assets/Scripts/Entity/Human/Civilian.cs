using IA2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(AI_Movement))]
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
    AI_Movement civilian_AI;
    Bunker nearestBunker;

    EventFSM<CivilianStates> civilianFSM;
    protected override void EntityAwake()
    {
        civilian_AI = GetComponent<AI_Movement>();
    }

    private void Start()
    {
        var run = RunToRefugee();
        var pray = Pray();
        var die = DieState();
        var idle = Idle();
        StateConfigurer.Create(idle)
            .SetTransition(CivilianStates.LookForRefugee, run)
            .SetTransition(CivilianStates.Die,die)
            .Done();

        StateConfigurer.Create(run)
          .SetTransition(CivilianStates.Pray, pray)
          .SetTransition(CivilianStates.Die, die)
          .Done();

        StateConfigurer.Create(pray)
            .SetTransition(CivilianStates.Die,die)
            .Done();
        StateConfigurer.Create(die).Done();

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
        };


        run.OnExit += (x) => { civilian_AI.OnDestinationReached -= TryEnterBunker; };
        return run;
    }

    public State<CivilianStates> Pray()
    {
        State<CivilianStates> pray = new State<CivilianStates>("Pray");

        //me imagino q aca se harian cosas del animator

        return pray;
    }

    public State<CivilianStates> DieState()
    {
        State<CivilianStates> die = new State<CivilianStates>("Die");

        die.OnEnter += (x) =>
        {
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
            gameObject.SetActive(true);
            civilianFSM.SendInput(CivilianStates.Pray);
        };
        gameObject.SetActive(false);

        // si el bunker sigue existiendo tratar de entrar
        //sino rezar C:
        // en el final todos estamos solos

    }



}
