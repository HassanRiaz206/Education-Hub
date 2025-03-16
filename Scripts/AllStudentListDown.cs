using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class AllStudentListDown : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Prefab that contains the TextMeshPro objects named 'name tmpro' and 'Roll no tmpro'.")]
    public GameObject studentPrefab;

    [Tooltip("Content panel of the ScrollView (ensure it has a Horizontal Layout Group).")]
    public Transform contentPanel;

    private DatabaseReference databaseReference;

    void Start()
    {
        // Initialize Firebase and set up the database reference.
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            DependencyStatus dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Set your Firebase database reference (if needed, configure your database URL elsewhere)
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                // Load the student data.
                LoadStudents();
            }
            else
            {
                Debug.LogError("Firebase dependencies are not resolved: " + dependencyStatus);
            }
        });
    }

    void LoadStudents()
    {
        // Access the "users" node in the database.
        databaseReference.Child("users").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error retrieving user data: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                // Loop through each user in the "users" node.
                foreach (DataSnapshot userSnapshot in snapshot.Children)
                {
                    IDictionary userData = userSnapshot.Value as IDictionary;
                    if (userData == null)
                        continue;

                    // Check if the user is a student.
                    if (userData["organization"].ToString() == "As a Student")
                    {
                        // Retrieve the student's full name and roll number.
                        string fullName = userData["fullName"].ToString();
                        string rollNumber = userData["rollNumber"].ToString();

                        // Instantiate the prefab as a child of the content panel.
                        GameObject newStudentPanel = Instantiate(studentPrefab, contentPanel);

                        // Find and update the TextMeshPro components in the prefab.
                        TMP_Text nameTMP = newStudentPanel.transform.Find("name tmpro").GetComponent<TMP_Text>();
                        TMP_Text rollNoTMP = newStudentPanel.transform.Find("Roll no tmpro").GetComponent<TMP_Text>();

                        nameTMP.text = fullName;
                        rollNoTMP.text = rollNumber;
                    }
                }
            }
        });
    }
}
