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
    class WorldComp_DisputeRoads : WorldObjectComp
    {
        private readonly IntRange NextTileBuffer = new IntRange(Global.DayInTicks/3, Global.DayInTicks);
        public bool active = false;
        private int timer=0;
        int set1, set2;
        List<int> path;

        public void StartComp(int set1, int set2, List<int> path)
        {
            this.active = true;
            this.set1 = set1;
            this.set2 = set2;
            this.path = path.ToList();
            timer = NextTileBuffer.RandomInRange;
        }

        public override void CompTick()
        {
            if (!active)
                return;
            if(!Find.TickManager.Paused)
                timer--;
            if (path.Count() <= 1 || !Find.WorldObjects.AnySettlementAt(set1)
                || !Find.WorldObjects.AnySettlementAt(set2))
            {
                active = false;
                Find.WorldObjects.Remove(this.parent);
            }
            if ( timer <= 0)
            {

                NextTile();
                timer = NextTileBuffer.RandomInRange;
            }
        }

        private void NextTile()
        {
            int i = 0;
            List<int> temp = new List<int>();
            if (path.Count == 1)
                return;
            while (path.Count()-1 >=2  && !Find.WorldGrid.IsNeighbor(path[i], path[i+1]))
            {
                temp.Add(path[i]);
                if (i == path.Count() - 2)
                {
                    temp.Add(path[i + 1]);
                    break;
                }
                i++;
            }
            foreach(int g in temp)
            {
                path.Remove(g);
            }

            Find.WorldGrid.OverlayRoad(path.First(), path[1], EndGameDefOf.StoneRoad);
            Find.World.renderer.SetDirty<WorldLayer_Roads>();
            path.Remove(path.First());

            WorldObject dispute = WorldObjectMaker.MakeWorldObject(EndGameDefOf.Roads_Camp);
            dispute.GetComponent<WorldComp_DisputeRoads>().StartComp(set1, set2, path);

            dispute.SetFaction(parent.Faction);
            dispute.Tile = path.First();

            Find.WorldObjects.Add(dispute);
            Find.WorldObjects.Remove(parent);
        }
        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra()+ "RoadsTimerDesc".Translate(timer/ Global.DayInTicks);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref active, "DisputeRoads_Active", false);
            Scribe_Values.Look(ref timer, "timer", 0);
            Scribe_Values.Look(ref set1, "set1");
            Scribe_Values.Look(ref set2, "set2");
            Scribe_Collections.Look(ref path, "path");
        }
    }

    public class WorldObjectCompProperties_DisputeRoads : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_DisputeRoads()
        {
            this.compClass = typeof(WorldComp_DisputeRoads);
        }
    }
}
