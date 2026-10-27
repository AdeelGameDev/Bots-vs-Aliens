using CodeMonkey.HealthSystemCM;
using UnityEngine;

public class Pea : MonoBehaviour
{
    private string[] splatSound = { "Splat1", "Splat2", "Splat3" };
    [SerializeField] private GameObject peahShootVFX;

    private void Start()
    {
        Destroy(gameObject, 5);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Zombie zombie))
        {
            zombie.GetComponent<HealthSystemComponent>().GetHealthSystem().Damage(15);
            AudioManager.Instance.Play(splatSound[Random.Range(0, splatSound.Length)]);
            Instantiate(peahShootVFX, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);

        }
    }
}
