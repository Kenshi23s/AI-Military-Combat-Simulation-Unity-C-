using AYellowpaper.SerializedCollections;
using FacundoColomboMethods;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
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
    [SerializeField] bool _canDebug;
    [SerializeField] Infantry _infantryPrefab;
    [SerializeField] Plane _planePrefab;
    [SerializeField] Civilian _civilianPrefab;
 
    public LayerMask NotSpawnable,Ground;

    [field: SerializeField] public float SeparationRadiusBetweenUnits { get; private set; }

    #region TeamsDictionary
    Dictionary<MilitaryTeam, List<Entity>> _teams = new Dictionary<MilitaryTeam, List<Entity>>();

    [SerializeField,SerializedDictionary("Team","Parameters")]
    SerializedDictionary<MilitaryTeam, TeamParameters> _matchParameters = new SerializedDictionary<MilitaryTeam, TeamParameters>();


    [Header("Team Indicator")]
    [SerializeField, SerializedDictionary("Type", "Sprite")]
    public SerializedDictionary<SerializableType, Sprite> sprites = new SerializedDictionary<SerializableType, Sprite>()
    {
        {new SerializableType(typeof(Plane)),default },
        {new SerializableType(typeof(Sniper)),default },
        {new SerializableType(typeof(Turret)),default },
        {new SerializableType(typeof(IMilitary)),default }

    };

    int _watchDog;

    public event Action OnLateUpdate;
  
    [SerializeField] TeamIndicator _prefabTeamIndicator;
    [SerializeField] float _unitsAboveMilitary;

    #region Civilian
    [Header("Civilian Spawn")]
    [SerializeField] Transform _civilianPos;
    [SerializeField] int _civiliansQuantity;
    [SerializeField] float _width_CivilianSpawn, _height_CivilianSpawn;
    #endregion

    #region MemberAdd

    public void AddToTeam(MilitaryTeam key,Entity value,Sprite icon = default)
    {
        if (!_teams[key].Contains(value))
        {
            _teams[key].Add(value);
            var indicator = Instantiate(_prefabTeamIndicator, value.transform.position + Vector3.up * _unitsAboveMilitary, Quaternion.identity);
            indicator.transform.SetParent(value.transform);

            if (icon == default)
                icon = GetSprite(typeof(IMilitary));
            //agarro solamente el nombre con esto
            var z = value.gameObject.name.Split("-");
            indicator.SetName(z.Last());
            indicator.AssignOwner(value as IMilitary, icon);
        }
       
    }

    public void AddToTeam(MilitaryTeam key, IEnumerable<Entity> values,Sprite icon = default)
    {
        foreach (var item in values.Where(x => !_teams[key].Contains(x)))
        {            
            _teams[key].Add(item);
            var indicator = Instantiate(_prefabTeamIndicator, item.transform.position + Vector3.up * _unitsAboveMilitary,Quaternion.identity);
            indicator.transform.SetParent(item.transform);


         
            if (icon == default)
                icon = GetSprite(typeof(IMilitary));

            var z = item.gameObject.name.Split("-");
            indicator.SetName(z.Last());
            indicator.AssignOwner(item as IMilitary, icon);
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

    public const int WaitFramesBeforeContinuing =144;
    #region UnityCalls
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
            StartCoroutine(SpawnFireteams(key, _matchParameters[key]));
           StartCoroutine(SpawnPlanes(key, _matchParameters[key]));
        }
        StartCoroutine(SpawnCivilians());
    }

    private void LateUpdate()
    {
        OnLateUpdate?.Invoke();
    }

    private void OnValidate()
    {
        if (_matchParameters.ContainsKey(MilitaryTeam.None))
        {
            _matchParameters.Remove(MilitaryTeam.None);
        }

    }
    #endregion

    #region SpawnMethds

    #region EntitySpawn
    IEnumerator SpawnFireteams(MilitaryTeam team, TeamParameters param)
    {
      

        GameObject newGO = Instantiate(new GameObject(team + " Infantry"), transform);

        for (int i = 0; i < param.FireteamQuantity; i++)
        {       
           FList<Infantry> members = new FList<Infantry>();

           GameObject fireteamGroup = Instantiate(new GameObject("Fireteam"+ ColomboMethods.GenerateName(5)), newGO.transform);

           for (int j = 0; j < param.membersPerFireteam; j++)
           {
                _watchDog = 0;
                if (GetRandomFreePosOnGround(param, out Vector3 pos))
                {
                    var newUnit = Instantiate(_infantryPrefab, pos, Quaternion.identity);
                    newUnit.transform.parent = fireteamGroup.transform;
                    members += newUnit;


                }
                else
                    break;
           }
         
           Fireteam newFT = new Fireteam(team, members.ToList());
           
            for (int k = 0; k < WaitFramesBeforeContinuing; k++) yield return null;
            
            AddToTeam(team, members);
        }
     
    }

    IEnumerator SpawnPlanes(MilitaryTeam team, TeamParameters parameters)
    {
        GameObject newGO = Instantiate(new GameObject(team + " Planes"), transform);
        for (int i = 0; i < parameters.planesQuantity; i++)
        {
            if (GetRandomFreePosOnAir(parameters,out Vector3 pos))
            {
                var x = Instantiate(_planePrefab,pos,Quaternion.identity);
                x.Initialize(team);
                AddToTeam(team, x, GetSprite(typeof(Plane)));
                x.transform.parent = newGO.transform;

                int actualFrameRate = ColomboMethods.GetActualFrameRate;

                for (int j = 0; j < WaitFramesBeforeContinuing; j++) yield return null;             
               
            }
            else           
                break;                               
        }
    }

    IEnumerator SpawnCivilians()
    {
        GameObject newGO = Instantiate(new GameObject("Civilians"), transform);

        for (int i = 0; i < _civiliansQuantity; i++)
        {
            if (GetRandomFreePosOnGround(_civilianPos.position, _width_CivilianSpawn, _height_CivilianSpawn, out var pos))
            {
                var new_civilian = Instantiate(_civilianPrefab, pos, Quaternion.identity);
                new_civilian.transform.parent = newGO.transform;

                int _frameRate = ColomboMethods.GetActualFrameRate;
                for (int j = 0; j < WaitFramesBeforeContinuing; j++) yield return null;

            }
            else
            {
                yield return null;
            }
          
            
        }
    }
    #endregion

    #region GetRandomPos
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
            _canDebug = false;
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

    bool GetRandomFreePosOnGround(TeamParameters  parameters, out Vector3 pos)
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

    bool GetRandomFreePosOnGround(Vector3 spawnArea,float width,float height, out Vector3 pos)
    {
        if (_watchDog >= 200)
        {
            pos = Vector3.zero;
            Debug.LogError("WATCHDOG AL LIMITE, CORTO EJECUCION");
            return false;
        }
        

        Vector3 randomPos = spawnArea + new Vector3(Random.Range(-width, width), 0, Random.Range(-height, height));


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

       
        Debug.Log(hit.point + "watchdog = " + _watchDog);



        _watchDog++;
        return GetRandomFreePosOnGround(spawnArea, width,height, out pos);
    }
    #endregion

    #endregion

    #region UsefulMethods
    public Sprite GetSprite(Type targetType)
    {
        foreach (var keys in sprites.Keys)
        {
            if (targetType == keys.type)
                return sprites[keys];

        }
        return sprites.Where(x => x.Key.type == typeof(IMilitary)).First().Value;
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
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {

        if (Application.isPlaying || !_canDebug) return;
        
         DrawCivilianSpawn(); DrawTeamSpawn();
    }

    void DrawTeamSpawn()
    {
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

    void DrawCivilianSpawn()
    {

        if (_civilianPos == null) return;
      
        Vector3 spawnArea = _civilianPos.position;
     
        Gizmos.color = Color.white;
        Gizmos.DrawLine(spawnArea + new Vector3(-_width_CivilianSpawn, 0, _height_CivilianSpawn), spawnArea + new Vector3(_width_CivilianSpawn, 0, _height_CivilianSpawn));
        Gizmos.DrawLine(spawnArea + new Vector3(_width_CivilianSpawn, 0, -_height_CivilianSpawn), spawnArea + new Vector3(_width_CivilianSpawn, 0, _height_CivilianSpawn));
        Gizmos.DrawLine(spawnArea + new Vector3(_width_CivilianSpawn, 0, -_height_CivilianSpawn), spawnArea + new Vector3(-_width_CivilianSpawn, 0, -_height_CivilianSpawn));
        Gizmos.DrawLine(spawnArea + new Vector3(-_width_CivilianSpawn, 0, _height_CivilianSpawn), spawnArea + new Vector3(-_width_CivilianSpawn, 0, -_height_CivilianSpawn));
        _watchDog = 0;
        if (GetRandomFreePosOnGround(_civilianPos.position,_width_CivilianSpawn,_height_CivilianSpawn, out Vector3 freepos))
        {
            Gizmos.DrawWireSphere(freepos, SeparationRadiusBetweenUnits);
            Gizmos.DrawLine(freepos, freepos + Vector3.up * 50);
        }
    }
    #endregion

}
