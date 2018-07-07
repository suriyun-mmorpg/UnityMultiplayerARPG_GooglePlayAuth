using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SocialPlatforms;
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
            Social.localUser.Authenticate((bool success) => {
                // handle success or failure
                if (success)
                {
                    // When google play login success, send login request to server
                    var userName = Social.localUser.userName;
                    var idToken = ((PlayGamesLocalUser)Social.localUser).GetIdToken();
                }
                else
                {
                    // Show error message
                    UISceneGlobal.Singleton.ShowMessageDialog("Cannot Login", "Cannot Login with Google Play");
                }
            });
        }
    }
}
