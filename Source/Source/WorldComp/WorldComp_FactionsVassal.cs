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
    class FE_WorldComp_FactionsVassal : WorldComponent
    {
        private int year = 5501;
        private int dayOfMonth = 0;
        public FE_WorldComp_FactionsVassal(World world) : base(world)
        {

        }

        public override void WorldComponentTick()
        {
            Thing silver = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Silver"));
            IntVec3 intVec3 = new IntVec3();
            string factionList = "";
            bool vassalPay = false, TributePay = false;


            foreach (LE_FactionInfo f in Utilities.FactionsWar().factionInfo)
            {
                if (f.vassalage == 2)
                {

                    if (Find.AnyPlayerHomeMap == null)
                    {
                        return;
                    }
                    if (f.faction.PlayerRelationKind == FactionRelationKind.Hostile)
                    {
                        f.vassalage = 0;
                        return;
                    }
                    // Goodwill decay
                    else if (Find.TickManager.TicksGame % (Global.DayInTicks * 7) == 0)
                    {
                        f.faction.TryAffectGoodwillWith(Faction.OfPlayer, -2, false, true, "FactionVassalageGoodWillDecay".Translate());
                    }
                    // Vassal Tribute
                    if (GenLocalDate.Year(Find.AnyPlayerHomeMap) == year && GenLocalDate.DayOfYear(Find.AnyPlayerHomeMap) == 0)
                    {
                        silver.stackCount = new IntRange(1900 + (f.faction.PlayerGoodwill * 5), 2800+(f.faction.PlayerGoodwill*5)).RandomInRange;
                        factionList += f.faction + ",";
                        intVec3 = DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap);
                        vassalPay = true;
                    }
                   
                    
                }
                
                if (f.vassalage == 1)
                {
                    if (f.faction.PlayerRelationKind == FactionRelationKind.Hostile)
                    {
                        f.vassalage = 0;
                        return;
                    }
                    // Goodwill decay
                    else if (Find.TickManager.TicksGame % (Global.DayInTicks * 7) == 0)
                    {
                        f.faction.TryAffectGoodwillWith(Faction.OfPlayer, -5, false, true, "FactionVassalageGoodWillDecay".Translate());
                    }
                    // Tribute
                    if (GenLocalDate.DayOfYear(Find.AnyPlayerHomeMap) == dayOfMonth)
                    {
                        silver.stackCount = new IntRange(850 + (f.faction.PlayerGoodwill * 8), 1300 + (f.faction.PlayerGoodwill * 8)).RandomInRange;
                        factionList += f.faction + ",";
                        intVec3 = DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap);
                        TributePay = true;
                    }
                }
            }
            if (GenLocalDate.Year(Find.AnyPlayerHomeMap) >= year)
            {
                year = GenLocalDate.Year(Find.AnyPlayerHomeMap) + 1;
            }
            if (GenLocalDate.DayOfYear(Find.AnyPlayerHomeMap) >= dayOfMonth )
            {
                dayOfMonth = ClosestNumberOf15(GenLocalDate.DayOfYear(Find.AnyPlayerHomeMap)+1);
               
            }
            if ((vassalPay || TributePay) && silver.stackCount>0 && intVec3.IsValid)
            {
                string text = "";
                if(vassalPay && TributePay)
                {
                    text += "FactionVassalSilverRecivedBoth".Translate(silver.stackCount);
                } else if (vassalPay)
                {
                    text += "FactionVassalSilverRecivedVassals".Translate(silver.stackCount);
                } else if(TributePay)
                {
                    text += "FactionVassalSilverRecivedTrivutaries".Translate(silver.stackCount);
                }

                factionList.Remove(factionList.Count()-1);
                DropPodUtility.DropThingsNear(intVec3, Find.AnyPlayerHomeMap, new List<Thing>() { silver }, 110, false, true, true);
                Find.LetterStack.ReceiveLetter("LetterFactionVassalSilverRecived".Translate(), text+factionList, LetterDefOf.PositiveEvent, null);
            }
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref year, "year");
            Scribe_Values.Look(ref dayOfMonth, "dayOfMonth");
        }

        private static int ClosestNumberOf15(int num)
        {
            
            int q = num / 15;
            int n2 = 15 * (q + 1);

            return n2;
        }
    }
}
