using IA2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

[RequireComponent(typeof(NewAIMovement))]
public abstract class MobileInfantry : Soldier, ICapturePointEntity, IFlockableEntity
{
    public Fireteam Fireteam { get; set; }

    #region ICapturePointEntity Properties And Events
    public bool CanCapture => Health.IsAlive;

    public CapturePoint Point { get; protected set; }

    public event Action OnPointEnter = delegate { };
    public event Action OnPointStay = delegate { };
    public event Action OnPointExit = delegate { };
    #endregion

    public bool InCombat { get; protected set; }

    public NewAIMovement Movement { get; private set; }
    public Vector3 Destination { get; protected set; }

    public bool IsLeader => this == Fireteam.Leader;

    [SerializeField] protected float _minDistanceFromDestination;

    protected override void Awake()
    {
        base.Awake();

        Movement = GetComponent<NewAIMovement>();
    }

    public void Initialize(MilitaryTeam team)
    {
        Team = team;

        CreateFSM();

        InCombat = false;
    }

    protected abstract void CreateFSM();

    public abstract void AwaitOrders();
    public abstract void FollowLeader();
    public abstract void LeaderMoveTo(Vector3 newDestination);

    #region ICapturePointEntity Methods

    public void PointEnter(CapturePoint point)
    {
        Point = point;
        DebugEntity.Log("PointEnter");
        OnPointEnter();
    }

    public void PointStay()
    {
        DebugEntity.Log("PointStay");
        OnPointStay();
    }

    public IEnumerable<Soldier> GetMilitaryAround()
    {
        var col = _gridEntity.GetEntitiesInRange(_fovAgent.ViewRadius)
         .Where(x => x != this)
         .OfType<Soldier>();

        return col;
    }

    public void PointExit(CapturePoint point)
    {
        Point = null;
        DebugEntity.Log("PointExit");
        OnPointExit();
    }

    #endregion

    #region IFlockable Methods
    public Vector3 GetPosition() => transform.position;

    public Vector3 GetVelocity() => Movement.ManualMovement.Velocity;
    #endregion

    protected IEnumerator FollowLeaderRoutine()
    {
        bool moving = true;
        GoToLeader();

        while (true)
        {
            for (int frames = 0; frames < 120; frames++) 
                yield return null;

            // Si estoy cerca, me dejo de mover y dejo de calcular el camino hacia el lider.
            if (Fireteam.IsNearLeader(this, _minDistanceFromDestination))
            {
                // Si no me estaba moviendo, no repito esta logica
                if (!moving)
                {
                    Movement.CancelMovement();
                    Anim.SetBool("Running", false);
                    moving = false;
                }

                DebugEntity.Log("El lider esta muy cerca, no es necesario calcular camino");
                continue;
            }

            GoToLeader();
        }

    }

    void GoToLeader()
    {
        Movement.SetDestination(Fireteam.Leader.transform.position);
        Anim.SetBool("Running", true);
    }
}
