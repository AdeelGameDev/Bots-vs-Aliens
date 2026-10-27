using System;
using UnityEngine;

public class GrassPatch : MonoBehaviour, IOnMouseUpInteractable 


{
    [SerializeField] private Transform spawnPoint;

    private IPlantable plantable;
    private SeedSO plantedSeed;
    public static event Action<SeedSO, GrassPatch> OnSeedPurchaseAction;

    private void Start()
    {
        SunCounter.OnSeedPurchased += SunCounter_OnSeedPurchased;
    }

    private void Plant_OnPlantRemoved(IPlantable obj)
    {
        if (obj == plantable)
            RemovePlant(false);
    }

    private void SunCounter_OnSeedPurchased(SeedSO seed, GrassPatch grassPatch)
    {
        if (this == grassPatch)
        {
            plantable = Instantiate(seed.plant, spawnPoint.position, Quaternion.identity).GetComponent<IPlantable>();
            plantedSeed = seed;
            plantable.OnPlantDead += Plant_OnPlantRemoved;
            AudioManager.Instance.Play("Plant");
            OnPlantPlaced?.Invoke(seed);
        }
    }

    public static event Action<SeedSO> OnPlantPlaced;

    public void OnMouseDownInteraction()
    {
        
    }

    public bool ContainPlant()
    {
        return plantable != null;
    }

    public static event Action OnPlantRemoved;

    /// <summary>Raised only when a bot is destroyed by aliens (not when shoveled).</summary>
    public static event Action OnPlantLost;

    public void RemovePlant(bool dropSun)
    {
        if (plantable == null) return;

        plantable.OnPlantDead -= Plant_OnPlantRemoved;

        Plant plant = plantable as Plant;
        if (plant != null)
        {
            if (dropSun)
            {
                // Partial salvage keeps the shovel useful without turning it into
                // a free full heal for expensive bots.
                int refundSuns = plantedSeed == null
                    ? 0
                    : (plantedSeed.seedCost >= 100 ? 2 : (plantedSeed.seedCost >= 50 ? 1 : 0));
                if (refundSuns > 0) plant.DropSun(refundSuns);
            }
            else
            {
                OnPlantLost?.Invoke();
            }
        }

        Destroy(plantable.GetGameObject());
        plantable = null;
        plantedSeed = null;
        OnPlantRemoved?.Invoke();
    }

    public void OnMouseUpInteraction()
    {
        if (!ContainPlant())
        {
            if (GameManager.Instance.GetSelectedSeedSO() == null)
                return;
            OnSeedPurchaseAction?.Invoke(GameManager.Instance.GetSelectedSeedSO(), this);
        }
        else
        {
            if (RemoveButtonUI.removeToolSelected)
                RemovePlant(true);
        }
    }

    private void OnDestroy()
    {
        SunCounter.OnSeedPurchased -= SunCounter_OnSeedPurchased;
    }
}
