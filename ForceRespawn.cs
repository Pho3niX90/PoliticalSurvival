namespace Oxide.Plugins {
    [Info("ForceRespawn", "Pho3niX90", "0.1.0")]
    [Description("Forces a player respawn when they cannot from death screen")]
    class ForceRespawn : RustPlugin {
        [Command("delpl")]
        void KillPLayerEnt2(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin || args.Length == 0) return;
            foreach (BasePlayer playerF in BasePlayer.activePlayerList) {
                if (playerF.IsDead() && args[0].Equals(playerF.UserIDString)) {
                    playerF.LifeStoryEnd();
                    playerF.Respawn();
                }
            }
        }

        [Command("frespawn")]
        void KillPLayerEnt3(BasePlayer player, string command, string[] args) {
            foreach (BasePlayer playerF in BasePlayer.activePlayerList) {
                if (playerF.IsDead() && player.UserIDString.Equals(playerF.UserIDString)) {
                    playerF.LifeStoryEnd();
                    playerF.Respawn();
                }
            }
        }

        [ChatCommand("delpl")]
        void KillPLayerEnt(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin || args.Length == 0) return;
            foreach (BasePlayer playerF in BasePlayer.activePlayerList) {
                if (playerF.IsDead() && args[0].Equals(playerF.UserIDString)) {
                    playerF.LifeStoryEnd();
                    playerF.Respawn();
                }
            }
        }
    }
}