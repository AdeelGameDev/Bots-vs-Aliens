using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerPointSet : MonoBehaviour
{
    public static event Action<List<Transform>> OnSpawnPointsSet;

    public List<Transform> currentSpawnPoints = new List<Transform>();
    public Transform[] allSpawnPoints; // One entry per lane, index matches lanesToUse

    private WaveConfigSO activeConfig;

    public void SetSpawnPoints(params int[] inputLanes)
    {
        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            Debug.LogError("SpawnerPointSet has no spawn points assigned.");
            return;
        }

        currentSpawnPoints.Clear();

        int laneCount = Mathf.Min(inputLanes != null ? inputLanes.Length : 0, allSpawnPoints.Length);
        for (int i = 0; i < laneCount; i++)
        {
            if (inputLanes[i] == 1 && allSpawnPoints[i] != null)
            {
                currentSpawnPoints.Add(allSpawnPoints[i]);
            }
        }

        OnSpawnPointsSet?.Invoke(currentSpawnPoints);
    }

    public void SetConfig(WaveConfigSO config)
    {
        activeConfig = config;
    }

    public bool IsLaneActive(int lane)
    {
        if (activeConfig != null) return activeConfig.IsLaneActive(lane);
        if (allSpawnPoints == null || lane < 0 || lane >= allSpawnPoints.Length) return false;
        return allSpawnPoints[lane] != null && currentSpawnPoints.Contains(allSpawnPoints[lane]);
    }

    /// <summary>Absolute lane spawn point, or null when that lane is closed.</summary>
    public Transform GetSpawnPoint(int lane)
    {
        if (allSpawnPoints == null || lane < 0 || lane >= allSpawnPoints.Length) return null;
        if (!IsLaneActive(lane)) return null;
        return allSpawnPoints[lane];
    }

    public List<int> GetActiveLaneIndices()
    {
        List<int> lanes = new List<int>();
        if (allSpawnPoints == null) return lanes;

        for (int i = 0; i < allSpawnPoints.Length; i++)
        {
            if (IsLaneActive(i) && allSpawnPoints[i] != null) lanes.Add(i);
        }

        return lanes;
    }
}
