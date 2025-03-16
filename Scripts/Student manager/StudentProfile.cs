using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class StudentProfile : MonoBehaviour
{
    // References to the TextMeshPro UI elements for full name and roll number.
    public TMP_Text fullNameText;
    public TMP_Text rollNoText;

    // Firebase Database reference.
    private DatabaseReference databaseReference;

    void Start()
    {
        // Check if UI elements are assigned.
        if (fullNameText == null || rollNoText == null)
        {
            Debug.LogError("UI elements (fullNameText or rollNoText) are not assigned in the Inspector!");
            return;
        }

        // Initialize the Firebase Database reference.
        try
        {
            databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Firebase database is not initialized: " + ex.Message);
            return;
        }

        // Ensure that the user is logged in as a student.
        if (UserSession.organization == "As a Student")
        {
            LoadStudentProfile();
        }
        else
        {
            Debug.LogWarning("This profile is intended for students only.");
        }
    }

    /// <summary>
    /// Loads the student profile data (full name and roll number) from Firebase and updates the UI.
    /// </summary>
    public void LoadStudentProfile()
    {
        if (string.IsNullOrEmpty(UserSession.username))
        {
            Debug.LogError("UserSession.username is not set. Cannot load profile.");
            return;
        }

        // Retrieve the student data using the username as the key.
        databaseReference.Child("users").Child(UserSession.username)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Error retrieving student profile: " + task.Exception);
                }
                else if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        // Convert the snapshot value to an IDictionary to access fields.
                        IDictionary userData = snapshot.Value as IDictionary;
                        if (userData == null)
                        {
                            Debug.LogError("User data is null or not in expected format.");
                            return;
                        }

                        // Extract only the full name and roll number.
                        string fullName = userData.Contains("fullName") ? userData["fullName"].ToString() : "N/A";
                        string rollNumber = userData.Contains("rollNumber") ? userData["rollNumber"].ToString() : "N/A";

                        // Update the UI elements.
                        fullNameText.text = fullName;
                        rollNoText.text = rollNumber;

                        Debug.Log("Student profile loaded successfully.");
                    }
                    else
                    {
                        Debug.LogWarning("Student profile not found for username: " + UserSession.username);
                    }
                }
            });
    }
}
