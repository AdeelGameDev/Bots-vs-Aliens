using UnityEngine;

public class SunSpawner : MonoBehaviour
{
    public GameObject sunPrefab; // The sun prefab to instantiate
    public float spawnInterval = 5f; // Time in seconds between each spawn
    public float startDelay = 3f; // Initial delay before starting to spawn suns
    public Vector2 xRange = new Vector2(-5f, 5f); // Range in the X axis for sun spawn
    public Vector2 zRange = new Vector2(-5f, 5f); // Range in the Z axis for sun spawn
    public float spawnHeight = 10f; // Height at which suns will spawn

    private float timer;
    [SerializeField] private bool isSpawningActive = false;

    public void SetSpawnInterval(float seconds)
    {
        if (seconds > 0f) spawnInterval = seconds;
    }

    /// <summary>Turns the sky-photon rain on. Also used to resume after a tutorial pause.</summary>
    public void Activate()
    {
        gameObject.SetActive(true);
        isSpawningActive = true;
        timer = startDelay;
    }

    public void Deactivate()
    {
        isSpawningActive = false;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        timer = startDelay;

        TutorialUI.OnSunSpawnerDeactivate += TutorialUI_OnSunSpawnerDeactivate;



    }

    private void Awake()
    {
        TutorialUI.OnSunSpawnerActivate += TutorialUI_OnSunSpawnerActivate;
    }

    private void TutorialUI_OnSunSpawnerDeactivate()
    {
        Deactivate();
    }

    private void TutorialUI_OnSunSpawnerActivate()
    {
        Activate();
    }

    private void OnDestroy()
    {
        TutorialUI.OnSunSpawnerActivate -= TutorialUI_OnSunSpawnerActivate;
        TutorialUI.OnSunSpawnerDeactivate -= TutorialUI_OnSunSpawnerDeactivate;
    }

    private void Update()
    {
        if (!isSpawningActive) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            SpawnSun();
            timer = CurrentSpawnInterval();
        }
    }

    private float CurrentSpawnInterval()
    {
        float multiplier = 1f;
        if (DifficultyDirector.Instance != null)
        {
            multiplier = DifficultyDirector.Instance.SunIntervalMultiplier;
        }

        return Mathf.Max(1.5f, spawnInterval * multiplier);
    }

    private void SpawnSun()
    {
        float xPosition = Random.Range(xRange.x, xRange.y);
        float zPosition = Random.Range(zRange.x, zRange.y);
        Vector3 spawnPosition = new Vector3(xPosition, spawnHeight, zPosition);
        Instantiate(sunPrefab, spawnPosition, Quaternion.identity);
    }
}
