using System;
using System.Linq;
using Verse;
using Verse.AI.Group;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Flavor_Expansion
{
    class WorldObjectComp_MercenaryBattle : WorldObjectComp
    {
        private War war;
        private bool MercenaryBattle_Active = false;
        private Faction askingFaction;

        public WorldObjectComp_MercenaryBattle()
        {

        }

        public override void CompTick()
        {
            if (!MercenaryBattle_Active)
                return;
            if (Utilities.FactionsWar().GetWars().Where(w => w.AttackerFaction() == war.AttackerFaction() && w.DefenderFaction() == war.DefenderFaction()).Count() == 0)
            {
                Find.WorldObjects.Remove(parent);
                MercenaryBattle_Active = false;
                return;
            }
            if (!ParentHasMap)
            {
                if(parent.GetComponent<TimeoutComp>().Passed)
                {
                    askingFaction.TryAffectGoodwillWith(Faction.OfPlayer, -50);
                    Utilities.FactionsWar().GetByFaction(askingFaction).resources -= Utilities.FactionsWar().GetByFaction(askingFaction).resources * (3 / 4);
                }
                return;
            }
            
            for (int i=0;i<2;i++)
            {
                Faction f = i == 1 ? war.AttackerFaction() : war.DefenderFaction();
                if (((MapParent)parent).Map.mapPawns.SpawnedPawnsInFaction(f).Count(p => GenHostility.IsActiveThreatTo(p, f == war.AttackerFaction() ? war.DefenderFaction() : war.AttackerFaction())) == 0)
                {
                    BattleEnd(f, war.AttackerFaction() == f ? war.DefenderFaction() : war.AttackerFaction());
                    break;
                }
            }
        }

        private void BattleEnd(Faction loser, Faction winner)
        {
            MercenaryBattle_Active = false;
            Utilities.FactionsWar().GetByFaction(loser).resources -= Math.Max(Utilities.FactionsWar().GetByFaction(loser).resources / 2, 1000);
            if (loser == askingFaction)
            {
                loser.TryAffectGoodwillWith(Faction.OfPlayer, 5);
                Find.LetterStack.ReceiveLetter("LetterLabelMercenaryBattleRequestOutcomeLose".Translate(), TranslatorFormattedStringExtensions.Translate("MercenaryBattleRequestOutcomeLose",loser.leader)
                    , LetterDefOf.NegativeEvent, null, parent.Faction, null);
            }
            else
            {
                loser.TryAffectGoodwillWith(Faction.OfPlayer, -10);
                winner.TryAffectGoodwillWith(Faction.OfPlayer, 25);
                Find.LetterStack.ReceiveLetter("LetterLabelMercenaryBattleRequestOutcomeWin".Translate(), TranslatorFormattedStringExtensions.Translate("MercenaryBattleRequestOutcomeWin",winner.leader)
                    , LetterDefOf.PositiveEvent, null, parent.Faction, null);
            }
        }

        public override void PostMapGenerate()
        {
            if (!MercenaryBattle_Active)
                return;
            MapParent parent = (MapParent)this.parent;
            foreach (Pawn p in parent.Map.mapPawns.SpawnedPawnsInFaction(this.parent.Faction))
                p.Destroy();

            int side = Rand.Chance(0.5f) ? 0 : 1;
            for (int i = 0; i < 2; i++)
            {
                Faction f = i == 1 ? war.AttackerFaction() : f = war.DefenderFaction();
                side = side == 0 ? 1 : 0;
                if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 vec3, parent.Map, 0, false, x => x.Standable(parent.Map) && (x.x == (side == 0 ? 0 : parent.Map.Size.x - 1))))
                {
                    vec3 = DropCellFinder.FindRaidDropCenterDistant(parent.Map);
                }
                Utilities.GenerateFighter(Mathf.Clamp((Utilities.FactionsWar().GetByFaction(f).resources / 10)+ EndGame_Settings.MassiveBattles + StorytellerUtility.DefaultThreatPointsNow(parent.Map), 1000,10000), LordMaker.MakeNewLord(f, new LordJob_AssaultColony(f, true, false, true), parent.Map), Utilities.GeneratePawnKindDef(65, f), parent.Map, f, vec3);
            }
        }

        public void StartComp(War war, Faction askingFaction ,IncidentParms parms)
        {
            this.war = war;
            MercenaryBattle_Active = true;
            this.askingFaction = askingFaction;
        }

        public override void PostExposeData()
        {
            Scribe_References.Look(ref war, "MercenaryBattle_War");
            Scribe_References.Look(ref askingFaction, "askingFaction");
            Scribe_Values.Look(ref MercenaryBattle_Active, "MercenaryBattle_Active");
        }
    }

    public class WorldObjectCompProperties_MercenaryBattle : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_MercenaryBattle() => compClass = typeof(WorldObjectComp_MercenaryBattle);
    }
}
