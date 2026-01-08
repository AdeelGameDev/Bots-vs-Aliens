using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnlockedSeedsData
{
    public List<string> unlockedSeedIDs = new List<string>();
}
public class UnlockedSeedsManager : MonoBehaviour
{
    private const string UNLOCKED_SEED_KEY = "UnlockedSeeds";

    [SerializeField] private SeedSO startingSeedSO;
    [SerializeField] private List<SeedSO> allSeeds; // assign all seed assets in Inspector

    private UnlockedSeedsData unlockedSeedsData = new UnlockedSeedsData();

    private void Awake()
    {
        LoadUnlockedSeeds();
    }

    public void UnlockNewSeed(SeedSO seedSO)
    {
        if (!unlockedSeedsData.unlockedSeedIDs.Contains(seedSO.seedID))
        {
            unlockedSeedsData.unlockedSeedIDs.Add(seedSO.seedID);
            SaveUnlockedSeeds();
            Debug.Log($"Unlocked new seed: {seedSO.seedPlantName}");
        }
    }

    private void SaveUnlockedSeeds()
    {
        string json = JsonUtility.ToJson(unlockedSeedsData);
        PlayerPrefs.SetString(UNLOCKED_SEED_KEY, json);
        PlayerPrefs.Save();
        Debug.Log($"Unlocked seeds saved: {json}");
    }

    private void LoadUnlockedSeeds()
    {
        if (PlayerPrefs.HasKey(UNLOCKED_SEED_KEY))
        {
            string json = PlayerPrefs.GetString(UNLOCKED_SEED_KEY);
            unlockedSeedsData = JsonUtility.FromJson<UnlockedSeedsData>(json);
            Debug.Log($"Loaded unlocked seeds: {json}");
        }
        else
        {
            unlockedSeedsData.unlockedSeedIDs.Add(startingSeedSO.seedID);
            SaveUnlockedSeeds();
        }
    }

    public List<SeedSO> GetUnlockedSeeds()
    {
        List<SeedSO> unlocked = new List<SeedSO>();
        foreach (string id in unlockedSeedsData.unlockedSeedIDs)
        {
            SeedSO seed = allSeeds.Find(s => s.seedID == id);
            if (seed != null)
                unlocked.Add(seed);
            else
                Debug.LogError($"No SeedSO found for id: {id}");
        }
        return unlocked;
    }
}
