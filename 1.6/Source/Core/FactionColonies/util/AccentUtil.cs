using System;
using System.Linq;
using FactionColonies.util;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public static class AccentUtil
    {
        // === Profit/Loss (Overview, Bills) ===
        public static readonly Color Income  = new Color(0.2f, 0.85f, 0.3f);
        public static readonly Color Expense = new Color(1.0f, 0.35f, 0.3f);

        // === Event Categories ===
        public static readonly Color EventSettlement   = new Color(0.2f, 0.9f, 0.85f);
        public static readonly Color EventConstruction = new Color(1.0f, 0.65f, 0.1f);
        public static readonly Color EventEconomy      = new Color(1.0f, 0.85f, 0.1f);
        public static readonly Color EventPolicy       = new Color(0.4f, 0.55f, 1.0f);
        public static readonly Color EventMilitary     = new Color(1.0f, 0.25f, 0.25f);
        public static readonly Color EventOther        = new Color(0.65f, 0.65f, 0.65f);

        // === Military Status ===
        public static readonly Color MilUnderAttack   = new Color(1.0f, 0.25f, 0.25f);
        public static readonly Color MilActiveMission = new Color(1.0f, 0.65f, 0.1f);
        public static readonly Color MilCooldown      = new Color(1.0f, 0.85f, 0.1f);
        public static readonly Color MilReady         = new Color(0.2f, 0.85f, 0.3f);
        public static readonly Color MilInactive      = new Color(0.65f, 0.65f, 0.65f);

        // === Stat Thresholds ===
        public static readonly Color StatGood   = new Color(0.2f, 0.85f, 0.3f);
        public static readonly Color StatMedium = new Color(1f, 0.7f, 0.2f);
        public static readonly Color StatBad    = new Color(1f, 0.35f, 0.3f);

        public static Color GetSettlementAccent(WorldSettlementFC s)
        {
            return s.settlementDef.accentColor ?? (s.getTotalProfit() >= 0 ? Income : Expense);
        }

        public static Color GetStatColor(float value, bool inverted)
        {
            if (inverted)
            {
                if (value <= 10f) return StatGood;
                if (value <= 30f) return StatMedium;
                return StatBad;
            }
            if (value >= 80f) return StatGood;
            if (value >= 50f) return StatMedium;
            return StatBad;
        }

        public static Color GetEventCategoryColor(FCEvent evt)
        {
            string name = evt.def.defName ?? "";
            if (name == "settleNewColony" || name == "upgradeSettlement")
                return EventSettlement;
            if (name == "constructBuilding")
                return EventConstruction;
            if (name == "taxColony" || name == "deliveryArrival")
                return EventEconomy;
            if (name == "enactSettlementPolicy" || name == "enactFactionPolicy")
                return EventPolicy;
            if (evt.isMilitaryEvent || name.StartsWith("raid") || name.StartsWith("enslave")
                || name.StartsWith("capture") || name == "cooldownMilitary" || name == "settlementBeingAttacked")
                return EventMilitary;
            return EventOther;
        }

        public static Color GetMilitaryAccent(WorldObjectComp_SettlementMilitary milComp)
        {
            if (milComp == null) return MilInactive;
            if (milComp.isUnderAttack) return MilUnderAttack;
            if (milComp.militaryBusy && milComp.militaryJob != MilitaryJob.Undefined
                && milComp.militaryJob != MilitaryJob.Cooldown)
                return MilActiveMission;
            if (milComp.militaryJob == MilitaryJob.Cooldown) return MilCooldown;
            if (milComp.militarySquad?.outfit != null && !milComp.militaryBusy)
                return MilReady;
            return MilInactive;
        }

        public static string GetMilitaryStatusLabel(WorldObjectComp_SettlementMilitary milComp, WorldSettlementFC settlement = null)
        {
            if (milComp == null) return "FCMilStatusNoSquad".Translate();
            if (milComp.isUnderAttack) return "FCMilStatusUnderAttack".Translate();
            if (milComp.militaryBusy)
            {
                switch (milComp.militaryJob)
                {
                    case MilitaryJob.Deploy:
                        return "FCMilStatusDeployed".Translate();
                    case MilitaryJob.RaidEnemySettlement:
                        return "FCMilStatusRaiding".Translate();
                    case MilitaryJob.EnslaveEnemySettlement:
                        return "FCMilStatusEnslaving".Translate();
                    case MilitaryJob.CaptureEnemySettlement:
                        return "FCMilStatusCapturing".Translate();
                    case MilitaryJob.DefendFriendlySettlement:
                        return "FCMilStatusDefending".Translate();
                    case MilitaryJob.Cooldown:
                        return GetCooldownLabel(settlement);
                    default:
                        return "FCMilStatusBusy".Translate();
                }
            }
            if (milComp.militarySquad?.outfit != null) return "FCMilStatusReady".Translate();
            return "FCMilStatusNoSquad".Translate();
        }

        private static string GetCooldownLabel(WorldSettlementFC settlement)
        {
            string label = "FCMilStatusCooldown".Translate();
            if (settlement == null) return label;

            FCEvent cooldownEvent = FactionCache.FactionComp?.events?
                .FirstOrDefault(e => e.def.defName == "cooldownMilitary" && e.location == settlement.Tile);
            if (cooldownEvent != null)
            {
                int ticksLeft = Math.Max(0, cooldownEvent.timeTillTrigger - Find.TickManager.TicksGame);
                label += " " + ticksLeft.ToTimeString();
            }
            return label;
        }
    }
}
