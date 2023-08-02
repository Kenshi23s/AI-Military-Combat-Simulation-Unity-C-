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

    public NewAIMovement Movement { get; private set; }

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

}
