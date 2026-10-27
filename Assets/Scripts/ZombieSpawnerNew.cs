using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays a <see cref="WaveConfigSO"/> script: waves, spawn groups, lanes and
/// pacing. Never writes back into the ScriptableObject, so authoring data stays
/// clean between runs.
///
/// A wave is given a bounded window. If the player clears it sooner, the next
/// wave comes sooner. If a straggler would otherwise stall the level, the next
/// wave starts anyway. That keeps pressure on without starving the player of
/// the recovery time a bad wave needs.
/// </summary>
public class ZombieSpawnerNew : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private SpawnerPointSet spawnerPointSet;

    [Tooltip("Grace period once the level actually starts before wave one.")]
    [SerializeField] private float startDelay = 3f;

    [Tooltip("Random extra delay added between spawns so waves never feel robotic.")]
    [SerializeField] private float spawnJitter = 0.25f;

    [Tooltip("Safety valve: the spawner never waits forever on a single stubborn wave.")]
    [SerializeField] private float maxConcurrentWait = 20f;

    public static event Action<int, int, bool> OnWaveStarted; // index, total, huge
    public static event Action OnAllWavesCleared;

    public bool IsSpawning { get; private set; }
    public int AliveZombies { get { return aliveZombies; } }

    private readonly List<int> laneBag = new List<int>();
    private int aliveZombies;
    private int pendingGroups;
    private bool stopping;

    private void Awake()
    {
        if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
        if (spawnerPointSet == null) spawnerPointSet = FindObjectOfType<SpawnerPointSet>();
    }

    public void BeginSpawning()
    {
        if (IsSpawning) return;

        WaveConfigSO level = levelManager != null ? levelManager.CurrentLevel : null;
        if (level == null || level.waves == null || level.waves.Count == 0)
        {
            Debug.LogWarning("ZombieSpawnerNew: level has no waves to spawn.");
            return;
        }

        stopping = false;
        IsSpawning = true;
        aliveZombies = 0;
        pendingGroups = 0;
        StartCoroutine(SpawnRoutine(level));
    }

    public void StopSpawning()
    {
        stopping = true;
        StopAllCoroutines();
        IsSpawning = false;
    }

    private IEnumerator SpawnRoutine(WaveConfigSO level)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, startDelay));

        int waveCount = level.waves.Count;
        float lastWaveStartTime = Time.time;

        for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
        {
            Wave wave = level.waves[waveIndex];
            if (wave == null || wave.spawns == null || wave.spawns.Count == 0) continue;

            DifficultyDirector director = DifficultyDirector.Instance;

            if (waveIndex > 0)
            {
                // Give the previous wave a bounded window to clear. A dominant
                // board is not allowed to farm sun behind a single straggler,
                // while a losing board still gets a real breather.
                float window = level.maxWaveWindow > 0f ? level.maxWaveWindow : 26f;
                if (director != null) window *= director.WaveWindowMultiplier;

                while (aliveZombies > 0 && (Time.time - lastWaveStartTime) < window)
                {
                    if (stopping) yield break;
                    yield return null;
                }

                // Never stack a banner on top of a wave that is still deploying.
                while (pendingGroups > 0)
                {
                    if (stopping) yield break;
                    yield return null;
                }

                float gap = Mathf.Max(0f, wave.delayBeforeWave);
                if (director != null) gap *= director.WaveGapMultiplier;
                if (gap > 0f) yield return new WaitForSeconds(gap);
                if (stopping) yield break;

                // Soft ceiling on simultaneous aliens: if the lawn is already
                // crowded, hold this wave briefly so the player can thin it out.
                int cap = director != null ? director.ConcurrentCap(waveIndex, wave.hugeWave) : int.MaxValue;
                float capWait = 0f;
                while (aliveZombies > cap && capWait < maxConcurrentWait)
                {
                    if (stopping) yield break;
                    capWait += Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(Mathf.Max(0f, wave.delayBeforeWave));
                if (stopping) yield break;
            }

            GetRamp(level, waveIndex, out float health, out float speed, out float damage);

            OnWaveStarted?.Invoke(waveIndex, waveCount, wave.hugeWave);
            if (director != null) director.ReportWave(waveIndex, waveCount);

            lastWaveStartTime = Time.time;
            StartWave(wave, health, speed, damage);

            if (director != null)
            {
                int bonus = director.BonusReinforcements(wave.hugeWave);
                if (bonus > 0) StartReinforcements(wave, bonus, health, speed, damage);
            }
        }

        // Every wave has been deployed; victory waits for the lawn to be clear.
        while (aliveZombies > 0)
        {
            if (stopping) yield break;
            yield return null;
        }

        IsSpawning = false;
        OnAllWavesCleared?.Invoke();
    }

    private void StartWave(Wave wave, float health, float speed, float damage)
    {
        foreach (WaveSpawn group in wave.spawns)
        {
            if (group == null || group.zombiePrefab == null || group.count <= 0) continue;

            pendingGroups++;
            StartCoroutine(SpawnGroup(group, wave, health, speed, damage, () => pendingGroups--));
        }
    }

    private void StartReinforcements(Wave wave, int count, float health, float speed, float damage)
    {
        List<GameObject> prefabs = new List<GameObject>();
        foreach (WaveSpawn spawn in wave.spawns)
        {
            if (spawn != null && spawn.zombiePrefab != null && !prefabs.Contains(spawn.zombiePrefab))
            {
                prefabs.Add(spawn.zombiePrefab);
            }
        }

        if (prefabs.Count == 0) return;

        WaveSpawn extra = new WaveSpawn
        {
            zombiePrefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)],
            count = count,
            lane = -1,
            spacing = Mathf.Max(0.8f, wave.spawnInterval),
            startDelay = 1.5f
        };

        pendingGroups++;
        StartCoroutine(SpawnGroup(extra, wave, health, speed, damage, () => pendingGroups--));
    }

    private IEnumerator SpawnGroup(WaveSpawn group, Wave wave, float healthMultiplier, float speedMultiplier, float damageMultiplier, Action onFinished)
    {
        if (group.startDelay > 0f)
        {
            yield return new WaitForSeconds(group.startDelay);
        }

        float spacing = group.spacing >= 0f ? group.spacing : Mathf.Max(0f, wave.spawnInterval);

        for (int i = 0; i < group.count; i++)
        {
            SpawnOne(group.zombiePrefab, PickLane(group.lane), healthMultiplier, speedMultiplier, damageMultiplier);

            if (i < group.count - 1 && spacing > 0f)
            {
                float jitter = spawnJitter > 0f ? UnityEngine.Random.Range(0f, spawnJitter) : 0f;
                yield return new WaitForSeconds(spacing + jitter);
            }
        }

        onFinished?.Invoke();
    }

    private static void GetRamp(WaveConfigSO level, int waveIndex, out float health, out float speed, out float damage)
    {
        int count = level.waves != null ? level.waves.Count : 1;
        float t = count <= 1 ? 1f : Mathf.Clamp01(waveIndex / (float)(count - 1));

        health = Mathf.Lerp(SafeMultiplier(level.zombieHealthStart), SafeMultiplier(level.zombieHealthEnd), t);
        speed = Mathf.Lerp(SafeMultiplier(level.zombieSpeedStart), SafeMultiplier(level.zombieSpeedEnd), t);
        damage = Mathf.Lerp(SafeMultiplier(level.zombieDamageStart), SafeMultiplier(level.zombieDamageEnd), t);
    }

    private static float SafeMultiplier(float value)
    {
        return value > 0f ? value : 1f;
    }

    private int PickLane(int requestedLane)
    {
        List<int> activeLanes = spawnerPointSet != null
            ? spawnerPointSet.GetActiveLaneIndices()
            : new List<int>();

        if (activeLanes.Count == 0) return -1;

        if (requestedLane >= 0 && activeLanes.Contains(requestedLane))
        {
            return requestedLane;
        }

        // Spread "any lane" groups fairly so consecutive zombies never stack up
        // in the same lane by accident.
        if (laneBag.Count == 0)
        {
            laneBag.AddRange(activeLanes);
            for (int i = laneBag.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = laneBag[i];
                laneBag[i] = laneBag[j];
                laneBag[j] = tmp;
            }
        }

        int lane = laneBag[0];
        laneBag.RemoveAt(0);
        return lane;
    }

    private void SpawnOne(GameObject prefab, int lane, float healthMultiplier, float speedMultiplier, float damageMultiplier)
    {
        Transform point = spawnerPointSet != null ? spawnerPointSet.GetSpawnPoint(lane) : null;

        if (point == null && spawnerPointSet != null && spawnerPointSet.currentSpawnPoints.Count > 0)
        {
            point = spawnerPointSet.currentSpawnPoints[UnityEngine.Random.Range(0, spawnerPointSet.currentSpawnPoints.Count)];
        }

        if (point == null)
        {
            Debug.LogWarning("ZombieSpawnerNew: no spawn point available for lane " + lane);
            return;
        }

        GameObject zombieObject = Instantiate(prefab, point.position, point.rotation);
        Zombie zombie = zombieObject.GetComponentInChildren<Zombie>();

        if (zombie != null)
        {
            float health = healthMultiplier;
            if (DifficultyDirector.Instance != null && DifficultyDirector.Instance.IsDifficultyActive)
            {
                health *= DifficultyDirector.Instance.EnemyHealthMultiplier;
            }

            zombie.ApplyDifficulty(health, speedMultiplier, damageMultiplier);

            aliveZombies++;
            zombie.OnKilled += HandleZombieKilled;
        }
        else
        {
            Debug.LogWarning("ZombieSpawnerNew: prefab has no Zombie component: " + prefab.name);
        }
    }

    private void HandleZombieKilled(Zombie zombie)
    {
        if (zombie != null) zombie.OnKilled -= HandleZombieKilled;
        aliveZombies = Mathf.Max(0, aliveZombies - 1);
    }

    private void OnDestroy()
    {
        StopSpawning();
    }
}
