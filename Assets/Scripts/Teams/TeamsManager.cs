using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public enum Team
{
    Red,
    Blue,
    None
}
public interface InitializeUnit
{
    void InitializeUnit(Team newTeam);
}
[System.Serializable]
public struct TeamParameters
{
    [Range(0,50)]public int FireteamQuantity;
    [Range(0,5)] public int membersPerFireteam;
    [Range(0,50)]public int planesQuantity;
    [Range(0,50)]public int tanksQuantity;
    public Transform SpawnArea;
    public float width, height;
}

public class TeamsManager : MonoSingleton<TeamsManager>
{

    [SerializeField] Infantry infantryPrefab;
    [SerializeField] Plane planePrefab;
   

    public LayerMask NotSpawnable,Ground;

    public float separationRadiusBetweenUnits;

    #region TeamsDictionary
    Dictionary<Team, List<Entity>> _teams = new Dictionary<Team, List<Entity>>();

    [SerializeField,SerializedDictionary("Team","Parameters")]
    SerializedDictionary<Team, TeamParameters> MatchParameters = new SerializedDictionary<Team, TeamParameters>();

    #region MemberAdd

    public void AddToTeam(Team key,Entity value)
    {
        if (!_teams[key].Contains(value))
        {
            _teams[key].Add(value);
        }
       
    }

    public void AddToTeam(Team key, IEnumerable<Entity> values)
    {
        foreach (var item in values.Where(x=> !_teams[key].Contains(x)))
        {            
            _teams[key].Add(item);           
        }
       

    }

    public void RemoveFromTeam(Team key, Entity value)
    {
        if (_teams[key].Contains(value))
        {
            _teams[key].Remove(value);
        }
    }

    public void RemoveFromTeam(Team key, IEnumerable<Entity> values)
    {
        foreach (var item in values.Where(x => _teams[key].Contains(x)))
        {
            _teams[key].Remove(item);
        }
    }
    #endregion
    #endregion

    protected override void SingletonAwake()
    {
        //inicializo las listas del diccionario
        //foreach (Team key in Enum.GetValues(typeof(Team)))
        //{
        //    _teams.Add(key, new List<Entity>());
        //    SpawnFireteams(key, MatchParameters[key]);
        //    SpawnPlanes(key, MatchParameters[key]);
        //}       
    }


    void SpawnFireteams(Team team,TeamParameters param)
    {
        List<Fireteam> fireteams = new List<Fireteam>(); 
        for (int i = 0; i < param.FireteamQuantity; i++)
        {
            var newFT = Instantiate(new Fireteam());

            for (int j = 0; j < param.membersPerFireteam; j++)
            {
                Vector3 pos = GetRandomFreePosOnGround(param);
                newFT.AddMember(Instantiate(infantryPrefab, pos, Quaternion.identity));
            }

            fireteams.Add(newFT);       
        }
    }

    void SpawnPlanes(Team team, TeamParameters parameters)
    {
        for (int i = 0; i < parameters.planesQuantity; i++)
        {
            GetRandomFreePosOnAir(parameters);
            var x = Instantiate(planePrefab);
            _teams[team].Add(x);
        }
    }

    /// <summary>
    /// obtiene una posicion random en la que no haya ninguna grid entity cerca
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// 

    Vector3 GetRandomFreePosOnAir(TeamParameters parameters)
    {
        float width = parameters.width;
        float height = parameters.height;
        Vector3 randomPos = parameters.SpawnArea.transform.position + new Vector3(Random.Range(-width, width), 0, Random.Range(-width, width));

        
            //si no hay ninguna grid entity cerca,devuelvo la posicion
            bool entityNearby = Physics.OverlapSphere(randomPos, separationRadiusBetweenUnits, NotSpawnable)
                .Where(x => x != this).Where(x => x.TryGetComponent(out GridEntity aux)).Any();

            //esto es pesadisimo, pero como solo se haria en el awake...
            if (!entityNearby) return randomPos;
        


        return GetRandomFreePosOnGround(parameters);
    }

    Vector3 GetRandomFreePosOnGround(TeamParameters parameters)
    {
        float width = parameters.width;
        float height = parameters.height;
        Vector3 randomPos = parameters.SpawnArea.transform.position + new Vector3(Random.Range(-width, width),0, Random.Range(-width, width));
        
        if (Physics.Raycast(randomPos,Vector3.down,out RaycastHit hit,Mathf.Infinity, Ground))
        {
            //si no hay ninguna grid entity cerca,devuelvo la posicion
            bool entityNearby = Physics.OverlapSphere(hit.point, separationRadiusBetweenUnits,NotSpawnable)
                .Where(x => x != this).Where(x => x.TryGetComponent(out GridEntity aux)).Any();
               
            //esto es pesadisimo, pero como solo se haria en el awake...
            if (!entityNearby) return hit.point;                  
        }      


        return GetRandomFreePosOnGround(parameters);
    }

    public IEnumerable<Fireteam> GetAllyFireteams(Team team)
    {
        return _teams[team].OfType<Infantry>().Select(x => x.myFireteam).Where(x => x != null).Distinct();
           
    }

    //public IEnumerable<Infantry> GetAllTanks(Team team)
    //{
    //    return _teams[team].OfType<Infantry>().Select(x => x.myFireteam).Distinct();

    //}

    public IEnumerable<Plane> GetTeamPlanes(Team team)
    {
        return _teams[team]
            .OfType<Plane>()
            .Where(x => x.actualState != PlaneStates.ABANDONED);
    }

    private void OnValidate()
    {
        if (MatchParameters.ContainsKey(Team.None))
        {
            MatchParameters.Remove(Team.None);
        }

    }
    public bool canDebug;
    private void OnDrawGizmos()
    {

        //if (Application.isPlaying || !canDebug) return;
        
        //foreach (var item in MatchParameters)
        //{
        //    Gizmos.color = item.Key == Team.Red ? Color.red : Color.blue;
        //    float width = (float)item.Value.width;
        //    float height = (float)item.Value.height;
        //    if (item.Value.SpawnArea == null) continue;
        //    Vector3 spawnArea = item.Value.SpawnArea.position;
            
        //    Gizmos.DrawLine(spawnArea + new Vector3(-width, 0, height), spawnArea + new Vector3(width, 0, height));
        //    Gizmos.DrawLine(spawnArea + new Vector3(width, 0, -height), spawnArea + new Vector3(width, 0, height));
        //    Gizmos.DrawLine(spawnArea + new Vector3(width, 0, -height), spawnArea + new Vector3(-width, 0, -height));
        //    Gizmos.DrawLine(spawnArea + new Vector3(-width, 0, height),  spawnArea  + new Vector3(-width, 0, -height));

        //    Vector3 freepos = GetRandomFreePosOnGround(item.Value);

        //    Gizmos.DrawWireSphere(freepos,separationRadiusBetweenUnits);
        //    Gizmos.DrawLine(freepos,freepos+Vector3.up * 50);

        //}
      
    }

   
}
