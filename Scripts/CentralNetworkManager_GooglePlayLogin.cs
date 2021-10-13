#if UNITY_STANDALONE && !CLIENT_BUILD
using Cysharp.Threading.Tasks;
using MiniJSON;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
#endif
using LiteNetLibManager;
using UnityEngine;

namespace MultiplayerARPG.MMO
{
    public partial class CentralNetworkManager
    {
        [Header("Google Play Login")]
        public ushort googlePlayLoginRequestType = 211;

#if UNITY_STANDALONE && !CLIENT_BUILD
        [DevExtMethods("RegisterServerMessages")]
        protected void RegisterServerMessages_GooglePlayLogin()
        {
            RegisterRequestToServer<RequestGooglePlayLoginMessage, ResponseUserLoginMessage>(googlePlayLoginRequestType, HandleRequestGooglePlayLogin);
        }

        protected async UniTaskVoid HandleRequestGooglePlayLogin(
            RequestHandlerData requestHandler, RequestGooglePlayLoginMessage request,
            RequestProceedResultDelegate<ResponseUserLoginMessage> result)
        {
            UITextKeys message = UITextKeys.NONE;
            string userId = string.Empty;
            string accessToken = string.Empty;
            long unbanTime = 0;
            // Validate by google api
            string url = "https://www.googleapis.com/oauth2/v3/tokeninfo?id_token=" + request.idToken;
            WebClient webClient = new WebClient();
            string json = webClient.DownloadString(url);
            Dictionary<string, object> dict = Json.Deserialize(json) as Dictionary<string, object>;
            if (dict.ContainsKey("sub") && dict.ContainsKey("email"))
            {
                // Send request to database server
                AsyncResponseData<DbGooglePlayLoginResp> resp = await DbServiceClient.RequestDbGooglePlayLogin(new DbGooglePlayLoginReq()
                {
                    id = (string)dict["sub"],
                    email = (string)dict["email"],
                });
                if (resp.ResponseCode == AckResponseCode.Success)
                {
                    userId = resp.Response.userId;
                }
            }
            // Response clients
            if (string.IsNullOrEmpty(userId))
            {
                message = UITextKeys.UI_ERROR_INVALID_USERNAME_OR_PASSWORD;
                userId = string.Empty;
            }
            else if (userPeersByUserId.ContainsKey(userId) || MapContainsUser(userId))
            {
                message = UITextKeys.UI_ERROR_ALREADY_LOGGED_IN;
                userId = string.Empty;
            }
            else
            {
                GetUserUnbanTimeResp resp = await DbServiceClient.GetUserUnbanTimeAsync(new GetUserUnbanTimeReq()
                {
                    UserId = userId
                });
                unbanTime = resp.UnbanTime;
                if (unbanTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    message = UITextKeys.UI_ERROR_USER_BANNED;
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
            }
            // Response
            result.Invoke(
                message == UITextKeys.NONE ? AckResponseCode.Success : AckResponseCode.Error,
                new ResponseUserLoginMessage()
                {
                    message = message,
                    userId = userId,
                    accessToken = accessToken,
                    unbanTime = unbanTime,
                });
        }
#endif

        public bool RequestGooglePlayLogin(string idToken, ResponseDelegate<ResponseUserLoginMessage> callback)
        {
            return ClientSendRequest(googlePlayLoginRequestType, new RequestGooglePlayLoginMessage()
            {
                idToken = idToken,
            }, responseDelegate: callback);
        }
    }
}
