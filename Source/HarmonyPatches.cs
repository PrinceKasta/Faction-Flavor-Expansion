using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using Harmony;
using Verse.Sound;
using System.Reflection;
using System.Reflection.Emit;
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
            harmonyInstance.Patch(AccessTools.Method(typeof(IncidentWorker_RaidFriendly), "TryExecuteWorker"), null,
                new HarmonyMethod(type: patchType, name: nameof(IncidentWorker_RaidFriendly_TryExecuteWorker_Patch)));
            harmonyInstance.Patch(AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker"), null,
                new HarmonyMethod(type: patchType, name: nameof(IncidentWorker_RaidEnemy_TryExecuteWorker_Patch)));
            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(MainTabWindow_Factions), name: "DoWindowContents"), prefix: null,
                postfix: new HarmonyMethod(type: patchType, name: nameof(MainTabWindow_Factions_DoWindowContents_Patch)), transpiler: null);
            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(MainTabWindow_Factions), name: "DoWindowContents"), prefix: null,
                postfix: new HarmonyMethod(type: patchType, name: nameof(MainTabWindow_Factions_DoWindowContents_Patch)), transpiler: null);
            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(TradeUtility), name: "GetPricePlayerBuy"), prefix: new HarmonyMethod(type: patchType, name: nameof(TradeUtility_GetPricePlayerBuy_Prefix_Patch)),
                postfix: null, transpiler: null);
            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(TradeUtility), name: "GetPricePlayerSell"), prefix: new HarmonyMethod(type: patchType, name: nameof(TradeUtility_GetPricePlayerSell_Prefix_Patch)),
                postfix: null, transpiler: null);
            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(Settlement), name: "GetFloatMenuOptions"), prefix: null,
                postfix: new HarmonyMethod(type: patchType, name: nameof(Settlement_GetFloatMenuOptions_Postfix)), transpiler: null);
            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(FactionDialogMaker), name: "FactionDialogFor"), prefix: null,
                postfix: new HarmonyMethod(type: patchType, name: nameof(FactionDialogMaker_FactionDialogFor_Postfix)), transpiler: null);
            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(SettlementDefeatUtility), name: "IsDefeated"), prefix: null,
                postfix: new HarmonyMethod(type: patchType, name: nameof(SettlementDefeatUtility_IsDefeated_Postfix)), transpiler: null);
            harmonyInstance.Patch(original: AccessTools.Method(type: typeof(Pawn), name: "Kill"), prefix: null,
                postfix: new HarmonyMethod(type: patchType, name: nameof(Pawn_Kill_Postfix)), transpiler: null);

            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }
        //--------------------------------------------------------------------------------------------------------

        public static void IncidentWorker_RaidFriendly_TryExecuteWorker_Patch(ref IncidentParms parms)
        {
            if (parms.faction == null || parms.points <= 35)
            {
                return;
            }
            if (Utilities.FactionsWar().GetByFaction(parms.faction) == null)
                return;
            
            parms.points *= 1 + Utilities.FactionsWar().GetByFaction(parms.faction).disposition * 0.01f;
            parms.points += Investments.InvestmentReourceWorth(Utilities.FactionsWar().GetByFaction(parms.faction));
        }

        //--------------------------------------------------------------------------------------------------------

        public static void IncidentWorker_RaidEnemy_TryExecuteWorker_Patch(ref IncidentParms parms)
        {
            if (Utilities.FactionsWar().GetByFaction(parms.faction) == null || parms.points <= 35)
                return;

            parms.points *= 1 + (Utilities.FactionsWar().GetByFaction(parms.faction).disposition * 0.1f);
        }

        //--------------------------------------------------------------------------------

        public static void TradeUtility_GetPricePlayerSell_Prefix_Patch(Thing thing, ref float priceGain_FactionBase)
        {
            if (!TradeSession.Active || Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction) == null)
                return;
            if (Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).investments.Relations > 0)
            {
                priceGain_FactionBase += 0.1f * Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).investments.Relations;
            }
            if (Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).disposition < 0)
            {
                priceGain_FactionBase += Math.Abs(Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).disposition * 0.05f);

            }
            if (priceGain_FactionBase > 0.5f)
                priceGain_FactionBase = 0.5f;
        }
        
        public static void TradeUtility_GetPricePlayerBuy_Prefix_Patch(ref float priceGain_FactionBase)
        {
            if (!TradeSession.Active || Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction) == null)
                return;
            if (Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).investments.Relations > 0)
            {
                priceGain_FactionBase += 0.1f * Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).investments.Relations;
            }
            if (Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).disposition < 0)
            {
                priceGain_FactionBase += Math.Abs(Utilities.FactionsWar().GetByFaction(TradeSession.trader.Faction).disposition * 0.05f);

            }
            if (priceGain_FactionBase > 0.5f)
                priceGain_FactionBase = 0.5f;
        }
        //----------------------------------------------------------------------------------------

        public static void Settlement_GetFloatMenuOptions_Postfix(Caravan caravan, Settlement __instance, ref IEnumerable<FloatMenuOption> __result)
        {
            if (__instance.GetComponent<WorldComp_SettlementDefender>().IsActive)
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();

                foreach (FloatMenuOption f in CaravanArrivalAction_Defend.GetFloatMenuOptions(caravan, __instance))
                {
                    list.Add(f);

                }
                __result = __result.Concat(list.AsEnumerable());
            }
        }
        public static void FactionDialogMaker_FactionDialogFor_Postfix(DiaNode __result, Pawn negotiator, Faction faction)
        {
            if (EndGame_Settings.FactionHistory || EndGame_Settings.FactionHistory || EndGame_Settings.FactionServitude)
            {
                __result.options.Insert(__result.options.Count - 1, FactionHistoryDialog.RequestFactionInfoOption(faction, negotiator));
            }
        }

        //--------------------------------------------------------------------------------

        /*
         * Each Worldobject defeated of a Enemy faction lowers that faction's resources.
         */
        public static void SettlementDefeatUtility_IsDefeated_Postfix(Map map, Faction faction, ref bool __result)
        {
            if (map.Parent.GetComponent<WorldComp_JointRaid>().IsActive || map.Parent.GetComponent<WorldComp_SettlementDefender>().IsActive)

            {
                __result = false;
            }
            if (__result && Utilities.FactionsWar().GetByFaction(faction) != null)
                Utilities.FactionsWar().GetByFaction(faction).resources -= FE_WorldComp_FactionsWar.SETTLEMENT_RESOURCE_VALUE;
        }

        /*
         * Each Pawn killed of a faction lowers that faction's resources.
         */
        public static void Pawn_Kill_Postfix(Pawn __instance)
        {
            if (Faction.OfPlayer != null && !__instance.NonHumanlikeOrWildMan() && !__instance.Faction.IsPlayer && __instance.Faction.PlayerGoodwill < 0 && Utilities.FactionsWar().GetByFaction(__instance.Faction) != null)
            {
                Utilities.FactionsWar().GetByFaction(__instance.Faction).resources -= 100;
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
        [HarmonyPatch(typeof(FactionGiftUtility), "GiveGift", typeof(List<ActiveDropPodInfo>), typeof(SettlementBase))]
        public static class FactionGiftUtility_GiveGift_Patch
        {
            public static void Prefix(List<ActiveDropPodInfo> pods, SettlementBase giveTo)
            {
                if (Utilities.FactionsWar().GetByFaction(giveTo.Faction) != null)
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

        public static void MainTabWindow_Factions_DoWindowContents_Patch(Rect fillRect)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            Window_Faction window = new Window_Faction();
            foreach (War war in Utilities.FactionsWar().GetWars())
            {
                FloatMenuOption floatMenuOption = new FloatMenuOption("WindowWarOverview".Translate(war.DefenderFaction(), war.AttackerFaction()), () =>
                 {
                     window.war = war;
                     Find.WindowStack.Add(window);
                 }, MenuOptionPriority.Default, null, null);
                options.Add(floatMenuOption);
            }

            if (Prefs.DevMode)
            {
                FloatMenuOption floatMenuOptionDevMode = new FloatMenuOption("(DevMode) Make War", () =>
                {
                    Utilities.FactionsWar().TryDeclareWar();
                }, MenuOptionPriority.Default, null, null
                );
                options.Add(floatMenuOptionDevMode);
            }
            if (options.NullOrEmpty())
            {
                return;
            }

            FloatMenu floatMenu = new FloatMenu(options, "FactionWars".Translate(), true);

            if (GUI.Button(new Rect(0, 6, 90, 30), ""))
            {
                Find.WindowStack.Add(new FloatMenu(options));

            }
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 6, 90, 30), "FactionWars".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}