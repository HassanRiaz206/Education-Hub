using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectionPanel : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public Button selectButton;
    public GameObject studyGroupPanel;
    public GameObject createGroupPanel;
    public GameObject yourGroupPanel;

    void Start()
    {
        selectButton.onClick.AddListener(OnSelectButtonPressed);
    }

    void OnSelectButtonPressed()
    {
        if (dropdown.value == 1) // "Create a new group"
        {
            studyGroupPanel.SetActive(false);
            createGroupPanel.SetActive(true);
        }
        else if (dropdown.value == 2) // "Your Groups"
        {
            studyGroupPanel.SetActive(false);
            yourGroupPanel.SetActive(true);
        }
    }
}
