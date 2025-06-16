using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void StartGame()
    {
        // Load your game scene
        SceneManager.LoadScene("SampleScene"); // Replace with your scene name
        
        // Or if you want to load the next scene in build order:
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    
    public void ExitGame()
    {
        // Exit the application
        Application.Quit();
        
        // For testing in editor:
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}