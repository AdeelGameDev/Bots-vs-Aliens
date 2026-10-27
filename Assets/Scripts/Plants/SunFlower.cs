using UnityEngine;

public class Sunflower : Plant
{
    public GameObject sunPrefab;
    public float generationInterval = 5f;

    [Tooltip("A fresh SolarCore produces its first photon sooner than its steady interval, " +
             "so the purchase pays off before the next wave.")]
    [SerializeField] private float firstGenerationDelay = 8f;

    [SerializeField] private Transform sunSpawnPoint;
    private SunflowerAnimator animator;
    private float timer;


    protected override void Start()
    {
        base.Start();
        float firstDelay = firstGenerationDelay > 0f ? firstGenerationDelay : generationInterval;
        timer = Mathf.Min(firstDelay, generationInterval);
        animator = GetComponent<SunflowerAnimator>();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            GenerateSun();
            timer = generationInterval;
        }
    }

    private void GenerateSun()
    {
        Instantiate(sunPrefab, sunSpawnPoint.position, Quaternion.identity);

        // Play produce animation
        if (animator != null)
            animator.PlayProduce();
    }
}
