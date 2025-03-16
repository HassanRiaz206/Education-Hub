using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class YourGroupPanelManager : MonoBehaviour
{
    public TMP_Dropdown groupDropdown;  // Dropdown for group list
    public TMP_Text groupNameText;      // To display group name
    public TMP_Text groupGradeText;     // To display group grade
    public TMP_Text groupSubjectText;   // To display group subject

    // Internal mapping: dropdown index -> GroupInfo.
    // Note: Mapping keys start at 1 because index 0 is reserved for "Select".
    private Dictionary<int, GroupInfo> groupMapping = new Dictionary<int, GroupInfo>();
    private DatabaseReference databaseReference;

    void Start()
    {
        // Clear existing options and add the default "Select" option.
        groupDropdown.options.Clear();
        groupDropdown.options.Add(new TMP_Dropdown.OptionData("Select"));
        groupDropdown.value = 0;
        groupDropdown.RefreshShownValue();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                AttachListener();  // Set up a real-time listener for changes.
            }
            else
            {
                Debug.LogError("Firebase dependencies not met: " + task.Result);
            }
        });

        groupDropdown.onValueChanged.AddListener(OnGroupSelected);
    }

    // Attach a listener to the teacher's groups node to update in real-time.
    void AttachListener()
    {
        string teacherId = UserSession.teacherId;
        if (string.IsNullOrEmpty(teacherId))
        {
            Debug.LogError("Teacher ID is not set. Please log in properly.");
            return;
        }
        databaseReference.Child("teacherGroups").Child(teacherId)
            .ValueChanged += HandleValueChanged;
    }

    // This method is called whenever the data at the teacherGroups/teacherId node changes.
    void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError("Error retrieving data: " + args.DatabaseError.Message);
            return;
        }
        UpdateDropdown(args.Snapshot);
    }

    // Update the dropdown with the latest data from Firebase.
    void UpdateDropdown(DataSnapshot snapshot)
    {
        groupMapping.Clear();
        groupDropdown.options.Clear();
        groupDropdown.options.Add(new TMP_Dropdown.OptionData("Select"));

        int index = 1; // Start from 1 because index 0 is reserved for "Select"
        foreach (DataSnapshot child in snapshot.Children)
        {
            if (child.Value == null) continue;

            IDictionary groupData = child.Value as IDictionary;
            string groupName = groupData.Contains("groupName") ? groupData["groupName"].ToString() : "Unknown";
            string groupGrade = groupData.Contains("groupGrade") ? groupData["groupGrade"].ToString() : "Unknown";
            string groupSubject = groupData.Contains("groupSubject") ? groupData["groupSubject"].ToString() : "Unknown";

            GroupInfo info = new GroupInfo(child.Key, groupName, groupGrade, groupSubject);
            groupMapping[index] = info;
            groupDropdown.options.Add(new TMP_Dropdown.OptionData(groupName));
            index++;
        }

        // If no groups exist, add a placeholder.
        if (index == 1)
        {
            groupDropdown.options.Add(new TMP_Dropdown.OptionData("No groups available"));
        }

        groupDropdown.value = 0;
        groupDropdown.RefreshShownValue();
    }

    // Called when a user selects an option from the dropdown.
    void OnGroupSelected(int index)
    {
        // If "Select" or "No groups available" is chosen, clear the details.
        if (index == 0 || groupDropdown.options[index].text == "No groups available")
        {
            groupNameText.text = "";
            groupGradeText.text = "";
            groupSubjectText.text = "";
            UserSession.selectedGroupKey = "";
            return;
        }

        // Display the group details when a valid group is selected.
        if (groupMapping.ContainsKey(index))
        {
            GroupInfo info = groupMapping[index];
            groupNameText.text = info.groupName;
            groupGradeText.text = info.groupGrade;
            groupSubjectText.text = info.groupSubject;
            UserSession.selectedGroupKey = info.groupKey;
        }
        else
        {
            groupNameText.text = "";
            groupGradeText.text = "";
            groupSubjectText.text = "";
            UserSession.selectedGroupKey = "";
        }
    }
}

public class GroupInfo
{
    public string groupKey;
    public string groupName;
    public string groupGrade;
    public string groupSubject;

    public GroupInfo(string key, string name, string grade, string subject)
    {
        groupKey = key;
        groupName = name;
        groupGrade = grade;
        groupSubject = subject;
    }
}
