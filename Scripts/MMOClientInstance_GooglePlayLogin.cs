using LiteNetLibManager;

namespace MultiplayerARPG.MMO
{
    public partial class MMOClientInstance
    {
        public void RequestGooglePlayLogin(string idToken, AckMessageCallback callback)
        {
            centralNetworkManager.RequestGooglePlayLogin(idToken, (responseCode, messageData) => OnRequestUserLogin(responseCode, messageData, callback));
        }
    }
}
