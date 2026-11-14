using IA2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

/// <summary>
/// Soldier that moves with NewAIMovement as a member of a Fireteam.
/// Captures points and takes part in flocking.
/// </summary>
[RequireComponent(typeof(NewAIMovement))]
public abstract class MobileInfantry : Soldier, ICapturePointEntity, IFlockableEntity
{
    // Frames between two path recalculations in FollowLeaderRoutine.
    private const int FramesBetweenLeaderChecks = 120;

    /// <summary>The Fireteam of this soldier.</summary>
    public Fireteam Fireteam { get; set; }

    #region ICapturePointEntity Properties And Events
    /// <summary>true while the soldier is alive.</summary>
    public bool CanCapture => Health.IsAlive;

    /// <summary>The CapturePoint the soldier is inside of.</summary>
    public CapturePoint Point { get; protected set; }

    /// <summary>Raised by PointEnter.</summary>
    public event Action OnPointEnter = delegate { };

    /// <summary>Raised by PointStay.</summary>
    public event Action OnPointStay = delegate { };

    /// <summary>Raised by PointExit.</summary>
    public event Action OnPointExit = delegate { };
    #endregion

    /// <summary>true while the soldier fights.</summary>
    public bool InCombat { get; protected set; }

    /// <summary>The NewAIMovement of this soldier.</summary>
    public NewAIMovement Movement { get; private set; }

    /// <summary>Point of the current LeaderMoveTo order.</summary>
    public Vector3 Destination { get; protected set; }

    /// <summary>true when this soldier is the Leader of its Fireteam.</summary>
    public bool IsLeader => this == Fireteam.Leader;

    [SerializeField] protected float _minDistanceFromDestination;

    protected override void Awake()
    {
        base.Awake();

        Movement = GetComponent<NewAIMovement>();
    }

    /// <summary>Sets the Team and creates the FSM.</summary>
    public void Initialize(MilitaryTeam team)
    {
        Team = team;

        CreateFSM();

        InCombat = false;
    }

    protected abstract void CreateFSM();

    /// <summary>Sends the FSM to its awaiting orders state.</summary>
    public abstract void AwaitOrders();

    /// <summary>Sends the FSM to its follow leader state.</summary>
    public abstract void FollowLeader();

    /// <summary>Sets Destination and sends the FSM to its leader move to state.</summary>
    public abstract void LeaderMoveTo(Vector3 newDestination);

    #region ICapturePointEntity Methods

    /// <summary>Sets Point and raises OnPointEnter.</summary>
    public void PointEnter(CapturePoint point)
    {
        Point = point;
        DebugEntity.Log("PointEnter");
        OnPointEnter();
    }

    /// <summary>Raises OnPointStay.</summary>
    public void PointStay()
    {
        DebugEntity.Log("PointStay");
        OnPointStay();
    }

    /// <summary>Clears Point and raises OnPointExit.</summary>
    public void PointExit(CapturePoint point)
    {
        Point = null;
        DebugEntity.Log("PointExit");
        OnPointExit();
    }

    #endregion

    /// <summary>Returns the other Soldier entities within the ViewRadius of the FOVAgent.</summary>
    public IEnumerable<Soldier> GetMilitaryAround()
    {
        return _gridEntity.GetEntitiesInRange(_fovAgent.ViewRadius)
            .Where(x => x != this)
            .OfType<Soldier>();
    }

    #region IFlockable Methods
    /// <summary>Returns the position of the soldier.</summary>
    public Vector3 GetPosition() => transform.position;

    /// <summary>Returns the velocity of the soldier.</summary>
    public Vector3 GetVelocity() => Movement.ManualMovement.Velocity;
    #endregion

    #region Coroutines

    /// <summary>Stops the coroutine in handle and starts routine in its place.</summary>
    protected void RestartCoroutine(ref Coroutine handle, IEnumerator routine)
    {
        StopTrackedCoroutine(ref handle);
        handle = StartCoroutine(routine);
    }

    /// <summary>Stops the coroutine in handle and sets handle to null.</summary>
    protected void StopTrackedCoroutine(ref Coroutine handle)
    {
        if (handle == null)
            return;

        StopCoroutine(handle);
        handle = null;
    }

    /// <summary>
    /// Moves the soldier to the Leader every FramesBetweenLeaderChecks frames.
    /// The soldier stops while it is near the Leader.
    /// </summary>
    protected IEnumerator FollowLeaderRoutine()
    {
        bool moving = true;
        GoToLeader();

        while (true)
        {
            for (int frames = 0; frames < FramesBetweenLeaderChecks; frames++)
                yield return null;

            // Si estoy cerca, me dejo de mover y dejo de calcular el camino hacia el lider.
            if (Fireteam.IsNearLeader(this, _minDistanceFromDestination))
            {
                // Si ya me habia frenado, no repito esta logica
                if (moving)
                {
                    Movement.CancelMovement();
                    Anim.SetBool("Running", false);
                    moving = false;
                }

                DebugEntity.Log("El lider esta muy cerca, no es necesario calcular camino");
                continue;
            }

            GoToLeader();
            moving = true;
        }
    }

    #endregion

    private void GoToLeader()
    {
        Movement.SetDestination(Fireteam.Leader.transform.position);
        Anim.SetBool("Running", true);
    }
}
