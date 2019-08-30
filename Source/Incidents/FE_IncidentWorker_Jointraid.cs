using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.AI.Group;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{
    class FE_IncidentWorker_Jointraid : IncidentWorker
    {

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Settlement tile;
            Faction ally;
            return base.CanFireNowSub(parms) && TryFindSettlement(out ally, out tile) && EndGame_Settings.JointRaid;
        }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Settlement Set;
            Faction  ally;

            if (!TryFindSettlement(out ally, out Set))
            {
                Log.Error("joinraid null");
                return false;
            }

            // Balance
            Log.Warning("" + SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow()));
            List<Thing> rewards = ThingSetMakerDefOf.Reward_StandardByDropPod.root.Generate(new ThingSetMakerParams()
            {
                totalMarketValueRange = new FloatRange?(SiteTuning.BanditCampQuestRewardMarketValueRange * SiteTuning.QuestRewardMarketValueThreatPointsFactor.Evaluate(StorytellerUtility.DefaultSiteThreatPointsNow()+500f))
            });
            string reward = "";
            foreach (Thing t in rewards)
            {
                reward += t.Label + "\n";
            }
            reward.Remove(reward.Count() - 2, 2);
            int random = new IntRange(Global.DayInTicks * 5, Global.DayInTicks * 7).RandomInRange;
            Set.GetComponent<WorldComp_JointRaid>().StartComp(random, ally ,rewards);
            string text = this.def.letterText.Formatted((NamedArgument)ally.leader.LabelShort, (NamedArgument)ally.def.leaderTitle, (NamedArgument)ally.Name, (NamedArgument)GenLabel.ThingsLabel(rewards, string.Empty), (NamedArgument)(random/Global.DayInTicks).ToString(), (NamedArgument)GenThing.GetMarketValue((IList<Thing>)rewards).ToStringMoney((string)null)).CapitalizeFirst();
            GenThing.TryAppendSingleRewardInfo(ref text, (IList<Thing>)rewards);
            Find.LetterStack.ReceiveLetter(this.def.letterLabel, text, this.def.letterDef, (LookTargets)((WorldObject)Set), ally, (string)null);
            return true;
        }
        private bool TryFindSettlement(out Faction ally, out Settlement Set)
        {

            IEnumerable<Settlement> friendly = (from f in Find.WorldObjects.Settlements
                                   where !f.Faction.IsPlayer && !f.Faction.defeated && f.Faction.PlayerRelationKind == FactionRelationKind.Ally && Utilities.Reachable(f.Tile, Find.AnyPlayerHomeMap.Tile, 120)
                                                select f);
            foreach (Settlement b in friendly)
            {
                if ((from s in Find.WorldObjects.Settlements
                     where !s.Faction.IsPlayer && !s.Faction.defeated && s.Faction.HostileTo(Faction.OfPlayer) && !s.Faction.def.hidden
                     && Utilities.Reachable(b, s, 50) && !s.GetComponent<WorldComp_JointRaid>().IsActive()
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
