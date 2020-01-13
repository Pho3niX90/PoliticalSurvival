﻿using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PoliticalSurvival", "Pho3niX90", "0.4.0")]
    [Description("Political Survival - Become the ruler, tax your subjects and keep them in line!")]
    class PoliticalSurvival : RustPlugin
    {
        public bool DebugMode = false;
        Settings settings;

        #region Settings Class
        public class Settings
        {
            public bool showWelcomeMsg = false;
            public Vector3 taxContainerVector3;
            public uint taxContainerID;
            public double tax;
            public ulong ruler;
            public string rulerName;
            public string realm;

            public Settings(Vector3 tcv4, uint txId, double tx, ulong rlr, string rlrname, string rlm)
            {
                taxContainerVector3 = tcv4;
                taxContainerID = txId;
                tax = tx;
                ruler = rlr;
                rulerName = rlrname;
                realm = rlm;
            }
            public Settings() { }
            public Settings setTaxContainerVector3(Vector3 vec)
            {
                taxContainerVector3 = vec;
                return this;
            }
            public Vector3 getTaxContainerVector3()
            {
                return taxContainerVector3;
            }
            public Settings setTaxContainerID(uint storage)
            {
                taxContainerID = storage;
                return this;
            }
            public uint getTaxContainerID()
            {
                return taxContainerID;
            }
            public Settings setTaxLevel(double tx)
            {
                tax = tx;
                return this;
            }
            public double getTaxLevel()
            {
                return tax;
            }
            public Settings setRuler(ulong rlr)
            {
                ruler = rlr;
                return this;
            }
            public ulong getRuler()
            {
                return ruler;
            }
            public Settings setRulerName(string name)
            {
                rulerName = name;
                return this;
            }
            public string getRulerName()
            {
                return rulerName;
            }
            public Settings setRealmName(string rlm)
            {
                realm = rlm;
                return this;
            }
            public string getRealmName()
            {
                return realm;
            }
        }
        #endregion
        #region Variables
        Dictionary<string, string> serverMessages;

        double TaxMin = 0.0;
        double TaxMax = 95.0;
        #endregion

        private void Init()
        {
            LoadServerMessages();
            LoadSettings();
            Puts("Political Survival is starting...");


            Puts("Realm name is " + settings.getRealmName());
            Puts("Tax level is " + settings.getTaxLevel());
            Puts("Ruler is " + settings.getRuler());
            Puts("TaxChest is set " + !settings.getTaxContainerVector3().Equals(Vector3.negativeInfinity));
            Puts("Political Survival: Started");
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (settings.showWelcomeMsg) PrintToChat(player.displayName + " " + lang.GetMessage("PlayerConnected", this, player.UserIDString) + " " + settings.getRealmName());
        }

        void OnPlayerDisconnected(BasePlayer player, string Reason)
        {
            if (settings.showWelcomeMsg) PrintToChat(player.displayName + " " + lang.GetMessage("PlayerDisconnected", this, player.UserIDString) + " " + settings.getRealmName());
        }

        #region GatheringHooks
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            DebugLog("OnDispenserGather start");
            if (dispenser == null || entity == null || Item == null || settings.getTaxContainerID() == 0) return;
            BasePlayer player = entity as BasePlayer;
            DebugLog("OnDispenserGather stage 2 " + item.flags.ToString() + " " + item.amount + " " + player.displayName);
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
        {
            DebugLog("OnPlantGather start");
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }
        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity as BasePlayer;
            DebugLog("OnDispenserBonus start");
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }
        private void OnQuarryGather(MiningQuarry quarry, Item Item)
        {
            DebugLog("OnQuarryGather start");
            int netAmount = AddToTaxContainer(Item, quarry.name);
            Item.amount = (netAmount > 0) ? netAmount : Item.amount;
        }


        private void OnExcavatorGather(ExcavatorArm excavator, Item item)
        {
            DebugLog("OnExcavatorGather start");
            int netAmount = AddToTaxContainer(item, excavator.name);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            DebugLog("OnCollectiblePickup start");
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnSurveyGather(SurveyCharge surveyCharge, Item item)
        {
            DebugLog("OnSurveyGather start");
            int netAmount = AddToTaxContainer(item, surveyCharge.name);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }
        #endregion
        int AddToTaxContainer(Item item, string displayName)
        {
            DebugLog("AddToTaxContainer start");
            if (item == null || settings.getTaxContainerID() == 0) return -1;
            if (settings.getTaxLevel() == 0 || settings.getRuler() == 0) return -1;
            if ((settings.getTaxContainerVector3() == Vector3.negativeInfinity && FindStorageContainer(settings.getTaxContainerID()) != null)
                || settings.getTaxContainerID() == 0 || settings.getTaxContainerVector3() == Vector3.negativeInfinity)
            {
                settings.setTaxContainerID(0).setTaxContainerVector3(Vector3.negativeInfinity);
                SaveSettings();
                Puts("There were no tax container set");
                return -1;
            }

            ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);
            int Tax = Convert.ToInt32(Math.Round((item.amount * settings.getTaxLevel()) / 100));

            if (ToAdd != null)
            {
                FindStorageContainer(settings.getTaxContainerID()).inventory.AddItem(ToAdd, Tax);
            }

            DebugLog("User " + displayName + " gathered " + item.amount + " x " + item.info.shortname + ", and " + Tax + " was taxed");
            DebugLog("items added to tax container");
            return item.amount - Tax;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            DebugLog("OnEntityDeath start");
            BasePlayer player = entity.ToPlayer();

            if (player != null)
            {
                if (IsRuler(player.userID))
                {
                    BasePlayer killer = null;

                    if (info != null)
                        killer = info.Initiator.ToPlayer();

                    if (killer != null && killer.userID != player.userID)
                    {
                        SetRuler(killer);
                        PrintToChat(string.Format(lang.GetMessage("RulerMurdered", this), killer.displayName));
                    }
                    else
                    {
                        settings.setRuler(0).setRulerName(null);
                        PrintToChat(string.Format(lang.GetMessage("RulerDied", this)));
                    }
                }
            }
        }

        [ChatCommand("settaxchest")]
        void SetTaxChestCommand(BasePlayer player, string command, string[] arguments)
        {
            if (!IsRuler(player.userID))
            {
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
                return;
            }
            var layers = LayerMask.GetMask("Deployed");
            RaycastHit hit = new RaycastHit();
            DebugLog("Test 1");
            if (Player != null && Physics.Raycast(player.eyes.HeadRay(), out hit, 50, layers))
            {
                DebugLog("Test 2");
                BaseEntity entity = hit.GetEntity();
                if (entity != null && (entity.ShortPrefabName.Contains("box.wooden") || entity.ShortPrefabName.Contains("cupboard.tool.deployed")))
                {
                    DebugLog("Test 3");
                    Vector3 boxPosition = entity.transform.position;
                    StorageContainer boxStorage = FindStorageContainer(boxPosition);

                    if (boxStorage != null)
                    {
                        DebugLog("Test 4");
                        settings.setTaxContainerVector3(boxPosition).setTaxContainerID(entity.net.ID);

                        if (entity.ShortPrefabName.Contains("box.wooden"))
                        {
                            entity.skinID = 1482844040; //https://steamcommunity.com/sharedfiles/filedetails/?id=1482844040&searchtext=
                            entity.SendNetworkUpdate();
                        }

                        DebugLog("Chest set");
                        SaveSettings();
                        SendReply(player, lang.GetMessage("SetNewTaxChest", this, player.UserIDString));
                    }
                }
                else
                {
                    DebugLog("Looking at " + entity.ShortPrefabName);
                    SendReply(player, lang.GetMessage("SetNewTaxChestNotFound", this, player.UserIDString));
                    SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
                }
            }
            else
            {
                SendReply(player, lang.GetMessage("SetNewTaxChestNotFound", this, player.UserIDString));
                SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
            }
        }

        [ChatCommand("pinfo")]
        void InfoCommand(BasePlayer player, string command, string[] arguments)
        {
            string RulerName = string.Empty;

            if (settings.getRuler() > 0)
            {
                BasePlayer BaseRuler = BasePlayer.FindAwakeOrSleeping(settings.getRuler().ToString());
                RulerName = BaseRuler != null ? BaseRuler.displayName : lang.GetMessage("ClaimRuler", this, player.UserIDString);
            }
            else
            {
                RulerName = lang.GetMessage("ClaimRuler", this, player.UserIDString);
            }


            if (settings.getRuler() != 0)
            {
                SendReply(player, lang.GetMessage("InfoRuler", this, player.UserIDString) + ": " + settings.getRulerName());
            }
            else
            {
                SendReply(player, lang.GetMessage("ClaimRuler", this, player.UserIDString) + ": " + settings.getRulerName());
            }
            SendReply(player, lang.GetMessage("InfoRealmName", this, player.UserIDString) + ": " + settings.getRealmName());
            SendReply(player, lang.GetMessage("InfoTaxLevel", this, player.UserIDString) + ": " + settings.getTaxLevel() + "%");
            if (IsRuler(player.userID))
            {
                SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
                SendReply(player, lang.GetMessage("InfoTaxCmd", this, player.UserIDString) + ": " + settings.getTaxLevel() + "%");
            }
        }

        [ChatCommand("claimruler")]
        void ClaimRuler(BasePlayer player, string command, string[] arguments)
        {
            if (settings.getRuler() < 1)
            {
                PrintToChat("<color=#008080ff><b>" + player.displayName + "</b></color> " + lang.GetMessage("IsNowRuler", this));
                SetRuler(player);
            }
        }

        [ChatCommand("settax")]
        void SetTaxCommand(BasePlayer player, string command, string[] arguments)
        {
            if (IsRuler(player.userID))
            {
                double newTaxLevel = 0.0;
                if (double.TryParse(MergeParams(0, arguments), out newTaxLevel))
                {
                    Puts("Tax have been changed by " + player.displayName + " from " + settings.getTaxLevel() + " to " + newTaxLevel);
                    if (newTaxLevel == settings.getTaxLevel())
                        return;

                    if (newTaxLevel > TaxMax)
                        newTaxLevel = TaxMax;
                    else if (newTaxLevel < TaxMin)
                        newTaxLevel = TaxMin;

                    SetTaxLevel(newTaxLevel);
                    PrintToChat(string.Format(lang.GetMessage("UpdateTaxMessage", this), player.displayName, newTaxLevel));
                }
            }
            else
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
        }
        //TODO ended here with case renaming to camelCase
        [ChatCommand("realmname")]
        void RealmNameCommand(BasePlayer player, string command, string[] arguments)
        {
            if (IsRuler(player.userID))
            {
                string NewName = MergeParams(0, arguments);

                if (!String.IsNullOrEmpty(NewName))
                {
                    SetRealmName(NewName);
                }
            }
            else
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
        }

        [ChatCommand("pplayers")]
        void PlayersCommand(BasePlayer player, string command, string[] arguments)
        {
            StringBuilder builder = new StringBuilder();
            int playerCount = BasePlayer.activePlayerList.Count;

            builder.Append(string.Format(lang.GetMessage("OnlinePlayers", this), playerCount) + " ");
            builder.Append(String.Join(", ", BasePlayer.activePlayerList));

            SendReply(player, builder.ToString());
        }

        bool IsPlayerOnline(string partialNameOrID)
        {
            return GetPlayer(partialNameOrID).IsConnected;
        }

        BasePlayer GetPlayer(string partialNameOrID)
        {
            return covalence.Players.FindPlayer(partialNameOrID).Object as BasePlayer;
        }

        string MergeParams(int start, string[] paramz)
        {
            var merged = new StringBuilder();
            for (int i = start; i < paramz.Length; i++)
            {
                if (i > start)
                    merged.Append(" ");
               merged.Append(paramz[i]);
            }

            return merged.ToString();
        }

        bool IsRuler(ulong steamId)
        {
            return settings.getRuler() == steamId;
        }

        void SetRuler(BasePlayer ruler)
        {
            Puts("New Ruler!");
            settings.setRuler(ruler.userID).setRulerName(ruler.displayName).setTaxContainerID(0).setTaxContainerVector3(Vector3.negativeInfinity);
            SaveSettings();
        }

        void SetTaxLevel(double newTaxLevel)
        {
            settings.setTaxLevel(newTaxLevel);
            SaveSettings();
        }

        void SetRealmName(string newName)
        {
            if (newName.Length > 36)
                newName = newName.Substring(0, 36);
            PrintToChat(string.Format(lang.GetMessage("RealmRenamed", this), newName));
            settings.setRealmName(newName);
            SaveSettings();
        }

        StorageContainer FindStorageContainer(Vector3 position)
        {
            foreach (StorageContainer cont in StorageContainer.FindObjectsOfType<StorageContainer>())
            {
                Vector3 ContPosition = cont.transform.position;
                if (ContPosition == position)
                {
                    Puts("Tax Container instance found: " + cont.GetEntity().GetInstanceID());
                    settings.setTaxContainerID(cont.net.ID);
                    return cont;
                }
            }
            return null;
        }

        StorageContainer FindStorageContainer(uint netid)
        {
            return (StorageContainer)BaseNetworkable.serverEntities.Find(netid);
        }

        void SaveSettings()
        {
            Puts(settings.getRulerName());
            Interface.Oxide.DataFileSystem.WriteObject<Settings>("PoliticalSurvivalSettings", settings, true);
        }

        void LoadSettings()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("PoliticalSurvivalSettings"))
            {
                settings = Interface.Oxide.DataFileSystem.ReadObject<Settings>("PoliticalSurvivalSettings");
                Puts("Settings loaded");
            }
            else
            {
                Puts("Settings doesn't exist, creating default");
                settings = new Settings()
                .setRuler(0)
                .setRealmName(lang.GetMessage("DefaultRealm", this))
                .setTaxLevel(0.0)
                .setTaxContainerID(0)
                .setTaxContainerVector3(Vector3.negativeInfinity);
                SaveSettings();
            }
        }

        private void DebugLog(string msg)
        {
            if (DebugMode) Puts(msg);
        }
        private void LoadServerMessages()
        {
            serverMessages = new Dictionary<string, string>();
            serverMessages.Add("StartingInformation", "<color=yellow>Welcome to {0}</color>. If you are new, we run a custom plugin where you can become the server Ruler, tax players, and control the economy. Type /pinfo for more information.");
            serverMessages.Add("PlayerConnected", "has connected to");
            serverMessages.Add("PlayerDisconnected", "has disconnected from");
            serverMessages.Add("RulerDied", "<color=#ff0000ff>The Ruler has died!</color>");
            serverMessages.Add("RulerMurdered", "<color=#ff0000ff>The Ruler has been murdered by {0}, who is now the new Ruler.</color>");
            serverMessages.Add("RealmRenamed", "The realm has been renamed to <color=#008080ff>{0}</color>");
            serverMessages.Add("DefaultRealm", "Land of the cursed");
            serverMessages.Add("OnlinePlayers", "Online players ({0}):");
            serverMessages.Add("PrivateError", "is either offline or you typed the name wrong.");
            serverMessages.Add("PrivateFrom", "PM from");
            serverMessages.Add("PrivateTo", "PM sent to");
            serverMessages.Add("RulerError", "You need to be the Ruler to do that!");
            serverMessages.Add("SettingNewTaxChest", "Look at a Wooden box  or TC and type <color=#008080ff>/settaxchest</color>");
            serverMessages.Add("SetNewTaxChestNotFound", "You must look at a wooden box or TC to set tax chest");
            serverMessages.Add("SetNewTaxChest", "You have set the new tax chest.");
            serverMessages.Add("ClaimRuler", "There is no ruler! <color=#008080ff>/claimruler</color> to become the new Ruler!");
            serverMessages.Add("IsNowRuler", "is now the Ruler!");
            serverMessages.Add("InfoRuler", "Ruler");
            serverMessages.Add("InfoRealmName", "Realm Name");
            serverMessages.Add("InfoTaxLevel", "Tax level");
            serverMessages.Add("InfoTaxCmd", "Use <color=#008080ff>/settax 0-95</color> to set tax level");
            serverMessages.Add("UpdateTaxMessage", "Ruler {0} has set Tax to {1}%");
            lang.RegisterMessages(serverMessages, this);
        }
    }
}