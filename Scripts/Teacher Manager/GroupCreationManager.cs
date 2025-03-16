using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class GroupCreationManager : MonoBehaviour
{
    // Panels
    public GameObject groupCreationPanel;   // Panel for group creation input fields
    public GameObject groupCreationPopup;     // Popup to show success
    public GameObject studyGroupPanel;        // Panel that is enabled after group creation

    // Input Fields
    public TMP_InputField groupNameInput;
    public TMP_InputField groupGradeInput;
    public TMP_InputField groupSubjectInput;

    private DatabaseReference databaseReference;

    void Start()
    {
        // Initialize Firebase Database
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
            }
            else
            {
                Debug.LogError("Firebase dependencies not met: " + dependencyStatus);
            }
        });

        // Set initial panel states
        groupCreationPopup.SetActive(false);
        studyGroupPanel.SetActive(false);
    }

    // Called when the teacher presses the Create button
    public void OnCreateGroup()
    {
        string groupName = groupNameInput.text.Trim();
        string groupGrade = groupGradeInput.text.Trim();
        string groupSubject = groupSubjectInput.text.Trim();

        // Validate all fields are filled
        if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(groupGrade) || string.IsNullOrEmpty(groupSubject))
        {
            Debug.LogWarning("Please fill in all group details.");
            return;
        }

        // Get teacher ID from the login process via UserSession
        string teacherId = UserSession.teacherId;
        if (string.IsNullOrEmpty(teacherId))
        {
            Debug.LogError("Teacher ID is not set. Please log in properly.");
            return;
        }

        // Prepare group data dictionary
        Dictionary<string, object> groupData = new Dictionary<string, object>()
        {
            { "groupName", groupName },
            { "groupGrade", groupGrade },
            { "groupSubject", groupSubject }
        };

        // Store group data under "teacherGroups/teacherId/..."
        databaseReference.Child("teacherGroups").Child(teacherId).Push().SetValueAsync(groupData)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Debug.Log("Group created successfully for teacher: " + teacherId);
                    // Disable group creation panel, show success popup, then enable study group panel
                    StartCoroutine(ShowPopupAndSwitchPanel());
                }
                else
                {
                    Debug.LogError("Error saving group data: " + task.Exception);
                }
            });
    }

    // Coroutine: disable the creation panel, show popup for 1 second,
    // then enable the study group panel and reset the input fields.
    IEnumerator ShowPopupAndSwitchPanel()
    {
        groupCreationPanel.SetActive(false);  // Hide group creation panel first
        groupCreationPopup.SetActive(true);     // Show popup
        yield return new WaitForSeconds(1f);
        groupCreationPopup.SetActive(false);    // Hide popup after 1 second
        studyGroupPanel.SetActive(true);        // Enable study group panel
        ResetInputFields();                     // Reset input fields for future use
    }

    // Resets all group creation input fields
    void ResetInputFields()
    {
        groupNameInput.text = "";
        groupGradeInput.text = "";
        groupSubjectInput.text = "";
    }
}
