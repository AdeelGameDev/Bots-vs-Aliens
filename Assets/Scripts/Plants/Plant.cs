using CodeMonkey.HealthSystemCM;
using System;
using UnityEngine;

public abstract class Plant : MonoBehaviour, IPlantable
{
    protected HealthSystemComponent healthSystemComponent;
    [SerializeField] private Sun dropSun;
    [SerializeField] private Transform dropSunSpawnPoint;
    public virtual event Action<IPlantable> OnPlantDead;
    public LayerMask zombieLayerMask;

    protected virtual void Awake()
    {
        healthSystemComponent = GetComponent<HealthSystemComponent>();
    }

    /// <summary>
    /// Salvage photons when the player shovels a bot. Deliberately a partial
    /// refund: a full refund would let a damaged wall be replanted for free.
    /// </summary>
    public virtual void DropSun(int sunCount = 1)
    {
        if (dropSun == null) return;

        for (int i = 0; i < sunCount; i++)
        {
            float offset = sunCount <= 1 ? 0f : Mathf.Lerp(-0.25f, 0.25f, i / (float)(sunCount - 1));
            Instantiate(dropSun, transform.position + new Vector3(offset, 0, 0), Quaternion.identity);
        }
    }


    protected virtual void Start()
    {
        healthSystemComponent.GetHealthSystem().OnDead += OnDead;
    }


    protected void OnDead(object sender, EventArgs e)
    {
        OnPlantDead?.Invoke(this);
        //DropSun();
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }


}
