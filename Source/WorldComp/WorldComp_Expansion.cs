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
   /*
    * this compoment manages faction expansion, when a new settlement is built
    * Each faction has a timer, when it ends the settlement is built
    */


    class FE_WorldComp_Expansion : WorldComponent
    {
     // Balance
        private static readonly IntRange tillExpansion = new IntRange(Global.DayInTicks*2, Global.DayInTicks *7);
        private Dictionary<int, int> factionsToExpand = new Dictionary<int, int>();


        public FE_WorldComp_Expansion(World world) : base(world)
        {


        }


        public override void WorldComponentTick()
        {
            if (!EndGame_Settings.FactionExpansion)
                return;

            List<Faction> trim = new List<Faction>();
            
            foreach (Faction f in Find.FactionManager.AllFactions.Where(x => !x.IsPlayer && !x.def.hidden).ToList())
            {
                if (f.defeated && factionsToExpand.ContainsKey(f.loadID))
                    factionsToExpand.Remove(f.loadID);
                else if (!factionsToExpand.ContainsKey(f.loadID))
                    factionsToExpand.Add(f.loadID, tillExpansion.RandomInRange-Find.WorldObjects.Settlements.Count * 500);
                else factionsToExpand[f.loadID]--;
            }
            foreach (KeyValuePair<int,int> i in factionsToExpand.ToList())
            {
                if (i.Value == 0)
                {
                    Faction current = Find.FactionManager.AllFactions.First(f => f.loadID == i.Key);
                    factionsToExpand[i.Key] = tillExpansion.RandomInRange - Find.WorldObjects.Settlements.Count * 50;
                    Settlement origin;
                    if (!Find.WorldObjects.Settlements.Where(x => x.Faction == current).TryRandomElement(out origin))
                        continue;
                    Settlement expand = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                    expand.SetFaction(current);
                    expand.Tile = TileFinder.RandomSettlementTileFor(expand.Faction, false, x => Find.WorldGrid.ApproxDistanceInTiles(Find.AnyPlayerHomeMap.Tile, x) <
                        Find.WorldGrid.ApproxDistanceInTiles(Find.AnyPlayerHomeMap.Tile, origin.Tile) - 20 && Find.WorldGrid.ApproxDistanceInTiles(x, origin.Tile) < 40
                        && TileFinder.IsValidTileForNewSettlement(x, (StringBuilder)null));
                    expand.Name = SettlementNameGenerator.GenerateSettlementName(expand);
                    Utilities.FactionsWar().GetByFaction(current).resources -= FE_WorldComp_FactionsWar.LARGE_EVENT_Cache_RESOURCE_VALUE;
                    Find.WorldObjects.Add(expand);
                    Messages.Message("MessageExpanded".Translate(origin,i.Key, expand),expand, MessageTypeDefOf.NeutralEvent, false);
                }
                
            }
            
        }


        public override void ExposeData()
        {
            Scribe_Collections.Look(ref factionsToExpand, "factionsToExpand", LookMode.Value, LookMode.Value);
            base.ExposeData();
        }
    }
}