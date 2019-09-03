using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using Verse.AI.Group;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{
    class FE_IncidentWorker_Bombardment : IncidentWorker
    {
        private static readonly IntRange bombardmentLength = new IntRange(2000, 4000);

        private static readonly IntRange countDown = new IntRange(2, 4);

        private readonly SimpleCurve silverCurve = new SimpleCurve()
        {
            {
                new CurvePoint(0f, 250f),
                true
            },
            {
                new CurvePoint(0.2f, 500f),
                true
            },
            {
                new CurvePoint(0.5f, 750f),
                true
            },
            {
                new CurvePoint(0.7f, 1000f),
                true
            },
            {
                new CurvePoint(1f, 1500f),
                true
            },
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && TryFindAdjcentSettlemet(out Settlement bomber) && HasEnoughValuableThings() && EndGame_Settings.Bombardment;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindAdjcentSettlemet(out Settlement bomber))
                return false;
            int silver = (int)silverCurve.Evaluate((int)silverCurve.Evaluate(1 - 1 / Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal));
            List<Thing> demand= new List<Thing>();

            GenerateDemands(demand, silver);

            silver = (int)GenThing.GetMarketValue(demand);

            int countdown = countDown.RandomInRange * Global.DayInTicks;
            string text = TranslatorFormattedStringExtensions.Translate("BombardmentThreat", bomber.Faction.leader, bomber.Faction.def.leaderTitle, bomber.Name, ((float)silver).ToStringMoney((string)null),GenLabel.ThingsLabel(demand,string.Empty), countdown.ToStringTicksToPeriod());
            GenThing.TryAppendSingleRewardInfo(ref text, (IList<Thing>)demand);
            DiaNode nodeRoot = new DiaNode(text);

            nodeRoot.options.Add(new DiaOption("BombardmentThreat_AcceptThings".Translate())
            {

                action = (Action)(() =>
                {
                    foreach (Thing t in demand)
                    {
                        TradeUtility.LaunchThingsOfType(t.def, t.stackCount, Find.AnyPlayerHomeMap, null);
                        }
                }),
                link = new DiaNode(TranslatorFormattedStringExtensions.Translate("BombardmentThreatAcceptThings", bomber.Faction.leader))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            if (TradeUtility.ColonyHasEnoughSilver(TradeUtility.PlayerHomeMapWithMostLaunchableSilver(), silver * 2))
            {
                nodeRoot.options.Add(new DiaOption("BombardmentThreat_AcceptSilver".Translate(((float)silver * 2).ToStringMoney()))
                {

                    action = (Action)(() =>
                    {
                        TradeUtility.LaunchSilver(Find.AnyPlayerHomeMap, silver * 2);
                    }),
                    link = new DiaNode(TranslatorFormattedStringExtensions.Translate("BombardmentThreatAcceptThings", bomber.Faction.leader))
                    {
                        options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                    }
                });
            }
            else
            {
                nodeRoot.options.Add(new DiaOption("BombardmentThreat_AcceptSilver".Translate(((float)silver * 2).ToStringMoney()))
                {
                    disabled = true,
                    disabledReason = "BombardmentThreat_AcceptSilverDisabled".Translate()

                });
            }
            nodeRoot.options.Add(new DiaOption("BombardmentThreat_Refusal".Translate())
            {

                action = (Action)(() =>
                {

                    Find.AnyPlayerHomeMap.GetComponent<FE_MapComponent_Bombardment>().StartComp(bombardmentLength.RandomInRange, bomber, countdown);

                }),
                link = new DiaNode("BombardmentThreatRefusal".Translate(bomber.Faction.leader))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            string title = "LetterLabelBombardmentTitle".Translate();
            Find.WindowStack.Add((Window)new Dialog_NodeTreeWithFactionInfo(nodeRoot, bomber.Faction, true, true, title));
            Find.Archive.Add((IArchivable)new ArchivedDialog(nodeRoot.text, title, bomber.Faction));
            
            return true;
        }

        private void GenerateDemands(List<Thing> demand , float silver)
        {
            for (int i = 0; i < TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(x => (x.stackCount * x.MarketValue) < silver).Count(); i++)
            {
                Thing thing = TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).RandomElementByWeight(x => x.MarketValue);
                //Log.Warning(silver.ToString() + " Demand value:" + GenThing.GetMarketValue(demand) + " ||| " + thing.Label + ": " + " market:" + thing.MarketValue + " stack: " + thing.stackCount + " Total: " + thing.MarketValue * thing.stackCount, true);

                if (demand.Contains(thing) || GenThing.GetMarketValue(demand) + thing.MarketValue * thing.stackCount > 1.3f * silver)
                {
                    continue;
                }
                demand.Add(thing);
                if (GenThing.GetMarketValue(demand) > silver)
                    break;
            }
            if (GenThing.GetMarketValue(demand) == 0 || GenThing.GetMarketValue(demand) < silver * 0.75)
            {

                Thing min = TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => !demand.Contains(t)).First();
                foreach (Thing t in TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap))
                {
                    //Log.Warning(silver.ToString() + " ||| " + t.Label + ": " + " market:" + t.MarketValue + " stack: " + t.stackCount + "," + t.MarketValue * t.stackCount, true);
                    if (t != min && !demand.Contains(t) && t.MarketValue * t.stackCount < min.MarketValue * min.stackCount)
                    {
                        if (t.MarketValue * t.stackCount < silver)
                        {
                            demand.Add(t);
                            if (GenThing.GetMarketValue(demand) >= silver)
                                break;
                        }
                        min = t;
                    }
                }
                if (GenThing.GetMarketValue(demand) < silver)
                    demand.Add(min);
            }
            
        }

        private bool TryFindAdjcentSettlemet(out Settlement bomber)
        {
            if ((from s in Find.WorldObjects.Settlements
                                    where s.Faction.HostileTo(Faction.OfPlayer) && !s.Faction.def.techLevel.IsNeolithicOrWorse() && Utilities.Reachable(Find.AnyPlayerHomeMap.Tile, s.Tile, 20)
                                    select s).TryRandomElement(out bomber))
                    return true;
                return false;
        }

        private bool HasEnoughValuableThings()
        {
            if (GenThing.GetMarketValue(TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).ToList())> (int)silverCurve.Evaluate(1 - 1 / Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal))
                return true;
            return false;
        }
    }
}
