using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates the UnitStats assets from the values in the unit prefabs and assigns each asset to its prefab.
/// An existing asset keeps its values, and a prefab with a stats asset keeps it.
/// </summary>
/// <remarks>
/// Runs from Tools > Unit Stats > Create From Prefabs, or in batch mode with
/// -executeMethod UnitStatsMigration.CreateFromPrefabs.
/// </remarks>
public static class UnitStatsMigration
{
    private const string StatsFolder = "Assets/Data/UnitStats";
    private const string PrefabsFolder = "Assets/Prefabs";

    /// <summary>Creates and assigns the stats assets of the 6 unit types.</summary>
    [MenuItem("Tools/Unit Stats/Create From Prefabs")]
    public static void CreateFromPrefabs()
    {
        EnsureFolder(StatsFolder);

        Migrate<AssaultInfantry, AssaultStats>("AssaultStats", (root, stats) =>
        {
            ReadHealth(root, stats.Health);
            ReadWeapon(root, stats.Weapon);
            ReadMovement(root, stats.Movement);
            ReadVision(root, stats.Vision);

            var unit = new SerializedObject(root.GetComponent<AssaultInfantry>());
            stats.MinDistanceFromDestination = ReadFloat(unit, "_minDistanceFromDestination", stats.MinDistanceFromDestination);
            stats.TimeBeforeSelectingTarget = ReadFloat(unit, "_timeBeforeSelectingTarget", stats.TimeBeforeSelectingTarget);
        });

        Migrate<MedicInfantry, MedicStats>("MedicStats", (root, stats) =>
        {
            ReadHealth(root, stats.Health);
            ReadMovement(root, stats.Movement);
            ReadVision(root, stats.Vision);

            var unit = new SerializedObject(root.GetComponent<MedicInfantry>());
            stats.MinDistanceFromDestination = ReadFloat(unit, "_minDistanceFromDestination", stats.MinDistanceFromDestination);
            stats.HealSpeed = ReadFloat(unit, "_healSpeed", stats.HealSpeed);
            stats.MaxHealDistance = ReadFloat(unit, "_maxHealDistance", stats.MaxHealDistance);
            stats.IdealHealDistance = ReadFloat(unit, "_idealHealDistance", stats.IdealHealDistance);
        });

        Migrate<Sniper, SniperStats>("SniperStats", (root, stats) =>
        {
            ReadHealth(root, stats.Health);
            ReadWeapon(root, stats.Weapon);
            ReadVision(root, stats.Vision);

            var unit = new SerializedObject(root.GetComponent<Sniper>());
            stats.FramesBetweenEnemySearch = ReadInt(unit, "FramesBetweenEnemySearch", stats.FramesBetweenEnemySearch);
            stats.AimSpeed = ReadFloat(unit, "_aimSpeed", stats.AimSpeed);
            stats.AddPerTimesFocused = ReadFloat(unit, "_addPerTimesFocused", stats.AddPerTimesFocused);
            stats.RequiredFocusTime = ReadFloat(unit, "_requiredFocusTime", stats.RequiredFocusTime);
            stats.MaxShootsInRow = ReadFloat(unit, "maxShootsInRow", stats.MaxShootsInRow);
        });

        Migrate<Turret, TurretStats>("TurretStats", (root, stats) =>
        {
            ReadHealth(root, stats.Health);
            ReadWeapon(root, stats.Weapon);
            ReadVision(root, stats.Vision);

            var unit = new SerializedObject(root.GetComponent<Turret>());
            stats.Weapon.BulletCooldown = ReadFloat(unit, "_cd_BetweenShots", stats.Weapon.BulletCooldown);
            stats.RotationSpeed = ReadFloat(unit, "rotationSpeed", stats.RotationSpeed);
            stats.BaseMinAngle = ReadFloat(unit, "_baseMinAngle", stats.BaseMinAngle);
            stats.CanonMinAngle = ReadFloat(unit, "_canonMinAngle", stats.CanonMinAngle);
            stats.SearchCooldown = ReadFloat(unit, "_cd_BetweenSearch", stats.SearchCooldown);
        });

        Migrate<Plane, PlaneStats>("PlaneStats", (root, stats) =>
        {
            ReadHealth(root, stats.Health);
            ReadWeapon(root, stats.Weapon);
            ReadMovement(root, stats.Movement);
            ReadVision(root, stats.Vision);

            var unit = new SerializedObject(root.GetComponent<Plane>());

            // The plane takes its view radius from _sightRadius.
            stats.Vision.ViewRadius = ReadFloat(unit, "_sightRadius", stats.Vision.ViewRadius);
            stats.LoseSightRadius = ReadFloat(unit, "_loseSightRadius", stats.LoseSightRadius);

            stats.AlignmentForce = ReadFloat(unit, "_flockingParameters.AlignmentForce", stats.AlignmentForce);
            stats.SeparationForce = ReadFloat(unit, "_flockingParameters._separationForce", stats.SeparationForce);
            stats.CohesionForce = ReadFloat(unit, "_flockingParameters._cohesionForce", stats.CohesionForce);

            stats.Misile.acceleration = ReadFloat(unit, "misileStats.acceleration", stats.Misile.acceleration);
            stats.Misile.maxSpeed = ReadFloat(unit, "misileStats.maxSpeed", stats.Misile.maxSpeed);
            stats.Misile.timeBeforeExplosion = ReadFloat(unit, "misileStats.timeBeforeExplosion", stats.Misile.timeBeforeExplosion);
            stats.Misile.damage = ReadInt(unit, "misileStats.damage", stats.Misile.damage);
            stats.Misile.explosionRadius = ReadFloat(unit, "misileStats.explosionRadius", stats.Misile.explosionRadius);

            stats.MisileCooldown = ReadFloat(unit, "misileCD", stats.MisileCooldown);
            stats.MinimumDistanceForStrike = ReadFloat(unit, "minimumDistanceForStrike", stats.MinimumDistanceForStrike);
            stats.ExplosionDamageOnDeath = ReadInt(unit, "_ExplosionDamageOnDeath", stats.ExplosionDamageOnDeath);
            stats.UnitsBehindPlane = ReadFloat(unit, "_unitsBehindPlane", stats.UnitsBehindPlane);
            stats.CollisionCheckDistance = ReadFloat(unit, "_collisionCheckDistance", stats.CollisionCheckDistance);
            stats.RandomDirectionCooldown = ReadFloat(unit, "_cd_OnRandomDir", stats.RandomDirectionCooldown);

            // The plane shoots its bursts with its own timing.
            stats.Weapon.BulletsPerBurst = ReadFloat(unit, "bulletsPerBurst", stats.Weapon.BulletsPerBurst);
            stats.Weapon.BulletCooldown = ReadFloat(unit, "BulletCD", stats.Weapon.BulletCooldown);
            stats.Weapon.BurstCooldown = ReadFloat(unit, "burstCD", stats.Weapon.BurstCooldown);
        });

        Migrate<Civilian, CivilianStats>("CivilianStats", (root, stats) =>
        {
            ReadHealth(root, stats.Health);
            ReadMovement(root, stats.Movement);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UnitStatsMigration] Done.");
    }

    // Creates the asset of TStats from the prefab with TUnit, when the asset is missing, and assigns it to the prefab.
    private static void Migrate<TUnit, TStats>(string assetName, Action<GameObject, TStats> fill)
        where TUnit : Component
        where TStats : UnitStats
    {
        string prefabPath = FindPrefabWith<TUnit>();
        if (prefabPath == null)
        {
            Debug.LogError($"[UnitStatsMigration] No prefab with {typeof(TUnit).Name} in {PrefabsFolder}.");
            return;
        }

        string assetPath = $"{StatsFolder}/{assetName}.asset";
        TStats stats = AssetDatabase.LoadAssetAtPath<TStats>(assetPath);
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

        try
        {
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<TStats>();
                fill(root, stats);
                AssetDatabase.CreateAsset(stats, assetPath);
                Debug.Log($"[UnitStatsMigration] Created {assetPath} from {prefabPath}.");
            }
            else
            {
                Debug.Log($"[UnitStatsMigration] {assetPath} already exists, its values are kept.");
            }

            var unit = new SerializedObject(root.GetComponent<TUnit>());
            SerializedProperty statsProperty = unit.FindProperty("_stats");

            if (statsProperty == null)
            {
                Debug.LogError($"[UnitStatsMigration] {typeof(TUnit).Name} has no _stats field.");
                return;
            }

            if (statsProperty.objectReferenceValue != null)
            {
                Debug.Log($"[UnitStatsMigration] {prefabPath} already has a stats asset, it is kept.");
                return;
            }

            statsProperty.objectReferenceValue = stats;
            unit.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[UnitStatsMigration] Assigned {assetPath} to {prefabPath}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // Returns the path of the first prefab in PrefabsFolder whose root has T, or null.
    private static string FindPrefabWith<T>() where T : Component
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.GetComponent<T>() != null)
                return path;
        }

        return null;
    }

    #region Component readers

    private static void ReadHealth(GameObject root, HealthStats stats)
    {
        var life = root.GetComponent<LifeComponent>();
        if (life == null) return;

        var so = new SerializedObject(life);
        stats.MaxLife = ReadInt(so, "_maxLife", stats.MaxLife);
        stats.DamageMultiplier = ReadFloat(so, "_dmgMultiplier", stats.DamageMultiplier);
        stats.DamageResist = ReadInt(so, "dmgResist", stats.DamageResist);
    }

    private static void ReadWeapon(GameObject root, WeaponStats stats)
    {
        var weapon = root.GetComponent<ShootComponent>();
        if (weapon == null) return;

        var so = new SerializedObject(weapon);
        stats.BulletDamage = ReadInt(so, "<BulletDamage>k__BackingField", stats.BulletDamage);
        stats.BulletSpread = ReadFloat(so, "_bulletspread", stats.BulletSpread);
        stats.BulletsPerBurst = ReadFloat(so, "_bulletsPerBurst", stats.BulletsPerBurst);
        stats.BulletCooldown = ReadFloat(so, "_bulletCD", stats.BulletCooldown);
        stats.BurstCooldown = ReadFloat(so, "_burstCD", stats.BurstCooldown);
    }

    private static void ReadMovement(GameObject root, MovementStats stats)
    {
        var physics = root.GetComponent<NewPhysicsMovement>();
        if (physics != null)
        {
            var so = new SerializedObject(physics);
            stats.Acceleration = ReadFloat(so, "_acceleration", stats.Acceleration);
            stats.MaxSpeed = ReadFloat(so, "_maxSpeed", stats.MaxSpeed);
        }

        var ai = root.GetComponent<NewAIMovement>();
        if (ai != null)
            stats.DestinationArriveDistance = ReadFloat(new SerializedObject(ai), "DestinationArriveDistance", stats.DestinationArriveDistance);
    }

    private static void ReadVision(GameObject root, VisionStats stats)
    {
        var fov = root.GetComponent<FOVAgent>();
        if (fov == null) return;

        var so = new SerializedObject(fov);
        stats.ViewRadius = ReadFloat(so, "<ViewRadius>k__BackingField", stats.ViewRadius);
        stats.ViewAngle = ReadFloat(so, "_viewAngle", stats.ViewAngle);
    }

    private static float ReadFloat(SerializedObject so, string path, float fallback)
    {
        SerializedProperty property = so.FindProperty(path);
        if (property == null)
        {
            Debug.LogWarning($"[UnitStatsMigration] {so.targetObject.GetType().Name}.{path} not found, using {fallback}.");
            return fallback;
        }

        return property.propertyType == SerializedPropertyType.Integer ? property.intValue : property.floatValue;
    }

    private static int ReadInt(SerializedObject so, string path, int fallback)
    {
        SerializedProperty property = so.FindProperty(path);
        if (property == null)
        {
            Debug.LogWarning($"[UnitStatsMigration] {so.targetObject.GetType().Name}.{path} not found, using {fallback}.");
            return fallback;
        }

        return property.propertyType == SerializedPropertyType.Float ? Mathf.RoundToInt(property.floatValue) : property.intValue;
    }

    #endregion

    // Creates folder and its parents when they are missing.
    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }
}
