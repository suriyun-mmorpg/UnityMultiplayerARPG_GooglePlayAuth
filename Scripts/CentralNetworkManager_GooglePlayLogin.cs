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
using UnityEngine;

namespace MultiplayerARPG.MMO
{
    public partial class CentralNetworkManager
    {
#if UNITY_STANDALONE && !CLIENT_BUILD
        public const int CUSTOM_REQUEST_GOOGLE_LOGIN = 111;
        [Header("Facebook Login")]
        public ushort googlePlayLoginRequestType = 211;

        [DevExtMethods("RegisterServerMessages")]
        protected void RegisterServerMessages_GooglePlayLogin()
        {
            RegisterServerRequest<RequestGooglePlayLoginMessage, ResponseUserLoginMessage>(googlePlayLoginRequestType, HandleRequestGooglePlayLogin);
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

        protected async UniTaskVoid HandleRequestGooglePlayLogin(
            RequestHandlerData requestHandler, RequestGooglePlayLoginMessage request,
            RequestProceedResultDelegate<ResponseUserLoginMessage> result)
        {
            ResponseUserLoginMessage.Error error = ResponseUserLoginMessage.Error.None;
            string userId = string.Empty;
            string accessToken = string.Empty;
            // Validate by google api
            string url = "https://www.googleapis.com/oauth2/v3/tokeninfo?id_token=" + request.idToken;
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
                NetDataReader dbReader = new NetDataReader(resp.Data.ToByteArray());
                userId = dbReader.GetString();
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
                userPeerInfo.connectionId = requestHandler.ConnectionId;
                userPeerInfo.userId = userId;
                userPeerInfo.accessToken = accessToken = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
                userPeersByUserId[userId] = userPeerInfo;
                userPeers[requestHandler.ConnectionId] = userPeerInfo;
                await DbServiceClient.UpdateAccessTokenAsync(new UpdateAccessTokenReq()
                {
                    UserId = userId,
                    AccessToken = accessToken
                });
            }
            // Response
            result.Invoke(
                error == ResponseUserLoginMessage.Error.None ? AckResponseCode.Success : AckResponseCode.Error,
                new ResponseUserLoginMessage()
                {
                    error = error,
                    userId = userId,
                    accessToken = accessToken,
                });
        }
#endif

        public bool RequestGooglePlayLogin(string idToken, ResponseDelegate extraResponseCallback)
        {
            return ClientSendRequest(googlePlayLoginRequestType, new RequestGooglePlayLoginMessage()
            {
                idToken = idToken,
            }, responseDelegate: extraResponseCallback);
        }
    }
}
