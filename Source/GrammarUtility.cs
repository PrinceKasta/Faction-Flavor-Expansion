using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using Verse.Grammar;
using Verse.AI.Group;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{
    class FE_GrammarUtility
    {
        public static string WarEvent(Faction raider = null, Faction victim = null, Settlement town = null)
        {
            GrammarRequest request = new GrammarRequest();
            if (victim == null && town == null)
            {
                request.Includes.Add(EndGameDefOf.FE_WarEvent_ArtifactCache);
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", raider));
                return GrammarResolver.Resolve("FE_WarEvent_ArtifactCache", request, (string)null, false, (string)null);
            }

            request.Includes.Add(EndGameDefOf.FE_WarEvent_RaidSuccess);
            request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", raider));
            request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION2", victim));
            request.Rules.AddRange(RulesForTown("AffectedTown", town));

            return GrammarResolver.Resolve("FE_RaidSuccess", request, (string)null, false, null);

        }

        public static string History(Faction subject = null, Faction victim = null, Settlement town = null, Pawn pawn = null, string towndestroyed = null)
        {
            GrammarRequest request = new GrammarRequest();
            request.Includes.Add(EndGameDefOf.FE_History);
            if (pawn !=null)
            {
                request.Rules.AddRange(GrammarUtility.RulesForPawn("Pawn", pawn));
                return GrammarResolver.Resolve("r_history_pawn", request, (string)null, false, (string)null);
            }
            if(town!= null)
            {
                request.Rules.AddRange(RulesForTown("AffectedTown", town));
                return GrammarResolver.Resolve("r_history_town", request, (string)null, false, (string)null);
            }
            if(subject!=null && victim != null)
            {
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", subject));
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION2", victim));
                return GrammarResolver.Resolve("r_history_faction", request, (string)null, false, (string)null);
            }
            if(subject!=null)
            {
                request.Rules.AddRange(GrammarUtility.RulesForFaction("FACTION1", subject));
                request.Rules.AddRange(RulesForString( "FACTION2", NameGenerator.GenerateName(subject.def.factionNameMaker, Find.FactionManager.AllFactionsVisible.Select<Faction, string>((Func<Faction, string>)(fac => fac.Name)))));
                return GrammarResolver.Resolve("r_history_factiondead", request, (string)null, false, (string)null);
            }
            if(towndestroyed !=null)
            {
                request.Rules.AddRange(RulesForString("AffectedTown", towndestroyed));
                return GrammarResolver.Resolve("r_history_townDestroyed", request, (string)null, false, (string)null);
            }
            return GrammarResolver.Resolve("r_history_generic", request, (string)null, false, (string)null);
        }

        public static IEnumerable<Rule> RulesForTown(string prefix, Settlement town)
        {
            yield return new Rule_String(prefix + "_name", town.Name);
            
        }

        public static IEnumerable<Rule> RulesForString(string prefix, string text)
        {
            yield return new Rule_String(prefix + "_name", text);

        }
    }
}
