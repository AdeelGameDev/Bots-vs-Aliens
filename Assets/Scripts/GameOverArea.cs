using UnityEngine;

/// <summary>
/// The house line. Letting a single alien through ends the run.
/// Zombies also read <see cref="HouseLineX"/> directly so detection does not
/// depend on physics triggers.
/// </summary>
public class GameOverArea : MonoBehaviour
{
    /// <summary>World X of the house line. Zombies crossing it end the run.</summary>
    public static float HouseLineX { get; private set; } = -9.5f;

    private void Awake()
    {
        HouseLineX = transform.position.x;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Zombie>() == null) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyDefeat();
        }
    }
}
