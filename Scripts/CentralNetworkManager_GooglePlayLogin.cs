#if (UNITY_EDITOR || !EXCLUDE_SERVER_CODES) && UNITY_STANDALONE
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
#endif
using Cysharp.Threading.Tasks;
using LiteNetLibManager;
using UnityEngine;

namespace MultiplayerARPG.MMO
{
    using MiniJSON;

    public partial class CentralNetworkManager
    {
        [Header("Google Play Login")]
        public ushort googlePlayLoginRequestType = 211;

        [DevExtMethods("RegisterServerMessages")]
        protected void RegisterServerMessages_GooglePlayLogin()
        {
            RegisterRequestToServer<RequestGooglePlayLoginMessage, ResponseUserLoginMessage>(googlePlayLoginRequestType, HandleRequestGooglePlayLogin);
        }

        protected async UniTaskVoid HandleRequestGooglePlayLogin(
            RequestHandlerData requestHandler, RequestGooglePlayLoginMessage request,
            RequestProceedResultDelegate<ResponseUserLoginMessage> result)
        {
#if (UNITY_EDITOR || !EXCLUDE_SERVER_CODES) && UNITY_STANDALONE
            long connectionId = requestHandler.ConnectionId;
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
                AsyncResponseData<DbGooglePlayLoginResp> resp = await DatabaseClient.RequestDbGooglePlayLogin(new DbGooglePlayLoginReq()
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
                result.InvokeError(new ResponseUserLoginMessage()
                {
                    message = UITextKeys.UI_ERROR_INVALID_USERNAME_OR_PASSWORD,
                });
                return;
            }
            if (_userPeersByUserId.ContainsKey(userId) || MapContainsUser(userId))
            {
                // Kick the user from game
                if (_userPeersByUserId.ContainsKey(userId))
                    KickClient(_userPeersByUserId[userId].connectionId, UITextKeys.UI_ERROR_ACCOUNT_LOGGED_IN_BY_OTHER);
                ClusterServer.KickUser(userId, UITextKeys.UI_ERROR_ACCOUNT_LOGGED_IN_BY_OTHER);
                RemoveUserPeerByUserId(userId, out _);
                result.InvokeError(new ResponseUserLoginMessage()
                {
                    message = UITextKeys.UI_ERROR_ALREADY_LOGGED_IN,
                });
                return;
            }
            DatabaseApiResult<GetUserUnbanTimeResp> unbanTimeResp = await DatabaseClient.GetUserUnbanTimeAsync(new GetUserUnbanTimeReq()
            {
                UserId = userId
            });
            if (!unbanTimeResp.IsSuccess)
            {
                result.InvokeError(new ResponseUserLoginMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            unbanTime = unbanTimeResp.Response.UnbanTime;
            if (unbanTime > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                result.InvokeError(new ResponseUserLoginMessage()
                {
                    message = UITextKeys.UI_ERROR_USER_BANNED,
                });
                return;
            }
            // Generate new access token
            accessToken = DataManager.GenerateAccessToken(userId);
            DatabaseApiResult updateAccessTokenResp = await DatabaseClient.UpdateAccessTokenAsync(new UpdateAccessTokenReq()
            {
                UserId = userId,
                AccessToken = accessToken,
            });
            if (!updateAccessTokenResp.IsSuccess)
            {
                result.InvokeError(new ResponseUserLoginMessage()
                {
                    message = UITextKeys.UI_ERROR_INTERNAL_SERVER_ERROR,
                });
                return;
            }
            // Update peer info
            CentralUserPeerInfo userPeerInfo = new CentralUserPeerInfo()
            {
                connectionId = connectionId,
                userId = userId,
                accessToken = accessToken,
            };
            _userPeersByUserId[userId] = userPeerInfo;
            _userPeers[connectionId] = userPeerInfo;
            // Response
            result.InvokeSuccess(new ResponseUserLoginMessage()
            {
                userId = userId,
                accessToken = accessToken,
                unbanTime = unbanTime,
            });
#endif
        }

        public bool RequestGooglePlayLogin(string idToken, ResponseDelegate<ResponseUserLoginMessage> callback)
        {
            return ClientSendRequest(googlePlayLoginRequestType, new RequestGooglePlayLoginMessage()
            {
                idToken = idToken,
            }, responseDelegate: callback);
        }
    }
}
