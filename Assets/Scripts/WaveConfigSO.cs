using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Authored description of a single level: which lanes are open, how much
/// photon energy the player starts with, and the exact wave script.
/// One asset per level. Assets are never modified at runtime.
/// </summary>
[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "ZombieSpawner/WaveConfig")]
public class WaveConfigSO : ScriptableObject
{
    [Header("Identity")]
    public string levelName = "Sector";
    [TextArea(2, 4)]
    public string briefing = "Hold the line.";

    [Header("Battlefield")]
    [Tooltip("Five entries. 1 = lane is open, 0 = lane is blocked off.")]
    public int[] lanesToUse = { 1, 1, 1, 1, 1 };

    [Header("Economy")]
    [Tooltip("Photons the player starts with. -1 keeps the SunCounter default.")]
    public int startingSun = -1;

    [Tooltip("Seconds between falling photons. 0 keeps the SunSpawner default.")]
    public float sunSpawnInterval = 0f;

    [Header("Difficulty Ramp")]
    [Tooltip("Alien health multiplier in wave one. Ramps to the end value by the final wave.")]
    [Range(0.5f, 4f)] public float zombieHealthStart = 1f;

    [Tooltip("Alien health multiplier in the final wave.")]
    [Range(0.5f, 4f)] public float zombieHealthEnd = 1.35f;

    [Tooltip("Alien move-speed multiplier in wave one.")]
    [Range(0.5f, 2f)] public float zombieSpeedStart = 1f;

    [Tooltip("Alien move-speed multiplier in the final wave.")]
    [Range(0.5f, 2f)] public float zombieSpeedEnd = 1.1f;

    [Tooltip("Alien damage multiplier in wave one.")]
    [Range(0.5f, 3f)] public float zombieDamageStart = 1f;

    [Tooltip("Alien damage multiplier in the final wave.")]
    [Range(0.5f, 3f)] public float zombieDamageEnd = 1.1f;

    [Tooltip("Maximum seconds the spawner waits for a wave to be fully cleared. " +
             "Prevents a slow straggler from becoming a free sun-farming window.")]
    [Range(5f, 60f)] public float maxWaveWindow = 26f;

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>();

    [Header("Rewards")]
    [Tooltip("Bots unlocked when this level is cleared.")]
    public List<SeedSO> unlockedSeeds = new List<SeedSO>();

    [Tooltip("Legacy single reward. Still honoured so older assets keep working.")]
    public SeedSO unlockedSeedSO;

    [Header("Flow")]
    public bool isTutorial = false;
    public bool isFinalLevel = false;

    public int LaneCount
    {
        get { return lanesToUse != null ? lanesToUse.Length : 0; }
    }

    public int ActiveLaneCount
    {
        get
        {
            if (lanesToUse == null) return 0;
            int count = 0;
            for (int i = 0; i < lanesToUse.Length; i++)
            {
                if (lanesToUse[i] == 1) count++;
            }
            return count;
        }
    }

    public bool IsLaneActive(int lane)
    {
        if (lanesToUse == null || lane < 0 || lane >= lanesToUse.Length) return false;
        return lanesToUse[lane] == 1;
    }

    /// <summary>All bots granted by this level, legacy field included, without duplicates.</summary>
    public List<SeedSO> GetRewards()
    {
        List<SeedSO> rewards = new List<SeedSO>();

        if (unlockedSeeds != null)
        {
            foreach (SeedSO seed in unlockedSeeds)
            {
                if (seed != null && !rewards.Contains(seed)) rewards.Add(seed);
            }
        }

        if (unlockedSeedSO != null && !rewards.Contains(unlockedSeedSO))
        {
            rewards.Add(unlockedSeedSO);
        }

        return rewards;
    }

    public int TotalZombieCount
    {
        get
        {
            int total = 0;
            if (waves == null) return 0;
            foreach (Wave wave in waves)
            {
                if (wave != null) total += wave.TotalZombieCount;
            }
            return total;
        }
    }
}

/// <summary>
/// A single wave. All spawn groups inside a wave start together, each with
/// its own optional stagger, and the wave is only finished once every zombie
/// from every group is dead.
/// </summary>
[System.Serializable]
public class Wave
{
    public string label = "Wave";

    [Tooltip("Seconds to wait before this wave starts. Applies to the first wave too.")]
    public float delayBeforeWave = 5f;

    [Tooltip("Default seconds between individual spawns inside this wave.")]
    public float spawnInterval = 1.5f;

    [Tooltip("Tints the wave banner red and plays the final wave sting.")]
    public bool hugeWave = false;

    public List<WaveSpawn> spawns = new List<WaveSpawn>();

    public int TotalZombieCount
    {
        get
        {
            int total = 0;
            if (spawns == null) return 0;
            foreach (WaveSpawn spawn in spawns)
            {
                if (spawn != null) total += Mathf.Max(0, spawn.count);
            }
            return total;
        }
    }
}

/// <summary>
/// One zombie type, a count and the lane it walks down.
/// </summary>
[System.Serializable]
public class WaveSpawn
{
    public GameObject zombiePrefab;

    [Min(1)]
    public int count = 1;

    [Tooltip("Lane index 0-4. -1 spreads this group across the open lanes.")]
    public int lane = -1;

    [Tooltip("Seconds between spawns in this group. -1 uses the wave spawn interval.")]
    public float spacing = -1f;

    [Tooltip("Seconds to wait after the wave starts before this group begins spawning.")]
    public float startDelay = 0f;
}
