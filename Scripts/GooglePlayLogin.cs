using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using LiteNetLibManager;
using GooglePlayGames;
using GooglePlayGames.BasicApi;

namespace MultiplayerARPG.MMO
{
    public class GooglePlayLogin : MonoBehaviour
    {
        private static readonly PlayGamesClientConfiguration ClientConfiguration = new PlayGamesClientConfiguration.Builder()
            // requests the email address of the player be available.
            // Will bring up a prompt for consent.
            .RequestEmail()
            // requests a server auth code be generated so it can be passed to an
            //  associated back end server application and exchanged for an OAuth token.
            .RequestServerAuthCode(false)
            // requests an ID token be generated.  This OAuth token can be used to
            //  identify the player to other services such as Firebase.
            .RequestIdToken()
            .Build();

        public bool debugLogEnabled;
        public UnityEvent onLoginSuccess;
        public UnityEvent onLoginFail;

        public void OnClickGooglePlayLogin()
        {
            PlayGamesPlatform.InitializeInstance(ClientConfiguration);
            PlayGamesPlatform.DebugLogEnabled = debugLogEnabled;
            PlayGamesPlatform.Activate();
            PlayGamesPlatform.Instance.SignOut();
            PlayGamesPlatform.Instance.Authenticate((success, message) => {
                if (success)
                {
                    // When google play login success, send login request to server
                    var id = PlayGamesPlatform.Instance.GetUserId();
                    var userName = PlayGamesPlatform.Instance.GetUserDisplayName();
                    var email = PlayGamesPlatform.Instance.GetUserEmail();
                    var idToken = PlayGamesPlatform.Instance.GetIdToken();
                    PlayGamesPlatform.Instance.GetServerAuthCode();
                    UISceneGlobal.Singleton.ShowMessageDialog("Login", " id: " + id + " userName: " + userName + " email: " + email + " idToken:" + idToken);
                    //RequestGooglePlayLogin(email, idToken);
                }
                else
                {
                    // Show error message
                    UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", "Cannot Login with Google Play: " + message);
                }
            });
        }

        private void RequestGooglePlayLogin(string email, string idToken)
        {
            var uiSceneGlobal = UISceneGlobal.Singleton;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(idToken))
            {
                uiSceneGlobal.ShowMessageDialog("Cannot login", "User Email or ID token is empty");
                return;
            }
            MMOClientInstance.Singleton.RequestGooglePlayLogin(email, idToken, OnLogin);
        }

        public void OnLogin(AckResponseCode responseCode, BaseAckMessage message)
        {
            var castedMessage = (ResponseUserLoginMessage)message;
            switch (responseCode)
            {
                case AckResponseCode.Error:
                    var errorMessage = string.Empty;
                    switch (castedMessage.error)
                    {
                        case ResponseUserLoginMessage.Error.AlreadyLogin:
                            errorMessage = "User already logged in";
                            break;
                        case ResponseUserLoginMessage.Error.InvalidUsernameOrPassword:
                            errorMessage = "Invalid username or password";
                            break;
                    }
                    UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", errorMessage);
                    if (onLoginFail != null)
                        onLoginFail.Invoke();
                    break;
                case AckResponseCode.Timeout:
                    UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", "Connection timeout");
                    if (onLoginFail != null)
                        onLoginFail.Invoke();
                    break;
                default:
                    if (onLoginSuccess != null)
                        onLoginSuccess.Invoke();
                    break;
            }
        }
    }
}
