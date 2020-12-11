using LiteNetLibManager;

namespace MultiplayerARPG.MMO
{
    public partial class MMOClientInstance
    {
        public void RequestGooglePlayLogin(string idToken, ResponseDelegate<ResponseUserLoginMessage> callback)
        {
            centralNetworkManager.RequestGooglePlayLogin(idToken, (responseHandler, responseCode, response) => OnRequestUserLogin(responseHandler, responseCode, response, callback));
        }
    }
}
