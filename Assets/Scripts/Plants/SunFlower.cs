using UnityEngine;

public class Sunflower : Plant
{
    public GameObject sunPrefab;
    public float generationInterval = 5f;
    [SerializeField] private Transform sunSpawnPoint;
    private SunflowerAnimator animator;
    private float timer;


    protected override void Start()
    {
        base.Start();
        timer = generationInterval;
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
