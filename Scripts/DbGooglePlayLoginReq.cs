using LiteNetLib.Utils;

namespace MultiplayerARPG.MMO
{
    public struct DbGooglePlayLoginReq : INetSerializable
    {
        public string id;
        public string email;

        public void Deserialize(NetDataReader reader)
        {
            id = reader.GetString();
            email = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(id);
            writer.Put(email);
        }
    }
}
