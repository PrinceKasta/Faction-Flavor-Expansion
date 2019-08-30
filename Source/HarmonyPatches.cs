using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;
using Verse.Sound;
using System.Reflection;
using RimWorld.Planet;
using Verse.AI.Group;
using UnityEngine;

namespace Flavor_Expansion
{

    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);

        static HarmonyPatches()
        {
            HarmonyInstance harmonyInstance = HarmonyInstance.Create(id: "rimworld.Faction Expansion");

            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }
        //--------------------------------------------------------------------------------------------------------
        [HarmonyPatch(typeof(IncidentWorker_RaidFriendly), "TryExecuteWorker", null)]
        public static class IncidentWorker_RaidFriendly_TryExecuteWorker_Patch
        {
            public static void Postfix(ref IncidentParms parms)
            {
                if (parms.faction == null)
                {
                    return;
                }
                if (Utilities.FactionsWar().GetByFaction(parms.faction) == null)
                    return;
                float count = Utilities.FactionsWar().GetByFaction(parms.faction).disposition * 0.01f;
                if (parms.points * (1 + count) > 35)
                {
                    parms.points *= 1 + count;
                }
                else
                {
                    parms.points = 35;
                }
            }
        }
        //--------------------------------------------------------------------------------------------------------
        [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker", null)]
        public static class IncidentWorker_RaidEnemy_TryExecuteWorker_Patch
        {
            public static void Prefix(ref IncidentParms parms)
            {
                if (Utilities.FactionsWar().GetByFaction(parms.faction) == null)
                    return;
                float count = Utilities.FactionsWar().GetByFaction(parms.faction).disposition * 0.1f;

                if (parms.points * (1 + count) > 35)
                {
                    parms.points *= (1 + count);
                }
            }
        }
        //--------------------------------------------------------------------------------
        [HarmonyPatch(typeof(TradeUtility), "GetPricePlayerSell", null)]
        public static class TradeUtility_GetPricePlayerSell_PrePostfix_Patch
        {
            public static void Prefix(Thing thing, ref float priceGain_FactionBase)
            {
                if (!TradeSession.Active || Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction) == null)
                    return;
                if (Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).disposition < 0)
                {
                    priceGain_FactionBase += Math.Abs(Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).disposition * 0.05f);
                    if (priceGain_FactionBase > 0.5f)
                        priceGain_FactionBase = 0.5f;
                }
            }
        }
        [HarmonyPatch(typeof(TradeUtility), "GetPricePlayerBuy", null)]
        public static class TradeUtility_GetPricePlayerBuy_PrePostfix_Patch
        {
            public static void Prefix(ref float priceGain_FactionBase)
            {
                if (!TradeSession.Active || Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction) == null)
                    return;
                if (Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).disposition < 0)
                {
                    priceGain_FactionBase += Math.Abs(Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).disposition * 0.05f);
                    if (priceGain_FactionBase > 0.5f)
                        priceGain_FactionBase = 0.5f;
                }

            }
        }
        //----------------------------------------------------------------------------------------



        [HarmonyPatch(typeof(Settlement), "GetFloatMenuOptions", null)]
        public static class Settlement_GetFloatMenuOptions_Patch
        {
            public static void Postfix(Caravan caravan, Settlement __instance, ref IEnumerable<FloatMenuOption> __result)
            {
                if (__instance.GetComponent<WorldComp_SettlementDefender>().IsActive())
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();

                    foreach (FloatMenuOption f in CaravanArrivalAction_Defend.GetFloatMenuOptions(caravan, __instance))
                    {
                        list.Add(f);

                    }
                    __result = __result.Concat((IEnumerable<FloatMenuOption>)list.AsEnumerable());
                }
            }
        }
        [HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor", null)]
        public static class FactionDialogMaker_FactionDialogFor_Patch
        {
            public static void Postfix(DiaNode __result, Pawn negotiator, Faction faction)
            {
                if (EndGame_Settings.FactionHistory || EndGame_Settings.FactionHistory || EndGame_Settings.FactionServitude)
                    __result.options.Insert(__result.options.Count - 1, (FactionHistoryDialog.RequestFactionInfoOption(faction, negotiator)));
            }
        }
        //--------------------------------------------------------------------------------

        /*
         * Each Worldobject defeated of a Enemy faction lowers that faction's resources.
         */
        [HarmonyPatch(typeof(SettlementDefeatUtility), "IsDefeated", null)]
        public static class SettlementDefeatUtility_IsDefeated_Patch
        {
            public static void Postfix(Map map, Faction faction, ref bool __result)
            {
                if (map.Parent.GetComponent<WorldComp_JointRaid>().IsActive() || map.Parent.GetComponent<WorldComp_SettlementDefender>().IsActive())

                {
                    __result = false;
                }
                if (__result && Utilities.FactionsWar().GetByFaction(faction) != null)
                    Utilities.FactionsWar().GetByFaction(faction).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE;
            }
        }
        /*
         * Each Pawn killed of a faction lowers that faction's resources.
         */
        [HarmonyPatch(typeof(Pawn), "Kill", null)]
        public static class Pawn_Kill_Patch
        {
            public static void Postfix(Pawn __instance)
            {
                if (!__instance.NonHumanlikeOrWildMan() && !__instance.Faction.IsPlayer && __instance.Faction.PlayerGoodwill < 0 && Utilities.FactionsWar().GetByFaction(__instance.Faction) != null)
                {
                    Utilities.FactionsWar().GetByFaction(__instance.Faction).resources -= 100;
                }
            }
        }

        /*
         * Each item gifted to a faction increases that faction's resources.
         */
        [HarmonyPatch(typeof(TradeDeal), "TryExecute", null)]
        public static class TradeDeal_TryExecute_Patch
        {
            public static void Postfix(TradeDeal __instance, bool __result)
            {
                if (TradeSession.giftMode == true && __result && Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction) != null)
                {
                    float totalValue = 0;
                    foreach (Tradeable t in __instance.AllTradeables)
                    {
                        totalValue += t.BaseMarketValue;
                    }
                    Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).resources += totalValue;
                }
            }
        }
        /*
         * Each item gifted by drop pods to a faction increases that faction's resources.
         */
        [HarmonyPatch(typeof(FactionGiftUtility), "GiveGift", typeof(List < ActiveDropPodInfo >), typeof(SettlementBase))]
        public static class FactionGiftUtility_GiveGift_Patch
        {
            public static void Prefix(List<ActiveDropPodInfo> pods, SettlementBase giveTo)
            {
                if(Utilities.FactionsWar().GetByFaction(giveTo.Faction) != null)
                {
                    float totalValue = 0;
                    foreach (ActiveDropPodInfo i in pods)
                    {
                        
                        foreach (Thing t in i.innerContainer)
                        {
                            totalValue += t.MarketValue;
                        }
                    }
                    Utilities.FactionsWar().GetByFaction(giveTo.Faction).resources += totalValue;
                }
            }
        }
    };

}