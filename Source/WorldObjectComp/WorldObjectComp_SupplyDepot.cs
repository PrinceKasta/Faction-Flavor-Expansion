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
using RimWorld.BaseGen;
using UnityEngine;

namespace Flavor_Expansion
{
    class WorldObjectComp_SupplyDepot : WorldObjectComp
    {
        public enum Type { Undefined,Weapons, Food};
        Type type= Type.Undefined;
        private bool active = false;
        public WorldObjectComp_SupplyDepot()
        {
            type = Type.Undefined;
        }
        public void StartComp(Type type)
        {
            if(type== Type.Undefined)
            {
                Log.Error("Type undefined");
                return;
            }
            this.type = type;
            active = true;
        }

        public bool IsActive()
        {
            return active;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!active)
                return;
            if (type == Type.Undefined)
                type = Rand.Chance(0.5f) ? Type.Food : Type.Weapons;
        }

        public override void PostMapGenerate()
        {
            if (!active)
            {
                return;
            }

            if (type == Type.Undefined)
                return;
            MapParent parent = (MapParent)this.parent;
            if (!parent.HasMap)
            {
                Log.Warning("No map");
                return;
            }
            int thingCount = 0;
            foreach(Thing t in parent.Map.listerThings.AllThings.Where(t=> t.def.PlayerAcquirable && t.def.CountAsResource && !(t.def.category== ThingCategory.Building)).ToList())
            {
                if ( type == Type.Weapons && !t.def.IsWeapon && Rand.Chance(0.8f))
                {
                    Log.Warning("swap weapon" + t.Label + ", " + t.InteractionCell.ToString());
                    GenerateWeapons(t.InteractionCell);
                    t.Destroy();
                    thingCount += 2;
                }
                else if(type == Type.Food && !t.def.IsNutritionGivingIngestible && Rand.Chance(0.8f))
                {
                    Log.Warning("swap food, "+t.Label+", "+t.InteractionCell.ToString());
                    GenerateFood(t.InteractionCell);
                    t.Destroy();
                    thingCount += 2;
                }
            }

            if (thingCount < 5)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (type == Type.Food)
                        GenerateFood(new IntVec3());
                    if (type == Type.Weapons)
                        GenerateWeapons(new IntVec3());
                    thingCount++;
                }
            }

        }

        private void GenerateWeapons(IntVec3 t)
        {
            MapParent parent = (MapParent)this.parent;
            ThingDef thingDef;
            if(!(from def in DefDatabase<ThingDef>.AllDefs
                                 where def.IsWeapon && def.BaseMarketValue > 20 && def.techLevel <= parent.Faction.def.techLevel && !def.label.Contains("tornado") && !def.label.Contains("orbital")
                                 select def).TryRandomElement(out thingDef))
            {
                Log.Error("Didn't find a sutiable weapon def for supplydepot which shouldn't happed");
                return;
            }
            
            for(int i=0; i<2;i++)
            {
                Thing weapon = ThingMaker.MakeThing(thingDef, GenStuff.RandomStuffByCommonalityFor(thingDef, parent.Faction.def.techLevel));
                weapon.TryGetComp<CompQuality>().SetQuality(Rand.Chance(0.5f) ? QualityCategory.Good : Rand.Chance(0.75f) ? QualityCategory.Excellent : QualityCategory.Normal, ArtGenerationContext.Outsider);
                if(Prefs.DevMode)
                    Log.Warning("Spawning: " + weapon.Label);
                if (t != new IntVec3() && !RCellFinder.TryFindRandomCellNearWith(t, x => x.Standable(parent.Map) && x.Roofed(parent.Map) && !x.Filled(parent.Map), parent.Map, out t, 1, 30))
                {
                    Log.Warning("t failed to find cell");
                    return;
                }
                if (t == new IntVec3() && !RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(x => x.Roofed(parent.Map) && !x.Filled(parent.Map) && x.IsValid && x.Standable(parent.Map), parent.Map, out t))
                    return;
                GenSpawn.Spawn(weapon, t, parent.Map);
            }

        }
        private void GenerateFood(IntVec3 t)
        {
            MapParent parent = (MapParent)this.parent;
            ThingDef thingDef = (from def in DefDatabase<ThingDef>.AllDefs
                                 where def.IsNutritionGivingIngestible && def.PlayerAcquirable && def.CountAsResource
                                 select def).RandomElement();

            for (int i = 0; i < 3; i++)
            {
                Log.Warning("" + t.ToString());
                Thing food = ThingMaker.MakeThing(thingDef, GenStuff.RandomStuffByCommonalityFor(thingDef, parent.Faction.def.techLevel));
                food.stackCount = food.def.stackLimit;
                if (t != new IntVec3() && !RCellFinder.TryFindRandomCellNearWith(t, x => x.Standable(parent.Map) && x.Roofed(parent.Map) && !x.Filled(parent.Map), parent.Map, out t, 1, 2))
                {
                    
                    Log.Error("Couldn'tfind cell near vec");
                    return;
                }
                if (t == new IntVec3() && !RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(x => x.Roofed(parent.Map) && !x.Filled(parent.Map) && x.IsValid && x.Standable(parent.Map), parent.Map, out t))
                {
                    Log.Error("Couldn'tfind cell near center of map");
                    return;
                }
            GenSpawn.Spawn(food, t, parent.Map);
            }

        }
        public override string CompInspectStringExtra()
        {
            if(active)
                return base.CompInspectStringExtra()+type.ToString();
            return base.CompInspectStringExtra();
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref active, "SupplyDepot_active");
            Scribe_Values.Look(ref type, "SupplyDepot_type");
        }
    }
    public class WorldObjectCompProperties_SupplyDepot : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SupplyDepot()
        {
            this.compClass = typeof(WorldObjectComp_SupplyDepot);
        }
    }
}
