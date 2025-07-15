using System;
using System.Collections;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Immutable.Passport;
using Immutable.Passport.Core.Logging;
using Immutable.Passport.Model;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.Networking;

public class PassportInitialisationScript : MonoBehaviour
{
#pragma warning disable CS8618
    [SerializeField] private GameObject TopPadding;
    [SerializeField] private Text Output;
#pragma warning restore CS8618

    void Start()
    {
        InitialisePassport();
    }

    private async void InitialisePassport()
    {
        ShowOutput("Initialising Passport...");

        string redirectUri;
        string logoutRedirectUri;

#if UNITY_WEBGL
            var url = Application.absoluteURL;
            var uri = new Uri(url);
            var scheme = uri.Scheme;
            var hostWithPort = uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            var fullPath = uri.AbsolutePath.EndsWith("/")
                ? uri.AbsolutePath
                : uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/') + 1);

            redirectUri = $"{scheme}://{hostWithPort}{fullPath}callback.html";
            logoutRedirectUri = $"{scheme}://{hostWithPort}{fullPath}logout.html";
#else
        redirectUri = "immutablerunner://callback";
        logoutRedirectUri = "immutablerunner://logout";
#endif

        try
        {
            // Set the log level for the SDK
            Passport.LogLevel = LogLevel.Debug;

            // Don't redact token values from logs
            Passport.RedactTokensInLogs = false;

            // Initialise Passport
            const string environment = Immutable.Passport.Model.Environment.DEVELOPMENT;
            const string clientId = "tPeZ5cY5hJBNXu7iX6NZbgypcic8CbEl";
            var passport = await Passport.Init(clientId, environment, redirectUri, logoutRedirectUri);
            SampleAppManager.PassportInstance = passport;
            
            PlayFabClientAPI.LoginWithEmailAddress(
                new LoginWithEmailAddressRequest
                {
                    TitleId = PlayFabSettings.TitleId,
                    Email = "natalie+playfab1@immutable.com",
                    Password = "", // TODO add password
                    InfoRequestParameters = new GetPlayerCombinedInfoRequestParams()
                    {
                        GetUserData = true
                    }
                },

                // Success
                async result =>
                {
                    Debug.Log($"SessionTicket: {result.SessionTicket}");
                    await PostSessionTicketAsync(result.SessionTicket);
                },

                // Failure
                error =>
                {
                    ShowOutput($"Failed to log into PlayFab {error.ErrorMessage}");
                });
        }
        catch (Exception ex)
        {
            Debug.LogException(ex, this);
            ShowOutput($"Initialise Passport error: {ex.Message}");
        }
    }
    
    private async UniTask PostSessionTicketAsync(string sessionTicket)
    {
        ShowOutput("Getting tokens...");
        
        var requestBody = new SessionTicketRequest { sessionTicket = sessionTicket };
        string json = JsonConvert.SerializeObject(requestBody);

        using var request = new UnityWebRequest("http://localhost:1111/login", "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var asyncOp = request.SendWebRequest();

        while (!asyncOp.isDone)
            await UniTask.Yield();

#if UNITY_2020_1_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            Debug.LogError($"Request failed: {request.error}");
        }
        else
        {
            try
            {
                var responseText = request.downloadHandler.text;
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseText);
                
                ShowOutput("Completing Login...");
                await Passport.Instance.CompleteLogin(tokenResponse);
                
                // Navigate to the unauthenticated scene after initialising Passport
                SceneManager.LoadScene("AuthenticatedScene");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse or complete login: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Prints the specified <code>message</code> to the output box.
    /// </summary>
    /// <param name="message">The message to print</param>
    private void ShowOutput(string message)
    {
        if (Output != null)
        {
            Output.text = message;
        }
    }
}

[Serializable]
public class SessionTicketRequest
{
    public string sessionTicket;
}
