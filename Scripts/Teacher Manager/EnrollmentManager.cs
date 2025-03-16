using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.UI;

public class EnrollmentManager : MonoBehaviour
{
    [Header("Dropdowns & Buttons")]
    [Tooltip("Dropdown for selecting a group (prepopulated with group names, default option 'Select').")]
    public TMP_Dropdown groupDropdown;

    [Tooltip("Dropdown for selecting a student roll number. It will show available students only when a group is selected.")]
    public TMP_Dropdown studentDropdown;

    [Tooltip("Button to add the selected student to the selected group.")]
    public Button addButton;

    [Header("Enrolled Students UI")]
    [Tooltip("ScrollView content panel (with a Horizontal Layout Group) where enrolled student entries are shown.")]
    public Transform enrolledStudentsPanel;

    [Tooltip("Prefab for an enrolled student entry. It must have child objects named 'name tmpro', 'Roll no tmpro' and 'DeleteButton'.")]
    public GameObject enrolledStudentPrefab;

    // Firebase database reference.
    private DatabaseReference dbReference;

    // Dictionary to store all student data from "users" (Key: roll number, Value: full name).
    private Dictionary<string, string> allStudentsDict = new Dictionary<string, string>();

    // List of roll numbers for students already enrolled in the currently selected group.
    private List<string> enrolledStudentsRolls = new List<string>();

    void Awake()
    {
        // Ensure all required UI references are set.
        if (groupDropdown == null) Debug.LogError("Group Dropdown is not assigned!");
        if (studentDropdown == null) Debug.LogError("Student Dropdown is not assigned!");
        if (addButton == null) Debug.LogError("Add Button is not assigned!");
        if (enrolledStudentsPanel == null) Debug.LogError("Enrolled Students Panel is not assigned!");
        if (enrolledStudentPrefab == null) Debug.LogError("Enrolled Student Prefab is not assigned!");
    }

    void Start()
    {
        // Initialize Firebase and get the database reference.
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                LoadAllStudents();
            }
            else
            {
                Debug.LogError("Firebase dependencies not available: " + task.Result);
            }
        });

        // Setup listeners.
        groupDropdown.onValueChanged.AddListener(delegate { OnGroupChanged(); });
        addButton.onClick.AddListener(OnAddStudent);

        // Initialize the student dropdown with default "Select".
        studentDropdown.ClearOptions();
        studentDropdown.AddOptions(new List<string> { "Select" });
    }

    /// <summary>
    /// Loads all student users from Firebase ("users" node) where organization equals "As a Student".
    /// </summary>
    void LoadAllStudents()
    {
        dbReference.Child("users").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error retrieving student data: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                try
                {
                    DataSnapshot snapshot = task.Result;
                    foreach (DataSnapshot userSnapshot in snapshot.Children)
                    {
                        IDictionary userData = userSnapshot.Value as IDictionary;
                        if (userData == null)
                            continue;
                        if (userData.Contains("organization") && userData["organization"].ToString() == "As a Student")
                        {
                            if (userData.Contains("rollNumber") && userData.Contains("fullName"))
                            {
                                string rollNumber = userData["rollNumber"].ToString();
                                string fullName = userData["fullName"].ToString();
                                if (!allStudentsDict.ContainsKey(rollNumber))
                                {
                                    allStudentsDict.Add(rollNumber, fullName);
                                }
                            }
                        }
                    }
                    // If a group is already selected, update the student dropdown.
                    if (groupDropdown.options.Count > 0 && groupDropdown.options[groupDropdown.value].text != "Select")
                    {
                        UpdateStudentDropdownOptions();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Exception in LoadAllStudents: " + ex.Message);
                }
            }
        });
    }

    /// <summary>
    /// Called when the group dropdown selection changes.
    /// If a valid group is selected, loads the group's enrollments from Firebase.
    /// </summary>
    void OnGroupChanged()
    {
        string selectedGroup = groupDropdown.options[groupDropdown.value].text;
        if (selectedGroup == "Select" || string.IsNullOrEmpty(selectedGroup))
        {
            ClearEnrolledStudentsUI();
            enrolledStudentsRolls.Clear();
            studentDropdown.ClearOptions();
            studentDropdown.AddOptions(new List<string> { "Select" });
        }
        else
        {
            LoadGroupEnrollments(selectedGroup);
        }
    }

    /// <summary>
    /// Loads the enrollment records for the given group from Firebase and updates the UI.
    /// </summary>
    /// <param name="group">The group name.</param>
    void LoadGroupEnrollments(string group)
    {
        ClearEnrolledStudentsUI();
        enrolledStudentsRolls.Clear();

        dbReference.Child("groupEnrollments").Child(group).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error loading enrollments: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                try
                {
                    DataSnapshot snapshot = task.Result;
                    foreach (DataSnapshot enrollmentSnapshot in snapshot.Children)
                    {
                        IDictionary enrollmentData = enrollmentSnapshot.Value as IDictionary;
                        if (enrollmentData == null)
                            continue;
                        if (enrollmentData.Contains("rollNumber") && enrollmentData.Contains("fullName"))
                        {
                            string rollNumber = enrollmentData["rollNumber"].ToString();
                            string fullName = enrollmentData["fullName"].ToString();
                            enrolledStudentsRolls.Add(rollNumber);

                            // Instantiate the enrollment UI entry.
                            GameObject entry = Instantiate(enrolledStudentPrefab, enrolledStudentsPanel);
                            TMP_Text nameTMP = entry.transform.Find("name tmpro")?.GetComponent<TMP_Text>();
                            TMP_Text rollTMP = entry.transform.Find("Roll no tmpro")?.GetComponent<TMP_Text>();

                            if (nameTMP != null && rollTMP != null)
                            {
                                nameTMP.text = fullName;
                                rollTMP.text = rollNumber;
                            }
                            else
                            {
                                Debug.LogError("Enrollment prefab is missing the required TextMeshPro components.");
                            }

                            // Set up the Delete button.
                            Button deleteButton = entry.transform.Find("DeleteButton")?.GetComponent<Button>();
                            if (deleteButton != null)
                            {
                                // Capture current group and roll number in the listener.
                                deleteButton.onClick.AddListener(() => { OnDeleteStudent(group, rollNumber, entry); });
                            }
                            else
                            {
                                Debug.LogError("DeleteButton not found in enrollment prefab.");
                            }
                        }
                    }
                    UpdateStudentDropdownOptions();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Exception in LoadGroupEnrollments: " + ex.Message);
                }
            }
        });
    }

    /// <summary>
    /// Updates the student dropdown with available roll numbers (those not already enrolled).
    /// </summary>
    void UpdateStudentDropdownOptions()
    {
        List<string> availableOptions = new List<string> { "Select" };
        foreach (KeyValuePair<string, string> kvp in allStudentsDict)
        {
            if (!enrolledStudentsRolls.Contains(kvp.Key))
            {
                availableOptions.Add(kvp.Key);
            }
        }
        studentDropdown.ClearOptions();
        studentDropdown.AddOptions(availableOptions);
    }

    /// <summary>
    /// Called when the Add button is clicked. Enrolls the selected student into the selected group.
    /// </summary>
    void OnAddStudent()
    {
        string selectedGroup = groupDropdown.options[groupDropdown.value].text;
        if (selectedGroup == "Select" || string.IsNullOrEmpty(selectedGroup))
        {
            Debug.LogWarning("Please select a valid group before adding a student.");
            return;
        }

        string selectedStudentRoll = studentDropdown.options[studentDropdown.value].text;
        if (selectedStudentRoll == "Select")
        {
            Debug.LogWarning("Please select a valid student roll number.");
            return;
        }

        if (enrolledStudentsRolls.Contains(selectedStudentRoll))
        {
            Debug.LogWarning("This student is already enrolled in the group.");
            return;
        }

        if (!allStudentsDict.TryGetValue(selectedStudentRoll, out string fullName))
        {
            Debug.LogError("Selected student not found in records.");
            return;
        }

        // Prepare the enrollment data.
        Dictionary<string, object> enrollmentData = new Dictionary<string, object>()
        {
            { "fullName", fullName },
            { "rollNumber", selectedStudentRoll }
        };

        // Write the enrollment data to Firebase.
        dbReference.Child("groupEnrollments").Child(selectedGroup).Child(selectedStudentRoll)
            .SetValueAsync(enrollmentData).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    // Instantiate the enrollment UI entry.
                    GameObject entry = Instantiate(enrolledStudentPrefab, enrolledStudentsPanel);
                    TMP_Text nameTMP = entry.transform.Find("name tmpro")?.GetComponent<TMP_Text>();
                    TMP_Text rollTMP = entry.transform.Find("Roll no tmpro")?.GetComponent<TMP_Text>();

                    if (nameTMP != null && rollTMP != null)
                    {
                        nameTMP.text = fullName;
                        rollTMP.text = selectedStudentRoll;
                    }
                    else
                    {
                        Debug.LogError("Enrollment prefab is missing required TextMeshPro components.");
                    }

                    // Set up the Delete button.
                    Button deleteButton = entry.transform.Find("DeleteButton")?.GetComponent<Button>();
                    if (deleteButton != null)
                    {
                        deleteButton.onClick.AddListener(() => { OnDeleteStudent(selectedGroup, selectedStudentRoll, entry); });
                    }
                    else
                    {
                        Debug.LogError("DeleteButton not found in enrollment prefab.");
                    }

                    // Update the enrolled list and refresh the student dropdown.
                    enrolledStudentsRolls.Add(selectedStudentRoll);
                    UpdateStudentDropdownOptions();
                }
                else
                {
                    Debug.LogError("Error enrolling student: " + task.Exception);
                }
            });
    }

    /// <summary>
    /// Called when the Delete button is clicked on an enrollment entry.
    /// Removes the student from Firebase and the UI, then updates the dropdown.
    /// </summary>
    /// <param name="group">The group name.</param>
    /// <param name="roll">The student roll number.</param>
    /// <param name="entry">The UI entry to remove.</param>
    void OnDeleteStudent(string group, string roll, GameObject entry)
    {
        dbReference.Child("groupEnrollments").Child(group).Child(roll)
            .RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    Destroy(entry);
                    if (enrolledStudentsRolls.Contains(roll))
                        enrolledStudentsRolls.Remove(roll);
                    UpdateStudentDropdownOptions();
                }
                else
                {
                    Debug.LogError("Error removing student from group: " + task.Exception);
                }
            });
    }

    /// <summary>
    /// Clears all enrollment UI entries from the enrolledStudentsPanel.
    /// </summary>
    void ClearEnrolledStudentsUI()
    {
        foreach (Transform child in enrolledStudentsPanel)
        {
            Destroy(child.gameObject);
        }
    }
}
