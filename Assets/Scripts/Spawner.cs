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
        [Tooltip("Maximum number of this enemy type allowed at once")]
        public int maxCount = 5;
        [HideInInspector] public int weightRangeStart;
        [HideInInspector] public List<GameObject> activeInstances = new List<GameObject>();
    }

    [System.Serializable]
    public class BossType
    {
        public GameObject prefab;
        [Tooltip("Time in seconds when this boss should spawn")]
        public float spawnTime = 150f; // 2:30 minutes = 150 seconds
        [Tooltip("Number of bosses to spawn at this time")]
        public int spawnCount = 1;
        [Tooltip("Minimum height to spawn this boss")]
        public float minSpawnHeight = -10f;
        [Tooltip("Maximum height to spawn this boss")]
        public float maxSpawnHeight = 10f;
        [HideInInspector] public bool hasSpawned = false;
    }

    [Header("SPAWN POINTS")]
    [Tooltip("Parent object containing all spawn points")]
    public Transform spawnPointsParent;
    [Tooltip("Cylinder reference for spawning enemies on surface")]
    public Transform cylinderTransform;

    [Header("ENEMY SETTINGS")]
    [Tooltip("List of enemy types and their spawn rules")]
    public List<EnemyType> enemyTypes = new List<EnemyType>();

    [Header("BOSS SETTINGS")]
    [Tooltip("List of boss enemies to spawn at specific times")]
    public List<BossType> bossTypes = new List<BossType>();
    [Tooltip("Whether to activate special effects when a boss spawns")]
    public bool enableBossEffects = true;
    [Tooltip("Audio to play when boss spawns")]
    public AudioClip bossSpawnSound;
    [Tooltip("Volume of boss spawn sound")]
    [Range(0f, 1f)] public float bossSpawnVolume = 0.8f;

    [Header("SPAWN SETTINGS")]
    [Tooltip("Time between spawn attempts")]
    public float spawnCooldown = 2f;
    [Tooltip("Tag used to find the player")]
    public string playerTag = "Player";
    [Tooltip("Draw spawn point gizmos in Scene view")]
    public bool debugDrawGizmos = true;
    [Tooltip("Enable debug logs for spawn heights")]
    public bool debugLogSpawnHeights = false;
    [Tooltip("Enable debug logs for enemy counts")]
    public bool debugLogEnemyCounts = true;
    [Tooltip("Enable debug logs for boss spawns")]
    public bool debugLogBossSpawns = true;

    private List<Transform> allSpawnPoints = new List<Transform>();
    private Transform playerTransform;
    private int totalSpawnWeight;
    private float cylinderRadius;
    private float gameTimer = 0f;
    private AudioSource audioSource;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        FindPlayer();
        FindCylinder();
        ValidateEnemyTypes();
        ValidateBossTypes();
        CacheSpawnPoints();
        ClearEnemyInstanceTracking();
        SetupAudio();

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
            StartCoroutine(BossSpawnRoutine());
        }
        else
        {
            Debug.LogError("Spawner initialization failed - check errors above");
            enabled = false;
        }
    }

    void SetupAudio()
    {
        // Setup audio source if boss effects are enabled
        if (enableBossEffects)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // 2D sound
                audioSource.playOnAwake = false;
            }
        }
    }

    void Update()
    {
        // Update game timer
        gameTimer += Time.deltaTime;
    }

    void ClearEnemyInstanceTracking()
    {
        // Clear any stale references
        foreach (var enemyType in enemyTypes)
        {
            enemyType.activeInstances.Clear();
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

    void ValidateBossTypes()
    {
        // Remove null entries
        int removed = bossTypes.RemoveAll(x => x.prefab == null);
        if (removed > 0)
        {
            Debug.LogWarning($"Removed {removed} null boss entries");
        }

        foreach (BossType boss in bossTypes)
        {
            // Validate height ranges
            if (boss.minSpawnHeight >= boss.maxSpawnHeight)
            {
                Debug.LogWarning($"Boss {boss.prefab.name} has invalid height range: min={boss.minSpawnHeight}, max={boss.maxSpawnHeight}. Fixing automatically.");
                boss.maxSpawnHeight = boss.minSpawnHeight + 20f; // Auto-fix
            }
        }

        // Sort bosses by spawn time
        bossTypes = bossTypes.OrderBy(b => b.spawnTime).ToList();
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

            // Clean up any destroyed enemies from tracking lists
            CleanupDestroyedEnemies();

            // Try to find an enemy type that hasn't reached its limit
            List<EnemyType> availableTypes = enemyTypes
                .Where(e => e.activeInstances.Count < e.maxCount)
                .ToList();

            if (availableTypes.Count == 0)
            {
                if (debugLogEnemyCounts)
                {
                    Debug.Log("All enemy types at maximum count. Skipping spawn.");
                }
                continue;
            }

            Transform spawnPoint = GetOptimalSpawnPoint();
            if (spawnPoint == null) continue;

            // Get position on cylinder surface
            Vector3 spawnPos = ProjectOnCylinder(spawnPoint.position);

            // Only consider enemy types that haven't reached their limit
            EnemyType enemyToSpawn = SelectRandomEnemyWithLimit(spawnPos.y, availableTypes);

            if (enemyToSpawn != null)
            {
                // Calculate rotation to face tangent to cylinder
                Vector3 toCenter = cylinderTransform.position - spawnPos;
                toCenter.y = 0;
                Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
                Quaternion spawnRotation = Quaternion.LookRotation(tangent, Vector3.up);

                GameObject spawnedEnemy = Instantiate(enemyToSpawn.prefab, spawnPos, spawnRotation);

                // Add enemy to tracking list
                enemyToSpawn.activeInstances.Add(spawnedEnemy);

                // Add component to handle enemy destruction tracking
                EnemyTracker tracker = spawnedEnemy.AddComponent<EnemyTracker>();
                tracker.Initialize(this, enemyToSpawn);

                if (debugLogSpawnHeights)
                {
                    Debug.Log($"Spawned {enemyToSpawn.prefab.name} at height {spawnPos.y}");
                }

                if (debugLogEnemyCounts)
                {
                    Debug.Log($"{enemyToSpawn.prefab.name} count: {enemyToSpawn.activeInstances.Count}/{enemyToSpawn.maxCount}");
                }
            }
        }
    }

    IEnumerator BossSpawnRoutine()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;

        while (true)
        {
            // Check each boss type that hasn't spawned yet
            foreach (BossType boss in bossTypes.Where(b => !b.hasSpawned))
            {
                // If it's time to spawn this boss
                if (gameTimer >= boss.spawnTime)
                {
                    // Spawn the boss(es)
                    for (int i = 0; i < boss.spawnCount; i++)
                    {
                        SpawnBoss(boss);

                        // Small delay between multiple boss spawns
                        if (i < boss.spawnCount - 1)
                            yield return new WaitForSeconds(1.5f);
                    }

                    boss.hasSpawned = true;

                    if (debugLogBossSpawns)
                    {
                        string minutes = Mathf.Floor(gameTimer / 60).ToString("00");
                        string seconds = Mathf.Floor(gameTimer % 60).ToString("00");
                        Debug.Log($"Boss wave spawned at {minutes}:{seconds} - {boss.spawnCount}x {boss.prefab.name}");
                    }

                    // Play boss spawn sound if enabled
                    if (enableBossEffects && audioSource != null && bossSpawnSound != null)
                    {
                        audioSource.PlayOneShot(bossSpawnSound, bossSpawnVolume);
                    }

                    // Trigger any special effects for boss spawn
                    if (enableBossEffects)
                    {
                        StartCoroutine(BossSpawnEffects());
                    }
                }
            }

            // Check every half second
            yield return new WaitForSeconds(0.5f);
        }
    }

    void SpawnBoss(BossType boss)
    {
        // Get spawn point furthest from player
        Transform spawnPoint = GetOptimalSpawnPoint();
        if (spawnPoint == null) return;

        // Ensure spawn point is within boss height range
        float spawnY = Mathf.Clamp(spawnPoint.position.y, boss.minSpawnHeight, boss.maxSpawnHeight);
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.y = spawnY;

        // Project onto cylinder surface
        Vector3 spawnPos = ProjectOnCylinder(spawnPosition);

        // Calculate rotation to face tangent to cylinder
        Vector3 toCenter = cylinderTransform.position - spawnPos;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        Quaternion spawnRotation = Quaternion.LookRotation(tangent, Vector3.up);

        // Spawn the boss
        GameObject spawnedBoss = Instantiate(boss.prefab, spawnPos, spawnRotation);

        // You might want to add a special component or tag to bosses
        if (!spawnedBoss.CompareTag("Boss"))
        {
            spawnedBoss.tag = "Boss";
        }
    }

    IEnumerator BossSpawnEffects()
    {
        // Example: Screen shake effect
        // You can replace this with whatever effects you want for boss spawns

        float shakeDuration = 1.0f;
        float elapsed = 0f;

        // Find the main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;

        Vector3 originalPos = mainCamera.transform.position;

        while (elapsed < shakeDuration)
        {
            float strength = (1 - (elapsed / shakeDuration)) * 0.2f;
            mainCamera.transform.position = originalPos + Random.insideUnitSphere * strength;

            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = originalPos;
    }

    void CleanupDestroyedEnemies()
    {
        foreach (var enemyType in enemyTypes)
        {
            // Remove null entries (destroyed enemies)
            int removed = enemyType.activeInstances.RemoveAll(e => e == null);
            if (removed > 0 && debugLogEnemyCounts)
            {
                Debug.Log($"Cleaned up {removed} destroyed {enemyType.prefab.name} instances");
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

    EnemyType SelectRandomEnemyWithLimit(float spawnHeight, List<EnemyType> availableTypes)
    {
        // Get enemies valid for this height and under their limit
        List<EnemyType> validEnemies = new List<EnemyType>();
        int validWeightTotal = 0;

        foreach (EnemyType enemy in availableTypes)
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
            foreach (EnemyType enemy in availableTypes)
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

        if (validEnemies.Count == 0)
        {
            return null;
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

        return validEnemies[Random.Range(0, validEnemies.Count)];
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

    // Method to be called by the EnemyTracker component when an enemy is destroyed
    public void OnEnemyDestroyed(GameObject enemy, EnemyType enemyType)
    {
        if (enemyType != null && enemyType.activeInstances.Contains(enemy))
        {
            enemyType.activeInstances.Remove(enemy);

            if (debugLogEnemyCounts)
            {
                Debug.Log($"{enemyType.prefab.name} destroyed. Count: {enemyType.activeInstances.Count}/{enemyType.maxCount}");
            }
        }
    }

    // Helper methods for UI/Debug
    public string GetFormattedGameTime()
    {
        int minutes = Mathf.FloorToInt(gameTimer / 60);
        int seconds = Mathf.FloorToInt(gameTimer % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    public float GetGameTime()
    {
        return gameTimer;
    }
}

// Helper component to track when enemies are destroyed
public class EnemyTracker : MonoBehaviour
{
    private Spawner spawner;
    private Spawner.EnemyType enemyType;

    public void Initialize(Spawner spawnerRef, Spawner.EnemyType type)
    {
        spawner = spawnerRef;
        enemyType = type;
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.OnEnemyDestroyed(gameObject, enemyType);
        }
    }
}