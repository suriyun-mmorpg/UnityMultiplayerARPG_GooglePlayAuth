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
        public bool debugLogEnabled;
        public UnityEvent onLoginSuccess;
        public UnityEvent onLoginFail;

        private void Start()
        {
#if UNITY_ANDROID
            var builder = new PlayGamesClientConfiguration.Builder()
                // requests the email address of the player be available.
                // Will bring up a prompt for consent.
                .RequestEmail()
                // requests a server auth code be generated so it can be passed to an
                //  associated back end server application and exchanged for an OAuth token.
                .RequestServerAuthCode(false)
                // requests an ID token be generated.  This OAuth token can be used to
                //  identify the player to other services such as Firebase.
                .RequestIdToken();
            var config = builder.Build();
            PlayGamesPlatform.InitializeInstance(config);
            PlayGamesPlatform.DebugLogEnabled = debugLogEnabled;
#endif
        }

        public void OnClickGooglePlayLogin()
        {
#if UNITY_ANDROID
            PlayGamesPlatform.Instance.SignOut();
            PlayGamesPlatform.Instance.Authenticate((success, message) => {
                if (success)
                {
                    // When google play login success, send login request to server
                    var idToken = PlayGamesPlatform.Instance.GetIdToken();
                    RequestGooglePlayLogin(idToken);
                }
                else
                {
                    // Show error message
                    UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", "Cannot Login with Google Play: " + message);
                }
            });
#else
            Debug.Log("Only Android can login with Google Play");
#endif
        }

        private void RequestGooglePlayLogin(string idToken)
        {
            UISceneGlobal uiSceneGlobal = UISceneGlobal.Singleton;
            if (string.IsNullOrEmpty(idToken))
            {
                uiSceneGlobal.ShowMessageDialog("Cannot login", "ID token is empty");
                return;
            }
            MMOClientInstance.Singleton.RequestGooglePlayLogin(idToken, OnLogin);
        }

        public void OnLogin(AckResponseCode responseCode, BaseAckMessage message)
        {
            if (responseCode == AckResponseCode.Timeout)
            {
                UISceneGlobal.Singleton.ShowMessageDialog(LanguageManager.GetText(UITextKeys.UI_LABEL_ERROR.ToString()), LanguageManager.GetText(UITextKeys.UI_ERROR_CONNECTION_TIMEOUT.ToString()));
                if (onLoginFail != null)
                    onLoginFail.Invoke();
                return;
            }
            ResponseUserLoginMessage castedMessage = message as ResponseUserLoginMessage;
            switch (responseCode)
            {
                case AckResponseCode.Error:
                    string errorMessage = string.Empty;
                    switch (castedMessage.error)
                    {
                        case ResponseUserLoginMessage.Error.AlreadyLogin:
                            errorMessage = LanguageManager.GetText(UITextKeys.UI_ERROR_ALREADY_LOGGED_IN.ToString());
                            break;
                        case ResponseUserLoginMessage.Error.InvalidUsernameOrPassword:
                            errorMessage = LanguageManager.GetText(UITextKeys.UI_ERROR_INVALID_USERNAME_OR_PASSWORD.ToString());
                            break;
                    }
                    UISceneGlobal.Singleton.ShowMessageDialog(LanguageManager.GetText(UITextKeys.UI_LABEL_ERROR.ToString()), errorMessage);
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
