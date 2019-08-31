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
    class WorldObjectComp_MercenaryBattle : WorldObjectComp
    {
        private War war;
        private bool MercenaryBattle_Active = false;
        private Faction askingFaction;
        private IIncidentTarget parms;

        public WorldObjectComp_MercenaryBattle()
        {

        }

        public override void CompTick()
        {
            
            if (!MercenaryBattle_Active)
                return;
            if (!ParentHasMap && this.parent.GetComponent<TimeoutComp>().Passed)
            {
                
                askingFaction.TryAffectGoodwillWith(Faction.OfPlayer, -50);
                Utilities.FactionsWar().GetResouceAmount(askingFaction, -Utilities.FactionsWar().GetResouceAmount(askingFaction) + -Utilities.FactionsWar().GetResouceAmount(askingFaction) / 4);
            }
            if ((Utilities.FactionsWar().GetWars().Where(w=> w.AttackerFaction() ==war.AttackerFaction() && w.DefenderFaction() == war.DefenderFaction()).Count()==0))
            {
                Find.WorldObjects.Remove(this.parent);
                MercenaryBattle_Active = false;
                return;
            }
            Faction f;
            MapParent parent = (MapParent)this.parent;

            if (!parent.HasMap)
                return;
            for (int i=0;i<2;i++)
            {
                if (i == 1)
                    f = war.AttackerFaction();
                else f = war.DefenderFaction();
                if (parent.Map.mapPawns.SpawnedPawnsInFaction(f).Where(p=> !p.Dead && !p.Downed).Count()==0)
                {
                    BattleEnd(f, war.AttackerFaction() == f ? war.DefenderFaction() : war.AttackerFaction());
                    break;
                }
            }
            base.CompTick();
        }

        private void BattleEnd(Faction loser, Faction winner)
        {
            MercenaryBattle_Active = false;
            Utilities.FactionsWar().GetResouceAmount(loser, -Math.Max(Utilities.FactionsWar().GetResouceAmount(loser) / 2, 1000));
            if (loser == askingFaction)
            {
                loser.TryAffectGoodwillWith(Faction.OfPlayer, 5);
                Find.LetterStack.ReceiveLetter("LetterLabelMercenaryBattleRequestOutcomeLose".Translate(), TranslatorFormattedStringExtensions.Translate("MercenaryBattleRequestOutcomeLose",loser.leader)
                    , LetterDefOf.NegativeEvent, null, parent.Faction, (string)null);

            }
            else
            {
                loser.TryAffectGoodwillWith(Faction.OfPlayer, -10);
                winner.TryAffectGoodwillWith(Faction.OfPlayer, 25);
                Find.LetterStack.ReceiveLetter("LetterLabelMercenaryBattleRequestOutcomeWin".Translate(), TranslatorFormattedStringExtensions.Translate("MercenaryBattleRequestOutcomeWin",winner.leader)
                    , LetterDefOf.PositiveEvent, null, parent.Faction, (string)null);
            }
            
        }

        public override void PostMapGenerate()
        {
            Faction f;
            MapParent parent = (MapParent)this.parent;
            if(!MercenaryBattle_Active || !parent.HasMap)
                return;
            foreach (Pawn p in parent.Map.mapPawns.SpawnedPawnsInFaction(this.parent.Faction).ToList())
                p.Destroy();
            List<PawnKindDef> kindDefs = new List<PawnKindDef>();
            IntVec3 vec3 = new IntVec3();
            for (int i = 0; i < 2; i++)
            {
                if (i == 1)
                    f = war.AttackerFaction();
                else f=war.DefenderFaction();

                kindDefs = Utilities.GeneratePawnKindDef(65,f);
                Lord lord = LordMaker.MakeNewLord(f, new LordJob_AssaultColony(f,true,false,true), parent.Map);
                int side = (Rand.Chance(0.5f) ? 0 : 1);
                if (!RCellFinder.TryFindRandomPawnEntryCell(out vec3, parent.Map, 0, false, x=> (x.Standable(parent.Map)) && (i== 0 ? (x.x == (side==0 ? 0 : parent.Map.Size.x - 1)) : x.x==(side==0 ? parent.Map.Size.x-1: 0))))
                    return;
                Log.Warning("" + vec3.x+", "+vec3.z);
                Log.Warning(Utilities.FactionsWar().GetResouceAmount(f) / 10 + Flavor_Expansion.EndGame_Settings.MassiveBattles + StorytellerUtility.DefaultThreatPointsNow(parms) + ",overall     "+ StorytellerUtility.DefaultThreatPointsNow(parms)+" defualt, "+ Utilities.FactionsWar().GetResouceAmount(f) / 10);
                Utilities.GenerateFighter(Mathf.Clamp(Utilities.FactionsWar().GetResouceAmount(f)/10+Flavor_Expansion.EndGame_Settings.MassiveBattles+ StorytellerUtility.DefaultThreatPointsNow(parms), 1000,10000), lord, kindDefs, parent.Map, f, vec3);
            }
            base.PostMapGenerate();
        }

        public void StartComp(War war, Faction askingFaction ,IncidentParms parms)
        {
            this.parms = parms.target;
            this.war = war;
            MercenaryBattle_Active = true;
            this.askingFaction = askingFaction;
        }

        public override void PostExposeData()
        {
            Scribe_References.Look(ref war, "MercenaryBattle_War");
            Scribe_References.Look(ref askingFaction, "askingFaction");
            Scribe_References.Look(ref parms, "prams");
            Scribe_Values.Look(ref MercenaryBattle_Active, "MercenaryBattle_Active");
            base.PostExposeData();
        }
    }

    public class WorldObjectCompProperties_MercenaryBattle : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_MercenaryBattle()
        {
            this.compClass = typeof(WorldObjectComp_MercenaryBattle);
        }
    }
}
