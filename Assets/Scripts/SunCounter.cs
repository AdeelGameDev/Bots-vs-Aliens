using System;
using UnityEngine;

public class SunCounter : MonoBehaviour
{
    public static event Action<int> OnSunCountUpdated;
    public static event Action<SeedSO, GrassPatch> OnSeedPurchased;
    public static event Action<SeedSO> OnSeedSelected;


    [SerializeField] private int sunCount;
    [SerializeField] private int startingSunCount;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sunCount = startingSunCount;
        OnSunCountUpdated?.Invoke(sunCount);

        Sun.OnSunCollected += Sun_OnSunCollected;
        SeedSlotUI.OnSeedSelectAction += SeedSlotUI_OnSeedSelectedAction;
        GrassPatch.OnSeedPurchaseAction += GrassPatch_OnSeedPurchaseAction;
    }

    private void GrassPatch_OnSeedPurchaseAction(SeedSO obj, GrassPatch grassPatch)
    {
        if (obj.seedCost <= sunCount)
        {
            ReduceSunCount(obj.seedCost);
            OnSeedPurchased?.Invoke(obj, grassPatch);
            OnSunCountUpdated?.Invoke(sunCount);
        }

    }





    private void SeedSlotUI_OnSeedSelectedAction(SeedSO seedSO)
    {
        SelectSeedIfPurchaseable(seedSO);
    }

    private void SelectSeedIfPurchaseable(SeedSO seedSO)
    {
        if (sunCount >= seedSO.seedCost)
        {
            OnSeedSelected?.Invoke(seedSO);
            AudioManager.Instance.Play("SeedLift");
        }
        else
        {
            AudioManager.Instance.Play("Buzzer");
            OnSeedNotPurchasable?.Invoke();
        }
    }

    public static event Action OnSeedNotPurchasable;

    private void Sun_OnSunCollected(int sunValue)
    {
        sunCount += sunValue;
        OnSunCountUpdated?.Invoke(sunCount);
    }

    public void ReduceSunCount(int count)
    {
        sunCount -= count;
        OnSunCountUpdated?.Invoke(sunCount);
    }





    private void OnDestroy()
    {
        Sun.OnSunCollected -= Sun_OnSunCollected;
        SeedSlotUI.OnSeedSelectAction -= SeedSlotUI_OnSeedSelectedAction;
        GrassPatch.OnSeedPurchaseAction -= GrassPatch_OnSeedPurchaseAction;
    }
}
