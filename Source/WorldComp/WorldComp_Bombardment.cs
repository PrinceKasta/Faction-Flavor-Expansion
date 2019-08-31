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
    class FE_MapComponent_Bombardment : MapComponent
    {
        // Balance
        private const float BombardmentChance = 0.000001f;
        private Settlement bomber;
        private int length=0;
        private static readonly IntRange bombardmentLength = new IntRange(2000, 4000);
        private static readonly IntRange bombardmentDamage = new IntRange(10, 20);


        public FE_MapComponent_Bombardment(Map map) : base(map)
        {
            
        }

        public void ForceStart(int length)
        {
            this.length = length;
        }

        public override void MapComponentTick()
        {
            if (!(map.ParentFaction == Faction.OfPlayer) || !EndGame_Settings.Bombardment)
                return;
            if (bomber == null && !(from s in Find.WorldObjects.Settlements
                                    where s.Faction.HostileTo(Faction.OfPlayer) && !s.Faction.def.techLevel.IsNeolithicOrWorse() && Utilities.Reachable(map.Tile, s.Tile, 20)
                                    select s).TryRandomElement(out bomber))
                return;
            if (length==0 && Rand.Chance(BombardmentChance))
            {
                length = bombardmentLength.RandomInRange;


                Messages.Message("MessageHositleBombing".Translate(bomber.Faction.def.pawnsPlural,bomber),new LookTargets(map.Parent),MessageTypeDefOf.ThreatBig,true);


            }
            if (length > 0)
                length--; 
            if ( length != 0 && length % 500==0)
            {
                Projectile_Explosive shell = (Projectile_Explosive)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Bullet_Shell_HighExplosive"));

                IntVec3 edge= CellFinder.RandomEdgeCell(map);

                IntVec3 intVec3= CellFinder.RandomNotEdgeCell(20, map);  
                GenSpawn.Spawn(shell, edge, map);
                shell.Launch(null, intVec3, intVec3, ProjectileHitFlags.IntendedTarget, shell);
                    
            }
            base.MapComponentTick();
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref bomber, "bomber");
            Scribe_Values.Look(ref length, "length");
        }


    }
}