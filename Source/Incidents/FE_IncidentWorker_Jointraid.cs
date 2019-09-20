using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class FE_IncidentWorker_Jointraid : IncidentWorker
    {
        public static readonly SimpleCurve SilverBonusRewardCurve = new SimpleCurve()
        {
            {
                new CurvePoint(75f, 500f),
                true
            },
            {
                new CurvePoint(87f, 1000f),
                true
            },
            {
                new CurvePoint(100f, 1500f),
                true
            }
        };

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return base.CanFireNowSub(parms) && TryFindSettlement(out Faction ally, out Settlement tile) && EndGame_Settings.JointRaid;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryFindSettlement(out Faction ally, out Settlement Set))
            {
                return false;
            }

            // Balance
            List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
            {
                totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow()+500f))
            });

            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = (int)SilverBonusRewardCurve.Evaluate(ally.PlayerGoodwill);
            
            int random = new IntRange(Global.DayInTicks * 15, Global.DayInTicks * 25).RandomInRange;
            Set.GetComponent<WorldComp_JointRaid>().StartComp(random, ally ,rewards ,silver);
            string text = def.letterText.Formatted(ally.leader.LabelShort, ally.def.leaderTitle, ally.Name, GenLabel.ThingsLabel(rewards, string.Empty), (random / Global.DayInTicks + Find.TickManager.TicksGame).ToString(), GenThing.GetMarketValue(rewards).ToStringMoney(null), silver.stackCount.ToString()).CapitalizeFirst();
            GenThing.TryAppendSingleRewardInfo(ref text, rewards);
            Find.LetterStack.ReceiveLetter(def.letterLabel, text, def.letterDef, Set, ally, null);
            return true;
        }
        private bool TryFindSettlement(out Faction ally, out Settlement Set)
        {
            foreach (Settlement b in Find.WorldObjects.Settlements.Where(f=> !f.Faction.IsPlayer && !f.Faction.defeated && f.Faction.PlayerRelationKind == FactionRelationKind.Ally && Utilities.Reachable(f.Tile, Find.AnyPlayerHomeMap.Tile, 120)).InRandomOrder())
            {
                if ((from s in Find.WorldObjects.Settlements
                     where !s.Faction.IsPlayer && !s.Faction.defeated && s.Faction.HostileTo(Faction.OfPlayer) && !s.Faction.def.hidden
                     && Utilities.Reachable(b, s, 100) && !s.GetComponent<WorldComp_JointRaid>().IsActive
                     select s).TryRandomElement(out Set))
                {
                    ally = b.Faction;
                    return true;
                }
            }
            Set = null;
            ally = null;
            return false;
        }
    }
}
