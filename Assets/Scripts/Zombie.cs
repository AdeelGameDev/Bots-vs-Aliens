using CodeMonkey.HealthSystemCM;
using System;
using System.Collections;
using UnityEngine;

[SelectionBase]
public class Zombie : MonoBehaviour
{
    [SerializeField] private float minSpeed = 0.5f; // Minimum speed range for zombies
    [SerializeField] private float maxSpeed = 2f; // Maximum speed range for zombies
    private float speed; // Current speed of the zombie
    public float damage = 10f;
    public float attackDelay = 1f;
    private bool isMoving = true;
    private bool isAttacking = false;
    private HealthSystemComponent targetPlant = null;
    [SerializeField] private float rayCastDistance;
    private HealthSystemComponent healthSystemComponent;
    [SerializeField] private Animator animator; // Reference to the Animator component
    [SerializeField] private LayerMask zombieLayerMask;
    [SerializeField] private GameObject attachedItem;

    private float baseDamage;
    private float speedMultiplier = 1f;
    private float damageMultiplier = 1f;
    private bool healthMultiplierApplied;
    private bool hasDroppedItem;

    /// <summary>Raised the instant the zombie dies. Use this for wave tracking.</summary>
    public event Action<Zombie> OnKilled;

    /// <summary>Raised once the corpse has been cleaned up (kept for older code).</summary>
    public event Action OnDeath;

    private void Awake()
    {
        healthSystemComponent = GetComponent<HealthSystemComponent>();
        baseDamage = damage;
    }

    /// <summary>
    /// Applies the level's difficulty ramp to this alien. Called by the spawner
    /// immediately after instantiation, before Start().
    /// </summary>
    public void ApplyDifficulty(float healthScale, float speedScale, float damageScale)
    {
        if (healthSystemComponent == null) healthSystemComponent = GetComponent<HealthSystemComponent>();

        if (!healthMultiplierApplied && healthSystemComponent != null)
        {
            healthMultiplierApplied = true;

            if (healthScale > 0f && !Mathf.Approximately(healthScale, 1f))
            {
                CodeMonkey.HealthSystemCM.HealthSystem health = healthSystemComponent.GetHealthSystem();
                if (health != null) health.SetHealthMax(health.GetHealthMax() * healthScale, true);
            }
        }

        speedMultiplier = Mathf.Max(0.05f, speedScale);
        damageMultiplier = Mathf.Max(0.05f, damageScale);
    }

    private void Start()
    {
        // Assign a random speed within the specified range
        speed = UnityEngine.Random.Range(minSpeed, maxSpeed) * speedMultiplier;
        damage = baseDamage * damageMultiplier;

        // Randomize the animation start time
        RandomizeAnimationStart();

        healthSystemComponent.GetHealthSystem().OnDead += Zombie_OnDead;
        healthSystemComponent.GetHealthSystem().OnHealthChanged += Zombie_OnHealthChanged;
    }

    private void RandomizeAnimationStart()
    {
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            animator.Play(stateInfo.fullPathHash, -1, UnityEngine.Random.Range(0f, 1f));
        }
    }

    private void Zombie_OnHealthChanged(object sender, EventArgs e)
    {
        if (hasDroppedItem || attachedItem == null) return;

        CodeMonkey.HealthSystemCM.HealthSystem health = healthSystemComponent.GetHealthSystem();
        if (health.GetHealthNormalized() <= 0.66f)
        {
            hasDroppedItem = true;
            attachedItem.transform.SetParent(null);
            Rigidbody rb = attachedItem.AddComponent<Rigidbody>();

            rb.AddExplosionForce(1000, attachedItem.transform.position, 3);
            Destroy(attachedItem, 2);
        }
    }

    private void Zombie_OnDead(object sender, EventArgs e)
    {
        OnKilled?.Invoke(this);
        animator.SetTrigger("OnDeath");
        GetComponent<BoxCollider>().enabled = false;
        isMoving = false; // Stop moving when dead
        StopAllCoroutines(); // Stop all coroutines when dead
        Invoke(nameof(DestroyGameObject), 4);
    }

    private void DestroyGameObject()
    {
        Destroy(gameObject);
        OnDeath?.Invoke();
    }

    private void Update()
    {
        if (!healthSystemComponent.GetHealthSystem().IsDead())
        {
            // The house line is defined by the GameOverArea object in the scene.
            // Checking it here is reliable even though zombies have no Rigidbody.
            if (transform.position.x <= GameOverArea.HouseLineX)
            {
                isMoving = false;
                if (GameManager.Instance != null) GameManager.Instance.NotifyDefeat();
                return;
            }

            if (isMoving)
            {
                transform.Translate(Vector3.forward * speed * Time.deltaTime);
                animator.SetBool("IsWalking", true); // Set the walking animation
            }
            else
            {
                animator.SetBool("IsWalking", false); // Stop the walking animation
            }

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, rayCastDistance, ~zombieLayerMask))
            {
                Debug.DrawRay(transform.position, transform.forward * rayCastDistance, Color.red);

                if (hit.collider.gameObject.TryGetComponent(out HealthSystemComponent healthSystem))
                {
                    isMoving = false;
                    targetPlant = healthSystem;
                }
            }
            else
            {
                Debug.DrawRay(transform.position, transform.forward * rayCastDistance, Color.green);
                isMoving = true;
            }

            if (!isMoving && targetPlant != null && !isAttacking)
            {
                StartCoroutine(AttackPlant());
            }
        }
    }

    IEnumerator AttackPlant()
    {
        isAttacking = true;
        animator.SetBool("IsAttacking", true); // Start the attack animation

        if (targetPlant == null || targetPlant.GetHealthSystem().IsDead())
        {
            isMoving = true;
            animator.SetBool("IsAttacking", false); // Stop the attack animation
        }
        else
        {
            targetPlant.GetHealthSystem().Damage(damage);
            yield return new WaitForSeconds(attackDelay);
        }

        isAttacking = false;
        animator.SetBool("IsAttacking", false); // Stop the attack animation
    }
}
