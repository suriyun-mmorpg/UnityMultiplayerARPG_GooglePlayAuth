using LiteNetLibManager;
using UnityEngine;

namespace MultiplayerARPG.MMO
{
    public partial class MMOClientInstance
    {
        [Header("Google Play Login")]
        public string googleWebClientId;

        public void RequestGooglePlayLogin(string idToken, ResponseDelegate<ResponseUserLoginMessage> callback)
        {
            centralNetworkManager.RequestGooglePlayLogin(idToken, (responseHandler, responseCode, response) => OnRequestUserLogin(responseHandler, responseCode, response, callback).Forget());
        }
    }
}
