using UnityEngine;

/// <summary>
/// Resolves the level the player should be on from saved progress and applies
/// its battlefield / economy settings. Runs in Awake so every other system can
/// read <see cref="CurrentLevel"/> during Start.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public const string LEVEL_PREF_KEY = "Level";

    [SerializeField] private LaneManager laneManager;
    [SerializeField] private SpawnerPointSet spawnerPointSet;
    [SerializeField] private WaveConfigSO[] allLevels;

    [Tooltip("Index of the level the player should currently be playing.")]
    [SerializeField] private int currentLevelIndex;

    private WaveConfigSO currentLevel;

    public WaveConfigSO CurrentLevel { get { return currentLevel; } }
    public int CurrentLevelIndex { get { return currentLevelIndex; } }
    public int LevelCount { get { return allLevels != null ? allLevels.Length : 0; } }

    /// <summary>True when the player has already finished every level.</summary>
    public bool IsGameComplete { get; private set; }

    private void Awake()
    {
        ResolveReferences();
        LoadProgress();
        ApplyLevel();
    }

    private void ResolveReferences()
    {
        if (laneManager == null) laneManager = FindObjectOfType<LaneManager>();
        if (spawnerPointSet == null) spawnerPointSet = FindObjectOfType<SpawnerPointSet>();
    }

    private void LoadProgress()
    {
        if (allLevels == null || allLevels.Length == 0)
        {
            Debug.LogError("LevelManager has no levels assigned.");
            return;
        }

        int savedIndex = PlayerPrefs.GetInt(LEVEL_PREF_KEY, 0);

        IsGameComplete = savedIndex >= allLevels.Length;
        currentLevelIndex = Mathf.Clamp(savedIndex, 0, allLevels.Length - 1);
        currentLevel = allLevels[currentLevelIndex];
    }

    private void ApplyLevel()
    {
        if (currentLevel == null) return;

        int[] lanes = NormaliseLanes(currentLevel.lanesToUse);

        if (laneManager != null) laneManager.SetLanes(lanes);
        if (spawnerPointSet != null)
        {
            spawnerPointSet.SetConfig(currentLevel);
            spawnerPointSet.SetSpawnPoints(lanes);
        }

        SunCounter sunCounter = FindObjectOfType<SunCounter>();
        if (sunCounter != null && currentLevel.startingSun >= 0)
        {
            sunCounter.ConfigureStartingSun(currentLevel.startingSun);
        }

        SunSpawner sunSpawner = FindObjectOfType<SunSpawner>();
        if (sunSpawner != null && currentLevel.sunSpawnInterval > 0f)
        {
            sunSpawner.SetSpawnInterval(currentLevel.sunSpawnInterval);
        }
    }

    private static int[] NormaliseLanes(int[] source)
    {
        int[] lanes = new int[5];
        if (source == null) return lanes;

        for (int i = 0; i < lanes.Length; i++)
        {
            lanes[i] = i < source.Length ? source[i] : 0;
        }

        return lanes;
    }
}
