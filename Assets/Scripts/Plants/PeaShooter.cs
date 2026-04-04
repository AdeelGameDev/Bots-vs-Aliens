using System.Collections;
using UnityEngine;

public class PeaShooter : Plant
{
    public GameObject peaPrefab;
    public Transform spawnPoint;
    public float fireInterval = 2f;
    public float fixedDistanceX = 10f;
    public float maxRaycastDistance = 10f;
    public Color rayColor = Color.red;

    private string[] firePeaSound = { "Throw1", "Throw2" };
    private float nextFireTime;

    [Header("Animation")]
    public CannonAnimator cannonAnimator; // reference to animator

    protected override void Start()
    {
        base.Start();
        StartCoroutine(FirePea());
    }

    private IEnumerator FirePea()
    {
        while (true)
        {
            if (Time.time > nextFireTime)
            {
                RaycastHit hit;
                Vector3 direction = spawnPoint.forward;

                float adjustedRaycastDistance = maxRaycastDistance;
                float spawnPointX = spawnPoint.position.x;
                float distanceToFixedX = Mathf.Abs(spawnPointX - fixedDistanceX);
                adjustedRaycastDistance = Mathf.Clamp(maxRaycastDistance - distanceToFixedX, 0f, maxRaycastDistance);

                if (Physics.Raycast(spawnPoint.position, direction, out hit, adjustedRaycastDistance, zombieLayerMask))
                {
                    Debug.DrawRay(spawnPoint.position, direction * adjustedRaycastDistance, rayColor);

                    if (hit.collider.GetComponent<Zombie>() != null)
                    {
                        // Stop idle and point forward
                        cannonAnimator.StopIdleAndPointForward();

                        // Fire projectile
                        GameObject pea = Instantiate(peaPrefab, spawnPoint.position, spawnPoint.localRotation);
                        AudioManager.Instance.Play(firePeaSound[Random.Range(0, firePeaSound.Length)]);
                        pea.GetComponent<Rigidbody>().linearVelocity = spawnPoint.forward * 10f;

                        // Play shoot animation
                        cannonAnimator.PlayShoot();

                        nextFireTime = Time.time + fireInterval;
                    }
                }
                else
                {
                    Debug.DrawRay(spawnPoint.position, direction * adjustedRaycastDistance, Color.gray);

                    // Resume idle if no zombie in range
                    cannonAnimator.PlayIdle();
                }
            }
            yield return null;
        }
    }
}
