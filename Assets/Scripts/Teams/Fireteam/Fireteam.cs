using FacundoColomboMethods;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class Fireteam
{
    public static void Create(MilitaryTeam newTeam, List<MobileInfantry> members)
    {
        new Fireteam(newTeam, members);
    }
    public Fireteam(MilitaryTeam newTeam, List<MobileInfantry> members)
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

        foreach (var member in _fireteamMembers.Where(x => x != Leader))
            member.Initialize(newTeam);

        Leader.Initialize(newTeam);
    }

    List<MobileInfantry> _fireteamMembers = new List<MobileInfantry>();

    public ReadOnlyCollection<MobileInfantry> FireteamMembers;

    public MobileInfantry Leader { get; private set; }

    public MilitaryTeam Team { get; private set; }



    #region MemberManagment
    public void AddMember(MobileInfantry infantry)
    {
        if (!_fireteamMembers.Contains(infantry))
        {
            _fireteamMembers.Add(infantry);
            infantry.Fireteam = this;
        }

    }

    public void AddMembers(IEnumerable<MobileInfantry> infantryCol)
    {
        foreach (var item in infantryCol.Where(x => !_fireteamMembers.Contains(x)))
        {
            _fireteamMembers.Add(item);
            item.Fireteam = this;
        }

    }

    public void RemoveMember(MobileInfantry infantry)
    {
        if (_fireteamMembers.Contains(infantry))
            _fireteamMembers.Remove(infantry);

        if (infantry == Leader && !_fireteamMembers.Any()) return;

        Leader = _fireteamMembers.PickRandom();
    }


    public void RemoveMembers(IEnumerable<MobileInfantry> infantryCol)
    {
        foreach (var item in infantryCol.Where(x => _fireteamMembers.Contains(x)))
            _fireteamMembers.Remove(item);

        if (infantryCol.Contains(Leader) && !_fireteamMembers.Any()) return;

        Leader = _fireteamMembers.PickRandom();
    }
    #endregion


    CapturePoint targetCapturePoint;
    public IEnumerator FindNearestUntakenPoint()
    {
        // Esperamos un frame para darle tiempo a las demas fsms a que se inicialicen. Idealmente no deberia ser asi esto.
        yield return null;
        // Conseguir el punto mas cercano
        var untakenPoint = CapturePointManager.instance.CapturePoints.Where(IsPointPriority);
        targetCapturePoint = untakenPoint.Minimum(x => Vector3.SqrMagnitude(x.transform.position - Leader.transform.position));

        targetCapturePoint.OnCaptureComplete += OnTargetPointCaptured;

        Leader.DebugEntity.Log($"Moviendome a la bandera {targetCapturePoint}");
        SendPatrolOrders(targetCapturePoint.transform.position);
    }

    void OnTargetPointCaptured(MilitaryTeam capturedBy) 
    {
        if (capturedBy != Team) return;

        Leader.AwaitOrders();
        foreach (var unit in _fireteamMembers.Where(x => x != Leader))
            unit.FollowLeader();

        targetCapturePoint.OnCaptureComplete -= OnTargetPointCaptured;
    }

    bool IsPointPriority(CapturePoint point)
    {
        return point.CapturedBy != Team 
            || point.CurrentState == CaptureState.Disputed 
            || (point.CurrentState == CaptureState.BeingCaptured && point.BeingCapturedBy != Team);
    }

    void SendPatrolOrders(Vector3 newDestination)
    {
        //lookup divide una coleccion en base a un predicado, no solo se puede dividir en booleanas
        //tambien se puede dividir en strings o enums

        //deberia poner a donde debe ir el lider
        //IA2-LINQ
        var split = _fireteamMembers.ToLookup(x => x == Leader);
        //agarro el lider y le digo q se mueva hacia la nueva destinacion
        split[true].First().LeaderMoveTo(newDestination);

        foreach (var members in split[false]) members.FollowLeader();
    }

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
            neartestInDanger = col.Minimum(x => Vector3.SqrMagnitude(x.transform.position - whoAsks.transform.position));
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
    public bool IsNearLeader(MobileInfantry member, float maxDistanceFromLeader)
    {
        return Vector3.SqrMagnitude(Leader.transform.position - member.transform.position) < maxDistanceFromLeader * maxDistanceFromLeader;
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
            .Minimum(x => Vector3.SqrMagnitude(x.Leader.transform.position - Leader.transform.position));
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
