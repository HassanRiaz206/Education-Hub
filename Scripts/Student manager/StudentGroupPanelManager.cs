using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class StudentGroupPanelManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Dropdown for selecting a group in which the student is enrolled.")]
    public TMP_Dropdown groupDropdown;

    [Tooltip("Text to display the selected group's name.")]
    public TMP_Text groupNameText;

    [Tooltip("Text to display the selected group's grade.")]
    public TMP_Text groupGradeText;

    [Tooltip("Text to display the selected group's subject.")]
    public TMP_Text groupSubjectText;

    // Firebase Database reference.
    private DatabaseReference dbReference;
    // Student's roll number (retrieved from the user's profile).
    private string studentRollNumber = "";
    // Mapping between dropdown index and group name (as stored in groupEnrollments).
    private Dictionary<int, string> groupMapping = new Dictionary<int, string>();

    void Start()
    {
        // Verify UI references are assigned.
        if (groupDropdown == null || groupNameText == null || groupGradeText == null || groupSubjectText == null)
        {
            Debug.LogError("Please assign all UI references in the Inspector.");
            return;
        }

        // Initialize the Firebase Database reference.
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // Set up the dropdown with a default "Select" option.
        groupDropdown.options.Clear();
        groupDropdown.options.Add(new TMP_Dropdown.OptionData("Select"));
        groupDropdown.value = 0;
        groupDropdown.RefreshShownValue();

        // Ensure the logged-in username is set.
        if (string.IsNullOrEmpty(UserSession.username))
        {
            Debug.LogError("UserSession.username is not set. Cannot load student data.");
            return;
        }

        // Retrieve the student's roll number from the "users" node.
        dbReference.Child("users").Child(UserSession.username).GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    IDictionary userData = snapshot.Value as IDictionary;
                    if (userData != null && userData.Contains("rollNumber"))
                    {
                        studentRollNumber = userData["rollNumber"].ToString();
                        // Now load the groups in which the student is enrolled.
                        LoadStudentEnrollments();
                    }
                    else
                    {
                        Debug.LogError("Roll number not found in user data.");
                    }
                }
                else
                {
                    Debug.LogError("User data does not exist for: " + UserSession.username);
                }
            }
            else
            {
                Debug.LogError("Error retrieving user data: " + task.Exception);
            }
        });

        // Listen for dropdown selection changes.
        groupDropdown.onValueChanged.AddListener(OnGroupSelected);
    }

    /// <summary>
    /// Loads all group enrollments and filters those in which the student is enrolled.
    /// The enrollment keys are treated as the group names.
    /// </summary>
    void LoadStudentEnrollments()
    {
        groupMapping.Clear();
        groupDropdown.options.Clear();
        groupDropdown.options.Add(new TMP_Dropdown.OptionData("Select"));

        dbReference.Child("groupEnrollments").GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                int index = 1; // Start at 1 since 0 is reserved for "Select"
                foreach (DataSnapshot groupSnapshot in snapshot.Children)
                {
                    // In your database, groupEnrollment keys are the group names (e.g., "Math Study").
                    string groupKey = groupSnapshot.Key;
                    // Check if this group enrollment contains the student's roll number.
                    if (groupSnapshot.HasChild(studentRollNumber))
                    {
                        groupMapping[index] = groupKey;
                        groupDropdown.options.Add(new TMP_Dropdown.OptionData(groupKey));
                        index++;
                    }
                }
                groupDropdown.value = 0;
                groupDropdown.RefreshShownValue();

                if (index == 1)
                {
                    Debug.Log("Student is not enrolled in any group.");
                }
            }
            else
            {
                Debug.LogError("Error loading group enrollments: " + task.Exception);
            }
        });
    }

    /// <summary>
    /// Called when the student selects a group from the dropdown.
    /// Loads and displays the corresponding group details.
    /// </summary>
    /// <param name="index">Dropdown index.</param>
    void OnGroupSelected(int index)
    {
        // Clear details if "Select" is chosen.
        if (index == 0)
        {
            groupNameText.text = "";
            groupGradeText.text = "";
            groupSubjectText.text = "";
            return;
        }

        if (groupMapping.ContainsKey(index))
        {
            string groupNameForQuery = groupMapping[index];
            // Now load the group details by matching the groupName.
            LoadGroupDetailsByGroupName(groupNameForQuery);
        }
        else
        {
            Debug.LogError("Group mapping not found for index: " + index);
        }
    }

    /// <summary>
    /// Searches the teacherGroups node for a group record where the group's "groupName" field matches the provided group name.
    /// </summary>
    /// <param name="groupNameForQuery">The group name to search for (from enrollment).</param>
    void LoadGroupDetailsByGroupName(string groupNameForQuery)
    {
        dbReference.Child("teacherGroups").GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                bool found = false;
                // Iterate over each teacher's groups.
                foreach (DataSnapshot teacherNode in snapshot.Children)
                {
                    foreach (DataSnapshot groupSnapshot in teacherNode.Children)
                    {
                        // Skip the "enrolledStudents" child if present.
                        if (groupSnapshot.Key == "enrolledStudents")
                            continue;

                        IDictionary groupData = groupSnapshot.Value as IDictionary;
                        if (groupData != null && groupData.Contains("groupName"))
                        {
                            string groupName = groupData["groupName"].ToString();
                            if (groupName == groupNameForQuery)
                            {
                                // Group details found.
                                string groupGrade = groupData.Contains("groupGrade") ? groupData["groupGrade"].ToString() : "Unknown";
                                string groupSubject = groupData.Contains("groupSubject") ? groupData["groupSubject"].ToString() : "Unknown";

                                groupNameText.text = groupName;
                                groupGradeText.text = groupGrade;
                                groupSubjectText.text = groupSubject;
                                found = true;
                                break;
                            }
                        }
                    }
                    if (found) break;
                }
                if (!found)
                {
                    Debug.LogWarning("Group details not found for group name: " + groupNameForQuery);
                    groupNameText.text = "";
                    groupGradeText.text = "";
                    groupSubjectText.text = "";
                }
            }
            else
            {
                Debug.LogError("Error loading teacher groups: " + task.Exception);
            }
        });
    }
}
