using UnityEngine;
using UnityEngine.SceneManagement;

public class GameExitManager : MonoBehaviour
{
    /// <summary>
    /// Called when the Exit button is pressed.
    /// Loads the MainMenu scene.
    /// </summary>
    public void ExitToMainMenu()
    {
        // Loads the scene named "MainMenu"
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Called when the Quit button is pressed.
    /// Quits the application.
    /// </summary>
    public void QuitGame()
    {
        // For debugging purposes, print a message in the editor.
        Debug.Log("Quit Game called. Exiting application...");
        // Quits the application (does not work in the editor).
        Application.Quit();
    }
}