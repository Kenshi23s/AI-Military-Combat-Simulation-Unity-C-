using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

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

    [SerializeField] Infantry _infantryPrefab;
    [SerializeField] Plane _planePrefab;
 
    public LayerMask NotSpawnable,Ground;

    [field: SerializeField] public float SeparationRadiusBetweenUnits { get; private set; }

    #region TeamsDictionary
    Dictionary<MilitaryTeam, List<Entity>> _teams = new Dictionary<MilitaryTeam, List<Entity>>();

    [SerializeField,SerializedDictionary("Team","Parameters")]
    SerializedDictionary<MilitaryTeam, TeamParameters> _matchParameters = new SerializedDictionary<MilitaryTeam, TeamParameters>();
    int _watchDog;

    public bool canDebug;
    #region MemberAdd

    public void AddToTeam(MilitaryTeam key,Entity value)
    {
        if (!_teams[key].Contains(value))
        {
            _teams[key].Add(value);
        }
       
    }

    public void AddToTeam(MilitaryTeam key, IEnumerable<Entity> values)
    {
        foreach (var item in values.Where(x=> !_teams[key].Contains(x)))
        {            
            _teams[key].Add(item);           
        }
       

    }

    public void RemoveFromTeam(MilitaryTeam key, Entity value)
    {
        if (_teams[key].Contains(value))
        {
            _teams[key].Remove(value);
        }
    }

    public void RemoveFromTeam(MilitaryTeam key, IEnumerable<Entity> values)
    {
        foreach (var item in values.Where(x => _teams[key].Contains(x)))       
            _teams[key].Remove(item);
        
    }
    #endregion
    #endregion

    protected override void SingletonAwake()
    {
        foreach (MilitaryTeam key in Enum.GetValues(typeof(MilitaryTeam)))
        {
            if (key == MilitaryTeam.None) continue;

            _teams.Add(key, new List<Entity>());
        }

    }

    private void Start()
    {
        foreach (MilitaryTeam key in _teams.Keys)
        {      

       
            SpawnFireteams(key, _matchParameters[key]);
            SpawnPlanes(key, _matchParameters[key]);
        }
    }


    void SpawnFireteams(MilitaryTeam team, TeamParameters param)
    {
        List<Fireteam> fireteams = new List<Fireteam>();
        
        for (int i = 0; i < param.FireteamQuantity; i++)
        {       
           FList<Infantry> members = new FList<Infantry>();

           for (int j = 0; j < param.membersPerFireteam; j++)
           {
                _watchDog = 0;
                if (GetRandomFreePosOnGround(param, out Vector3 pos))
                    members += Instantiate(_infantryPrefab, pos, Quaternion.identity);
                else
                    return;
           }      

           Fireteam newFT = new Fireteam(team, members.ToList());
           fireteams.Add(newFT);         
        }
        var col = fireteams.SelectMany(x => x.FireteamMembers);
        AddToTeam(team, col);
    }

    void SpawnPlanes(MilitaryTeam team, TeamParameters parameters)
    {
        for (int i = 0; i < parameters.planesQuantity; i++)
        {
            if (GetRandomFreePosOnAir(parameters,out Vector3 pos))
            {
                var x = Instantiate(_planePrefab,pos,Quaternion.identity);
                _teams[team].Add(x);
            }
            else           
                return;                               
        }
    }

    /// <summary>
    /// obtiene una posicion random en la que no haya ninguna grid entity cerca
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    /// 

  
    bool GetRandomFreePosOnAir(TeamParameters parameters,out Vector3 pos)
    {  
        if (_watchDog >= 1000)
        {
            pos = Vector3.zero;
            canDebug = false;
            Debug.LogError("STACK OVERFLOW AL BUSCAR POSICIONES RANDOMS EN EL AIRE");
            return false;
        }
        float width = parameters.width;
        float height = parameters.height;
        Vector3 randomPos = parameters.SpawnArea.transform.position + new Vector3(Random.Range(-width, width), 0, Random.Range(-height, height));
        
        //si no hay ninguna grid entity cerca,devuelvo la posicion
        bool entityNearby = Physics.OverlapSphere(randomPos, SeparationRadiusBetweenUnits, NotSpawnable)
            .Where(x => x != this).Where(x => x.TryGetComponent(out GridEntity aux)).Any();

        //esto es pesadisimo, pero como solo se haria en el awake...
        if (!entityNearby) 
        {
            pos = randomPos;
            return true;
        }

        _watchDog++;
        return GetRandomFreePosOnAir(parameters,out pos);
    }

    bool GetRandomFreePosOnGround(TeamParameters parameters, out Vector3 pos)
    {
        if (_watchDog >= 200)
        {
            pos = Vector3.zero;
            Debug.LogError("WATCHDOG AL LIMITE, CORTO EJECUCION");
            return false;
        }
        float width = parameters.width;
        float height = parameters.height;
       
        Vector3 randomPos = parameters.SpawnArea.transform.position + new Vector3(Random.Range(-width, width), 0, Random.Range(-height, height));


        if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, float.MaxValue, Ground))
        {
            //si no hay ninguna grid entity cerca,devuelvo la posicion
            var entityNearby = Physics.OverlapSphere(hit.point, SeparationRadiusBetweenUnits, NotSpawnable)
                .Where(x => x != this)
                .Where(x => x.TryGetComponent(out GridEntity aux));


            //esto es pesadisimo, pero como solo se haria en el awake...
            if (!entityNearby.Any())
            {
             
                pos = hit.point;
                return true;
            }
            else
                Debug.Log($"no puedo spawnear aca, hay {entityNearby.Count()} cerca, hago recursion C: ");
        }

        Debug.Log(randomPos);
        Debug.Log(hit.point+"watchdog = "+_watchDog);



        _watchDog++;
        return GetRandomFreePosOnGround(parameters, out pos);
    }

    public IEnumerable<Fireteam> GetAllyFireteams(MilitaryTeam team)
    {
        return _teams[team].OfType<Infantry>().Select(x => x.MyFireteam).Where(x => x != null).Distinct();       
    }

    public IEnumerable<Plane> GetTeamPlanes(MilitaryTeam team)
    {
        return _teams[team]
            .OfType<Plane>()
            .Where(x => x.actualState != PlaneStates.ABANDONED);
    }

    private void OnValidate()
    {
        if (_matchParameters.ContainsKey(MilitaryTeam.None))
        {
            _matchParameters.Remove(MilitaryTeam.None);
        }

    }

    private void OnDrawGizmos()
    {

        if (Application.isPlaying || !canDebug) return;

        foreach (var item in _matchParameters)
        {
            Gizmos.color = item.Key == MilitaryTeam.Red ? Color.red : Color.blue;
            float width = (float)item.Value.width;
            float height = (float)item.Value.height;
            if (item.Value.SpawnArea == null) continue;
            Vector3 spawnArea = item.Value.SpawnArea.position;

            Gizmos.DrawLine(spawnArea + new Vector3(-width, 0, height), spawnArea + new Vector3(width, 0, height));
            Gizmos.DrawLine(spawnArea + new Vector3(width, 0, -height), spawnArea + new Vector3(width, 0, height));
            Gizmos.DrawLine(spawnArea + new Vector3(width, 0, -height), spawnArea + new Vector3(-width, 0, -height));
            Gizmos.DrawLine(spawnArea + new Vector3(-width, 0, height), spawnArea + new Vector3(-width, 0, -height));
            _watchDog = 0;
            if (GetRandomFreePosOnGround(item.Value, out Vector3 freepos))
            {
                Gizmos.DrawWireSphere(freepos, SeparationRadiusBetweenUnits);
                Gizmos.DrawLine(freepos, freepos + Vector3.up * 50);
            }
        }

    }

   
}
