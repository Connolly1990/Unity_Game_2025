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
        [Range(1, 100)] public int spawnWeight = 50;
        public float minSpawnHeight = -10f;
        public float maxSpawnHeight = 10f;
        public int maxCount = 5;
        [HideInInspector] public int weightRangeStart;
        [HideInInspector] public List<GameObject> activeInstances = new List<GameObject>();
    }

    [System.Serializable]
    public class BossType
    {
        public GameObject prefab;
        public float spawnTime = 150f;
        public int spawnCount = 1;
        public float minSpawnHeight = -10f;
        public float maxSpawnHeight = 10f;
        [HideInInspector] public bool hasSpawned = false;
    }

    [System.Serializable]
    public class PowerUpType
    {
        public GameObject prefab;
        public string powerUpName = "Power-Up";
        [Range(1, 100)] public int spawnWeight = 25;
        public float minSpawnHeight = -10f;
        public float maxSpawnHeight = 10f;
        public AudioClip spawnSFX;
        public AudioClip collectSFX;
        [Range(0f, 1f)] public float spawnVolume = 0.6f;
        [HideInInspector] public List<GameObject> activeInstances = new List<GameObject>();
    }

    [Header("SPAWN POINTS")]
    public Transform spawnPointsParent;
    public Transform cylinderTransform;

    [Header("ENEMY SETTINGS")]
    public List<EnemyType> enemyTypes = new List<EnemyType>();

    [Header("BOSS SETTINGS")]
    public List<BossType> bossTypes = new List<BossType>();
    public bool enableBossEffects = true;
    public AudioClip bossSpawnSound;
    [Range(0f, 1f)] public float bossSpawnVolume = 0.8f;

    [Header("HEALTH ICON SETTINGS")]
    public GameObject healthIconPrefab;
    public float healthIconSpawnInterval = 30f;
    public int maxHealthIcons = 3;
    public float healthIconMinHeight = -10f;
    public float healthIconMaxHeight = 10f;

    [Header("POWER-UP SETTINGS")]
    public List<PowerUpType> powerUpTypes = new List<PowerUpType>();
    public float powerUpSpawnInterval = 45f;
    public float powerUpCollisionRadius = 2f;

    [Header("SPAWN SETTINGS")]
    public float spawnCooldown = 2f;
    public string playerTag = "Player";

    private List<Transform> allSpawnPoints = new List<Transform>();
    private Transform playerTransform;
    private int totalSpawnWeight;
    private float cylinderRadius;
    private float gameTimer = 0f;
    private AudioSource audioSource;
    private List<GameObject> activeHealthIcons = new List<GameObject>();
    private float lastHealthIconSpawnTime = 0f;

    // Power-up cycle system
    private List<PowerUpType> availablePowerUps = new List<PowerUpType>();
    private float lastPowerUpSpawnTime = 0f;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        FindPlayer();
        FindCylinder();
        ValidateTypes();
        CacheSpawnPoints();
        ClearInstanceTracking();
        SetupAudio();
        InitializePowerUpCycle();

        if (CheckSetupValidity())
        {
            StartCoroutine(SpawnRoutine());
            StartCoroutine(BossSpawnRoutine());
            StartCoroutine(HealthIconSpawnRoutine());
            StartCoroutine(PowerUpSpawnRoutine());
        }
        else
        {
            Debug.LogError("Spawner initialization failed");
            enabled = false;
        }
    }

    void ValidateTypes()
    {
        enemyTypes.RemoveAll(x => x.prefab == null);
        bossTypes.RemoveAll(x => x.prefab == null);
        powerUpTypes.RemoveAll(x => x.prefab == null);

        // Fix invalid height ranges
        foreach (var enemy in enemyTypes)
        {
            if (enemy.minSpawnHeight >= enemy.maxSpawnHeight)
                enemy.maxSpawnHeight = enemy.minSpawnHeight + 20f;
        }

        foreach (var boss in bossTypes)
        {
            if (boss.minSpawnHeight >= boss.maxSpawnHeight)
                boss.maxSpawnHeight = boss.minSpawnHeight + 20f;
        }

        foreach (var powerUp in powerUpTypes)
        {
            if (powerUp.minSpawnHeight >= powerUp.maxSpawnHeight)
                powerUp.maxSpawnHeight = powerUp.minSpawnHeight + 20f;
        }

        // Setup enemy weights
        totalSpawnWeight = 0;
        foreach (EnemyType enemy in enemyTypes)
        {
            enemy.weightRangeStart = totalSpawnWeight;
            totalSpawnWeight += enemy.spawnWeight;
        }

        bossTypes = bossTypes.OrderBy(b => b.spawnTime).ToList();
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

    void InitializePowerUpCycle()
    {
        availablePowerUps = new List<PowerUpType>(powerUpTypes);
    }

    void Update()
    {
        gameTimer += Time.deltaTime;
    }

    void ClearInstanceTracking()
    {
        foreach (var enemyType in enemyTypes)
            enemyType.activeInstances.Clear();

        activeHealthIcons.Clear();

        foreach (var powerUp in powerUpTypes)
            powerUp.activeInstances.Clear();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
            playerTransform = playerObj.transform;
        else
            Debug.LogError($"No GameObject found with tag '{playerTag}'");
    }

    void FindCylinder()
    {
        if (cylinderTransform == null)
        {
            cylinderTransform = GameObject.FindGameObjectWithTag("Level")?.transform;
            if (cylinderTransform == null)
            {
                Debug.LogError("No cylinder transform found");
                return;
            }
        }
        cylinderRadius = cylinderTransform.localScale.x * 0.5f;
    }

    void CacheSpawnPoints()
    {
        allSpawnPoints.Clear();
        if (spawnPointsParent == null) return;

        foreach (Transform mainSpawn in spawnPointsParent)
        {
            allSpawnPoints.Add(mainSpawn);
            foreach (Transform child in mainSpawn)
                allSpawnPoints.Add(child);
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

            if (availableTypes.Count == 0) continue;

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
            }
        }
    }

    IEnumerator HealthIconSpawnRoutine()
    {
        yield return new WaitForSeconds(healthIconSpawnInterval);

        while (true)
        {
            if (healthIconPrefab != null && CanSpawnHealthIcon())
                SpawnHealthIcon();

            yield return new WaitForSeconds(healthIconSpawnInterval);
        }
    }

    IEnumerator PowerUpSpawnRoutine()
    {
        yield return new WaitForSeconds(15f);

        while (true)
        {
            if (gameTimer - lastPowerUpSpawnTime >= powerUpSpawnInterval && availablePowerUps.Count > 0)
            {
                SpawnRandomPowerUp();
            }

            yield return new WaitForSeconds(2f);
        }
    }

    bool CanSpawnHealthIcon()
    {
        CleanupDestroyedHealthIcons();
        return activeHealthIcons.Count < maxHealthIcons;
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
            tracker = spawnedHealthIcon.AddComponent<HealthIconTracker>();
        tracker.Initialize(this);

        lastHealthIconSpawnTime = gameTimer;
    }

    void SpawnRandomPowerUp()
    {
        if (availablePowerUps.Count == 0)
        {
            // Reset cycle when all power-ups have been spawned
            availablePowerUps = new List<PowerUpType>(powerUpTypes);
            return;
        }

        // Clean up destroyed power-ups
        foreach (var powerUp in powerUpTypes)
            CleanupDestroyedPowerUps(powerUp);

        // Select random power-up from available ones
        int randomIndex = Random.Range(0, availablePowerUps.Count);
        PowerUpType selectedPowerUp = availablePowerUps[randomIndex];

        // Find a valid spawn position
        Vector3 spawnPos = FindValidPowerUpSpawnPosition(selectedPowerUp);
        if (spawnPos == Vector3.zero) return; // No valid position found

        // Spawn the power-up
        Quaternion spawnRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        GameObject spawnedPowerUp = Instantiate(selectedPowerUp.prefab, spawnPos, spawnRotation);

        selectedPowerUp.activeInstances.Add(spawnedPowerUp);

        PowerUpTracker tracker = spawnedPowerUp.GetComponent<PowerUpTracker>();
        if (tracker == null)
            tracker = spawnedPowerUp.AddComponent<PowerUpTracker>();
        tracker.Initialize(this, selectedPowerUp);

        // Play spawn sound
        if (audioSource != null && selectedPowerUp.spawnSFX != null)
            audioSource.PlayOneShot(selectedPowerUp.spawnSFX, selectedPowerUp.spawnVolume);

        // Remove from available list (knockout system)
        availablePowerUps.RemoveAt(randomIndex);
        lastPowerUpSpawnTime = gameTimer;
    }

    Vector3 FindValidPowerUpSpawnPosition(PowerUpType powerUp)
    {
        int attempts = 0;
        int maxAttempts = 20;

        while (attempts < maxAttempts)
        {
            Transform spawnPoint = GetRandomSpawnPoint();
            if (spawnPoint == null) break;

            float spawnY = Mathf.Clamp(spawnPoint.position.y, powerUp.minSpawnHeight, powerUp.maxSpawnHeight);
            Vector3 spawnPosition = spawnPoint.position;
            spawnPosition.y = spawnY;

            Vector3 candidatePos = ProjectOnCylinder(spawnPosition);

            // Check for collisions with existing power-ups
            bool positionValid = true;
            foreach (var powerUpType in powerUpTypes)
            {
                foreach (var existingPowerUp in powerUpType.activeInstances)
                {
                    if (existingPowerUp != null &&
                        Vector3.Distance(candidatePos, existingPowerUp.transform.position) < powerUpCollisionRadius)
                    {
                        positionValid = false;
                        break;
                    }
                }
                if (!positionValid) break;
            }

            if (positionValid)
                return candidatePos;

            attempts++;
        }

        return Vector3.zero; // No valid position found
    }

    Transform GetRandomSpawnPoint()
    {
        if (allSpawnPoints.Count == 0) return null;
        return allSpawnPoints[Random.Range(0, allSpawnPoints.Count)];
    }

    void CleanupDestroyedHealthIcons()
    {
        activeHealthIcons.RemoveAll(h => h == null);
    }

    void CleanupDestroyedPowerUps(PowerUpType powerUp)
    {
        powerUp.activeInstances.RemoveAll(p => p == null);
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

                    if (enableBossEffects && audioSource != null && bossSpawnSound != null)
                        audioSource.PlayOneShot(bossSpawnSound, bossSpawnVolume);

                    if (enableBossEffects)
                        StartCoroutine(BossSpawnEffects());
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
            spawnedBoss.tag = "Boss";
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
            enemyType.activeInstances.RemoveAll(e => e == null);
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
            if (spawnHeight >= enemy.minSpawnHeight && spawnHeight <= enemy.maxSpawnHeight)
            {
                validEnemies.Add(enemy);
                validWeightTotal += enemy.spawnWeight;
            }
        }

        if (validEnemies.Count == 0)
        {
            // Auto-adjust height restrictions
            float margin = 0.1f;
            foreach (EnemyType enemy in availableTypes)
            {
                if (spawnHeight < enemy.minSpawnHeight)
                    enemy.minSpawnHeight = spawnHeight - margin;
                else if (spawnHeight > enemy.maxSpawnHeight)
                    enemy.maxSpawnHeight = spawnHeight + margin;

                validEnemies.Add(enemy);
                validWeightTotal += enemy.spawnWeight;
            }
        }

        if (validEnemies.Count == 0) return null;

        int randomWeight = Random.Range(0, validWeightTotal);
        int accumulatedWeight = 0;

        foreach (EnemyType enemy in validEnemies)
        {
            accumulatedWeight += enemy.spawnWeight;
            if (randomWeight < accumulatedWeight)
                return enemy;
        }

        return validEnemies[Random.Range(0, validEnemies.Count)];
    }

    // Public methods for tracking destroyed objects
    public void OnEnemyDestroyed(GameObject enemy, EnemyType enemyType)
    {
        if (enemyType != null && enemyType.activeInstances.Contains(enemy))
            enemyType.activeInstances.Remove(enemy);
    }

    public void OnHealthIconDestroyed(GameObject healthIcon)
    {
        if (activeHealthIcons.Contains(healthIcon))
            activeHealthIcons.Remove(healthIcon);
    }

    public void OnPowerUpDestroyed(GameObject powerUpObj, PowerUpType powerUpType)
    {
        if (powerUpType != null && powerUpType.activeInstances.Contains(powerUpObj))
            powerUpType.activeInstances.Remove(powerUpObj);
    }
}

// Tracker classes remain the same
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
            spawner.OnEnemyDestroyed(gameObject, enemyType);
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
            spawner.OnHealthIconDestroyed(gameObject);
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
            spawner.OnPowerUpDestroyed(gameObject, powerUpType);
    }
}