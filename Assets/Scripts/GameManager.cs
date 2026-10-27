using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface IPlantable
{
    event Action<IPlantable> OnPlantDead;

    GameObject GetGameObject();
}

public enum GameState
{
    Intro,
    Playing,
    LevelCleared,
    Defeat,
    GameComplete
}

/// <summary>
/// Owns the run: intro card, level start, wave tracking, rewards, win/lose
/// presentation and progression between levels.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    public SeedSO selectedSeedSo;

    /// <summary>Raised when a bot is permanently unlocked. GameFacade persists it.</summary>
    public event Action<SeedSO> OnUnlockNewSeed;

    /// <summary>Raised once the seed bar should be rebuilt.</summary>
    public event Action OnInstantiateUnlockedSeeds;

    public event Action<GameState> OnStateChanged;

    public GameState State { get; private set; } = GameState.Intro;

    [Header("Scene references")]
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private ZombieSpawnerNew zombieSpawner;
    [SerializeField] private SunCounter sunCounter;
    [SerializeField] private SunSpawner sunSpawner;
    [SerializeField] private UnlockedSeedsManager unlockedSeedsManager;
    [SerializeField] private DifficultyDirector difficultyDirector;

    private GameFlowUI flowUI;

    private float levelStartUnscaledTime;
    private int wavesStarted;

    protected override void Awake()
    {
        base.Awake();

        ResolveReferences();
        EnsureFlowUI();
        EnsureDifficultyDirector();

        // A reload always starts from a clean timescale.
        Time.timeScale = 1f;
    }

    private void Start()
    {
        OnInstantiateUnlockedSeeds?.Invoke();

        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        SunCounter.OnSeedSelected += SunCounter_OnSeedSelected;
        GrassPatch.OnPlantPlaced += GrassPatch_OnPlantPlaced;
        ZombieSpawnerNew.OnWaveStarted += ZombieSpawnerNew_OnWaveStarted;
        ZombieSpawnerNew.OnAllWavesCleared += ZombieSpawnerNew_OnAllWavesCleared;
        TutorialUI.OnZombieSpawnerActivate += TutorialUI_OnZombieSpawnerActivate;
        TutorialUI.OnZombieSpawnerDeactivate += TutorialUI_OnZombieSpawnerDeactivate;

        if (levelManager == null || levelManager.CurrentLevel == null)
        {
            Debug.LogError("GameManager: no level available.");
            return;
        }

        if (levelManager.IsGameComplete)
        {
            SetState(GameState.GameComplete);
            Time.timeScale = 0f;
            flowUI.ShowGameComplete(levelManager.LevelCount);
            return;
        }

        SetState(GameState.Intro);
        Time.timeScale = 0f;
        flowUI.ShowIntro(levelManager.CurrentLevel, levelManager.CurrentLevelIndex, levelManager.LevelCount);
    }

    private void ResolveReferences()
    {
        if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
        if (zombieSpawner == null) zombieSpawner = FindObjectOfType<ZombieSpawnerNew>();
        if (sunCounter == null) sunCounter = FindObjectOfType<SunCounter>();
        if (sunSpawner == null) sunSpawner = FindObjectOfType<SunSpawner>();
        if (unlockedSeedsManager == null) unlockedSeedsManager = FindObjectOfType<UnlockedSeedsManager>();
        if (difficultyDirector == null) difficultyDirector = FindObjectOfType<DifficultyDirector>();
    }

    private void EnsureDifficultyDirector()
    {
        if (difficultyDirector != null) return;

        GameObject directorObject = new GameObject("DifficultyDirector");
        directorObject.transform.SetParent(null);
        difficultyDirector = directorObject.AddComponent<DifficultyDirector>();
    }

    private void EnsureFlowUI()
    {
        flowUI = GameFlowUI.Instance;
        if (flowUI != null) return;

        GameObject uiObject = new GameObject("GameFlowUI");
        uiObject.transform.SetParent(null);
        flowUI = uiObject.AddComponent<GameFlowUI>();
    }

    // ---------------------------------------------------------------- flow

    /// <summary>Called by the intro card button.</summary>
    public void BeginPlay()
    {
        if (State != GameState.Intro) return;

        WaveConfigSO level = levelManager != null ? levelManager.CurrentLevel : null;
        if (level == null) return;

        SetState(GameState.Playing);
        Time.timeScale = 1f;
        levelStartUnscaledTime = Time.unscaledTime;
        wavesStarted = 0;

        flowUI.HideOverlay();
        flowUI.UpdateWaveProgress(0, level.waves != null ? level.waves.Count : 0, level.levelName);

        // In the tutorial the first wave is still several lessons away.
        if (level.isTutorial) flowUI.HideProgress();

        if (!level.isTutorial)
        {
            if (sunSpawner != null) sunSpawner.Activate();
            zombieSpawner.BeginSpawning();
        }
        else if (FindObjectOfType<TutorialUI>() == null)
        {
            // Safety net: tutorial scene object missing, do not soft-lock the level.
            if (sunSpawner != null) sunSpawner.Activate();
            zombieSpawner.BeginSpawning();
        }
    }

    /// <summary>Reloads the current level (retry).</summary>
    public void RetryLevel()
    {
        ReloadScene();
    }

    /// <summary>Loads the next level, or returns to the finale when everything is done.</summary>
    public void ContinueToNextLevel()
    {
        ReloadScene();
    }

    /// <summary>Victory screen: keep unlocks, replay from the first real level.</summary>
    public void PlayAgainFromStart()
    {
        PlayerPrefs.SetInt(LevelManager.LEVEL_PREF_KEY, levelManager != null && levelManager.LevelCount > 1 ? 1 : 0);
        PlayerPrefs.Save();
        ReloadScene();
    }

    public void ReplayFinale()
    {
        int last = levelManager != null ? Mathf.Max(0, levelManager.LevelCount - 1) : 0;
        PlayerPrefs.SetInt(LevelManager.LEVEL_PREF_KEY, last);
        PlayerPrefs.Save();
        ReloadScene();
    }

    private void ReloadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --------------------------------------------------------- progression

    private void ZombieSpawnerNew_OnWaveStarted(int index, int total, bool huge)
    {
        wavesStarted = index + 1;
        flowUI.ShowWaveBanner(index, total, huge);
        flowUI.UpdateWaveProgress(wavesStarted, total, levelManager.CurrentLevel.levelName);

        if (huge)
        {
            AudioManager.Instance.Play("FinalWaveSound");
        }
    }

    private void ZombieSpawnerNew_OnAllWavesCleared()
    {
        HandleLevelCleared();
    }

    private void HandleLevelCleared()
    {
        if (State != GameState.Playing) return;

        SetState(GameState.LevelCleared);
        Time.timeScale = 0f;

        WaveConfigSO level = levelManager.CurrentLevel;
        List<SeedSO> rewards = level.GetRewards();

        foreach (SeedSO reward in rewards)
        {
            if (reward == null) continue;
            OnUnlockNewSeed?.Invoke(reward);
        }

        int nextIndex = levelManager.CurrentLevelIndex + 1;
        bool finaleCleared = nextIndex >= levelManager.LevelCount;

        PlayerPrefs.SetInt(LevelManager.LEVEL_PREF_KEY, Mathf.Max(PlayerPrefs.GetInt(LevelManager.LEVEL_PREF_KEY, 0), nextIndex));
        PlayerPrefs.Save();

        AudioManager.Instance.Play("FinalWaveSound");

        float elapsed = Time.unscaledTime - levelStartUnscaledTime;

        if (finaleCleared)
        {
            flowUI.ShowGameComplete(levelManager.LevelCount);
            SetState(GameState.GameComplete);
        }
        else
        {
            flowUI.ShowLevelCleared(level, rewards, nextIndex, levelManager.LevelCount, elapsed);
        }
    }

    public void NotifyDefeat()
    {
        if (State != GameState.Playing) return;

        SetState(GameState.Defeat);
        Time.timeScale = 0f;
        zombieSpawner.StopSpawning();
        AudioManager.Instance.Play("Explosion");

        WaveConfigSO level = levelManager != null ? levelManager.CurrentLevel : null;
        int totalWaves = level != null && level.waves != null ? level.waves.Count : 0;
        flowUI.ShowDefeat(level, wavesStarted, totalWaves);
    }

    private void SetState(GameState state)
    {
        State = state;
        OnStateChanged?.Invoke(state);
    }

    // ------------------------------------------------------------- seeds

    public SeedSO GetSelectedSeedSO()
    {
        return selectedSeedSo;
    }

    public bool IsSeedSelected()
    {
        return selectedSeedSo != null;
    }

    private void TutorialUI_OnZombieSpawnerActivate()
    {
        if (State == GameState.Playing) zombieSpawner.BeginSpawning();
    }

    private void TutorialUI_OnZombieSpawnerDeactivate()
    {
        if (zombieSpawner != null) zombieSpawner.StopSpawning();
    }

    private void OnDestroy()
    {
        SunCounter.OnSeedSelected -= SunCounter_OnSeedSelected;
        GrassPatch.OnPlantPlaced -= GrassPatch_OnPlantPlaced;
        ZombieSpawnerNew.OnWaveStarted -= ZombieSpawnerNew_OnWaveStarted;
        ZombieSpawnerNew.OnAllWavesCleared -= ZombieSpawnerNew_OnAllWavesCleared;
        TutorialUI.OnZombieSpawnerActivate -= TutorialUI_OnZombieSpawnerActivate;
        TutorialUI.OnZombieSpawnerDeactivate -= TutorialUI_OnZombieSpawnerDeactivate;
    }

    private void GrassPatch_OnPlantPlaced(SeedSO seed)
    {
        selectedSeedSo = null;
    }

    private void SunCounter_OnSeedSelected(SeedSO seedSO)
    {
        selectedSeedSo = seedSO;
    }
}
