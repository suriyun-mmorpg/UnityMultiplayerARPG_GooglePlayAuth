﻿namespace MultiplayerARPG.MMO
{
    public partial class BaseDatabase
    {
        public const byte AUTH_TYPE_GOOGLE_PLAY = 3;
        public abstract string GooglePlayLogin(string gId, string email);
    }
}
