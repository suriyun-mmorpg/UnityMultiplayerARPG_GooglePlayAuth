using Mono.Data.Sqlite;

namespace MultiplayerARPG.MMO
{
    public partial class SQLiteDatabase
    {
        public override string GooglePlayLogin(string gId, string email)
        {
            string id = string.Empty;
            SQLiteRowsReader reader = ExecuteReader("SELECT id FROM userlogin WHERE username=@username AND password=@password AND authType=@authType LIMIT 1",
                new SqliteParameter("@username", "g_" + gId),
                new SqliteParameter("@password", GenericUtils.GetMD5(gId)),
                new SqliteParameter("@authType", AUTH_TYPE_GOOGLE_PLAY));

            if (reader.Read())
                id = reader.GetString("id");
            else
            {
                ExecuteNonQuery("INSERT INTO userlogin (id, username, password, email, authType) VALUES (@id, @username, @password, @email, @authType)",
                    new SqliteParameter("@id", GenericUtils.GetUniqueId()),
                    new SqliteParameter("@username", "g_" + gId),
                    new SqliteParameter("@password", GenericUtils.GetMD5(gId)),
                    new SqliteParameter("@email", email),
                    new SqliteParameter("@authType", AUTH_TYPE_GOOGLE_PLAY));

                // Read last entry
                reader = ExecuteReader("SELECT id FROM userlogin WHERE username=@username AND password=@password AND authType=@authType LIMIT 1",
                    new SqliteParameter("@username", "g_" + gId),
                    new SqliteParameter("@password", GenericUtils.GetMD5(gId)),
                    new SqliteParameter("@authType", AUTH_TYPE_GOOGLE_PLAY));

                if (reader.Read())
                    id = reader.GetString("id");
            }
            return id;
        }
    }
}
