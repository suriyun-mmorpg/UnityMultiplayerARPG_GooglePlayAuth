using LiteNetLib;
using LiteNetLibManager;
using MiniJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace MultiplayerARPG.MMO
{
    public partial class CentralNetworkManager
    {
        public uint RequestGooglePlayLogin(string idToken, AckMessageCallback callback)
        {
            RequestGooglePlayLoginMessage message = new RequestGooglePlayLoginMessage();
            message.idToken = idToken;
            return Client.ClientSendAckPacket(DeliveryMethod.ReliableOrdered, MMOMessageTypes.RequestGooglePlayLogin, message, callback);
        }

        protected void HandleRequestGooglePlayLogin(LiteNetLibMessageHandler messageHandler)
        {
            StartCoroutine(HandleRequestGooglePlayLoginRoutine(messageHandler));
        }

        IEnumerator HandleRequestGooglePlayLoginRoutine(LiteNetLibMessageHandler messageHandler)
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
                string gId = (string)dict["sub"];
                string email = (string)dict["email"];
                GooglePlayLoginJob job = new GooglePlayLoginJob(Database, gId, email);
                job.Start();
                yield return StartCoroutine(job.WaitFor());
                userId = job.result;
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
                UpdateAccessTokenJob updateAccessTokenJob = new UpdateAccessTokenJob(Database, userId, accessToken);
                updateAccessTokenJob.Start();
                yield return StartCoroutine(updateAccessTokenJob.WaitFor());
            }
            ResponseUserLoginMessage responseMessage = new ResponseUserLoginMessage();
            responseMessage.ackId = message.ackId;
            responseMessage.responseCode = error == ResponseUserLoginMessage.Error.None ? AckResponseCode.Success : AckResponseCode.Error;
            responseMessage.error = error;
            responseMessage.userId = userId;
            responseMessage.accessToken = accessToken;
            ServerSendPacket(connectionId, DeliveryMethod.ReliableOrdered, MMOMessageTypes.ResponseUserLogin, responseMessage);
        }
    }
}
