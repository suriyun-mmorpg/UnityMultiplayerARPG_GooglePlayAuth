using Cysharp.Threading.Tasks;
using LiteNetLibManager;

namespace MultiplayerARPG.MMO
{
    public partial interface IDatabaseClient
    {
        UniTask<AsyncResponseData<DbGooglePlayLoginResp>> RequestDbGooglePlayLogin(DbGooglePlayLoginReq request);
    }
}
