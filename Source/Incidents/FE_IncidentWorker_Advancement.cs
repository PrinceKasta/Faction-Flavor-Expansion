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
    class FE_IncidentWorker_Advancement : IncidentWorker
    {
        private readonly SimpleCurve silverCurve = new SimpleCurve()
        {
            {
                new CurvePoint(1f, 1000f),
                true
            },
            {
                new CurvePoint(2f, 1500f),
                true
            },
            {
                new CurvePoint(5f, 2500f),
                true
            },
            {
                new CurvePoint(7f, 3500f),
                true
            },
        };
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            ResearchProjectDef def;
            Settlement settlement;
            Faction faction;
            return base.CanFireNowSub(parms) && TryFindFactions(out faction) && TryFindSettlement(out settlement, faction) && Find.ResearchManager.AnyProjectIsAvailable && TryFindSutiableResearch(out def, faction) && TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, (int)silverCurve.Evaluate((int)def.techLevel));
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Faction faction;
            ResearchProjectDef def;
            Settlement settlement;

            if (!TryFindFactions(out faction) || !TryFindSutiableResearch(out def, faction) || !TryFindSettlement(out settlement, faction) || !TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, (int)silverCurve.Evaluate((int)def.techLevel)))
                return false;
            
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = (int)silverCurve.Evaluate((int)def.techLevel);
            
            string text = TranslatorFormattedStringExtensions.Translate("ResearchGained", faction.leader, silver.stackCount, def.label);
            DiaNode nodeRoot = new DiaNode(text);
            nodeRoot.options.Add(new DiaOption("ResearchGained_Purchase".Translate(silver.stackCount))
            {

                action = (Action)(() =>
                {
                    Find.ResearchManager.FinishProject(def, false, PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Villager, faction, PawnGenerationContext.NonPlayer, -1, false, false, false, false, false)));
                    TradeUtility.LaunchSilver(Find.AnyPlayerHomeMap, silver.stackCount);
                }),
                link = new DiaNode(TranslatorFormattedStringExtensions.Translate("ResearchGainedPurchase", def.label))
                {
                    options = {
                         new DiaOption("OK".Translate()) { resolveTree = true }
                       }
                }
            });
            nodeRoot.options.Add(new DiaOption("ResearchGained_Decline".Translate())
            {

                resolveTree = true
            });
            string title = "ResearchGainedTitle".Translate();
            Find.WindowStack.Add((Window)new Dialog_NodeTreeWithFactionInfo(nodeRoot, faction, true, true, title));
            Find.Archive.Add((IArchivable)new ArchivedDialog(nodeRoot.text, title, faction));

            Find.ResearchManager.FinishProject(def, false, PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Villager, faction, PawnGenerationContext.NonPlayer, -1, false, false, false, false, false)));
            
            return false;
        }

        private bool TryFindSettlement(out Settlement settlement, Faction ally)
        {
            if ((from f in Find.WorldObjects.Settlements
                  where f.Faction == ally && f.Spawned
                  select f).TryRandomElement(out settlement))
                return true;
            return false;
        }

        private bool TryFindSutiableResearch(out ResearchProjectDef tech, Faction ally)
        {
            if ((from re in DefDatabase<ResearchProjectDef>.AllDefs
                 where re.CanStartNow && re.PrerequisitesCompleted && re.techLevel <= ally.def.techLevel && !re.IsFinished
                 select re).TryRandomElement(out tech))
                return true;

            return false;
        }

        private bool TryFindFactions(out Faction alliedFaction)
        {
            if ((from x in Find.FactionManager.AllFactions
                 where !x.IsPlayer && x.PlayerGoodwill>85 && !x.defeated  && !x.def.techLevel.IsNeolithicOrWorse()
                 select x).TryRandomElement(out alliedFaction))
            {
                return true;
            }
            alliedFaction = null;
            return false;
        }
    }
}
