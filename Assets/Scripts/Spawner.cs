using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Spawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemyType
    {
        public GameObject prefab;
        [Tooltip("Relative spawn chance (e.g., 60 = 60% chance)")]
        [Range(1, 100)] public int spawnWeight = 50;
        [Tooltip("Minimum height to spawn this enemy")]
        public float minSpawnHeight = -10f;
        [Tooltip("Maximum height to spawn this enemy")]
        public float maxSpawnHeight = 10f;
        [HideInInspector] public int weightRangeStart;
    }

    [Header("SPAWN POINTS")]
    [Tooltip("Parent object containing all spawn points")]
    public Transform spawnPointsParent;
    [Tooltip("Cylinder reference for spawning enemies on surface")]
    public Transform cylinderTransform;

    [Header("ENEMY SETTINGS")]
    [Tooltip("List of enemy types and their spawn rules")]
    public List<EnemyType> enemyTypes = new List<EnemyType>();

    [Header("SPAWN SETTINGS")]
    [Tooltip("Time between spawn attempts")]
    public float spawnCooldown = 2f;
    [Tooltip("Tag used to find the player")]
    public string playerTag = "Player";
    [Tooltip("Draw spawn point gizmos in Scene view")]
    public bool debugDrawGizmos = true;
    [Tooltip("Enable debug logs for spawn heights")]
    public bool debugLogSpawnHeights = false;

    private List<Transform> allSpawnPoints = new List<Transform>();
    private Transform playerTransform;
    private int totalSpawnWeight;
    private float cylinderRadius;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        FindPlayer();
        FindCylinder();
        ValidateEnemyTypes();
        CacheSpawnPoints();

        if (CheckSetupValidity())
        {
            // Debug log for spawn point heights
            if (debugLogSpawnHeights)
            {
                foreach (Transform point in allSpawnPoints)
                {
                    Debug.Log($"Spawn point '{point.name}': height = {point.position.y}");
                }
            }

            StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.LogError("Spawner initialization failed - check errors above");
            enabled = false;
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogError($"No GameObject found with tag '{playerTag}'");
        }
    }

    void FindCylinder()
    {
        if (cylinderTransform == null)
        {
            cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;
            if (cylinderTransform == null)
            {
                Debug.LogError("No cylinder transform found. Tag a GameObject as 'Level' or assign it directly.");
                return;
            }
        }

        cylinderRadius = cylinderTransform.localScale.x * 0.5f;
    }

    void ValidateEnemyTypes()
    {
        // Remove null entries
        int removed = enemyTypes.RemoveAll(x => x.prefab == null);
        if (removed > 0)
        {
            Debug.LogWarning($"Removed {removed} null enemy entries");
        }

        if (enemyTypes.Count == 0)
        {
            Debug.LogError("No valid enemy types configured!");
            return;
        }

        // Calculate weight ranges for probability distribution
        totalSpawnWeight = 0;
        foreach (EnemyType enemy in enemyTypes)
        {
            enemy.weightRangeStart = totalSpawnWeight;
            totalSpawnWeight += enemy.spawnWeight;

            // Validate height ranges
            if (enemy.minSpawnHeight >= enemy.maxSpawnHeight)
            {
                Debug.LogWarning($"Enemy {enemy.prefab.name} has invalid height range: min={enemy.minSpawnHeight}, max={enemy.maxSpawnHeight}. Fixing automatically.");
                enemy.maxSpawnHeight = enemy.minSpawnHeight + 20f; // Auto-fix
            }
        }
    }

    void CacheSpawnPoints()
    {
        allSpawnPoints.Clear();

        if (spawnPointsParent == null)
        {
            Debug.LogError("SpawnPointsParent not assigned!");
            return;
        }

        // Get all spawn points from hierarchy
        foreach (Transform mainSpawn in spawnPointsParent)
        {
            allSpawnPoints.Add(mainSpawn);
            foreach (Transform child in mainSpawn)
            {
                allSpawnPoints.Add(child);
            }
        }

        if (allSpawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points found in hierarchy!");
        }
    }

    bool CheckSetupValidity()
    {
        return playerTransform != null &&
               cylinderTransform != null &&
               enemyTypes.Count > 0 &&
               allSpawnPoints.Count > 0;
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnCooldown);

            Transform spawnPoint = GetOptimalSpawnPoint();
            if (spawnPoint == null) continue;

            // Get position on cylinder surface
            Vector3 spawnPos = ProjectOnCylinder(spawnPoint.position);

            EnemyType enemyToSpawn = SelectRandomEnemy(spawnPos.y);
            if (enemyToSpawn != null)
            {
                // Calculate rotation to face tangent to cylinder
                Vector3 toCenter = cylinderTransform.position - spawnPos;
                toCenter.y = 0;
                Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
                Quaternion spawnRotation = Quaternion.LookRotation(tangent, Vector3.up);

                GameObject spawnedEnemy = Instantiate(enemyToSpawn.prefab, spawnPos, spawnRotation);

                if (debugLogSpawnHeights)
                {
                    Debug.Log($"Spawned {enemyToSpawn.prefab.name} at height {spawnPos.y}");
                }
            }
        }
    }

    Vector3 ProjectOnCylinder(Vector3 position)
    {
        if (cylinderTransform == null) return position;

        // Find angle on cylinder
        Vector3 toCylinder = position - cylinderTransform.position;
        float angle = Mathf.Atan2(toCylinder.x, toCylinder.z);

        // Project to cylinder surface
        return new Vector3(
            cylinderTransform.position.x + cylinderRadius * Mathf.Sin(angle),
            position.y,
            cylinderTransform.position.z + cylinderRadius * Mathf.Cos(angle)
        );
    }

    Transform GetOptimalSpawnPoint()
    {
        if (allSpawnPoints.Count == 0 || playerTransform == null)
            return null;

        // Find spawn point furthest from player (considering we're on a cylinder)
        return allSpawnPoints
            .OrderByDescending(p => CalculateCylinderDistance(p.position, playerTransform.position))
            .ThenByDescending(p => Mathf.Abs(p.position.y - playerTransform.position.y))
            .FirstOrDefault();
    }

    float CalculateCylinderDistance(Vector3 point1, Vector3 point2)
    {
        if (cylinderTransform == null) return Vector3.Distance(point1, point2);

        // Calculate angles on cylinder
        Vector3 toPoint1 = point1 - cylinderTransform.position;
        Vector3 toPoint2 = point2 - cylinderTransform.position;
        float angle1 = Mathf.Atan2(toPoint1.x, toPoint1.z);
        float angle2 = Mathf.Atan2(toPoint2.x, toPoint2.z);

        // Calculate angular distance (shortest way around the cylinder)
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle1 * Mathf.Rad2Deg, angle2 * Mathf.Rad2Deg) * Mathf.Deg2Rad);

        // Convert to arc length
        float arcDistance = angleDiff * cylinderRadius;

        // Also consider the y-distance
        float yDistance = Mathf.Abs(point1.y - point2.y);

        // Combine distances (Pythagoras)
        return Mathf.Sqrt(arcDistance * arcDistance + yDistance * yDistance);
    }

    EnemyType SelectRandomEnemy(float spawnHeight)
    {
        // Get enemies valid for this height
        List<EnemyType> validEnemies = new List<EnemyType>();
        int validWeightTotal = 0;

        foreach (EnemyType enemy in enemyTypes)
        {
            if (spawnHeight >= enemy.minSpawnHeight &&
                spawnHeight <= enemy.maxSpawnHeight)
            {
                validEnemies.Add(enemy);
                validWeightTotal += enemy.spawnWeight;
            }
        }

        if (validEnemies.Count == 0)
        {
            if (debugLogSpawnHeights)
            {
                Debug.LogWarning($"No valid enemies for height {spawnHeight}. Adjusting height restrictions to include this spawn point.");
            }

            // Automatically adjust the height range of enemies to include this spawn point
            float margin = 0.1f; // Small buffer
            foreach (EnemyType enemy in enemyTypes)
            {
                if (spawnHeight < enemy.minSpawnHeight)
                {
                    enemy.minSpawnHeight = spawnHeight - margin;
                }
                else if (spawnHeight > enemy.maxSpawnHeight)
                {
                    enemy.maxSpawnHeight = spawnHeight + margin;
                }
                validEnemies.Add(enemy);
                validWeightTotal += enemy.spawnWeight;
            }
        }

        // Weighted random selection
        int randomWeight = Random.Range(0, validWeightTotal);
        int accumulatedWeight = 0;

        foreach (EnemyType enemy in validEnemies)
        {
            accumulatedWeight += enemy.spawnWeight;
            if (randomWeight < accumulatedWeight)
            {
                return enemy;
            }
        }

        return validEnemies.Count > 0 ? validEnemies[Random.Range(0, validEnemies.Count)] : enemyTypes[0];
    }

    void OnDrawGizmos()
    {
        if (!debugDrawGizmos || spawnPointsParent == null) return;

        Gizmos.color = Color.green;
        foreach (Transform spawn in spawnPointsParent)
        {
            // If cylinder exists, project spawn point onto it
            Vector3 spawnPos = cylinderTransform != null ? ProjectOnCylinder(spawn.position) : spawn.position;
            Gizmos.DrawWireSphere(spawnPos, 0.3f);

            foreach (Transform point in spawn)
            {
                Vector3 childPos = cylinderTransform != null ? ProjectOnCylinder(point.position) : point.position;
                Gizmos.DrawLine(spawnPos, childPos);
                Gizmos.DrawWireSphere(childPos, 0.2f);
            }
        }
    }
}