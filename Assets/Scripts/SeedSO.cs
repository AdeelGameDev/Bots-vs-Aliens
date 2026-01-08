using UnityEngine;

[CreateAssetMenu(fileName = "SeedSO", menuName = "Scriptable Objects/SeedSO")]
public class SeedSO : ScriptableObject
{
    public string seedID;          // unique identifier (e.g., "sunflower", "peashooter")
    public Plant plant;
    public int seedCost;
    public string seedPlantName;
    public float seedRechargeTime;
    public Sprite seedIconImage;
}