using UnityEngine;
using UnityEngine.Events;
using LiteNetLibManager;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
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
            PlayGamesPlatform.DebugLogEnabled = debugLogEnabled;
#endif
        }

        public void OnClickGooglePlayLogin()
        {
#if UNITY_ANDROID
            PlayGamesPlatform.Instance.Authenticate((status) =>
            {
                switch (status)
                {
                    case SignInStatus.InternalError:
                        UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", "Internal Errror, cannot Login with Google Play.");
                        break;
                    case SignInStatus.Canceled:
                        UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", "Login with Google Play was cancelled.");
                        break;
                    default:
                        // When google play login success, send login request to server
                        PlayGamesPlatform.Instance.RequestServerSideAccess(false, (idToken) =>
                        {
                            RequestGooglePlayLogin(idToken);
                        });
                        break;
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
