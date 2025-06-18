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

    [System.Serializable]
    public class PowerUpType
    {
        [Header("Basic Settings")]
        public GameObject prefab;
        [Tooltip("Name of the power-up for identification")]
        public string powerUpName = "Power-Up";

        [Header("Spawn Settings")]
        [Tooltip("Relative spawn chance compared to other power-ups")]
        [Range(1, 100)] public int spawnWeight = 25;
        [Tooltip("Time between spawns of this power-up type (seconds)")]
        public float spawnInterval = 45f;
        [Tooltip("Maximum number of this power-up type allowed at once")]
        public int maxCount = 2;
        [Tooltip("Minimum height to spawn this power-up")]
        public float minSpawnHeight = -10f;
        [Tooltip("Maximum height to spawn this power-up")]
        public float maxSpawnHeight = 10f;

        [Header("Audio")]
        [Tooltip("Sound to play when this power-up spawns")]
        public AudioClip spawnSFX;
        [Tooltip("Sound to play when this power-up is collected")]
        public AudioClip collectSFX;
        [Tooltip("Volume of spawn sound")]
        [Range(0f, 1f)] public float spawnVolume = 0.6f;

        [HideInInspector] public List<GameObject> activeInstances = new List<GameObject>();
        [HideInInspector] public float lastSpawnTime = 0f;
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

    [Header("HEALTH ICON SETTINGS")]
    [Tooltip("Health icon prefab to spawn")]
    public GameObject healthIconPrefab;
    [Tooltip("Time between health icon spawns (seconds)")]
    public float healthIconSpawnInterval = 30f;
    [Tooltip("Maximum number of health icons allowed at once")]
    public int maxHealthIcons = 3;
    [Tooltip("Minimum height to spawn health icons")]
    public float healthIconMinHeight = -10f;
    [Tooltip("Maximum height to spawn health icons")]
    public float healthIconMaxHeight = 10f;
    [Tooltip("Enable debug logs for health icon spawns")]
    public bool debugLogHealthIcons = true;

    [Header("POWER-UP SETTINGS")]
    [Tooltip("List of power-up types and their spawn rules")]
    public List<PowerUpType> powerUpTypes = new List<PowerUpType>();
    [Tooltip("Global power-up spawn rate multiplier")]
    [Range(0.1f, 3f)] public float powerUpSpawnRateMultiplier = 1f;
    [Tooltip("Enable debug logs for power-up spawns")]
    public bool debugLogPowerUps = true;

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
    private List<GameObject> activeHealthIcons = new List<GameObject>();
    private float lastHealthIconSpawnTime = 0f;

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
        ValidateHealthIconSettings();
        ValidatePowerUpSettings();
        CacheSpawnPoints();
        ClearInstanceTracking();
        SetupAudio();

        if (CheckSetupValidity())
        {
            if (debugLogSpawnHeights)
            {
                foreach (Transform point in allSpawnPoints)
                {
                    Debug.Log($"Spawn point '{point.name}': height = {point.position.y}");
                }
            }

            StartCoroutine(SpawnRoutine());
            StartCoroutine(BossSpawnRoutine());
            StartCoroutine(HealthIconSpawnRoutine());
            StartCoroutine(PowerUpSpawnRoutine());
        }
        else
        {
            Debug.LogError("Spawner initialization failed - check errors above");
            enabled = false;
        }
    }

    void ValidateHealthIconSettings()
    {
        if (healthIconPrefab == null)
        {
            Debug.LogWarning("Health icon prefab not assigned. Health icon spawning will be disabled.");
            return;
        }

        if (healthIconMinHeight >= healthIconMaxHeight)
        {
            Debug.LogWarning($"Health icon has invalid height range: min={healthIconMinHeight}, max={healthIconMaxHeight}. Fixing automatically.");
            healthIconMaxHeight = healthIconMinHeight + 20f;
        }

        HealthIconTracker tracker = healthIconPrefab.GetComponent<HealthIconTracker>();
        if (tracker == null)
        {
            Debug.LogWarning("Health icon prefab doesn't have HealthIconTracker component. Make sure to add it to track destruction.");
        }
    }

    void ValidatePowerUpSettings()
    {
        int removed = powerUpTypes.RemoveAll(x => x.prefab == null);
        if (removed > 0)
        {
            Debug.LogWarning($"Removed {removed} null power-up entries");
        }

        foreach (PowerUpType powerUp in powerUpTypes)
        {
            if (powerUp.minSpawnHeight >= powerUp.maxSpawnHeight)
            {
                Debug.LogWarning($"Power-up {powerUp.powerUpName} has invalid height range: min={powerUp.minSpawnHeight}, max={powerUp.maxSpawnHeight}. Fixing automatically.");
                powerUp.maxSpawnHeight = powerUp.minSpawnHeight + 20f;
            }

            if (powerUp.spawnInterval < 5f)
            {
                Debug.LogWarning($"Power-up {powerUp.powerUpName} has very short spawn interval ({powerUp.spawnInterval}s). Consider increasing it.");
            }
        }

        if (debugLogPowerUps && powerUpTypes.Count > 0)
        {
            Debug.Log($"Initialized {powerUpTypes.Count} power-up types");
        }
    }

    void SetupAudio()
    {
        if (enableBossEffects || powerUpTypes.Count > 0)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f;
                audioSource.playOnAwake = false;
            }
        }
    }

    void Update()
    {
        gameTimer += Time.deltaTime;
    }

    void ClearInstanceTracking()
    {
        foreach (var enemyType in enemyTypes)
        {
            enemyType.activeInstances.Clear();
        }
        activeHealthIcons.Clear();

        foreach (var powerUp in powerUpTypes)
        {
            powerUp.activeInstances.Clear();
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

        totalSpawnWeight = 0;
        foreach (EnemyType enemy in enemyTypes)
        {
            enemy.weightRangeStart = totalSpawnWeight;
            totalSpawnWeight += enemy.spawnWeight;

            if (enemy.minSpawnHeight >= enemy.maxSpawnHeight)
            {
                Debug.LogWarning($"Enemy {enemy.prefab.name} has invalid height range: min={enemy.minSpawnHeight}, max={enemy.maxSpawnHeight}. Fixing automatically.");
                enemy.maxSpawnHeight = enemy.minSpawnHeight + 20f;
            }
        }
    }

    void ValidateBossTypes()
    {
        int removed = bossTypes.RemoveAll(x => x.prefab == null);
        if (removed > 0)
        {
            Debug.LogWarning($"Removed {removed} null boss entries");
        }

        foreach (BossType boss in bossTypes)
        {
            if (boss.minSpawnHeight >= boss.maxSpawnHeight)
            {
                Debug.LogWarning($"Boss {boss.prefab.name} has invalid height range: min={boss.minSpawnHeight}, max={boss.maxSpawnHeight}. Fixing automatically.");
                boss.maxSpawnHeight = boss.minSpawnHeight + 20f;
            }
        }

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

            CleanupDestroyedEnemies();

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

            Vector3 spawnPos = ProjectOnCylinder(spawnPoint.position);
            EnemyType enemyToSpawn = SelectRandomEnemyWithLimit(spawnPos.y, availableTypes);

            if (enemyToSpawn != null)
            {
                Vector3 toCenter = cylinderTransform.position - spawnPos;
                toCenter.y = 0;
                Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
                Quaternion spawnRotation = Quaternion.LookRotation(tangent, Vector3.up);

                GameObject spawnedEnemy = Instantiate(enemyToSpawn.prefab, spawnPos, spawnRotation);
                enemyToSpawn.activeInstances.Add(spawnedEnemy);

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

    IEnumerator HealthIconSpawnRoutine()
    {
        yield return new WaitForSeconds(healthIconSpawnInterval);

        while (true)
        {
            if (healthIconPrefab != null && CanSpawnHealthIcon())
            {
                SpawnHealthIcon();
            }

            yield return new WaitForSeconds(healthIconSpawnInterval);
        }
    }

    IEnumerator PowerUpSpawnRoutine()
    {
        yield return new WaitForSeconds(15f);

        while (true)
        {
            foreach (PowerUpType powerUp in powerUpTypes)
            {
                if (CanSpawnPowerUp(powerUp))
                {
                    SpawnPowerUp(powerUp);
                }
            }

            yield return new WaitForSeconds(2f);
        }
    }

    bool CanSpawnHealthIcon()
    {
        CleanupDestroyedHealthIcons();
        return activeHealthIcons.Count < maxHealthIcons;
    }

    bool CanSpawnPowerUp(PowerUpType powerUp)
    {
        CleanupDestroyedPowerUps(powerUp);
        if (powerUp.activeInstances.Count >= powerUp.maxCount)
            return false;

        float adjustedInterval = powerUp.spawnInterval / powerUpSpawnRateMultiplier;
        return (gameTimer - powerUp.lastSpawnTime) >= adjustedInterval;
    }

    void SpawnHealthIcon()
    {
        Transform spawnPoint = GetRandomSpawnPoint();
        if (spawnPoint == null) return;

        float spawnY = Mathf.Clamp(spawnPoint.position.y, healthIconMinHeight, healthIconMaxHeight);
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.y = spawnY;

        Vector3 spawnPos = ProjectOnCylinder(spawnPosition);
        Quaternion spawnRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        GameObject spawnedHealthIcon = Instantiate(healthIconPrefab, spawnPos, spawnRotation);

        activeHealthIcons.Add(spawnedHealthIcon);

        HealthIconTracker tracker = spawnedHealthIcon.GetComponent<HealthIconTracker>();
        if (tracker == null)
        {
            tracker = spawnedHealthIcon.AddComponent<HealthIconTracker>();
        }
        tracker.Initialize(this);

        lastHealthIconSpawnTime = gameTimer;

        if (debugLogHealthIcons)
        {
            Debug.Log($"Health icon spawned at height {spawnPos.y}. Active count: {activeHealthIcons.Count}/{maxHealthIcons}");
        }
    }

    void SpawnPowerUp(PowerUpType powerUp)
    {
        Transform spawnPoint = GetOptimalSpawnPoint();
        if (spawnPoint == null) return;

        float spawnY = Mathf.Clamp(spawnPoint.position.y, powerUp.minSpawnHeight, powerUp.maxSpawnHeight);
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.y = spawnY;

        Vector3 spawnPos = ProjectOnCylinder(spawnPosition);
        Quaternion spawnRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        GameObject spawnedPowerUp = Instantiate(powerUp.prefab, spawnPos, spawnRotation);

        powerUp.activeInstances.Add(spawnedPowerUp);

        PowerUpTracker tracker = spawnedPowerUp.GetComponent<PowerUpTracker>();
        if (tracker == null)
        {
            tracker = spawnedPowerUp.AddComponent<PowerUpTracker>();
        }
        tracker.Initialize(this, powerUp);

        powerUp.lastSpawnTime = gameTimer;

        if (audioSource != null && powerUp.spawnSFX != null)
        {
            audioSource.PlayOneShot(powerUp.spawnSFX, powerUp.spawnVolume);
        }

        if (debugLogPowerUps)
        {
            Debug.Log($"Power-up '{powerUp.powerUpName}' spawned at height {spawnPos.y}. Active count: {powerUp.activeInstances.Count}/{powerUp.maxCount}");
        }
    }

    Transform GetRandomSpawnPoint()
    {
        if (allSpawnPoints.Count == 0) return null;
        return allSpawnPoints[Random.Range(0, allSpawnPoints.Count)];
    }

    void CleanupDestroyedHealthIcons()
    {
        int removed = activeHealthIcons.RemoveAll(h => h == null);
        if (removed > 0 && debugLogHealthIcons)
        {
            Debug.Log($"Cleaned up {removed} destroyed health icons");
        }
    }

    void CleanupDestroyedPowerUps(PowerUpType powerUp)
    {
        int removed = powerUp.activeInstances.RemoveAll(p => p == null);
        if (removed > 0 && debugLogPowerUps)
        {
            Debug.Log($"Cleaned up {removed} destroyed {powerUp.powerUpName} power-ups");
        }
    }

    IEnumerator BossSpawnRoutine()
    {
        yield return null;

        while (true)
        {
            foreach (BossType boss in bossTypes.Where(b => !b.hasSpawned))
            {
                if (gameTimer >= boss.spawnTime)
                {
                    for (int i = 0; i < boss.spawnCount; i++)
                    {
                        SpawnBoss(boss);

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

                    if (enableBossEffects && audioSource != null && bossSpawnSound != null)
                    {
                        audioSource.PlayOneShot(bossSpawnSound, bossSpawnVolume);
                    }

                    if (enableBossEffects)
                    {
                        StartCoroutine(BossSpawnEffects());
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    void SpawnBoss(BossType boss)
    {
        Transform spawnPoint = GetOptimalSpawnPoint();
        if (spawnPoint == null) return;

        float spawnY = Mathf.Clamp(spawnPoint.position.y, boss.minSpawnHeight, boss.maxSpawnHeight);
        Vector3 spawnPosition = spawnPoint.position;
        spawnPosition.y = spawnY;

        Vector3 spawnPos = ProjectOnCylinder(spawnPosition);

        Vector3 toCenter = cylinderTransform.position - spawnPos;
        toCenter.y = 0;
        Vector3 tangent = Vector3.Cross(toCenter.normalized, Vector3.up);
        Quaternion spawnRotation = Quaternion.LookRotation(tangent, Vector3.up);

        GameObject spawnedBoss = Instantiate(boss.prefab, spawnPos, spawnRotation);

        if (!spawnedBoss.CompareTag("Boss"))
        {
            spawnedBoss.tag = "Boss";
        }
    }

    IEnumerator BossSpawnEffects()
    {
        float shakeDuration = 1.0f;
        float elapsed = 0f;

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

        Vector3 toCylinder = position - cylinderTransform.position;
        float angle = Mathf.Atan2(toCylinder.x, toCylinder.z);

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

        return allSpawnPoints
            .OrderByDescending(p => CalculateCylinderDistance(p.position, playerTransform.position))
            .ThenByDescending(p => Mathf.Abs(p.position.y - playerTransform.position.y))
            .FirstOrDefault();
    }

    float CalculateCylinderDistance(Vector3 point1, Vector3 point2)
    {
        if (cylinderTransform == null) return Vector3.Distance(point1, point2);

        Vector3 toPoint1 = point1 - cylinderTransform.position;
        Vector3 toPoint2 = point2 - cylinderTransform.position;
        float angle1 = Mathf.Atan2(toPoint1.x, toPoint1.z);
        float angle2 = Mathf.Atan2(toPoint2.x, toPoint2.z);

        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(angle1 * Mathf.Rad2Deg, angle2 * Mathf.Rad2Deg) * Mathf.Deg2Rad);
        float arcDistance = angleDiff * cylinderRadius;
        float yDistance = Mathf.Abs(point1.y - point2.y);

        return Mathf.Sqrt(arcDistance * arcDistance + yDistance * yDistance);
    }

    EnemyType SelectRandomEnemyWithLimit(float spawnHeight, List<EnemyType> availableTypes)
    {
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

            float margin = 0.1f;
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
            Vector3 spawnPos = cylinderTransform != null ? ProjectOnCylinder(spawn.position) : spawn.position;
            Gizmos.DrawWireSphere(spawnPos, 0.3f);

            foreach (Transform point in spawn)
            {
                Vector3 childPos = cylinderTransform != null ? ProjectOnCylinder(point.position) : point.position;
                Gizmos.DrawLine(spawnPos, childPos);
                Gizmos.DrawWireSphere(childPos, 0.2f);
            }
        }

        if (healthIconPrefab != null)
        {
            Gizmos.color = Color.blue;
            foreach (GameObject healthIcon in activeHealthIcons)
            {
                if (healthIcon != null)
                {
                    Gizmos.DrawWireCube(healthIcon.transform.position, Vector3.one * 0.5f);
                }
            }
        }
    }

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

    public void OnHealthIconDestroyed(GameObject healthIcon)
    {
        if (activeHealthIcons.Contains(healthIcon))
        {
            activeHealthIcons.Remove(healthIcon);

            if (debugLogHealthIcons)
            {
                Debug.Log($"Health icon destroyed. Count: {activeHealthIcons.Count}/{maxHealthIcons}");
            }
        }
    }

    public void OnPowerUpDestroyed(GameObject powerUpObj, PowerUpType powerUpType)
    {
        if (powerUpType != null && powerUpType.activeInstances.Contains(powerUpObj))
        {
            powerUpType.activeInstances.Remove(powerUpObj);

            if (debugLogPowerUps)
            {
                Debug.Log($"Power-up '{powerUpType.powerUpName}' destroyed. Count: {powerUpType.activeInstances.Count}/{powerUpType.maxCount}");
            }
        }
    }
}

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

public class HealthIconTracker : MonoBehaviour
{
    private Spawner spawner;

    public void Initialize(Spawner spawnerRef)
    {
        spawner = spawnerRef;
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.OnHealthIconDestroyed(gameObject);
        }
    }
}

public class PowerUpTracker : MonoBehaviour
{
    private Spawner spawner;
    private Spawner.PowerUpType powerUpType;

    public void Initialize(Spawner spawnerRef, Spawner.PowerUpType type)
    {
        spawner = spawnerRef;
        powerUpType = type;
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.OnPowerUpDestroyed(gameObject, powerUpType);
        }
    }
}