using System.Threading.Tasks;

namespace MultiplayerARPG.MMO
{
    public partial class BaseDatabase
    {
        public const byte AUTH_TYPE_GOOGLE_PLAY = 3;
        public abstract Task<string> GooglePlayLogin(string gpgId, string email);
    }
}
