using AYellowpaper.SerializedCollections;
using FacundoColomboMethods;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>Spawn settings of one team.</summary>
[System.Serializable]
public struct TeamParameters
{
    [Range(0,50)]public int FireteamQuantity;
    [Range(0,5)] public int membersPerFireteam;
    [Range(0,50)]public int planesQuantity;
    public Transform SpawnArea;
    public float width, height;
}

/// <summary>
/// Spawns the fireteams, planes and civilians of the match.
/// Stores the entities of each team with their TeamIndicator.
/// </summary>
/// <remarks>
/// <para>The spawns begin with StartMatch. Without a MatchSetupPanel in the scene, Start calls it with DefaultSettings.</para>
/// <para>AddToTeam, GetSprite, GetAllyFireteams and the team membership checks run in O(1) per entity.</para>
/// </remarks>
public class TeamsManager : MonoSingleton<TeamsManager>
{
    /// <summary>Layers where an entity blocks a spawn position.</summary>
    public LayerMask NotSpawnable;

    /// <summary>Layers of the ground for the spawn raycasts.</summary>
    public LayerMask Ground;

    /// <summary>Minimum distance between a spawn position and a GridEntity.</summary>
    [field: SerializeField] public float SeparationRadiusBetweenUnits { get; private set; }

    /// <summary>Entities of each team, in the order they were added.</summary>
    public Dictionary<MilitaryTeam, List<Entity>> _teams = new Dictionary<MilitaryTeam, List<Entity>>();

    /// <summary>Icon of the TeamIndicator for each entity type.</summary>
    [Header("Team Indicator")]
    [SerializeField, SerializedDictionary("Type", "Sprite")]
    public SerializedDictionary<SerializableType, Sprite> sprites = new SerializedDictionary<SerializableType, Sprite>()
    {
        {new SerializableType(typeof(Plane)), default },
        {new SerializableType(typeof(Sniper)), default },
        {new SerializableType(typeof(Turret)), default },
        {new SerializableType(typeof(IMilitary)), default }
    };

    [SerializeField] private bool _canDebug;
    [SerializeField] private MobileInfantry _assaultPrefab, _medicPrefab;
    [SerializeField] private Plane _planePrefab;
    [SerializeField] private Civilian _civilianPrefab;

    [SerializeField,SerializedDictionary("Team","Parameters")]
    private SerializedDictionary<MilitaryTeam, TeamParameters> _matchParameters = new SerializedDictionary<MilitaryTeam, TeamParameters>();

    [SerializeField] private TeamIndicator _prefabTeamIndicator;
    [SerializeField] private float _unitsAboveMilitary;

    [Header("Civilian Spawn")]
    [SerializeField] private Transform _civilianPos;
    [SerializeField] private int _civiliansQuantity;
    [SerializeField] private float _width_CivilianSpawn, _height_CivilianSpawn;

    // Same entities as _teams, for O(1) membership checks.
    private readonly Dictionary<MilitaryTeam, HashSet<Entity>> _teamSets = new Dictionary<MilitaryTeam, HashSet<Entity>>();

    // Fireteams of each team with at least one AssaultInfantry in _teams.
    private readonly Dictionary<MilitaryTeam, List<Fireteam>> _fireteams = new Dictionary<MilitaryTeam, List<Fireteam>>();
    private readonly HashSet<Fireteam> _registeredFireteams = new HashSet<Fireteam>();

    // Planes of each team in _teams.
    private readonly Dictionary<MilitaryTeam, List<Plane>> _planes = new Dictionary<MilitaryTeam, List<Plane>>();

    // sprites indexed by System.Type. Built on the first GetSprite call.
    private Dictionary<Type, Sprite> _spritesByType;

    /// <summary>Frames between two consecutive spawns.</summary>
    public const int WaitFramesBeforeContinuing = 144;
    private const int MaxGroundSpawnAttempts = 200;
    private const int MaxAirSpawnAttempts = 1000;

    /// <summary>true after StartMatch.</summary>
    public bool MatchStarted { get; private set; }

    /// <summary>Raised in LateUpdate.</summary>
    public event Action OnLateUpdate;

    /// <summary>Raised by StartMatch with the settings of the match.</summary>
    public event Action<MatchSettings> OnMatchStarted;

    #region MemberAdd

    /// <summary>
    /// Adds the entity to the team and creates its TeamIndicator.
    /// The icon defaults to the sprite of IMilitary.
    /// </summary>
    public void AddToTeam(MilitaryTeam key, Entity value, Sprite icon = default)
    {
        if (icon == null)
            icon = GetSprite(typeof(IMilitary));

        RegisterEntity(key, value, icon);
    }

    /// <summary>
    /// Adds each entity to the team and creates its TeamIndicator.
    /// The icon defaults to the sprite of IMilitary.
    /// </summary>
    /// <remarks>O(m) for m entities.</remarks>
    public void AddToTeam(MilitaryTeam key, IEnumerable<Entity> values, Sprite icon = default)
    {
        if (icon == null)
            icon = GetSprite(typeof(IMilitary));

        foreach (var value in values)
            RegisterEntity(key, value, icon);
    }

    /// <summary>Removes the entity from the team. Its Fireteam stays in GetAllyFireteams.</summary>
    public void RemoveFromTeam(MilitaryTeam key, Entity value)
    {
        if (!_teamSets[key].Remove(value))
            return;

        _teams[key].Remove(value);

        if (value is Plane plane)
            _planes[key].Remove(plane);
    }

    /// <summary>Removes each entity from the team with RemoveFromTeam.</summary>
    public void RemoveFromTeam(MilitaryTeam key, IEnumerable<Entity> values)
    {
        foreach (var value in values)
            RemoveFromTeam(key, value);
    }

    private void RegisterEntity(MilitaryTeam key, Entity value, Sprite icon)
    {
        if (!_teamSets[key].Add(value))
            return;

        _teams[key].Add(value);

        if (value is Plane plane)
            _planes[key].Add(plane);

        if (value is AssaultInfantry assault && assault.Fireteam != null && _registeredFireteams.Add(assault.Fireteam))
            _fireteams[key].Add(assault.Fireteam);

        CreateTeamIndicator(value, icon);
    }

    private void CreateTeamIndicator(Entity value, Sprite icon)
    {
        var indicator = Instantiate(_prefabTeamIndicator, value.transform.position + Vector3.up * _unitsAboveMilitary, Quaternion.identity);
        indicator.transform.SetParent(value.transform);

        // The indicator shows the part of the name after the last "-".
        string[] nameParts = value.gameObject.name.Split('-');
        indicator.SetName(nameParts[nameParts.Length - 1]);
        indicator.AssignOwner(value as IMilitary, icon);
    }

    #endregion

    #region UnityCalls

    protected override void SingletonAwake()
    {
        foreach (MilitaryTeam key in Enum.GetValues(typeof(MilitaryTeam)))
        {
            if (key == MilitaryTeam.None) continue;

            _teams.Add(key, new List<Entity>());
            _teamSets.Add(key, new HashSet<Entity>());
            _fireteams.Add(key, new List<Fireteam>());
            _planes.Add(key, new List<Plane>());
        }
    }

    private void Start()
    {
        // Without a setup panel in the scene the match starts right away with the inspector values.
        if (FindObjectOfType<MatchSetupPanel>(true) == null)
            StartMatch(DefaultSettings());
    }

    private void LateUpdate()
    {
        OnLateUpdate?.Invoke();
    }

    private void OnValidate()
    {
        if (_matchParameters.ContainsKey(MilitaryTeam.None))
            _matchParameters.Remove(MilitaryTeam.None);

        _spritesByType = null;
    }

    #endregion

    #region Match

    /// <summary>Returns the settings in the inspector of TeamsManager and GameManager.</summary>
    public MatchSettings DefaultSettings()
    {
        var settings = new MatchSettings
        {
            Civilians = _civiliansQuantity,
            TimeScale = GameManager.instance != null ? GameManager.instance.timeScale : 1f
        };

        foreach (MilitaryTeam team in _teams.Keys)
        {
            TeamMatchSettings teamSettings = settings.ForTeam(team);
            if (teamSettings == null || !_matchParameters.TryGetValue(team, out TeamParameters parameters))
                continue;

            teamSettings.Fireteams = parameters.FireteamQuantity;
            teamSettings.MembersPerFireteam = parameters.membersPerFireteam;
            teamSettings.Planes = parameters.planesQuantity;
        }

        return settings;
    }

    /// <summary>
    /// Spawns the fireteams, planes and civilians of settings and applies its TimeScale.
    /// Raises OnMatchStarted. A second call does nothing.
    /// </summary>
    public void StartMatch(MatchSettings settings)
    {
        if (MatchStarted)
            return;

        MatchStarted = true;
        Time.timeScale = settings.TimeScale;

        foreach (MilitaryTeam team in _teams.Keys)
        {
            TeamMatchSettings teamSettings = settings.ForTeam(team);
            if (teamSettings == null || !_matchParameters.TryGetValue(team, out TeamParameters parameters))
                continue;

            // The spawn area comes from the inspector. The quantities come from settings.
            parameters.FireteamQuantity = teamSettings.Fireteams;
            parameters.membersPerFireteam = teamSettings.MembersPerFireteam;
            parameters.planesQuantity = teamSettings.Planes;

            StartCoroutine(SpawnFireteams(team, parameters));
            StartCoroutine(SpawnPlanes(team, parameters));
        }

        StartCoroutine(SpawnCivilians(settings.Civilians));
        OnMatchStarted?.Invoke(settings);
    }

    #endregion

    #region SpawnMethods

    private IEnumerator SpawnFireteams(MilitaryTeam team, TeamParameters param)
    {
        Transform group = CreateGroup(team + " Infantry", transform);

        for (int i = 0; i < param.FireteamQuantity; i++)
        {
            FList<MobileInfantry> members = new FList<MobileInfantry>();

            Transform fireteamGroup = CreateGroup("Fireteam" + ColomboMethods.GenerateName(5), group);
            bool medicSpawned = false;

            for (int j = 0; j < param.membersPerFireteam; j++)
            {
                if (!TryGetRandomFreePosOnGround(param.SpawnArea.position, param.width, param.height, out Vector3 pos))
                    break;

                MobileInfantry newUnit;

                if (!medicSpawned && Random.Range(0, 4) == 0)
                {
                    newUnit = Instantiate(_medicPrefab, pos, Quaternion.identity);
                    medicSpawned = true;
                }
                else
                    newUnit = Instantiate(_assaultPrefab, pos, Quaternion.identity);

                newUnit.transform.parent = fireteamGroup;

                //IA2-LINQ
                members += newUnit;
            }

            if (!members.Any())
                yield break;

            //IA2-LINQ
            var nameAndSurname = members.Zip(members.Count().GenerateNames(6), (x, y) => Tuple.Create(x, x.name + " " + y));

            foreach (var zip in nameAndSurname) zip.Item1.gameObject.name = zip.Item2;

            Fireteam.Create(team, members.ToList());

            for (int k = 0; k < WaitFramesBeforeContinuing; k++) yield return null;

            AddToTeam(team, members);
        }
    }

    private IEnumerator SpawnPlanes(MilitaryTeam team, TeamParameters parameters)
    {
        Transform group = CreateGroup(team + " Planes", transform);

        for (int i = 0; i < parameters.planesQuantity; i++)
        {
            if (!TryGetRandomFreePosOnAir(parameters, out Vector3 pos))
                break;

            var plane = Instantiate(_planePrefab, pos, Quaternion.identity);
            plane.Initialize(team);

            AddToTeam(team, plane, GetSprite(typeof(Plane)));
            plane.transform.parent = group;

            for (int j = 0; j < WaitFramesBeforeContinuing; j++) yield return null;
        }
    }

    private IEnumerator SpawnCivilians(int quantity)
    {
        Transform group = CreateGroup("Civilians", transform);

        for (int i = 0; i < quantity; i++)
        {
            if (TryGetRandomFreePosOnGround(_civilianPos.position, _width_CivilianSpawn, _height_CivilianSpawn, out var pos))
            {
                var newCivilian = Instantiate(_civilianPrefab, pos, Quaternion.identity);
                newCivilian.transform.parent = group;

                for (int j = 0; j < WaitFramesBeforeContinuing; j++) yield return null;
            }
            else
            {
                yield return null;
            }
        }
    }

    private static Transform CreateGroup(string name, Transform parent)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    #endregion

    #region GetRandomPos

    // Random position in the air of the spawn area with no GridEntity within SeparationRadiusBetweenUnits.
    private bool TryGetRandomFreePosOnAir(TeamParameters parameters, out Vector3 pos)
    {
        for (int attempt = 0; attempt < MaxAirSpawnAttempts; attempt++)
        {
            Vector3 randomPos = RandomPointInArea(parameters.SpawnArea.position, parameters.width, parameters.height);

            if (!IsGridEntityNearby(randomPos))
            {
                pos = randomPos;
                return true;
            }
        }

        pos = Vector3.zero;
        Debug.LogError("STACK OVERFLOW AL BUSCAR POSICIONES RANDOMS EN EL AIRE");
        return false;
    }

    // Random point on the ground of the area with no GridEntity within SeparationRadiusBetweenUnits.
    private bool TryGetRandomFreePosOnGround(Vector3 center, float width, float height, out Vector3 pos)
    {
        for (int attempt = 0; attempt < MaxGroundSpawnAttempts; attempt++)
        {
            Vector3 randomPos = RandomPointInArea(center, width, height);

            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, float.MaxValue, Ground) && !IsGridEntityNearby(hit.point))
            {
                pos = hit.point;
                return true;
            }
        }

        pos = Vector3.zero;
        Debug.LogError("WATCHDOG AL LIMITE, CORTO EJECUCION");
        return false;
    }

    private static Vector3 RandomPointInArea(Vector3 center, float width, float height)
    {
        return center + new Vector3(Random.Range(-width, width), 0, Random.Range(-height, height));
    }

    private bool IsGridEntityNearby(Vector3 position)
    {
        foreach (Collider collider in Physics.OverlapSphere(position, SeparationRadiusBetweenUnits, NotSpawnable))
        {
            if (collider.TryGetComponent(out GridEntity _))
                return true;
        }

        return false;
    }

    #endregion

    #region UsefulMethods

    /// <summary>
    /// Returns the sprite for targetType in sprites.
    /// Returns the sprite of IMilitary for a type outside sprites.
    /// </summary>
    public Sprite GetSprite(Type targetType)
    {
        if (_spritesByType == null)
            BuildSpriteCache();

        if (_spritesByType.TryGetValue(targetType, out Sprite sprite))
            return sprite;

        _spritesByType.TryGetValue(typeof(IMilitary), out sprite);
        return sprite;
    }

    private void BuildSpriteCache()
    {
        _spritesByType = new Dictionary<Type, Sprite>();

        foreach (var pair in sprites)
        {
            if (pair.Key?.type != null && !_spritesByType.ContainsKey(pair.Key.type))
                _spritesByType.Add(pair.Key.type, pair.Value);
        }
    }

    /// <summary>Returns the fireteams of the team with at least one AssaultInfantry added with AddToTeam.</summary>
    public IReadOnlyList<Fireteam> GetAllyFireteams(MilitaryTeam team)
    {
        return _fireteams[team];
    }

    /// <summary>Returns the planes of the team whose actualState is different from ABANDONED.</summary>
    public IEnumerable<Plane> GetTeamPlanes(MilitaryTeam team)
    {
        foreach (Plane plane in _planes[team])
        {
            if (plane != null && plane.actualState != PlaneStates.ABANDONED)
                yield return plane;
        }
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (Application.isPlaying || !_canDebug) return;

        DrawCivilianSpawn(); DrawTeamSpawn();
    }

    private void DrawTeamSpawn()
    {
        foreach (var item in _matchParameters)
        {
            if (item.Value.SpawnArea == null) continue;

            Gizmos.color = item.Key == MilitaryTeam.Red ? Color.red : Color.blue;
            DrawSpawnArea(item.Value.SpawnArea.position, item.Value.width, item.Value.height);
        }
    }

    private void DrawCivilianSpawn()
    {
        if (_civilianPos == null) return;

        Gizmos.color = Color.white;
        DrawSpawnArea(_civilianPos.position, _width_CivilianSpawn, _height_CivilianSpawn);
    }

    // Draws the rectangle of the area and a sample free spawn position.
    private void DrawSpawnArea(Vector3 center, float width, float height)
    {
        Gizmos.DrawLine(center + new Vector3(-width, 0, height), center + new Vector3(width, 0, height));
        Gizmos.DrawLine(center + new Vector3(width, 0, -height), center + new Vector3(width, 0, height));
        Gizmos.DrawLine(center + new Vector3(width, 0, -height), center + new Vector3(-width, 0, -height));
        Gizmos.DrawLine(center + new Vector3(-width, 0, height), center + new Vector3(-width, 0, -height));

        if (TryGetRandomFreePosOnGround(center, width, height, out Vector3 freePos))
        {
            Gizmos.DrawWireSphere(freePos, SeparationRadiusBetweenUnits);
            Gizmos.DrawLine(freePos, freePos + Vector3.up * 50);
        }
    }

    #endregion
}
