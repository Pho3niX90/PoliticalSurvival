using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System.Collections;
using System.Globalization;

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
            try
            {
                lang.RegisterMessages(new Dictionary<string, string>
                {
                    { "localtime", "Local time is {localtime}." },
                    { "nodamage", "You are not allowed to offline raid." },
                    { "starts", "LocalTimeDamageControl starts at {starts}." },
                    { "remains", "LocalTimeDamageControl remains on for {remains}." },
                    { "status", "LocalTimeDamageControl is {status}." },
                    { "duration", "LocalTimeDamageControl duration is {duration} minutes." },
                    { "errorstart", "Error, please 24 hour time format: i.e 08:00 for 8 am." },
                    { "errormin", "Error, please enter an integer i.e: 60 for 60 minutes." },
                    { "errorhour", "Error, please enter an integer i.e: 2 for 180 minutes." },
                    { "help1", "/lset start 08:00 ~ Set start time for damage control." },
                    { "help2", "/lset minutes 60  ~ Set duration in minutes for damage control."},
                    { "help3", "/lset hours 12    ~ Set duration in hours for damage control."},
                    { "help4", "/lset off         ~ Turn off damage control."},
                    { "help5", "/lset on          ~ Turn on damage control during set times. "},
                    { "help6", "- starts at {starttime} ends at {endtime}."}
            }, this, "en");

            }
            catch (Exception ex)
            {
                PrintError($"Error Loaded: {ex.StackTrace}");
            }
        }
        void Unload()
        {
        }


        public DateTime getStartTime()
        {
            return DateTime.Parse(Config["LocalTimeDamageControlStart"].ToString());
        }

        public DateTime getEndTime()
        {
            return DateTime.Parse(Config["LocalTimeDamageControlStart"].ToString()).AddMinutes(int.Parse(Config["LocalTimeDamageControlDuratationMinutes"].ToString()));
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity is BasePlayer) return null;                          // we do not care about KOS
                if (entity.OwnerID == info.InitiatorPlayer.userID) return null; // the owner can damage his/her own stuff

                if (info.InitiatorPlayer != null)
                    PrintWarning(info.InitiatorPlayer, "User " + " damaged " + entity.);

                info.damageTypes.ScaleAll(0.0f);                                // no damage
                return false;
            }
            catch (Exception ex)
            {
                PrintError("Error OfflineRaidDetect: " + ex.Message);
            }
            return null;
        }

    }
}