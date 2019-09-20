using System;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

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
            return base.CanFireNowSub(parms) && TryFindFactions(out Faction faction) && TryFindSettlement(out Settlement settlement, faction) && Find.ResearchManager.AnyProjectIsAvailable && TryFindSutiableResearch(out ResearchProjectDef def, faction) && TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, (int)silverCurve.Evaluate((int)def.techLevel));
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindFactions(out Faction faction) || !TryFindSutiableResearch(out ResearchProjectDef def, faction) || !TryFindSettlement(out Settlement settlement, faction) || !TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, (int)silverCurve.Evaluate((int)def.techLevel)))
                return false;
            
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = (int)silverCurve.Evaluate((int)def.techLevel);

            DiaNode nodeRoot = new DiaNode(TranslatorFormattedStringExtensions.Translate("ResearchGained", faction.leader, silver.stackCount, def.label));
            nodeRoot.options.Add(new DiaOption("ResearchGained_Purchase".Translate(silver.stackCount))
            {

                action = () =>
                {
                    Find.ResearchManager.FinishProject(def, false, PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Villager, faction, PawnGenerationContext.NonPlayer, -1, false, false, false, false, false)));
                    TradeUtility.LaunchSilver(Find.AnyPlayerHomeMap, silver.stackCount);
                },
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
            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(nodeRoot, faction, true, true, title));
            Find.Archive.Add(new ArchivedDialog(nodeRoot.text, title, faction));

            Find.ResearchManager.FinishProject(def, false, PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Villager, faction, PawnGenerationContext.NonPlayer, -1, false, false, false, false, false)));
            
            return false;
        }

        private bool TryFindSettlement(out Settlement settlement, Faction ally) => Find.WorldObjects.Settlements.Where(f => f.Faction == ally && f.Spawned).TryRandomElement(out settlement) ? true : false;

        private bool TryFindSutiableResearch(out ResearchProjectDef tech, Faction ally) => DefDatabase<ResearchProjectDef>.AllDefs.Where(re => re.CanStartNow && re.PrerequisitesCompleted && re.techLevel <= ally.def.techLevel && !re.IsFinished).TryRandomElement(out tech)
                ? true
                : false;

        private bool TryFindFactions(out Faction alliedFaction) => Find.FactionManager.AllFactions.Where(x => !x.IsPlayer && x.PlayerGoodwill > 85 && !x.defeated && !x.def.techLevel.IsNeolithicOrWorse()).TryRandomElement(out alliedFaction)
                ? true
                : false;
    }
}
