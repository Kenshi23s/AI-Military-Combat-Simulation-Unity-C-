using FacundoColomboMethods;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class Fireteam
{

    public Fireteam(MilitaryTeam newTeam, List<Infantry> members)
    {
        Team = newTeam;

        AddMembers(members);

        if (Leader == null)
        {
            // Elegir lider al azar
            Leader = _fireteamMembers.PickRandom();
            Leader.DebugEntity.AddGizmoAction(DrawConnectionToMembers);
            Leader.gameObject.name = "[Lider]" + Leader.gameObject.name;
            FireteamMembers = _fireteamMembers.AsReadOnly();
        }

        foreach (var item in _fireteamMembers.Where(x => x != Leader))
            item.InitializeUnit(newTeam);
        Leader.InitializeUnit(newTeam);
    }

    List<Infantry> _fireteamMembers = new List<Infantry>();

    public ReadOnlyCollection<Infantry> FireteamMembers;


    public Infantry Leader { get; private set; }

    public MilitaryTeam Team { get; private set; }



    #region MemberManagment
    public void AddMember(Infantry infantry)
    {
        if (!_fireteamMembers.Contains(infantry))
        {
            _fireteamMembers.Add(infantry);
            infantry.SetFireteam(this);
        }

    }

    public void AddMembers(IEnumerable<Infantry> infantryCol)
    {
        foreach (var item in infantryCol.Where(x => !_fireteamMembers.Contains(x)))
        {
            _fireteamMembers.Add(item);
            item.SetFireteam(this);
        }

    }

    public void RemoveMember(Infantry infantry)
    {
        if (_fireteamMembers.Contains(infantry))
            _fireteamMembers.Remove(infantry);

        if (infantry == Leader && !_fireteamMembers.Any()) return;

        Leader = _fireteamMembers.PickRandom();
    }


    public void RemoveMembers(IEnumerable<Infantry> infantryCol)
    {
        foreach (var item in infantryCol.Where(x => _fireteamMembers.Contains(x)))
            _fireteamMembers.Remove(item);

        if (infantryCol.Contains(Leader) && !_fireteamMembers.Any()) return;

        Leader = _fireteamMembers.PickRandom();
    }
    #endregion

    public IEnumerator FindNearestUntakenPoint()
    {
        yield return null;
        var untakenPoint = CapturePointManager.instance.CapturePoints.Where(IsPointPriority);
        var nearest = untakenPoint.Minimum(x => Vector3.SqrMagnitude(x.transform.position - Leader.transform.position));

        nearest.OnCaptureComplete += (capturedBy) =>
        {
            if (capturedBy != Team) return;

            Leader.WaitOrdersTransition();
            foreach (var unit in _fireteamMembers.Where(x => x != Leader))
                unit.FollowLeaderTransition();
        };

        Leader.DebugEntity.Log($"Moviendome a la bandera {nearest}");
        SendPatrolOrders(nearest.transform.position);
    }

    bool IsPointPriority(CapturePoint point)
    {
        return point.CapturedBy != Team || point.CurrentState == CaptureState.Disputed || (point.CurrentState == CaptureState.BeingCaptured && point.BeingCapturedBy != Team);
    }

    void SendPatrolOrders(Vector3 newDestination)
    {
        //lookup divide una coleccion en base a un predicado, no solo se puede dividir en booleanas
        //tambien se puede dividir en strings o enums

        //deberia poner a donde debe ir el lider

        var split = _fireteamMembers.ToLookup(x => x == Leader);
        //agarro el lider y le digo q se mueva hacia la nueva destinacion
        split[true].First().MoveTowardsTransition(newDestination);

        foreach (var members in split[false]) members.FollowLeaderTransition();
    }

    #region GetMethods


    public IEnumerable<Medic> GetMedics() => _fireteamMembers.OfType<Medic>();
    #endregion

    #region UsefulQuestions
    //pregunta si algun aliado de la escuadra tiene enemigos cerca
    public bool AlliesWithEnemiesNearby(Entity whoAsks, out Entity neartestInDanger)
    {
        neartestInDanger = null;
        var col = FireteamMembers.Where(x => x != whoAsks).Where(x => x.GetMilitaryAround().Any(x => x.Health.IsAlive && x.Team != Team));
        //si tiene algun aliado de la escuadra que no sea el con enemigo cerca que tengan vida
        if (col.Any())
        {
            //le dice que es verdadero y le devuelve el mas cercano en peligro
            neartestInDanger = col.Minimum(x => Vector3.Distance(x.transform.position, whoAsks.transform.position));
            return true;
        }
        return false;
    }
    //para saber si esta escuadra tiene a alguno de sus miembros  en combate
    public bool FireteamInCombat()
    {
        foreach (var item in FireteamMembers)
        {
            if (item.InCombat)
                return true;
        }

        return false;
    }
    //saber si la unidad esta cerca del lider
    public bool IsNearLeader(Infantry member, float MinimumDistanceFromLeader)
    {
        return Vector3.Distance(Leader.transform.position, member.transform.position) < MinimumDistanceFromLeader;
    }
    #endregion


    /// <summary>
    /// para pedir apoyo a otras unidades
    /// </summary>
    /// <param name="enemyPosition"></param>
    public void RequestSupport(Vector3 enemyPosition)
    {
        string debug = "Fireteam solicita apoyo";


        //si hay un avion cercano para bombardeo
        var planes = TeamsManager.instance.GetTeamPlanes(Team).Where(x => x.actualState == PlaneStates.FLY_AROUND);
        if (planes.Any())
        {       //lo pido
            debug += ", hay un avion disponible, viene a bombardear!";
            planes.PickRandom().CallAirStrike(enemyPosition);
            return;
        }

        //sino, pido apoyo a alguna escuadra que no este en combate
        var nearestFireteam = TeamsManager.instance
            .GetAllyFireteams(Team)
            .Where(x => !x.FireteamInCombat())
            .Minimum(x => Vector3.Distance(x.Leader.transform.position, Leader.transform.position));
        if (nearestFireteam != null)
        {
            nearestFireteam.HelpNearFireteam(enemyPosition);
            debug += ", hay un fireteam disponible, viene a ayudar!";
        }

        debug += ", sin respuesta, el Fireteam esta solo en esta :C";


    }

    public void HelpNearFireteam(Vector3 EnemyPos)
    {
        SendPatrolOrders(EnemyPos);
    }

    public void DrawConnectionToMembers()
    {

        foreach (var item in FireteamMembers.Where(x => x != Leader))
        {
            Gizmos.color = Team == MilitaryTeam.Red ? Color.red : Color.blue;
            Gizmos.DrawLine(Leader.transform.position, item.transform.position);
        }
    }


}
