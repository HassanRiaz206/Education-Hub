using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    // Panels
    public GameObject loginPanel;
    public GameObject createAccountPanel;
    public GameObject accountCreationPopup;
    public GameObject loginFailedPopup;

    // Login Panel Fields
    public TMP_InputField loginUsernameField;
    public TMP_InputField loginPasswordField;

    // Create Account Panel Fields
    public TMP_InputField fullNameField;
    public TMP_InputField createUsernameField;
    public TMP_InputField createPasswordField;
    public TMP_Dropdown organizationDropdown;

    private DatabaseReference databaseReference;

    void Start()
    {
        // Ensure popups are off at startup
        accountCreationPopup.SetActive(false);
        loginFailedPopup.SetActive(false);

        // Initialize Firebase Database
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    // Called when the Submit button in the Create Account Panel is clicked
    public void OnSubmitCreateAccount()
    {
        string fullName = fullNameField.text.Trim();
        string username = createUsernameField.text.Trim();
        string password = createPasswordField.text.Trim();
        string organization = organizationDropdown.options[organizationDropdown.value].text;

        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Please fill in all fields.");
            return;
        }

        // Check if the username already exists
        databaseReference.Child("users").Child(username)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error checking user existence");
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        Debug.LogWarning("Username already exists!");
                        // Optionally inform the user that the username is taken.
                    }
                    else
                    {
                        if (organization == "As a Student")
                        {
                            // Retrieve the current roll number counter and increment it
                            databaseReference.Child("rollNumberCounter")
                                .GetValueAsync().ContinueWithOnMainThread(t =>
                                {
                                    int rollNumber = 1;
                                    if (t.Result.Exists)
                                    {
                                        int.TryParse(t.Result.Value.ToString(), out rollNumber);
                                        rollNumber++;
                                    }
                                    databaseReference.Child("rollNumberCounter").SetValueAsync(rollNumber);
                                    string rollNumberFormatted = rollNumber.ToString("D3");

                                    Dictionary<string, object> userData = new Dictionary<string, object>()
                                    {
                                        { "fullName", fullName },
                                        { "username", username },
                                        { "password", password },
                                        { "organization", organization },
                                        { "rollNumber", rollNumberFormatted }
                                    };

                                    databaseReference.Child("users").Child(username)
                                        .SetValueAsync(userData).ContinueWithOnMainThread(saveTask =>
                                        {
                                            if (saveTask.IsCompleted)
                                            {
                                                Debug.Log("User created successfully with roll number " + rollNumberFormatted);
                                                StartCoroutine(ShowPopupAndSwitchPanel(accountCreationPopup, createAccountPanel, loginPanel));
                                            }
                                            else
                                            {
                                                Debug.LogError("Error saving user data");
                                            }
                                        });
                                });
                        }
                        else // Organization is "As a Teacher"
                        {
                            Dictionary<string, object> userData = new Dictionary<string, object>()
                            {
                                { "fullName", fullName },
                                { "username", username },
                                { "password", password },
                                { "organization", organization },
                                // Teachers don't have a roll number
                                { "rollNumber", "" }
                            };

                            databaseReference.Child("users").Child(username)
                                .SetValueAsync(userData).ContinueWithOnMainThread(saveTask =>
                                {
                                    if (saveTask.IsCompleted)
                                    {
                                        Debug.Log("Teacher account created successfully");
                                        StartCoroutine(ShowPopupAndSwitchPanel(accountCreationPopup, createAccountPanel, loginPanel));
                                    }
                                    else
                                    {
                                        Debug.LogError("Error saving teacher data");
                                    }
                                });
                        }
                    }
                }
            });
    }

    // Called when the Login button in the Login Panel is clicked
    public void OnLogin()
    {
        string username = loginUsernameField.text.Trim();
        string password = loginPasswordField.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Please fill in both username and password.");
            return;
        }

        databaseReference.Child("users").Child(username)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error retrieving user data");
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        IDictionary userData = snapshot.Value as IDictionary;
                        string storedPassword = userData["password"].ToString();
                        if (password == storedPassword)
                        {
                            string organization = userData["organization"].ToString();

                            // Set UserSession variables for later use.
                            UserSession.username = username;
                            UserSession.organization = organization;

                            if (organization == "As a Student")
                            {
                                DemoSceneManager.targetScene = "StudentManager";
                                UserSession.teacherId = ""; // Not applicable for students
                            }
                            else if (organization == "As a Teacher")
                            {
                                UserSession.teacherId = username;
                                DemoSceneManager.targetScene = "TeacherManager";
                            }
                            SceneManager.LoadScene("DemoScene");
                        }
                        else
                        {
                            StartCoroutine(ShowPopup(loginFailedPopup));
                        }
                    }
                    else
                    {
                        StartCoroutine(ShowPopup(loginFailedPopup));
                    }
                }
            });
    }

    IEnumerator ShowPopupAndSwitchPanel(GameObject popup, GameObject panelToDisable, GameObject panelToEnable)
    {
        panelToDisable.SetActive(false); // Disable the create account panel
        popup.SetActive(true);             // Show popup
        yield return new WaitForSeconds(1f);
        popup.SetActive(false);            // Hide popup after 1 second
        panelToEnable.SetActive(true);     // Enable login panel
        ResetInputFields(panelToDisable);
    }

    IEnumerator ShowPopup(GameObject popup)
    {
        popup.SetActive(true);
        yield return new WaitForSeconds(1f);
        popup.SetActive(false);
    }

    void ResetInputFields(GameObject panel)
    {
        TMP_InputField[] inputFields = panel.GetComponentsInChildren<TMP_InputField>();
        foreach (TMP_InputField input in inputFields)
        {
            input.text = "";
        }
        TMP_Dropdown dropdown = panel.GetComponentInChildren<TMP_Dropdown>();
        if (dropdown != null)
        {
            dropdown.value = 0;
        }
    }
}
