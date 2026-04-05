using CodeMonkey.HealthSystemCM;
using UnityEngine;

public class Squash : Plant
{
    [SerializeField] private Transform rayCastPoint;
    [SerializeField] protected float distance;
    private CrushPodAnimator animator;

    private string[] squashSounds = { "Squash1", "Squash2" };
    private bool firstTime = true;
    private Zombie zombie;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<CrushPodAnimator>();
    }

    private void Update()
    {
        // Check both directions
        if (CheckForZombie(Vector3.right) || CheckForZombie(Vector3.left))
        {
            if (firstTime && zombie != null)
            {
                AudioManager.Instance.Play(squashSounds[Random.Range(0, squashSounds.Length)]);

                animator.PlayAttack(zombie.transform.position, () =>
                {
                    KillZombie();
                });

                firstTime = false;
            }
        }

        // Debug rays
        Debug.DrawRay(rayCastPoint.position, Vector3.right * distance, Color.red);
        Debug.DrawRay(rayCastPoint.position, Vector3.left * distance, Color.red);
    }

    private bool CheckForZombie(Vector3 direction)
    {
        if (Physics.Raycast(rayCastPoint.position, direction, out RaycastHit hit, distance, zombieLayerMask))
        {
            if (hit.collider.TryGetComponent(out zombie))
            {
                return true;
            }
        }
        return false;
    }

    public void DamagePlant()
    {
        healthSystemComponent.GetHealthSystem().SetHealth(0);
    }

    public void KillZombie()
    {
        if (zombie != null)
        {
            zombie.GetComponent<HealthSystemComponent>().GetHealthSystem().Damage(1000);
        }
    }
}
