using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Flavor_Expansion
{
    public class Gift_RewardGeneratorBasedTMagic
    {
        
        private const float MedicineChance = 0.05f;
        private const float FoodChance = 0.3f;
        private const float ArmorChance = 0.2f;
        private const float MiscChance = 0.1f;
        private const float WeaponsChance = 0.2f;

        private static readonly IntRange MedicineStackRange = new IntRange(2, 8);
        private static readonly IntRange MedicineCountRange = new IntRange(1, 4);
        private static readonly IntRange FoodStackRange = new IntRange(40, 75);
        private static readonly IntRange FoodCountRange = new IntRange(1, 4);
        private static readonly IntRange ArmorCountRange = new IntRange(1, 2);
        private static readonly IntRange MiscCountRange = new IntRange(1, 2);
        private static readonly IntRange WeaponsCountRange = new IntRange(1, 2);

        private float collectiveMarketValue = 0;

        public List<Thing> Generate(int totalMarketValue, List<Thing> outThings)
        {

            for (int j = 0; j < 10; j++)
            {
                //Medicine
                if (Rand.Chance(MedicineChance) && (totalMarketValue - collectiveMarketValue) > 100)
                {
                    IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                                       where def.IsMedicine
                                                       select def;
                    int randomInRange = MedicineCountRange.RandomInRange;
                    for (int i = 0; i < randomInRange; i++)
                    {
                        Thing thing = ThingMaker.MakeThing(enumerable.ToList().RandomElement());
                        if (thing.Label.Contains("Glitter"))
                        {
                            thing.stackCount = Mathf.Clamp(MedicineStackRange.RandomInRange, 1, 2);
                            collectiveMarketValue += thing.stackCount * 100;
                        }
                        else thing.stackCount = MedicineStackRange.RandomInRange;
                        if (thing.MarketValue * thing.stackCount > totalMarketValue)
                            continue;
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue * thing.stackCount;
                    }
                
                }
                //Food
                if (Rand.Chance(FoodChance) && (totalMarketValue - collectiveMarketValue) > 100)
                {
                    IEnumerable<ThingDef> enumerable = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsNutritionGivingIngestible && def.PlayerAcquirable && def.CountAsResource && def.BaseMarketValue < 15 && !def.label.Contains("Human"));
                    int randomInRange = FoodCountRange.RandomInRange;
                    Thing thing = ThingMaker.MakeThing(enumerable.RandomElement(), null);
                    thing.stackCount = 0;
                    for (int i = 0; i < randomInRange; i++)
                    {
                        thing.stackCount += FoodStackRange.RandomInRange;
                        if (thing.MarketValue * thing.stackCount > totalMarketValue * 1.5)
                            continue;
                    }
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue * thing.stackCount;
                }
                //Armor
                if (Rand.Chance(ArmorChance) && (totalMarketValue - collectiveMarketValue) > 100)
                {
                    IEnumerable<ThingDef> enumerable = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsApparel && def.BaseMarketValue > 100);
                    int randomInRange = ArmorCountRange.RandomInRange;
                    for (int i = 0; i < randomInRange; i++)
                    {
                        ThingDef thingDef = enumerable.RandomElement();
                        Thing thing = ThingMaker.MakeThing(thingDef,GenStuff.RandomStuffByCommonalityFor(thingDef,Find.FactionManager.OfPlayer.def.techLevel));
                        if (thing.MarketValue > totalMarketValue)
                            continue;
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue;
                    }
                }
                //Weapons
                if (Rand.Chance(WeaponsChance) && (totalMarketValue - collectiveMarketValue) > 100)
                {
                    IEnumerable<ThingDef> enumerable = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsWeapon && def.BaseMarketValue > 20 && !def.label.Contains("tornado") && !def.label.Contains("orbital"));
                    int randomInRange = WeaponsCountRange.RandomInRange;
                    for (int i = 0; i < randomInRange; i++)
                    {
                        ThingDef thingDef = enumerable.RandomElement();
                        Thing thing = ThingMaker.MakeThing(thingDef, GenStuff.RandomStuffByCommonalityFor(thingDef, Find.FactionManager.OfPlayer.def.techLevel));
                        if (thing.MarketValue > totalMarketValue)
                            continue;
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue;
                    }
                }
                //Misc
                if (Rand.Chance(MiscChance) && (totalMarketValue - collectiveMarketValue) > 100)
                {
                    IEnumerable<ThingDef> enumerable = DefDatabase<ThingDef>.AllDefs.Where(def => def.PlayerAcquirable && def.CountAsResource && !def.IsNutritionGivingIngestible && !def.IsWeapon && !def.IsApparel && !def.IsMedicine && def.stackLimit == 1);
                    int randomInRange = MiscCountRange.RandomInRange;
                    for (int i = 0; i < randomInRange; i++)
                    {
                        Thing thing = ThingMaker.MakeThing(enumerable.RandomElement());
                        thing.stackCount = randomInRange;
                        if (thing.MarketValue * thing.stackCount > totalMarketValue)
                            continue;
                        outThings.Add(thing);
                        collectiveMarketValue += thing.MarketValue * thing.stackCount;
                    }
                }
            }
            return outThings;
        }
    }
    public class Aid_RewardGeneratorBasedTMagic
    {

        private float collectiveMarketValue = 0;

        public List<Thing> Generate(int totalMarketValue, int StarvingCount, int InjuredCount, List<Thing> outThings, Faction faction)
        {
            IntRange MedicineStackRange = new IntRange(InjuredCount, InjuredCount+3);
            IntRange FoodStackRange = new IntRange(20 + StarvingCount, 40 + StarvingCount);

            for (int j = 0; j < 10; j++)
            {
                //Medicine
                if ((totalMarketValue - collectiveMarketValue) > 0 && InjuredCount > 0)
                {
                    IEnumerable<ThingDef> enumerableMedicine = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsMedicine && def.techLevel == faction.def.techLevel);
                    Thing thing = ThingMaker.MakeThing(enumerableMedicine.RandomElement());
                    thing.stackCount = MedicineStackRange.RandomInRange;
                    if (thing.MarketValue * thing.stackCount > totalMarketValue)
                        continue;
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue * thing.stackCount;
                }
                if ((totalMarketValue - collectiveMarketValue) > 0 && StarvingCount > 0)
                {
                    IEnumerable<ThingDef> enumerableFood = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsNutritionGivingIngestible && def.PlayerAcquirable && def.CountAsResource && !def.IsDrug && !def.label.Contains("Human"));
                    ThingDef thingDef = enumerableFood.RandomElement();
                    Thing thing = ThingMaker.MakeThing(thingDef, null);
                    thing.stackCount += FoodStackRange.RandomInRange;
                    if (thing.MarketValue * thing.stackCount > totalMarketValue * 1.5)
                        continue;
                    outThings.Add(thing);
                    collectiveMarketValue += thing.MarketValue * thing.stackCount;
                }
            }
            List<Thing> mergeThings =new List<Thing>();
            foreach(Thing thing in outThings)
            {
                bool found = false;
                foreach(Thing merge in mergeThings)
                {
                    if (merge.def == thing.def)
                    {
                        merge.stackCount += thing.stackCount;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    mergeThings.Add(thing);
                }
            }
            return mergeThings;
        }
    
    }
}
    

