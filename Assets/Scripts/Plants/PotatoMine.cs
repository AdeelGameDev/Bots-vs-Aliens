using CodeMonkey.HealthSystemCM;
using UnityEngine;

[SelectionBase]
public class PotatoMine : Plant
{
    public float armingTime = 3f;
    public float explosionRadius = 3f;
    public int explosionDamage = 100;
    [SerializeField] private Transform rayCastPoint;
    [SerializeField] private Animator animator;
    //[SerializeField] private ParticleSystem explosionVFX;

    private bool isArmed = false;
    private float armStartTime;

    protected override void Start()
    {
        base.Start();
        armStartTime = Time.time;
    }

    void Update()
    {
        if (!isArmed && Time.time >= armStartTime + armingTime)
        {
            Arm();
        }

        if (isArmed)
        {
            Debug.DrawRay(rayCastPoint.position, Vector3.right * explosionRadius, Color.red);

            if (Physics.Raycast(rayCastPoint.position, Vector3.right, out RaycastHit hit, explosionRadius, zombieLayerMask))
            {
                if (hit.collider.TryGetComponent(out Zombie zombie))
                {
                    if (!isExploded)
                        Explode();
                }
            }
        }
    }

    private bool isExploded = false;

    /// <summary>
    /// Arms the mine. Called by the timer below, and again by the animation event
    /// if the clip has one. The timer path means the mine always works even if the
    /// clip never fires the event.
    /// </summary>
    public void Arm()
    {
        if (isArmed) return;

        isArmed = true;

        if (animator != null)
        {
            animator.SetTrigger("ArmedAndReady");
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.Play("DirtRise");
        }
    }

    // Kept for the animation clip's event, if it has one.
    public void OnArmedAnimationFinished()
    {
        Arm();
    }


    void Explode()
    {
        isExploded = true;

      //  Instantiate(explosionVFX, transform.position, Quaternion.identity);
        AudioManager.Instance.Play("Explosion");

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent(out Zombie zombie))
            {
                HealthSystemComponent healthSystem = collider.GetComponent<HealthSystemComponent>();
                if (healthSystem != null)
                {
                    healthSystem.GetHealthSystem().Damage(explosionDamage);
                }
            }
        }

        Debug.Log("Potato Mine exploded!");
        healthSystemComponent.GetHealthSystem().SetHealth(0);
    }
}
