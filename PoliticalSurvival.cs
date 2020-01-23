using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins {
    [Info("PoliticalSurvival", "Pho3niX90", "0.5.1")]
    [Description("Political Survival - Become the ruler, tax your subjects and keep them in line!")]
    class PoliticalSurvival : RustPlugin {
        public bool DebugMode = false;
        Settings settings;
        private Core.Libraries.Time _time = GetLibrary<Core.Libraries.Time>();
        static PoliticalSurvival _instance;

        #region Settings Class
        public class Settings {
            public bool showWelcomeMsg = false;
            public Vector3 taxContainerVector3;
            public uint taxContainerID;
            public double tax;
            public ulong ruler;
            public string rulerName;
            public uint rulerSince;
            public string realm;
            public int taxMin;
            public int taxMax;
            public int maxHelis;

            public int heliItemCost;
            public int heliItemCostQty;

            public bool broadcastRulerPosition;
            public int broadcastRulerPositionAfter;

            public Settings(Vector3 tcv4, uint txId, double tx, ulong rlr, string rlrname, string rlm, int taxMi, int taxMx) {
                taxContainerVector3 = tcv4;
                taxContainerID = txId;
                tax = tx;
                ruler = rlr;
                rulerName = rlrname;
                realm = rlm;
                taxMin = taxMi;
                taxMax = taxMx;
            }

            public Settings() { }

            public Settings SetRulerSince(uint since) {
                rulerSince = since;
                return this;
            }

            public long GetRulerSince() {
                return rulerSince;
            }

            public bool GetBroadcastRuler() {
                return broadcastRulerPosition;
            }

            public int GetBroadcastRulerAfter() {
                return broadcastRulerPositionAfter;
            }

            public int GetMaxHelis() {
                return maxHelis;
            }

            public int GetHeliCostItem() {
                return (heliItemCost != 0 ? heliItemCost : 317398316);
            }

            public int GetHeliCostQty() {
                return (heliItemCostQty == 0) ? 1000 : heliItemCostQty;
            }

            public Settings SetTaxContainerVector3(Vector3 vec) {
                taxContainerVector3 = vec;
                return this;
            }

            public Vector3 GetTaxContainerVector3() {
                return taxContainerVector3;
            }
            public Settings SetTaxContainerID(uint storage) {
                taxContainerID = storage;
                return this;
            }
            public uint GetTaxContainerID() {
                return taxContainerID;
            }
            public Settings SetTaxLevel(double tx) {
                tax = tx;
                return this;
            }
            public double GetTaxLevel() {
                return tax;
            }
            public Settings SetRuler(ulong rlr) {
                ruler = rlr;
                rulerSince = (new Core.Libraries.Time()).GetUnixTimestamp();
                return this;
            }
            public ulong GetRuler() {
                return ruler;
            }
            public double GetRuleLengthInMinutes() {
                return (new Core.Libraries.Time().GetUnixTimestamp() - rulerSince) / 60.0 / 1000.0;
            }
            public Settings SetRulerName(string name) {
                rulerName = name;
                return this;
            }
            public string GetRulerName() {
                return rulerName;
            }
            public Settings SetRealmName(string rlm) {
                realm = rlm;
                return this;
            }
            public string GetRealmName() {
                return realm;
            }
            public int GetTaxMin() {
                return taxMin;
            }
            public int GetTaxMax() {
                return taxMax == 0 ? 95 : taxMax;
            }
        }
        #endregion

        #region Components

        #region Heli Vars
        public int HeliLifeTimeMinutes = 5;
        public float HeliBaseHealth = 50000.0f;
        public float HeliSpeed = 50f;
        public float HeliSpeedMax = 200f;
        public int NumRockets = 50;
        public float ScanFrequencySeconds = 5;
        public float TargetVisible = 1000;
        public float MaxTargetRange = 300;
        public bool NotifyPlayers = true;
        public BasePlayer target;
        #endregion

        class HeliComponent : FacepunchBehaviour {
            private BaseHelicopter heli;
            private PatrolHelicopterAI AI;
            private bool isFlying = true;
            private bool isRetiring = false;
            float timer;
            float timerAdd;

            void Awake() {
                heli = GetComponent<BaseHelicopter>();
                AI = heli.GetComponent<PatrolHelicopterAI>();
                heli.startHealth = _instance.HeliBaseHealth;
                AI.maxSpeed = Mathf.Clamp(_instance.HeliSpeed, 0.1f, _instance.HeliSpeedMax);
                AI.numRocketsLeft = _instance.NumRockets;

                attachGuns(AI);
                timerAdd = (Time.realtimeSinceStartup + Convert.ToSingle(_instance.HeliLifeTimeMinutes * 60));
                InvokeRepeating("ScanForTargets", _instance.ScanFrequencySeconds, _instance.ScanFrequencySeconds);
            }

            void FixedUpdate() {
                timer = Time.realtimeSinceStartup;

                if (timer >= timerAdd && !isRetiring) {
                    isRetiring = true;
                }
                if (isRetiring && isFlying) {
                    CancelInvoke("ScanForTargets");
                    isFlying = false;
                    heliRetire();
                }
            }

            internal void ScanForTargets() {
                foreach (ulong targetSteamId in _instance.target.Team.members) {
                    BasePlayer teamMemberToAttack = BasePlayer.Find(targetSteamId.ToString());

                    if (teamMemberToAttack.IsConnected) {
                        UpdateTargets(teamMemberToAttack);
                        _instance.DebugLog("Heli target found " + teamMemberToAttack);
                    }
                    UpdateAi();
                }
            }

            void UpdateAi() {
                _instance.DebugLog("Heli updating AI");
                AI.UpdateTargetList();
                AI.MoveToDestination();
                AI.UpdateRotation();
                AI.UpdateSpotlight();
                AI.AIThink();
                AI.DoMachineGuns();
            }

            void UpdateTargets(BasePlayer Player) {
                AI._targetList.Add(new PatrolHelicopterAI.targetinfo((BaseEntity)Player, Player));
            }

            internal void attachGuns(PatrolHelicopterAI helicopter) {
                if (helicopter == null) return;
                var guns = new List<HelicopterTurret>();
                guns.Add(helicopter.leftGun);
                guns.Add(helicopter.rightGun);
                for (int i = 0; i < guns.Count; i++) {
                    // Leave these as hardcoded for now
                    var turret = guns[i];
                    turret.fireRate = 0.125f;
                    turret.timeBetweenBursts = 3f;
                    turret.burstLength = 3f;
                    turret.maxTargetRange = _instance.MaxTargetRange;
                }
            }

            internal void heliRetire() {
                AI.Retire();
            }

            public void UnloadComponent() {
                Destroy(this);
            }

            void OnDestroy() {
                CancelInvoke("ScanForTargets");
            }
        }
        #endregion

        #region Variables
        Dictionary<string, string> serverMessages;
        int worldSize;
        BasePlayer currentRuler;
        private ILocator liveLocator = null;
        private ILocator locator = null;
        private bool Changed = false;
        private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();
        protected Dictionary<string, Timer> Timers { get { return timers; } }
        #endregion

        private void Init() {
            _instance = this;
            LoadServerMessages();
            LoadSettings();
            Puts("Political Survival is starting...");

            worldSize = ConVar.Server.worldsize;
            liveLocator = new RustIOLocator(worldSize);
            locator = new LocatorWithDelay(liveLocator, 60);


            Puts("Realm name is " + settings.GetRealmName());
            Puts("Tax level is " + settings.GetTaxLevel());
            Puts("Ruler is " + settings.GetRuler());
            Puts("TaxChest is set " + !settings.GetTaxContainerVector3().Equals(Vector3.negativeInfinity));
            Puts("Political Survival: Started");
            currentRuler = GetPlayer(settings.GetRuler().ToString());

            if (settings.GetRulerSince() == 0) {
                settings.SetRulerSince(_time.GetUnixTimestamp());
                SaveSettings();
            }

            if (currentRuler != null) {
                if (settings.GetBroadcastRuler())
                    Timers.Add("AdviseRulerPosition", timer.Repeat(settings.GetBroadcastRulerAfter(), 0, () => AdviseRulerPosition()));

                //TODO add a timer to notify of no ruler
                /*
                if (GameConfig.IsHelpNotiferEnabled)
                    Timers.Add("HelpNotifier", timer.Repeat(GameConfig.HelpNotifierInverval, 0, () => AdviseRules()));
                */
                Timers.Add("RulerPromote", timer.Repeat(30, 0, () => TryForceRuler()));
            }
            SaveSettings();
        }

        void Unload() {
            Puts("Unload called");
            foreach (Timer t in timers.Values)
                t.Destroy();
            timers.Clear();
        }

        void OnPlayerInit(BasePlayer player) {
            if (settings.showWelcomeMsg) PrintToChat(player.displayName + " " + lang.GetMessage("PlayerConnected", this, player.UserIDString) + " " + settings.GetRealmName());
        }

        void OnPlayerDisconnected(BasePlayer player, string reason) {
            if (settings.showWelcomeMsg) PrintToChat(player.displayName + " " + lang.GetMessage("PlayerDisconnected", this, player.UserIDString) + " " + settings.GetRealmName());
        }

        #region GatheringHooks
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item) {
            DebugLog("OnDispenserGather start");
            if (dispenser == null || entity == null || Item == null || settings.GetTaxContainerID() == 0) return;
            BasePlayer player = entity as BasePlayer;
            DebugLog("OnDispenserGather stage 2 " + item.flags.ToString() + " " + item.amount + " " + player.displayName);
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        void OnCropGather(PlantEntity plant, Item item, BasePlayer player) {
            DebugLog("OnPlantGather start");
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) {
            BasePlayer player = entity as BasePlayer;
            DebugLog("OnDispenserBonus start");
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item) {
            DebugLog("OnQuarryGather start");
            int netAmount = AddToTaxContainer(item, quarry.name);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }


        private void OnExcavatorGather(ExcavatorArm excavator, Item item) {
            DebugLog("OnExcavatorGather start");
            int netAmount = AddToTaxContainer(item, excavator.name);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnCollectiblePickup(Item item, BasePlayer player) {
            DebugLog("OnCollectiblePickup start");
            int netAmount = AddToTaxContainer(item, player.displayName);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }

        private void OnSurveyGather(SurveyCharge surveyCharge, Item item) {
            DebugLog("OnSurveyGather start");
            int netAmount = AddToTaxContainer(item, surveyCharge.name);
            item.amount = (netAmount > 0) ? netAmount : item.amount;
        }
        #endregion

        int AddToTaxContainer(Item item, string displayName) {
            DebugLog("AddToTaxContainer start");
            if (item == null || settings.GetTaxContainerID() == 0) return -1;
            DebugLog("AddToTaxContainer st1");
            if (settings.GetTaxLevel() == 0 || settings.GetRuler() == 0) return -1;
            DebugLog("AddToTaxContainer st2");
            if ((settings.GetTaxContainerID() != 0 && FindStorageContainer(settings.GetTaxContainerID()) == null)
                || settings.GetTaxContainerVector3() == Vector3.negativeInfinity) {
                DebugLog("AddToTaxContainer st3");
                settings.SetTaxContainerID(0).SetTaxContainerVector3(Vector3.negativeInfinity);
                DebugLog("AddToTaxContainer st4");
                Puts("There were no tax container set");
                return -1;
            }

            ItemDefinition ToAdd = ItemManager.FindItemDefinition(item.info.itemid);
            int Tax = Convert.ToInt32(Math.Ceiling((item.amount * settings.GetTaxLevel()) / 100));

            if (ToAdd != null) {
                FindStorageContainer(settings.GetTaxContainerID()).inventory.AddItem(ToAdd, Tax);
            }

            DebugLog("User " + displayName + " gathered " + item.amount + " x " + item.info.shortname + ", and " + Tax + " was taxed");
            DebugLog("items added to tax container");
            return item.amount - Tax;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info) {
            DebugLog("OnEntityDeath start");
            BasePlayer player = entity.ToPlayer();

            if (player != null) {
                if (IsRuler(player.userID)) {
                    BasePlayer killer = null;

                    if (info != null)
                        killer = info.Initiator.ToPlayer();

                    if (killer != null && killer.userID != player.userID && !(player is NPCPlayer)) {
                        SetRuler(killer);
                        PrintToChat(string.Format(lang.GetMessage("RulerMurdered", this), killer.displayName));
                    } else {
                        settings.SetRuler(0).SetRulerName(null);
                        PrintToChat(string.Format(lang.GetMessage("RulerDied", this)));
                    }
                    SaveSettings();
                }
            }
        }

        #region Commands
        [ChatCommand("heli")]
        void HeliCommmand(BasePlayer player, string command, string[] args) {
            if (!IsRuler(player.userID)) { PrintToChat(player, "You aren't the boss"); return; }
            if (args.Length != 1) { PrintToChat(player, "Usage '/heli player' where player can also be partial name"); return; }

            BasePlayer playerToAttack = GetPlayer(args[0]);
            if (playerToAttack == null) { PrintToChat(player, "player \"{0}\" not found, or ambiguous", args[0]); return; }

            if (!CanAffordheliStrike(player)) {
                PrintToChat(player, "Ordering a heli strike costs {0} {1}", settings.GetHeliCostQty(), ItemManager.FindItemDefinition(settings.GetHeliCostItem()).displayName.english); return;
            }

            int heliCount = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>().Count();
            if (heliCount >= settings.GetMaxHelis()) {
                PrintToChat(player, "Insufficient airspace for more than {0} helicopters, please wait for existing patrols to complete", settings.GetMaxHelis()); return;
            }

            OrderheliStrike(playerToAttack);
            PrintToChat(player, "The heli is inbound");
        }

        [ChatCommand("taxrange")]
        void AdmSetTaxChestCommand(BasePlayer player, string command, string[] arguments) {
            if (player.IsAdmin && arguments.Length == 2) {
                int.TryParse(arguments[0], out settings.taxMin);
                int.TryParse(arguments[1], out settings.taxMax);
            }
        }

        [ChatCommand("settaxchest")]
        void SetTaxChestCommand(BasePlayer player, string command, string[] arguments) {
            if (!IsRuler(player.userID)) {
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
                return;
            }
            var layers = LayerMask.GetMask("Deployed");
            RaycastHit hit = new RaycastHit();
            DebugLog("Test 1");
            if (Player != null && Physics.Raycast(player.eyes.HeadRay(), out hit, 50, layers)) {
                DebugLog("Test 2");
                BaseEntity entity = hit.GetEntity();
                if (entity != null && (entity.ShortPrefabName.Contains("box.wooden") || entity.ShortPrefabName.Contains("cupboard.tool.deployed"))) {
                    DebugLog("Test 3");
                    Vector3 boxPosition = entity.transform.position;
                    StorageContainer boxStorage = FindStorageContainer(boxPosition);

                    if (boxStorage != null) {
                        DebugLog("Test 4");
                        settings.SetTaxContainerVector3(boxPosition).SetTaxContainerID(entity.net.ID);

                        if (entity.ShortPrefabName.Contains("box.wooden")) {
                            entity.skinID = 1482844040; //https://steamcommunity.com/sharedfiles/filedetails/?id=1482844040&searchtext=
                            entity.SendNetworkUpdate();
                        }

                        DebugLog("Chest set");
                        SaveSettings();
                        SendReply(player, lang.GetMessage("SetNewTaxChest", this, player.UserIDString));
                    }
                } else {
                    DebugLog("Looking at " + entity.ShortPrefabName);
                    SendReply(player, lang.GetMessage("SetNewTaxChestNotFound", this, player.UserIDString));
                    SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
                }
            } else {
                SendReply(player, lang.GetMessage("SetNewTaxChestNotFound", this, player.UserIDString));
                SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
            }
        }

        [ChatCommand("tax")]
        void InfoCommand2(BasePlayer player, string command, string[] arguments) {
            InfoCommand(player, command, arguments);
        }
        [ChatCommand("rinfo")]
        void InfoCommand(BasePlayer player, string command, string[] arguments) {
            string RulerName = string.Empty;

            if (settings.GetRuler() > 0) {
                BasePlayer BaseRuler = BasePlayer.FindAwakeOrSleeping(settings.GetRuler().ToString());
                RulerName = BaseRuler != null ? BaseRuler.displayName : lang.GetMessage("ClaimRuler", this, player.UserIDString);
            } else {
                RulerName = lang.GetMessage("ClaimRuler", this, player.UserIDString);
            }


            if (settings.GetRuler() != 0) {
                SendReply(player, "<color=#008080ff>" + lang.GetMessage("InfoRuler", this, player.UserIDString) + ": </color>" + settings.GetRulerName());
            } else {
                SendReply(player, lang.GetMessage("ClaimRuler", this, player.UserIDString) + ": " + settings.GetRulerName());
            }
            SendReply(player, "<color=#008080ff>" + lang.GetMessage("InfoRealmName", this, player.UserIDString) + ": </color>" + settings.GetRealmName());
            SendReply(player, "<color=#008080ff>" + lang.GetMessage("InfoTaxLevel", this, player.UserIDString) + ": </color>" + settings.GetTaxLevel() + "%");
            if (IsRuler(player.userID)) {
                SendReply(player, lang.GetMessage("SettingNewTaxChest", this, player.UserIDString));
                SendReply(player, string.Format(lang.GetMessage("InfoTaxCmd", this, player.UserIDString), settings.GetTaxMin(), settings.GetTaxMax()) + ": " + settings.GetTaxLevel() + "%");
            }
        }

        [ChatCommand("claimruler")]
        void ClaimRuler(BasePlayer player, string command, string[] arguments) {
            if (settings.GetRuler() < 1) {
                PrintToChat("<color=#008080ff><b>" + player.displayName + "</b></color> " + lang.GetMessage("IsNowRuler", this));
                SetRuler(player);
            }
        }

        [ChatCommand("settax")]
        void SetTaxCommand(BasePlayer player, string command, string[] arguments) {
            if (IsRuler(player.userID)) {
                double newTaxLevel = 0.0;
                if (double.TryParse(MergeParams(0, arguments), out newTaxLevel)) {
                    Puts("Tax have been changed by " + player.displayName + " from " + settings.GetTaxLevel() + " to " + newTaxLevel);
                    if (newTaxLevel == settings.GetTaxLevel())
                        return;

                    if (newTaxLevel > settings.GetTaxMax())
                        newTaxLevel = settings.GetTaxMax();
                    else if (newTaxLevel < settings.GetTaxMin())
                        newTaxLevel = settings.GetTaxMin();

                    SetTaxLevel(newTaxLevel);
                    PrintToChat(string.Format(lang.GetMessage("UpdateTaxMessage", this), player.displayName, newTaxLevel));
                }
            } else
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
        }
        //TODO ended here with case renaming to camelCase
        [ChatCommand("realmname")]
        void RealmNameCommand(BasePlayer player, string command, string[] arguments) {
            if (IsRuler(player.userID)) {
                string NewName = MergeParams(0, arguments);

                if (!String.IsNullOrEmpty(NewName)) {
                    SetRealmName(NewName);
                }
            } else
                SendReply(player, lang.GetMessage("RulerError", this, player.UserIDString));
        }

        [ChatCommand("rplayers")]
        void PlayersCommand(BasePlayer player, string command, string[] arguments) {
            StringBuilder builder = new StringBuilder();
            int playerCount = BasePlayer.activePlayerList.Count;

            builder.Append(string.Format(lang.GetMessage("OnlinePlayers", this), playerCount) + " ");
            List<string> players = new List<string>();

            foreach (BasePlayer pl in BasePlayer.activePlayerList) {
                players.Add(pl.displayName);
            }
            builder.Append(String.Join(", ", players));

            SendReply(player, builder.ToString());
        }
        #endregion

        bool IsPlayerOnline(string partialNameOrID) {
            return GetPlayer(partialNameOrID).IsConnected;
        }

        BasePlayer GetPlayer(string partialNameOrID) {
            return BasePlayer.Find(partialNameOrID);
        }

        string MergeParams(int start, string[] paramz) {
            var merged = new StringBuilder();
            for (int i = start; i < paramz.Length; i++) {
                if (i > start)
                    merged.Append(" ");
                merged.Append(paramz[i]);
            }

            return merged.ToString();
        }

        bool IsRuler(ulong steamId) {
            return settings.GetRuler() == steamId;
        }

        public bool CanAffordheliStrike(BasePlayer player) {
            return player.inventory.GetAmount(settings.GetHeliCostItem()) >= settings.GetHeliCostQty();
        }

        public void OrderheliStrike(BasePlayer playerToAttack) {
            // Deduct the cost
            List<Item> collector = new List<Item>();
            currentRuler.inventory.Take(collector, settings.GetHeliCostItem(), settings.GetHeliCostQty());

            //spawn the birdie
            BaseHelicopter ent = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true) as BaseHelicopter;
            if (ent != null && playerToAttack != null) {
                target = playerToAttack;
                ent.GetComponent<PatrolHelicopterAI>().SetInitialDestination(playerToAttack.transform.position + new Vector3(0.0f, 10f, 0.0f), 0.25f);
                ent.Spawn();
                ent.gameObject.AddComponent<HeliComponent>();

                timer.Once(HeliLifeTimeMinutes * 60, () => ent.GetComponent<HeliComponent>().heliRetire());
            }
        }

        void SetRuler(BasePlayer ruler) {
            Puts("New Ruler! " + ruler.displayName);
            settings.SetRuler(ruler.userID).SetRulerName(ruler.displayName).SetTaxContainerID(0).SetTaxContainerVector3(Vector3.negativeInfinity).SetRealmName(GetMsg("DefaultRealm")).SetRulerSince(_time.GetUnixTimestamp());

            if (settings.GetBroadcastRuler()) {
                timers["AdviseRulerPosition"].Destroy();
                timers.Remove("AdviseRulerPosition");
                Timers.Add("AdviseRulerPosition", timer.Repeat(settings.GetBroadcastRulerAfter(), 0, () => AdviseRulerPosition()));
            }

            SaveSettings();
        }

        void SetTaxLevel(double newTaxLevel) {
            settings.SetTaxLevel(newTaxLevel);
            SaveSettings();
        }

        void SetRealmName(string newName) {
            if (newName.Length > 36)
                newName = newName.Substring(0, 36);
            PrintToChat(string.Format(lang.GetMessage("RealmRenamed", this), newName));
            settings.SetRealmName(newName);
            SaveSettings();
        }

        StorageContainer FindStorageContainer(Vector3 position) {
            foreach (StorageContainer cont in StorageContainer.FindObjectsOfType<StorageContainer>()) {
                Vector3 ContPosition = cont.transform.position;
                if (ContPosition == position) {
                    Puts("Tax Container instance found: " + cont.GetEntity().GetInstanceID());
                    settings.SetTaxContainerID(cont.net.ID);
                    return cont;
                }
            }
            return null;
        }

        StorageContainer FindStorageContainer(uint netid) {
            return (StorageContainer)BaseNetworkable.serverEntities.Find(netid);
        }

        #region Player Grid Coordinates and Locators
        public interface ILocator {
            string GridReference(Component component, out bool moved);
        }

        public class RustIOLocator : ILocator {
            public RustIOLocator(int worldSize) {
                translate = worldSize / 2f;
                scale = worldSize / 26f;
            }

            private readonly float translate;
            private readonly float scale;

            public string GridReference(Component component, out bool moved) {
                var pos = component.transform.position;
                float x = pos.x + translate;
                float z = pos.z + translate;

                int lat = (int)Math.Floor(x / scale);
                char latChar = (char)('A' + lat);
                int lon = 26 - (int)Math.Floor(z / scale);

                moved = false; // We dont know, so just return false
                return string.Format("{0}{1}", latChar, lon);
            }
        }

        public class LocatorWithDelay : ILocator {
            public LocatorWithDelay(ILocator liveLocator, int updateInterval) {
                this.liveLocator = liveLocator;
                this.updateInterval = updateInterval;
            }

            private readonly ILocator liveLocator;
            private readonly int updateInterval;
            private readonly Dictionary<Component, ExpiringCoordinates> locations = new Dictionary<Component, ExpiringCoordinates>();

            public string GridReference(Component component, out bool moved) {
                ExpiringCoordinates item = null;
                bool m;

                if (locations.ContainsKey(component)) {
                    item = locations[component];
                    if (item.Expires < DateTime.Now) {
                        string location = liveLocator.GridReference(component, out m);
                        item.GridChanged = item.Location != location;
                        item.Location = location;
                        item.Expires = DateTime.Now.AddSeconds(updateInterval);
                    }
                } else {
                    item = new ExpiringCoordinates();
                    item.Location = liveLocator.GridReference(component, out m);
                    item.GridChanged = true;
                    item.Expires = DateTime.Now.AddSeconds(updateInterval);
                    locations.Add(component, item);
                }

                moved = item.GridChanged;
                return item.Location;
            }

            class ExpiringCoordinates {
                public string Location { get; set; }
                public bool GridChanged { get; set; }
                public DateTime Expires { get; set; }
            }
        }
        #endregion
        #region Timers and Events

        // AdviceBossPosition is called every n seconds, and updates all played on the current
        // location of the Boss.This is intended to be a negative aspect of being the Boss.
        // If there is no Boss, players are reminded how to become the Boss.
        // TODO: If there is no Boss, after x minutes, just promote someone.
        void AdviseRulerPosition() {
            if (currentRuler != null) {
                bool moved;
                string rulerCoords = locator.GridReference(currentRuler, out moved);

                if (moved)
                    PrintToChat(GetMsg("RulerLocation_Moved"), currentRuler.displayName, rulerCoords);
                else
                    PrintToChat(GetMsg("RulerLocation_Static"), currentRuler.displayName, rulerCoords);
                /*} else
                    PrintToChat(Text.Broadcast_ClaimAvailable);
           }*/
            }
        }

        // AdviseRules is called every m seconds, and reminds players where they can find the
        // Game Mode rules. Useful at the moment, but probably a bit annoying in the long term
        public void AdviseRules() {
            // PrintToChat(Text.Broadcast_HelpAdvice);
        }

        public void TryForceRuler() {
            if (currentRuler == null && TryForceNewRuler())
                PrintToChat("{0} has been made the new Ruler. Kill him!", currentRuler.displayName);
        }

        public bool TryForceNewRuler() {
            if (currentRuler != null) return false;

            SetRuler(BasePlayer.activePlayerList.GetRandom());

            return true;
        }

        #endregion
        void SaveSettings() {
            if (settings.heliItemCost == 0) {
                settings.heliItemCost = settings.GetHeliCostItem();
            }
            if (settings.heliItemCostQty == 0) {
                settings.heliItemCostQty = settings.GetHeliCostQty();
            }
            Interface.Oxide.DataFileSystem.WriteObject<Settings>("PoliticalSurvivalSettings", settings, true);
        }

        void LoadSettings() {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("PoliticalSurvivalSettings")) {
                settings = Interface.Oxide.DataFileSystem.ReadObject<Settings>("PoliticalSurvivalSettings");
                Puts("Settings loaded");
            } else {
                Puts("Settings doesn't exist, creating default");
                settings = new Settings()
                .SetRuler(0)
                .SetRealmName(lang.GetMessage("DefaultRealm", this))
                .SetTaxLevel(0.0)
                .SetTaxContainerID(0)
                .SetTaxContainerVector3(Vector3.negativeInfinity);
                SaveSettings();
            }
        }

        void DebugLog(string msg) {
            if (DebugMode) Puts(msg);
        }
        string GetMsg(string msg) => lang.GetMessage(msg, this);
        void LoadServerMessages() {
            serverMessages = new Dictionary<string, string>();
            serverMessages.Add("StartingInformation", "<color=yellow>Welcome to {0}</color>. If you are new, we run a custom plugin where you can become the server Ruler, tax players, and control the economy. Type <color=#008080ff>/rinfo</color> for more information.");
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
            serverMessages.Add("InfoTaxCmd", "Use <color=#008080ff>/settax {0}-{1}</color> to set tax level");
            serverMessages.Add("RulerLocation_Moved", "Ruler {0} is on the move, now at {1}.");
            serverMessages.Add("RulerLocation_Static", "Ruler {0} is camping out at {1}");
            lang.RegisterMessages(serverMessages, this);
        }
    }
}