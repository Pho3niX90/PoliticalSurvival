using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;

namespace Oxide.Plugins
{
    [Info("OfflineRaidDetect", "Pho3niX90", "1.0.1", ResourceId = 0)]
    class OfflineRaidDetect : RustPlugin
    {
        protected override void LoadDefaultConfig()
        {
        }
        void Init()
        {
        }
        void Loaded()
        {
        }
        void Unload()
        {
        }


        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {

            if (hitInfo != null && entity != null)
            {
                try
                {
                    Report(entity, hitInfo);
                }
                catch (Exception ex)
                {
                    //PrintError(ex.Message);
                    // PrintError(ex.StackTrace);
                }
            }
            return null;
        }

        static string[] ignoreEntities = new string[]{
                 "loot_barrel_1"
                ,"loot_barrel_2"
                ,"oil_barrel"
                ,"hobobarrel_static"
                ,"hotairballoon"
                ,"scientist_corpse"
                ,"bear"
                ,"boar"
                ,"chicken"
                ,"horse"
                ,"stag"
                ,"wolf"
                ,"patrolhelicopter"
                ,"recycler_static"
        };

        object Report(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseAnimalNPC) return null;
            if (info != null && info.InitiatorPlayer != null && entity.OwnerID == info.InitiatorPlayer.userID) return null; // the owner can damage his/her own stuff
            if (ContainsAny(entity.ShortPrefabName, ignoreEntities)) return null;
            if (entity.OwnerID == 0) return null;

            string entityOwner = (FindPlayer(entity.OwnerID) != null) ? FindPlayer(entity.OwnerID).displayName : entity.OwnerID.ToString();

            string playerStatus = FindPlayerStatus(entity.OwnerID);
            string possibleRaid = "";
            if (playerStatus != "Online")
                possibleRaid = "!!!!! - ";

            string Msg = possibleRaid + "User " + info.InitiatorPlayer.displayName
               + " damaged "
               + entity.ShortPrefabName
               + "(" + entity.health + ")"
               + " with "
               + info.WeaponPrefab.ShortPrefabName
               + " that belongs to "
               + entityOwner
               + ":" + playerStatus;

            PrintError(Msg);
            LogToFile("", $"[{DateTime.Now}] {Msg}", this);
            return null;
        }


        BasePlayer FindPlayer(ulong playerID)
        {
            if (playerID.IsSteamId())
            {
                BasePlayer player = FindPlayerByPartialName(playerID.ToString());

                if (player)
                {
                    return player;
                }
                else
                {
                    BasePlayer p = covalence.Players.FindPlayerById(playerID.ToString())?.Object as BasePlayer;
                    return p;
                }
            }
            return null;
        }

        BasePlayer FindPlayerByPartialName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            IPlayer player = covalence.Players.FindPlayer(name);

            if (player != null)
            {
                return (BasePlayer)player.Object;
            }

            return null;
        }


        string FindPlayerStatus(ulong playerID)
        {
            if (playerID.IsSteamId())
            {
                var player = FindPlayerByPartialName(playerID.ToString());
                if (player)
                {
                    if (player.IsSleeping())
                    {
                        return "Sleeping";
                    }

                    return "Online";
                }

                var p = covalence.Players.FindPlayerById(playerID.ToString());
                if (p != null)
                {
                    return "Offline";
                }
            }

            return $"Unknown";
        }

        bool ContainsAny(string haystack, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (haystack.Contains(needle))
                    return true;
            }
            return false;
        }
    }
}