using UnityEngine;
using UnityEngine.UI;

public class RemoveButtonUI : MonoBehaviour
{
    public static bool removeToolSelected;
    private Button removeButton;
    private Outline buttonOutline;

    private void Awake()
    {
        removeButton = GetComponent<Button>();
        buttonOutline = GetComponent<Outline>();

        removeButton.onClick.AddListener(ToggleRemoveTool);
    }

    private void Start()
    {
        // Static tool state must not leak across a retry or the next level.
        removeToolSelected = false;

        if (PlayerPrefs.GetInt("Level") == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        UpdateVisual();
        GrassPatch.OnPlantRemoved += GrassPatch_OnPlantRemoved;


    }

    private void ToggleRemoveTool()
    {
        removeToolSelected = !removeToolSelected;
        AudioManager.Instance.Play("Shovel");
        UpdateVisual();
    }

    private void GrassPatch_OnPlantRemoved()
    {
        removeToolSelected = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (buttonOutline != null)
        {
            buttonOutline.enabled = removeToolSelected;
        }
    }

    private void OnDestroy()
    {
        GrassPatch.OnPlantRemoved -= GrassPatch_OnPlantRemoved;
    }
}
