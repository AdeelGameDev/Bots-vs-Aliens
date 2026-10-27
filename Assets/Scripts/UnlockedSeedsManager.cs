using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnlockedSeedsData
{
    public List<string> unlockedSeedIDs = new List<string>();
}

/// <summary>
/// PlayerPrefs-backed collection of bots the player has permanently unlocked.
/// </summary>
public class UnlockedSeedsManager : MonoBehaviour
{
    private const string UNLOCKED_SEED_KEY = "UnlockedSeeds";

    [Header("Setup")]
    [SerializeField] private SeedSO startingSeedSO;
    [SerializeField] private List<SeedSO> allSeeds;

    [Header("Debug")]
    [SerializeField] private bool unlockAllSeedsForTesting = false;

    private UnlockedSeedsData unlockedSeedsData = new UnlockedSeedsData();

    private void Awake()
    {
        LoadUnlockedSeeds();

        if (unlockAllSeedsForTesting)
        {
            UnlockAllSeeds();
        }
    }

    public void UnlockNewSeed(SeedSO seedSO)
    {
        if (seedSO == null) return;

        string id = Normalise(seedSO.seedID);
        if (string.IsNullOrEmpty(id)) return;

        if (!Contains(id))
        {
            unlockedSeedsData.unlockedSeedIDs.Add(id);
            SaveUnlockedSeeds();
            Debug.Log("Unlocked new seed: " + seedSO.seedPlantName);
        }
    }

    public void UnlockSeeds(IEnumerable<SeedSO> seeds)
    {
        if (seeds == null) return;

        bool changed = false;
        foreach (SeedSO seed in seeds)
        {
            if (seed == null) continue;

            string id = Normalise(seed.seedID);
            if (string.IsNullOrEmpty(id) || Contains(id)) continue;

            unlockedSeedsData.unlockedSeedIDs.Add(id);
            changed = true;
        }

        if (changed) SaveUnlockedSeeds();
    }

    public bool HasSeed(SeedSO seedSO)
    {
        return seedSO != null && Contains(Normalise(seedSO.seedID));
    }

    public List<SeedSO> GetAllSeeds()
    {
        return allSeeds != null ? new List<SeedSO>(allSeeds) : new List<SeedSO>();
    }

    private bool Contains(string normalisedID)
    {
        foreach (string id in unlockedSeedsData.unlockedSeedIDs)
        {
            if (Normalise(id) == normalisedID) return true;
        }

        return false;
    }

    private static string Normalise(string id)
    {
        return id == null ? string.Empty : id.Trim();
    }

    private void UnlockAllSeeds()
    {
        unlockedSeedsData.unlockedSeedIDs.Clear();

        if (allSeeds != null)
        {
            foreach (SeedSO seed in allSeeds)
            {
                if (seed == null) continue;
                string id = Normalise(seed.seedID);
                if (!string.IsNullOrEmpty(id) && !unlockedSeedsData.unlockedSeedIDs.Contains(id))
                {
                    unlockedSeedsData.unlockedSeedIDs.Add(id);
                }
            }
        }

        SaveUnlockedSeeds();
        Debug.Log("All seeds unlocked (TEST MODE)");
    }

    private void SaveUnlockedSeeds()
    {
        string json = JsonUtility.ToJson(unlockedSeedsData);
        PlayerPrefs.SetString(UNLOCKED_SEED_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadUnlockedSeeds()
    {
        unlockedSeedsData = new UnlockedSeedsData();

        if (PlayerPrefs.HasKey(UNLOCKED_SEED_KEY))
        {
            string json = PlayerPrefs.GetString(UNLOCKED_SEED_KEY);
            UnlockedSeedsData loaded = JsonUtility.FromJson<UnlockedSeedsData>(json);
            if (loaded != null && loaded.unlockedSeedIDs != null)
            {
                unlockedSeedsData = loaded;
            }
        }

        // Tidy up ids saved with stray whitespace and guarantee the starter bot.
        for (int i = 0; i < unlockedSeedsData.unlockedSeedIDs.Count; i++)
        {
            unlockedSeedsData.unlockedSeedIDs[i] = Normalise(unlockedSeedsData.unlockedSeedIDs[i]);
        }

        if (startingSeedSO != null)
        {
            string starterID = Normalise(startingSeedSO.seedID);
            if (!string.IsNullOrEmpty(starterID) && !Contains(starterID))
            {
                unlockedSeedsData.unlockedSeedIDs.Add(starterID);
            }
        }

        SaveUnlockedSeeds();
    }

    public List<SeedSO> GetUnlockedSeeds()
    {
        List<SeedSO> unlocked = new List<SeedSO>();
        if (allSeeds == null) return unlocked;

        foreach (string id in unlockedSeedsData.unlockedSeedIDs)
        {
            string normalised = Normalise(id);
            SeedSO seed = allSeeds.Find(s => s != null && Normalise(s.seedID) == normalised);

            if (seed != null)
            {
                if (!unlocked.Contains(seed)) unlocked.Add(seed);
            }
            else
            {
                Debug.LogWarning("No SeedSO found for id: " + id);
            }
        }

        return unlocked;
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UNLOCKED_SEED_KEY);
        PlayerPrefs.Save();
        LoadUnlockedSeeds();
        Debug.Log("Seed progress reset");
    }
}
