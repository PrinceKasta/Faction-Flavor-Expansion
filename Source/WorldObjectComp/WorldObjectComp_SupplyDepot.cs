using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class WorldObjectComp_SupplyDepot : WorldObjectComp
    {
        public enum Type {Undefined, Weapons, Food};

        private Type type = Type.Undefined;
        private bool active = false;
        public WorldObjectComp_SupplyDepot()
        {
            type = Type.Undefined;
        }
        public void StartComp(Type type)
        {
            if(type == Type.Undefined)
            {
                Log.Error("Type undefined");
                return;
            }
            this.type = type;
            active = true;
        }

        public bool IsActive => active;

        public override void CompTick()
        {
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

            int thingCount = 0;
            foreach(Thing t in ((MapParent)this.parent).Map.listerThings.AllThings.Where(t=> t.def.PlayerAcquirable && t.def.CountAsResource && !(t.def.category== ThingCategory.Building)).ToList())
            {
                if ( type == Type.Weapons && !t.def.IsWeapon && Rand.Chance(0.8f))
                {
                    GenerateWeapons(t.InteractionCell);
                    t.Destroy();
                    thingCount += 2;
                }
                else if(type == Type.Food && !t.def.IsNutritionGivingIngestible && Rand.Chance(0.8f))
                {
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
            if(!DefDatabase<ThingDef>.AllDefs.Where(def=> def.IsWeapon && def.BaseMarketValue > 20 && def.techLevel <= parent.Faction.def.techLevel && !def.label.Contains("tornado") && !def.label.Contains("orbital")).TryRandomElement(out ThingDef thingDef))
            {
                Log.Error("Didn't find a sutiable weapon def for supplydepot which shouldn't happen");
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
            DefDatabase<ThingDef>.AllDefs.Where(def=> def.IsNutritionGivingIngestible && def.PlayerAcquirable && def.CountAsResource).TryRandomElement(out ThingDef thingDef);

            for (int i = 0; i < 3; i++)
            {
                Thing food = ThingMaker.MakeThing(thingDef, GenStuff.RandomStuffByCommonalityFor(thingDef, parent.Faction.def.techLevel));
                food.stackCount = food.def.stackLimit;
                if (t != new IntVec3() && !RCellFinder.TryFindRandomCellNearWith(t, x => x.Standable(parent.Map) && x.Roofed(parent.Map) && !x.Filled(parent.Map), parent.Map, out t, 1, 2))
                {
                    Log.Error("Couldn'tfind cell near vec");
                    return;
                }
                if (t == new IntVec3() && !RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(x => x.Roofed(parent.Map) && !x.Filled(parent.Map) && x.IsValid && x.Standable(parent.Map), parent.Map, out t))
                {
                    Log.Error("Couldn't find cell near center of map");
                    return;
                }
            GenSpawn.Spawn(food, t, parent.Map);
            }

        }
        public override string CompInspectStringExtra() => active ? base.CompInspectStringExtra() + type.ToString() : base.CompInspectStringExtra();

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref active, "SupplyDepot_active");
            Scribe_Values.Look(ref type, "SupplyDepot_type");
        }
    }
    public class WorldObjectCompProperties_SupplyDepot : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SupplyDepot() => compClass = typeof(WorldObjectComp_SupplyDepot);
    }
}
