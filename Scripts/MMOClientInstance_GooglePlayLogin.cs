using LiteNetLibManager;

namespace MultiplayerARPG.MMO
{
    public partial class MMOClientInstance
    {
        public void RequestGooglePlayLogin(string idToken, AckMessageCallback<ResponseUserLoginMessage> callback)
        {
            centralNetworkManager.RequestGooglePlayLogin(idToken, (messageData) => OnRequestUserLogin(messageData, callback));
        }
    }
}
