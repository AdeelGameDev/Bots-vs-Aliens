using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LaneManager laneManager;
    [SerializeField] private SpawnerPointSet spawnerPointSet;
    [SerializeField] private WaveConfigSO[] allLevels;

    private WaveConfigSO currentLevel;



    private void Start()
    {
        SetLevel();
    }


    public WaveConfigSO GetCurrentLevel()
    {
        return currentLevel;
    }

    private void SetLevel()
    {
        currentLevel = allLevels[allLevels.Length - 1];

        laneManager.SetLanes(currentLevel.lanesToUse[0], currentLevel.lanesToUse[1], currentLevel.lanesToUse[2], currentLevel.lanesToUse[3], currentLevel.lanesToUse[4]);
        spawnerPointSet.SetSpawnPoints(currentLevel.lanesToUse[0], currentLevel.lanesToUse[1], currentLevel.lanesToUse[2], currentLevel.lanesToUse[3], currentLevel.lanesToUse[4]);
    }
}
