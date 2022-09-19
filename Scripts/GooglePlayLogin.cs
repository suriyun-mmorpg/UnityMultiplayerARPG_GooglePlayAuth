using UnityEngine;
using UnityEngine.Events;
using LiteNetLibManager;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
#if UNITY_ANDROID
using Google;
using Google.Impl;
#endif

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
            GoogleSignIn.Configuration = new GoogleSignInConfiguration();
#endif
        }

        public async void OnClickGooglePlayLogin()
        {
#if UNITY_ANDROID
            GoogleSignIn.Configuration.WebClientId = MMOClientInstance.Singleton.googleWebClientId;
            GoogleSignIn.Configuration.RequestEmail = true;
            GoogleSignIn.Configuration.RequestAuthCode = true;
            GoogleSignIn.Configuration.RequestIdToken = true;

            try
            {
                GoogleSignInUser result = await GoogleSignIn.DefaultInstance.SignIn();
                RequestGooglePlayLogin(result.IdToken);
            }
            catch (GoogleSignIn.SignInException ex)
            {
                string error = ex.Status.ToString();
                Logging.LogError("Error occuring when signin with Google: " + Regex.Replace(error, "(\\B[A-Z])", " $1"));
                UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", "Error, cannot Login with Google Play.");
            }
            catch (System.Exception ex)
            {
                Logging.LogError("Error occuring when signin with Google: " + ex.StackTrace);
                UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", "Error, cannot Login with Google Play.");
            }
#else
            Debug.Log("Only Android can login with Google Play");
            await UniTask.Yield();
#endif
        }

        private void RequestGooglePlayLogin(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                UISceneGlobal.Singleton.ShowMessageDialog("Cannot login", "ID token is empty");
                return;
            }
            MMOClientInstance.Singleton.RequestGooglePlayLogin(idToken, OnLogin);
        }

        public void OnLogin(ResponseHandlerData responseHandler, AckResponseCode responseCode, ResponseUserLoginMessage response)
        {
            if (responseCode.ShowUnhandledResponseMessageDialog(response.message))
            {
                if (onLoginFail != null)
                    onLoginFail.Invoke();
                return;
            }
            if (onLoginSuccess != null)
                onLoginSuccess.Invoke();
        }
    }
}
