using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Immutable.Passport;

public class LogoutScript : MonoBehaviour
{
    /// <summary>
    /// Logs out of Passport using the selected auth method.
    /// </summary>
    public void Logout()
    {
        LogoutAsync();
    }

    private async UniTaskVoid LogoutAsync()
    {
        if (Passport.Instance == null)
        {
            Debug.LogError("Passport instance is null");
            return;
        }
        try
        {
            await Passport.Instance.Logout();
            SampleAppManager.IsConnectedToZkEvm = false;
            AuthenticatedSceneManager.NavigateToUnauthenticatedScene();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to logout: {ex.Message}");
        }
    }
}