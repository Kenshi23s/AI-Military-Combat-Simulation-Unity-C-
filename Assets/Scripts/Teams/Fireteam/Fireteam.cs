using FacundoColomboMethods;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

/// <summary>
/// Group of MobileInfantry of one team with a Leader.
/// The Leader moves to capture points and the other members follow it.
/// </summary>
public class Fireteam
{
    /// <summary>Creates a Fireteam with the given members and a random Leader.</summary>
    public static void Create(MilitaryTeam newTeam, List<MobileInfantry> members)
    {
        new Fireteam(newTeam, members);
    }

    /// <summary>
    /// Adds the members, picks a random Leader and initializes every member with newTeam.
    /// </summary>
    /// <param name="newTeam">Team of the members.</param>
    /// <param name="members">Members of the fireteam. The list needs at least one member.</param>
    public Fireteam(MilitaryTeam newTeam, List<MobileInfantry> members)
    {
        Team = newTeam;
        FireteamMembers = _fireteamMembers.AsReadOnly();

        AddMembers(members);
        PromoteToLeader(_fireteamMembers.PickRandom());

        foreach (var member in _fireteamMembers)
        {
            if (member != Leader)
                member.Initialize(newTeam);
        }

        Leader.Initialize(newTeam);
    }

    private List<MobileInfantry> _fireteamMembers = new List<MobileInfantry>();

    private CapturePoint targetCapturePoint;

    /// <summary>Read-only view of the members.</summary>
    public ReadOnlyCollection<MobileInfantry> FireteamMembers;

    /// <summary>The member that gives the orders.</summary>
    public MobileInfantry Leader { get; private set; }

    /// <summary>Team of the fireteam.</summary>
    public MilitaryTeam Team { get; private set; }

    #region MemberManagment

    /// <summary>Adds the infantry to the members and sets its Fireteam to this fireteam.</summary>
    public void AddMember(MobileInfantry infantry)
    {
        if (_fireteamMembers.Contains(infantry))
            return;

        _fireteamMembers.Add(infantry);
        infantry.Fireteam = this;
    }

    /// <summary>Adds each infantry with AddMember.</summary>
    public void AddMembers(IEnumerable<MobileInfantry> infantryCol)
    {
        foreach (var infantry in infantryCol)
            AddMember(infantry);
    }

    /// <summary>
    /// Removes the infantry from the members.
    /// A random member becomes the Leader when the infantry was the Leader.
    /// </summary>
    public void RemoveMember(MobileInfantry infantry)
    {
        if (!_fireteamMembers.Remove(infantry))
            return;

        if (infantry == Leader && _fireteamMembers.Count > 0)
            PromoteToLeader(_fireteamMembers.PickRandom());
    }

    /// <summary>
    /// Removes each infantry from the members.
    /// A random member becomes the Leader when the Leader was removed.
    /// </summary>
    public void RemoveMembers(IEnumerable<MobileInfantry> infantryCol)
    {
        bool leaderRemoved = false;

        foreach (var infantry in infantryCol)
        {
            if (_fireteamMembers.Remove(infantry) && infantry == Leader)
                leaderRemoved = true;
        }

        if (leaderRemoved && _fireteamMembers.Count > 0)
            PromoteToLeader(_fireteamMembers.PickRandom());
    }

    private void PromoteToLeader(MobileInfantry member)
    {
        Leader = member;
        Leader.DebugEntity.AddGizmoAction(DrawConnectionToMembers);
        Leader.gameObject.name = "[Lider]" + Leader.gameObject.name;
    }

    #endregion

    #region Orders

    /// <summary>
    /// Sends the Leader to the nearest capture point with priority for this team.
    /// The other members follow the Leader.
    /// </summary>
    /// <remarks>Waits one frame before the search.</remarks>
    public IEnumerator FindNearestUntakenPoint()
    {
        // Esperamos un frame para darle tiempo a las demas fsms a que se inicialicen. Idealmente no deberia ser asi esto.
        yield return null;

        Vector3 leaderPosition = Leader.transform.position;
        CapturePoint nearestPoint = CapturePointManager.instance.CapturePoints
            .Where(IsPointPriority)
            .Minimum(x => Vector3.SqrMagnitude(x.transform.position - leaderPosition));

        if (nearestPoint == null)
            yield break;

        targetCapturePoint = nearestPoint;
        targetCapturePoint.OnCaptureComplete += OnTargetPointCaptured;

        Leader.DebugEntity.Log($"Moviendome a la bandera {targetCapturePoint}");
        SendPatrolOrders(targetCapturePoint.transform.position);
    }

    /// <summary>Sends the fireteam to EnemyPos.</summary>
    public void HelpNearFireteam(Vector3 EnemyPos)
    {
        SendPatrolOrders(EnemyPos);
    }

    /// <summary>
    /// Calls an air strike from a random plane of the team in FLY_AROUND.
    /// Without a plane in FLY_AROUND, sends the nearest available allied fireteam.
    /// </summary>
    /// <param name="enemyPosition">Target of the support.</param>
    /// <remarks>
    /// An allied fireteam is available when it is a different fireteam, its Leader is alive
    /// and FireteamInCombat returns false.
    /// </remarks>
    public void RequestSupport(Vector3 enemyPosition)
    {
        TeamsManager manager = TeamsManager.instance;

        var availablePlanes = new List<Plane>();
        foreach (Plane plane in manager.GetTeamPlanes(Team))
        {
            if (plane.actualState == PlaneStates.FLY_AROUND)
                availablePlanes.Add(plane);
        }

        if (availablePlanes.Count > 0)
        {
            availablePlanes.PickRandom().CallAirStrike(enemyPosition);
            Leader.DebugEntity.Log("Fireteam solicita apoyo, hay un avion disponible, viene a bombardear!");
            return;
        }

        Vector3 leaderPosition = Leader.transform.position;
        Fireteam nearestFireteam = null;
        float nearestSqrDistance = float.MaxValue;

        foreach (Fireteam fireteam in manager.GetAllyFireteams(Team))
        {
            if (fireteam == this || !fireteam.Leader.IsAlive || fireteam.FireteamInCombat())
                continue;

            float sqrDistance = Vector3.SqrMagnitude(fireteam.Leader.transform.position - leaderPosition);
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestFireteam = fireteam;
            }
        }

        if (nearestFireteam != null)
        {
            nearestFireteam.HelpNearFireteam(enemyPosition);
            Leader.DebugEntity.Log("Fireteam solicita apoyo, hay un fireteam disponible, viene a ayudar!");
            return;
        }

        Leader.DebugEntity.Log("Fireteam solicita apoyo, sin respuesta, el Fireteam esta solo en esta :C");
    }

    private void OnTargetPointCaptured(MilitaryTeam capturedBy)
    {
        if (capturedBy != Team) return;

        Leader.AwaitOrders();
        foreach (var unit in _fireteamMembers)
        {
            if (unit != Leader)
                unit.FollowLeader();
        }

        targetCapturePoint.OnCaptureComplete -= OnTargetPointCaptured;
    }

    private bool IsPointPriority(CapturePoint point)
    {
        return point.CapturedBy != Team
            || point.CurrentState == CaptureState.Disputed
            || (point.CurrentState == CaptureState.BeingCaptured && point.BeingCapturedBy != Team);
    }

    private void SendPatrolOrders(Vector3 newDestination)
    {
        //lookup divide una coleccion en base a un predicado, no solo se puede dividir en booleanas
        //tambien se puede dividir en strings o enums
        //IA2-LINQ
        var split = _fireteamMembers.ToLookup(x => x == Leader);

        split[true].First().LeaderMoveTo(newDestination);

        foreach (var member in split[false]) member.FollowLeader();
    }

    #endregion

    #region UsefulQuestions

    /// <summary>
    /// Returns true when a member other than whoAsks has an alive enemy Soldier around.
    /// </summary>
    /// <param name="whoAsks">Entity excluded from the search.</param>
    /// <param name="neartestInDanger">The member in danger nearest to whoAsks. Set to null when the method returns false.</param>
    /// <remarks>One GetMilitaryAround call per member.</remarks>
    public bool AlliesWithEnemiesNearby(Entity whoAsks, out Entity neartestInDanger)
    {
        neartestInDanger = null;
        float nearestSqrDistance = float.MaxValue;
        Vector3 askerPosition = whoAsks.transform.position;

        foreach (MobileInfantry member in _fireteamMembers)
        {
            if (member == whoAsks || !HasEnemyAround(member))
                continue;

            float sqrDistance = Vector3.SqrMagnitude(member.transform.position - askerPosition);
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                neartestInDanger = member;
            }
        }

        return neartestInDanger != null;
    }

    private bool HasEnemyAround(MobileInfantry member)
    {
        foreach (Soldier soldier in member.GetMilitaryAround())
        {
            if (soldier.Health.IsAlive && soldier.Team != Team)
                return true;
        }

        return false;
    }

    /// <summary>Returns true when a member is InCombat.</summary>
    public bool FireteamInCombat()
    {
        foreach (var member in _fireteamMembers)
        {
            if (member.InCombat)
                return true;
        }

        return false;
    }

    /// <summary>Returns true when member is closer to the Leader than maxDistanceFromLeader.</summary>
    public bool IsNearLeader(MobileInfantry member, float maxDistanceFromLeader)
    {
        return Vector3.SqrMagnitude(Leader.transform.position - member.transform.position) < maxDistanceFromLeader * maxDistanceFromLeader;
    }

    #endregion

    /// <summary>Draws a line from the Leader to each member.</summary>
    public void DrawConnectionToMembers()
    {
        Gizmos.color = Team == MilitaryTeam.Red ? Color.red : Color.blue;

        foreach (var member in _fireteamMembers)
        {
            if (member != Leader)
                Gizmos.DrawLine(Leader.transform.position, member.transform.position);
        }
    }
}
