#if UNITY_STANDALONE && !CLIENT_BUILD
using Cysharp.Threading.Tasks;

namespace MultiplayerARPG.MMO
{
    public partial class BaseDatabase
    {
        public const byte AUTH_TYPE_GOOGLE_PLAY = 3;
        public abstract UniTask<string> GooglePlayLogin(string gpgId, string email);
    }
}
#endif