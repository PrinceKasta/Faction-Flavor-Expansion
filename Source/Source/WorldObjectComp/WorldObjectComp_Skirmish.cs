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
    class WorldObjectComp_Skirmish : WorldObjectComp
    {
        private bool active = false;
        List<Pawn> f1 = new List<Pawn>();
        List<Pawn> f2 = new List<Pawn>();
        private float f1Max =0, f2Max=0;
        Faction enemy;

        public WorldObjectComp_Skirmish()
        {
            active = false;
        }
        public void StartComp(Faction enemy)
        {
            this.active = true;
            this.enemy = enemy;
        }
        public bool IsActive()
        {
            return active;
        }

        public override void CompTick()
        {
            if (!active)
                return;
            MapParent parent = (MapParent) this.parent;
            if (!parent.HasMap)
                return;
            foreach(Pawn p in parent.Map.mapPawns.AllPawnsSpawned.Where(x=> !x.Dead && !x.Downed && (x.Faction==parent.Faction || x.Faction== enemy)))
            {
                if(!f1.Contains(p) && p.Faction==parent.Faction)
                {
                    f1.Add(p);
                }
                else if (!f2.Contains(p) && p.Faction == enemy)
                {
                    f2.Add(p);
                }
            }
            if (f1.Count > f1Max)
                f1Max = f1.Count;
            if (f2.Count > f2Max)
                f2Max = f2.Count;
            foreach (Pawn p in f1.ToList())
            {
                if (p.Dead && p.Downed)
                    f1.Remove(p);
            }
            foreach (Pawn p in f2.ToList())
            {
                if (p.Dead && p.Downed)
                    f2.Remove(p);
            }
            if(f1.Count==0 || f2.Count ==0)
            {
                f1Max = Utilities.FactionsWar().GetResouceAmount(parent.Faction) / f1Max / 10;
                f2Max = Utilities.FactionsWar().GetResouceAmount(enemy) / f2Max / 10;
                Utilities.FactionsWar().GetResouceAmount(parent.Faction, (-Utilities.FactionsWar().GetResouceAmount(parent.Faction) / 10) + (f1Max * f1.Count));
                Utilities.FactionsWar().GetResouceAmount(enemy, (-Utilities.FactionsWar().GetResouceAmount(enemy) / 10) + (f2Max * f2.Count));
                active = false;
            }
            base.CompTick();
        }

        public override void PostMapGenerate()
        {
            if (!active)
                return;
            base.PostMapGenerate();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref f1, "f1", LookMode.Reference);
            Scribe_Collections.Look(ref f2, "f2", LookMode.Reference);
            Scribe_References.Look(ref enemy, "enemy");
            Scribe_Values.Look(ref active, "Skirmish active");
            Scribe_Values.Look(ref f1Max, "f1Max");
            Scribe_Values.Look(ref f2Max, "f2Max");
        }
    }

    public class WorldObjectCompProperties_Skirmish : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_Skirmish()
        {
            this.compClass = typeof(WorldObjectComp_Skirmish);
        }
    }
}
