﻿using LiteNetLib.Utils;

namespace MultiplayerARPG.MMO
{
    public struct RequestGooglePlayLoginMessage : INetSerializable
    {
        public string idToken;

        public void Deserialize(NetDataReader reader)
        {
            idToken = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(idToken);
        }
    }
}
