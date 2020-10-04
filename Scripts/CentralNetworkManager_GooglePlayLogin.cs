#if UNITY_STANDALONE && !CLIENT_BUILD
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using LiteNetLib.Utils;
using MiniJSON;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#endif
using LiteNetLibManager;

namespace MultiplayerARPG.MMO
{
    public partial class CentralNetworkManager
    {
#if UNITY_STANDALONE && !CLIENT_BUILD
        public const int CUSTOM_REQUEST_GOOGLE_LOGIN = 111;

        [DevExtMethods("RegisterServerMessages")]
        protected void RegisterServerMessages_GooglePlayLogin()
        {
            RegisterServerMessage(MMOMessageTypes.RequestGooglePlayLogin, HandleRequestGooglePlayLogin);
        }

        [DevExtMethods("OnStartServer")]
        protected void OnStartServer_GoogleLogin()
        {
            DatabaseServiceImplement.onCustomRequest -= onCustomRequest_GoogleLogin;
            DatabaseServiceImplement.onCustomRequest += onCustomRequest_GoogleLogin;
        }

        public async Task<CustomResp> onCustomRequest_GoogleLogin(int type, ByteString data)
        {
            string userId = string.Empty;
            if (type == CUSTOM_REQUEST_GOOGLE_LOGIN)
            {
                NetDataReader reader = new NetDataReader(data.ToByteArray());
                userId = await MMOServerInstance.Singleton.DatabaseNetworkManager.Database.GooglePlayLogin(reader.GetString(), reader.GetString());
            }
            NetDataWriter writer = new NetDataWriter();
            writer.Put(userId);
            return new CustomResp()
            {
                Type = CUSTOM_REQUEST_GOOGLE_LOGIN,
                Data = ByteString.CopyFrom(writer.Data)
            };
        }

        protected void HandleRequestGooglePlayLogin(LiteNetLibMessageHandler messageHandler)
        {
            HandleRequestGooglePlayLoginRoutine(messageHandler).Forget();
        }

        async UniTaskVoid HandleRequestGooglePlayLoginRoutine(LiteNetLibMessageHandler messageHandler)
        {
            long connectionId = messageHandler.connectionId;
            RequestGooglePlayLoginMessage message = messageHandler.ReadMessage<RequestGooglePlayLoginMessage>();
            ResponseUserLoginMessage.Error error = ResponseUserLoginMessage.Error.None;
            string userId = string.Empty;
            string accessToken = string.Empty;
            // Validate by google api
            string url = "https://www.googleapis.com/oauth2/v3/tokeninfo?id_token=" + message.idToken;
            WebClient webClient = new WebClient();
            string json = webClient.DownloadString(url);
            Dictionary<string, object> dict = Json.Deserialize(json) as Dictionary<string, object>;
            if (dict.ContainsKey("sub") && dict.ContainsKey("email"))
            {
                string gpgId = (string)dict["sub"];
                string email = (string)dict["email"];
                // Send request to database server
                NetDataWriter writer = new NetDataWriter();
                writer.Put(gpgId);
                writer.Put(email);
                CustomResp resp = await DbServiceClient.CustomAsync(new CustomReq()
                {
                    Type = CUSTOM_REQUEST_GOOGLE_LOGIN,
                    Data = ByteString.CopyFrom(writer.Data)
                });
                // Receive response from database server
                NetDataReader reader = new NetDataReader(resp.Data.ToByteArray());
                userId = reader.GetString();
            }
            // Response clients
            if (string.IsNullOrEmpty(userId))
            {
                error = ResponseUserLoginMessage.Error.InvalidUsernameOrPassword;
                userId = string.Empty;
            }
            else if (userPeersByUserId.ContainsKey(userId) || MapContainsUser(userId))
            {
                error = ResponseUserLoginMessage.Error.AlreadyLogin;
                userId = string.Empty;
            }
            else
            {
                CentralUserPeerInfo userPeerInfo = new CentralUserPeerInfo();
                userPeerInfo.connectionId = connectionId;
                userPeerInfo.userId = userId;
                userPeerInfo.accessToken = accessToken = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
                userPeersByUserId[userId] = userPeerInfo;
                userPeers[connectionId] = userPeerInfo;
                await DbServiceClient.UpdateAccessTokenAsync(new UpdateAccessTokenReq()
                {
                    UserId = userId,
                    AccessToken = accessToken
                });
            }
            ResponseUserLoginMessage responseMessage = new ResponseUserLoginMessage();
            responseMessage.ackId = message.ackId;
            responseMessage.responseCode = error == ResponseUserLoginMessage.Error.None ? AckResponseCode.Success : AckResponseCode.Error;
            responseMessage.error = error;
            responseMessage.userId = userId;
            responseMessage.accessToken = accessToken;
            ServerSendResponse(connectionId, MMOMessageTypes.ResponseUserLogin, responseMessage);
        }
#endif

        public uint RequestGooglePlayLogin(string idToken, AckMessageCallback callback)
        {
            RequestGooglePlayLoginMessage message = new RequestGooglePlayLoginMessage();
            message.idToken = idToken;
            return ClientSendRequest(MMOMessageTypes.RequestGooglePlayLogin, message, callback);
        }
    }
}
