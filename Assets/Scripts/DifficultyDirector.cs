using UnityEngine;

/// <summary>
/// Keeps a run inside a tense middle band instead of letting either side
/// snowball. It reads the player's economy and board strength, then makes small,
/// bounded adjustments to wave pacing, reinforcements and alien durability.
///
/// This is deliberately a gentle rubber band, not a cheat: the authored level
/// data still decides the shape of the level. The director only trims the
/// extremes:
///   - a player who is far ahead gets waves a little sooner and a few extra
///     aliens, so the board never becomes a spectator sport;
///   - a player who is collapsing gets slightly longer gaps, a lower
///     simultaneous-alien ceiling and a little more passive sun, so one bad
///     wave does not become an unrecoverable death spiral.
/// </summary>
public class DifficultyDirector : Singleton<DifficultyDirector>
{
    [Header("Tuning")]
    [Tooltip("Ignore the director during the opening build-up.")]
    [SerializeField] private float openingGraceSeconds = 35f;

    [Tooltip("How quickly the director reacts. Lower is smoother.")]
    [SerializeField] private float reactionSpeed = 0.45f;

    [Tooltip("Maximum extra aliens added to a dominant player's huge wave.")]
    [SerializeField] private int maxHugeWaveReinforcements = 4;

    [Tooltip("Maximum extra aliens added to a dominant player's normal wave.")]
    [SerializeField] private int maxNormalWaveReinforcements = 2;

    [Header("Debug")]
    [SerializeField] private bool logChanges = false;

    /// <summary>-1 = player is struggling, 0 = on curve, +1 = player is dominating.</summary>
    public float Advantage { get; private set; }

    public int WaveIndex { get; private set; }
    public int WaveCount { get; private set; }

    private SunCounter sunCounter;
    private LevelManager levelManager;

    private float levelStartTime;
    private bool levelStarted;

    private float lossPressure;
    private float metricTimer;
    private float targetAdvantage;
    private float lastLoggedAdvantage;

    private int sunBank;
    private float attackers;
    private float producers;

    private float WaveT
    {
        get { return WaveCount <= 1 ? 1f : Mathf.Clamp01(WaveIndex / (float)(WaveCount - 1)); }
    }

    private bool IsTutorial
    {
        get
        {
            if (levelManager == null) levelManager = FindObjectOfType<LevelManager>();
            return levelManager != null && levelManager.CurrentLevel != null && levelManager.CurrentLevel.isTutorial;
        }
    }

    public bool IsDifficultyActive
    {
        get
        {
            return levelStarted
                && !IsTutorial
                && (Time.time - levelStartTime) >= openingGraceSeconds;
        }
    }

    /// <summary>Health scale applied to newly spawned aliens.</summary>
    public float EnemyHealthMultiplier
    {
        get
        {
            if (!IsDifficultyActive) return 1f;
            return Mathf.Lerp(0.88f, 1.12f, Mathf.InverseLerp(-1f, 1f, Advantage));
        }
    }

    /// <summary>Interval scale for passive photon rain. Bigger means less income.</summary>
    public float SunIntervalMultiplier
    {
        get
        {
            if (!IsDifficultyActive) return 1f;
            return Mathf.Lerp(0.85f, 1.35f, Mathf.InverseLerp(-1f, 1f, Advantage));
        }
    }

    /// <summary>Scale for the pause between waves. Bigger means more breathing room.</summary>
    public float WaveGapMultiplier
    {
        get
        {
            if (!IsDifficultyActive) return 1f;
            return Mathf.Lerp(1.35f, 0.8f, Mathf.InverseLerp(-1f, 1f, Advantage));
        }
    }

    /// <summary>Scale for how long the spawner waits for a wave to be cleared.</summary>
    public float WaveWindowMultiplier
    {
        get
        {
            if (!IsDifficultyActive) return 1f;
            return Mathf.Lerp(1.3f, 0.85f, Mathf.InverseLerp(-1f, 1f, Advantage));
        }
    }

    /// <summary>Called by the spawner as each wave begins.</summary>
    public void ReportWave(int index, int total)
    {
        if (!levelStarted)
        {
            levelStarted = true;
            levelStartTime = Time.time;
        }

        WaveIndex = Mathf.Max(0, index);
        WaveCount = Mathf.Max(1, total);
    }

    /// <summary>Extra aliens added to the current wave when the player is ahead.</summary>
    public int BonusReinforcements(bool hugeWave)
    {
        if (!IsDifficultyActive || WaveIndex < 1) return 0;

        float dominance = Mathf.Max(0f, Advantage);
        int cap = hugeWave ? maxHugeWaveReinforcements : maxNormalWaveReinforcements;
        float waveWeight = 0.55f + 0.45f * WaveT;
        return Mathf.Clamp(Mathf.RoundToInt(dominance * cap * waveWeight), 0, cap);
    }

    /// <summary>
    /// How many aliens may be alive before the next wave is allowed to start.
    /// A soft ceiling that gives a losing board room to recover.
    /// </summary>
    public int ConcurrentCap(int waveIndex, bool hugeWave)
    {
        int cap = 5 + Mathf.Max(0, waveIndex) * 2;
        if (hugeWave) cap += 4;

        if (IsDifficultyActive)
        {
            if (Advantage >= 0f) cap += Mathf.RoundToInt(Advantage * 3f);
            else cap -= Mathf.RoundToInt(-Advantage * 4f);
        }

        return Mathf.Max(4, cap);
    }

    private void OnEnable()
    {
        GrassPatch.OnPlantLost += HandlePlantLost;
    }

    private void OnDisable()
    {
        GrassPatch.OnPlantLost -= HandlePlantLost;
    }

    private void OnDestroy()
    {
        // Domain-reload safety: the static event outlives this scene object.
        GrassPatch.OnPlantLost -= HandlePlantLost;
    }

    private void Update()
    {
        lossPressure = Mathf.MoveTowards(lossPressure, 0f, Time.deltaTime / 12f);

        if (!IsDifficultyActive)
        {
            Advantage = Mathf.MoveTowards(Advantage, 0f, Time.deltaTime * 0.8f);
            return;
        }

        metricTimer -= Time.deltaTime;
        if (metricTimer <= 0f)
        {
            metricTimer = 0.25f;
            RefreshMetrics();
            targetAdvantage = CalculateAdvantage();
        }

        Advantage = Mathf.MoveTowards(Advantage, targetAdvantage, Time.deltaTime * reactionSpeed);

        if (logChanges && Mathf.Abs(Advantage - lastLoggedAdvantage) > 0.35f)
        {
            lastLoggedAdvantage = Advantage;
            Debug.Log(string.Format(
                "DifficultyDirector: advantage {0:0.00}, wave {1}/{2}, bank {3}, attackers {4:0.0}, producers {5:0.0}",
                Advantage, WaveIndex + 1, WaveCount, sunBank, attackers, producers));
        }
    }

    private void RefreshMetrics()
    {
        if (sunCounter == null) sunCounter = FindObjectOfType<SunCounter>();
        sunBank = sunCounter != null ? sunCounter.GetSunCount() : 0;

        Plant[] plants = FindObjectsOfType<Plant>();
        attackers = 0f;
        producers = 0f;
        int burst = 0;

        foreach (Plant plant in plants)
        {
            if (plant is PeaShooter) attackers += 1f;
            else if (plant is Sunflower) producers += 1f;
            else if (plant is Squash || plant is PotatoMine) burst++;
        }

        // Instant-damage bots are useful but not a sustained front line.
        attackers += burst * 0.25f;
    }

    private float CalculateAdvantage()
    {
        float waveT = WaveT;

        float expectedAttackers = 1.5f + waveT * 7.5f;
        float expectedProducers = 1.5f + waveT * 3.0f;

        float attackScore = attackers / Mathf.Max(1f, expectedAttackers);
        float economyScore = producers / Mathf.Max(1f, expectedProducers);
        float bankScore = Mathf.Clamp01(sunBank / Mathf.Max(150f, 200f + expectedAttackers * 60f));

        float power = 0.5f * attackScore + 0.35f * economyScore + 0.15f * bankScore;
        float advantage = Mathf.Clamp((power - 1f) * 1.6f, -1f, 1f);
        advantage -= lossPressure * 0.55f;

        return Mathf.Clamp(advantage, -1f, 1f);
    }

    private void HandlePlantLost()
    {
        lossPressure = Mathf.Clamp01(lossPressure + 0.4f);
    }
}
