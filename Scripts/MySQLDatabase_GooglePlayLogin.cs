using MySqlConnector;
using System.Threading.Tasks;

namespace MultiplayerARPG.MMO
{
    public partial class MySQLDatabase
    {
        public override async Task<string> GooglePlayLogin(string gpgId, string email)
        {
            string id = string.Empty;
            await ExecuteReader((reader) =>
            {
                if (reader.Read())
                    id = reader.GetString("id");
            }, "SELECT id FROM userlogin WHERE username=@username AND password=@password AND authType=@authType LIMIT 1",
                new MySqlParameter("@username", "g_" + gpgId),
                new MySqlParameter("@password", GenericUtils.GetMD5(gpgId)),
                new MySqlParameter("@authType", AUTH_TYPE_GOOGLE_PLAY));

            if (string.IsNullOrEmpty(id))
            {
                id = GenericUtils.GetUniqueId();
                await ExecuteNonQuery("INSERT INTO userlogin (id, username, password, email, authType) VALUES (@id, @username, @password, @email, @authType)",
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@username", "g_" + gpgId),
                    new MySqlParameter("@password", GenericUtils.GetMD5(gpgId)),
                    new MySqlParameter("@email", email),
                    new MySqlParameter("@authType", AUTH_TYPE_GOOGLE_PLAY));
            }
            return id;
        }
    }
}
