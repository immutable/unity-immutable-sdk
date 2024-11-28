using UnityEngine;
using UnityEngine.SceneManagement;

public class LaunchBrowserScript : MonoBehaviour
{
    
    /// <summary>
    /// Navigates back to the authenticated scene.
    /// </summary>
    public void Cancel()
    {
        SceneManager.LoadScene("AuthenticatedScene");
    }
}
