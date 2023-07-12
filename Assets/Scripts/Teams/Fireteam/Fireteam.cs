using FacundoColomboMethods;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using static Infantry;

public class Fireteam : MonoBehaviour
{
    List<Infantry> _fireteamMembers = new List<Infantry>();
    public ReadOnlyCollection<Infantry> fireteamMembers;
    public Infantry Leader { get; private set; }

    public Team myTeam { get; private set; }

   
    public float MinimumDistanceFromLeader { get; private set; }

    #region MemberManagment
    public void AddMember(Infantry infantry)
    {
        if (!_fireteamMembers.Contains(infantry))        
            _fireteamMembers.Add(infantry);                  
    }

    public void AddMember(IEnumerable<Infantry> infantry)
    {
        foreach (var item in infantry.Where(x => !_fireteamMembers.Contains(x)))      
            _fireteamMembers.Add(item);           
    }

    public void RemoveMember(Infantry infantry)
    {
        if (_fireteamMembers.Contains(infantry))        
            _fireteamMembers.Remove(infantry);       
    }

    public void RemoveMember(IEnumerable<Infantry> infantry)
    {
        foreach (var item in infantry.Where(x => _fireteamMembers.Contains(x)))       
            _fireteamMembers.Remove(item);
    }
    #endregion

    private void Start()
    {
        if (Leader==null)
        {
            Leader = _fireteamMembers.PickRandom();
            fireteamMembers = _fireteamMembers.AsReadOnly();
        }
    }

    public Vector3 LookForNearestZone()
    {
        //obtengo mis zonas en disputa
        var disputedMine = CapturePointManager.instance.CapturePoints.Where(x => x.Key == myTeam)
            .SelectMany(x => x.Value)
            .Where(x => x.CurrentCaptureState == CapturePoint.ZoneStates.Disputed)
            .Select(x => x);

        //y las zonas neut
        var enemyZones = CapturePointManager.instance.CapturePoints
            .Where(x => x.Key != myTeam)
            .SelectMany(x => x.Value)
            .Select(x => x);
        //las uno
        var concat = disputedMine.Concat(enemyZones).Distinct();
        //y obtengo las mas cercanas, pongo el distinct por las dudas de q haya alguno repetido
        //Distinct: saca los elementos que sean iguales o esten "clonados"

        return concat.Distinct().Minimum(x => Vector3.Distance(x.transform.position,Leader.transform.position)).transform.position;
    
    }

    void SendPatrolOrders(Vector3 NewDestination)
    {
        //lookup divide una coleccion en base a un predicado, no solo se puede dividir en booleanas
        //tambien se puede dividir en strings o enums

        //deberia poner a donde debe ir el lider

        var split = _fireteamMembers.ToLookup(x => x == Leader);
        //agarro el lider y le digo q se mueva hacia la nueva destinacion
        split[true].First().MoveTowardsTransition(NewDestination);

        foreach (var members in split[false]) members.FollowLeaderTransition();
    }

    #region GetMethods
    public IEnumerable<Mechanic> GetMechanics() => _fireteamMembers.OfType<Mechanic>();

    public IEnumerable<Medic> GetMedics() => _fireteamMembers.Select(x => x as GridEntity).OfType<Medic>();
    #endregion

    #region UsefulQuestions
    //pregunta si algun aliado de la escuadra tiene enemigos cerca
    public bool AlliesWithEnemiesNearby(Entity whoAsks,out Entity neartestInDanger)
    {
        neartestInDanger = null;
        var col = fireteamMembers.Where(x => x != whoAsks).Where(x => x.GetEntitiesAround().Any(x => x.health.isAlive && x.MyTeam != myTeam));
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
        foreach (var item in fireteamMembers)
        {
            if (item.inCombat)
                return true;
        }

        return false;
    }
    //saber si la unidad esta cerca del lider
    public bool IsNearLeader(Infantry member)
    {
        return Vector3.Distance(Leader.transform.position,member.transform.position) < MinimumDistanceFromLeader;
    }
    #endregion


    //para pedir apoyo a otras unidades
    public void RequestSupport(Vector3 enemyPosition)
    {
        //si hay un avion cercano para bombardeo
        var planes = TeamsManager.instance.GetTeamPlanes(myTeam).Where(x => x.actualState == PlaneStates.FLY_AROUND);
        if (planes.Any())
        {       //lo pido
            planes.PickRandom().CallAirStrike(enemyPosition);
            return;
        }
                  
        //sino, pido apoyo a alguna escuadra que no este en combate
        var nearestFireteam = TeamsManager.instance
            .GetAllyFireteams(myTeam)
            .Where(x => x.FireteamInCombat())
            .Minimum(x => Vector3.Distance(x.Leader.transform.position,transform.position));
        if (nearestFireteam != null)       
            nearestFireteam.HelpNearFireteam(enemyPosition);
        
    }

    public void HelpNearFireteam(Vector3 EnemyPos)
    {
        Leader.MoveTowardsTransition(EnemyPos);
    }

   
}
