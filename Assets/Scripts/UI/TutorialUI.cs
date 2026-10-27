using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    private enum TutorialStep
    {
        TapOnSeedPacket,
        PlantOnGrass,
        NicelyDone,
        EnableSunSpawner,
        KeepCollecting,
        YouCollectedEnough,
        PlantAgain,
        DonLetZombie,
    }

    public static event Action OnSunSpawnerActivate;
    public static event Action OnSunSpawnerDeactivate;
    public static event Action OnZombieSpawnerActivate;
    public static event Action OnZombieSpawnerDeactivate;
    public static event Action OnTutorialIndicatorActivate;
    public static event Action OnTutorialIndicatorDeactivate;

    private const string TAP_ON_SEED_PACKET = "Tap on a seed packet to pick it up!";
    private const string PLANT_ON_GRASS = "Tap on the grass to plant your seed!";
    private const string NICELY_DONE = "Nicely done! You can also drag and drop the seed packets";
    private const string TAP_ON_THE_FALLING_SUN = "Tap on the falling sun to collect it";
    private const string KEEP_ON_COLLECTING = "Keep on collecting sun";
    private const string EXCELLENT_YOU_COLLECTED_ENOUGH = "Excellent! You collected enough for the next plant";
    private const string TAP_ON_THE_PEASHOOTER = "Tap on the peashooter to plant again";
    private const string DONT_LET_ZOMBIE = "Don't let the zombie reach your house";

    [SerializeField] private GameObject arrow;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private GameObject infoTextParent;
    [SerializeField] private GameObject interactivityBlocker;

    private TutorialStep currentStep;

    private void Start()
    {
        interactivityBlocker.SetActive(false);

        if (!IsTutorialLevel())
        {
            OnSunSpawnerActivate?.Invoke();
            Destroy(gameObject);
            return;
        }

        UpdateTutorialStep(TutorialStep.TapOnSeedPacket);

        SeedSlotUI.OnSeedSelected += HandleSeedSelected;
        SeedSlotUI.OnSeedUnSelected += HandleSeedUnSelected;
        GrassPatch.OnPlantPlaced += HandlePlantPlaced;
        SunCounter.OnSunCountUpdated += SunCounter_OnSunCountUpdated;

        StartCoroutine(InitialDeactivate());
    }

    /// <summary>The level asset decides whether this is the tutorial, not a magic index.</summary>
    private static bool IsTutorialLevel()
    {
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null && levelManager.CurrentLevel != null)
        {
            return levelManager.CurrentLevel.isTutorial;
        }

        return PlayerPrefs.GetInt(LevelManager.LEVEL_PREF_KEY, 0) == 0;
    }

    private IEnumerator InitialDeactivate()
    {
        yield return new WaitForSeconds(0.1f);
        OnSunSpawnerDeactivate?.Invoke();
        OnZombieSpawnerDeactivate?.Invoke();
    }

    private void SunCounter_OnSunCountUpdated(int count)
    {
        if (currentStep == TutorialStep.EnableSunSpawner && infoText.text == TAP_ON_THE_FALLING_SUN)
        {
            UpdateTutorialStep(TutorialStep.KeepCollecting);
        }

        if (currentStep == TutorialStep.KeepCollecting && count >= 100)
        {
            UpdateTutorialStep(TutorialStep.YouCollectedEnough);
        }
    }

    private void HandlePlantPlaced(SeedSO seed)
    {
        if (currentStep == TutorialStep.PlantOnGrass)
        {
            UpdateTutorialStep(TutorialStep.NicelyDone);
        }
        if (currentStep == TutorialStep.PlantAgain)
        {
            UpdateTutorialStep(TutorialStep.DonLetZombie);
        }

        OnTutorialIndicatorDeactivate?.Invoke();
    }

    private void HandleSeedUnSelected()
    {
        if (currentStep == TutorialStep.PlantOnGrass)
        {
            UpdateTutorialStep(TutorialStep.TapOnSeedPacket);
        }
        if (currentStep == TutorialStep.PlantAgain)
        {
            arrow.gameObject.SetActive(true);
            infoTextParent.SetActive(true);
        }

        OnTutorialIndicatorDeactivate?.Invoke();
    }

    private void HandleSeedSelected()
    {
        if (currentStep == TutorialStep.TapOnSeedPacket)
        {
            UpdateTutorialStep(TutorialStep.PlantOnGrass);
        }

        if (currentStep == TutorialStep.PlantAgain)
        {
            arrow.gameObject.SetActive(false);
            infoTextParent.SetActive(false);
        }
    }

    private void UpdateTutorialStep(TutorialStep step)
    {
        currentStep = step;

        switch (step)
        {
            case TutorialStep.TapOnSeedPacket:
                infoText.text = TAP_ON_SEED_PACKET;
                arrow.SetActive(true);
                break;

            case TutorialStep.PlantOnGrass:
                infoText.text = PLANT_ON_GRASS;
                OnTutorialIndicatorActivate?.Invoke();
                arrow.SetActive(false);
                break;

            case TutorialStep.NicelyDone:
                StartCoroutine(NicelyDoneCoroutine());
                break;

            case TutorialStep.EnableSunSpawner:
                OnSunSpawnerActivate?.Invoke();
                infoText.text = TAP_ON_THE_FALLING_SUN;
                break;

            case TutorialStep.KeepCollecting:
                infoText.text = KEEP_ON_COLLECTING;
                break;

            case TutorialStep.YouCollectedEnough:
                StartCoroutine(CollectedEnoughCoroutine());
                break;

            case TutorialStep.PlantAgain:
                interactivityBlocker.SetActive(false);
                infoText.text = TAP_ON_THE_PEASHOOTER;
                arrow.SetActive(true);
                break;

            case TutorialStep.DonLetZombie:
                infoTextParent.gameObject.SetActive(true);
                infoText.text = DONT_LET_ZOMBIE;
                Invoke(nameof(EnableZombieSpanwer), 2);
                break;
        }
    }

    private IEnumerator NicelyDoneCoroutine()
    {
        infoText.text = NICELY_DONE;
        arrow.SetActive(false);
        yield return new WaitForSeconds(2f);
        UpdateTutorialStep(TutorialStep.EnableSunSpawner);
    }

    private IEnumerator CollectedEnoughCoroutine()
    {
        interactivityBlocker.SetActive(true);
        infoText.text = EXCELLENT_YOU_COLLECTED_ENOUGH;
        yield return new WaitForSeconds(2f);
        UpdateTutorialStep(TutorialStep.PlantAgain);
    }

    private void EnableZombieSpanwer()
    {
        infoTextParent.gameObject.SetActive(false);
        OnZombieSpawnerActivate?.Invoke();
    }
    private void OnDestroy()
    {
        SeedSlotUI.OnSeedSelected -= HandleSeedSelected;
        SeedSlotUI.OnSeedUnSelected -= HandleSeedUnSelected;
        SunCounter.OnSunCountUpdated -= SunCounter_OnSunCountUpdated;
        GrassPatch.OnPlantPlaced -= HandlePlantPlaced;
    }
}
