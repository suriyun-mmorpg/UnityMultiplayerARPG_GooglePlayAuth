using Cysharp.Threading.Tasks;
using Insthync.DevExtension;
using LiteNetLibManager;

namespace MultiplayerARPG.MMO
{
    public partial class DatabaseNetworkManager
    {
        public const int CUSTOM_REQUEST_GOOGLE_LOGIN = 1011;

        [DevExtMethods("RegisterMessages")]
        protected void RegisterMessages_GooglePlayLogin()
        {
            RegisterRequestToServer<DbGooglePlayLoginReq, DbGooglePlayLoginResp>(CUSTOM_REQUEST_GOOGLE_LOGIN, DbGooglePlayLogin);
        }

        public UniTask<AsyncResponseData<DbGooglePlayLoginResp>> RequestDbGooglePlayLogin(DbGooglePlayLoginReq request)
        {
            return Client.SendRequestAsync<DbGooglePlayLoginReq, DbGooglePlayLoginResp>(CUSTOM_REQUEST_GOOGLE_LOGIN, request);
        }

        protected async UniTaskVoid DbGooglePlayLogin(RequestHandlerData requestHandler, DbGooglePlayLoginReq request, RequestProceedResultDelegate<DbGooglePlayLoginResp> result)
        {
#if (UNITY_EDITOR || UNITY_SERVER || !EXCLUDE_SERVER_CODES) && UNITY_STANDALONE
            result.InvokeSuccess(new DbGooglePlayLoginResp()
            {
                userId = await Database.GooglePlayLogin(request.id, request.email),
            });
#endif
        }
    }
}
