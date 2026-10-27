using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SeedSlotUI : MonoBehaviour, IPointerDownHandler
{
    public static event Action<SeedSO> OnSeedSelectAction;

    public static event Action OnSeedSelected;
    public static event Action OnSeedUnSelected;

    [SerializeField] private SeedSO seedSO;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI plantNameText;
    [SerializeField] private Image cooldownImage;
    [SerializeField] private Image seedIconImage;
    [SerializeField] private GameObject selectedImage;

    private Button seedButton;
    private float timer = 0f;
    [SerializeField] private bool isSeedAvailable = true;

    private void Awake()
    {
        seedButton = GetComponent<Button>();
        //seedButton.onClick.AddListener(SelectSeed);
        SunCounter.OnSunCountUpdated += SunCounter_OnSunCountUpdated;



    }
    [SerializeField] private bool isSeedSelected = false;


    //Do this when the mouse is clicked over the selectable object this script is attached to.
    public void OnPointerDown(PointerEventData eventData)
    {
        SelectSeed();
    }


    public void SetSeedSO(SeedSO seedSO)
    {
        this.seedSO = seedSO;
    }

    private int sunCount;

    private void Start()
    {
        UpdateValues();

        selectedImage.SetActive(false);
        cooldownImage.gameObject.SetActive(false); // Disable image initially

        // Slots are created at runtime, so the initial broadcast is missed.
        // Pull the current count once to show the right state immediately.
        SunCounter sunCounter = FindObjectOfType<SunCounter>();
        if (sunCounter != null) SunCounter_OnSunCountUpdated(sunCounter.GetSunCount());

        SunCounter.OnSeedSelected += HighlightSelectedSeed;
        GrassPatch.OnPlantPlaced += GrassPatch_OnPlantPlaced; ;
    }

    private void GrassPatch_OnPlantPlaced(SeedSO obj)
    {
        DisableSeedSlot(obj);
        if (seedSO == obj)
        {
            ResetAndActivateCoolDown();
            isSeedAvailable = false;
        }
    }

    private void SunCounter_OnSunCountUpdated(int sunCount)
    {
        //Debug.Log("SunCount : " + sunCount);
        this.sunCount = sunCount;

        if (sunCount >= seedSO.seedCost && isSeedAvailable)
        {
            EnableSeedSlot();
        }
        else
        {
            DisableSeedSlot(seedSO);
        }

    }

    private void OnDestroy()
    {
        SunCounter.OnSeedSelected -= HighlightSelectedSeed;
        GrassPatch.OnPlantPlaced -= GrassPatch_OnPlantPlaced;
        SunCounter.OnSunCountUpdated -= SunCounter_OnSunCountUpdated;

    }

    private void Update()
    {
        if (!isSeedAvailable)
        {
            timer += Time.deltaTime;
            cooldownImage.fillAmount = timer / seedSO.seedRechargeTime; // Update fill amount based on timer
            if (timer >= seedSO.seedRechargeTime)
            {
                EnableSeedSlot();

            }
        }
    }

    private void SelectSeed()
    {
        if (isSeedSelected)
        {
            GameManager.Instance.selectedSeedSo = null;
            isSeedSelected = false;
            OnSeedUnSelected?.Invoke();
            selectedImage.SetActive(false);
            return;
        }

        if (!isSeedAvailable)
        {
            // Seed Timer is filling
            AudioManager.Instance.Play("Buzzer");
            return;
        }

        OnSeedSelectAction?.Invoke(seedSO);
    }

    private void UpdateValues()
    {
        costText.text = $"{seedSO.seedCost}";

        if (seedSO.seedIconImage == null)
        {
            plantNameText.text = seedSO.seedPlantName;
        }
        else
        {
            seedIconImage.sprite = seedSO.seedIconImage;
        }

    }

    private void HighlightSelectedSeed(SeedSO selectedSeed)
    {
        if (seedSO == selectedSeed)
        {
            isSeedSelected = true;
            selectedImage.SetActive(true);
            OnSeedSelected?.Invoke();
        }
        else
        {
            isSeedSelected = false;
            selectedImage.SetActive(false);
        }
    }

    private void DisableSeedSlot(SeedSO plantedSeed)
    {
        if (seedSO == plantedSeed)
        {
            seedButton.interactable = false;
            selectedImage.SetActive(false);
            isSeedSelected = false;



            // isSeedAvailable = false;
        }
    }

    private void ResetAndActivateCoolDown()
    {
        cooldownImage.fillAmount = 0; // Reset fill amount
        timer = 0;
        cooldownImage.gameObject.SetActive(true); // Disable image when seed slot is enabled
    }

    private void DeactivateCoolDown()
    {
        cooldownImage.gameObject.SetActive(false); // Disable image when seed slot is enabled
    }

    private void EnableSeedSlot()
    {
        DeactivateCoolDown();

        isSeedAvailable = true;


        if (sunCount >= seedSO.seedCost)
        {
            seedButton.interactable = true;
        }
        else
        {
            seedButton.interactable = false;
        }
    }
}
