using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Flavor_Expansion
{
    class FactionDialogUtilities
    {
        public static string ArmoryTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_ArmoryLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Armory).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeArmoryNWeaponry(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
            Thing plasteel = ThingMaker.MakeThing(ThingDefOf.Plasteel);

            switch(level)
            {
                case 0:
                    // 1000 silver, 150 steel
                    silver.stackCount = 1000;
                    steel.stackCount = 150;
                    requirements.Add(silver);
                    requirements.Add(steel);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 1:
                    // 1000 silver, 150 steel
                    silver.stackCount = 1750;
                    steel.stackCount = 300;
                    requirements.Add(silver);
                    requirements.Add(steel);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 2:
                    // 1000 silver, 150 steel
                    silver.stackCount = 3000;
                    steel.stackCount = 500;
                    requirements.Add(silver);
                    requirements.Add(steel);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 3:
                    // 1000 silver, 150 steel
                    silver.stackCount = 3000;
                    steel.stackCount = 875;
                    plasteel.stackCount = 300;
                    requirements.Add(silver);
                    requirements.Add(steel);
                    requirements.Add(plasteel);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == plasteel.def).Sum(t => t.stackCount) >= plasteel.stackCount)
                        {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static string WeaponryTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_WeaponryLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Weaponry).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static string RawMaterialsTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_RawMaterialLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Mining).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeRawMaterials(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);

            switch (level)
            {
                case 0:
                    silver.stackCount = 2000;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap,silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                case 1:
                    silver.stackCount = 3000;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                case 2:
                    silver.stackCount = 4000;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                case 3:
                    silver.stackCount = 5000;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static string MedicineTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_MedicineLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Medicine).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeMedicine(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            Thing cloth = ThingMaker.MakeThing(ThingDefOf.Cloth, ThingDefOf.Cloth.IsStuff ? GenStuff.RandomStuffByCommonalityFor(ThingDefOf.Cloth, faction.def.techLevel) : null);
            switch (level)
            {
                case 0:
                    silver.stackCount = 2500;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                case 1:
                    silver.stackCount = 5000;
                    cloth.stackCount = 300;
                    requirements.Add(silver);
                    requirements.Add(cloth);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == cloth.def).Sum(t => t.stackCount) >= cloth.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 2:
                    silver.stackCount = 7000;
                    cloth.stackCount = 600;
                    requirements.Add(silver);
                    requirements.Add(cloth);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == cloth.def).Sum(t => t.stackCount) >= cloth.stackCount)
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static string DruglabsTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_DruglabsLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Druglabs).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeDruglabs(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            Thing hops = ThingMaker.MakeThing(ThingCategoryDefOf.PlantMatter.childThingDefs.Find(x=> x.defName== "RawHops"));
            Thing smokeleaf = ThingMaker.MakeThing(ThingCategoryDefOf.PlantMatter.childThingDefs.Find(x => x.defName == "SmokeleafLeaves"));
            Thing psyLeaves= ThingMaker.MakeThing(ThingCategoryDefOf.PlantMatter.childThingDefs.Find(x => x.defName == "PsychoidLeaves"));
            switch (level)
            {
                case 0:
                    silver.stackCount = 1000;
                    requirements.Add(silver);
                    hops.stackCount = 200;
                    requirements.Add(hops);
                    smokeleaf.stackCount = 200;
                    requirements.Add(smokeleaf);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == hops.def).Sum(t => t.stackCount) >= hops.stackCount && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == smokeleaf.def).Sum(t => t.stackCount) >= smokeleaf.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 1:
                    silver.stackCount = 4500;
                    requirements.Add(silver);
                    psyLeaves.stackCount = 150;
                    requirements.Add(psyLeaves);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == psyLeaves.def).Sum(t => t.stackCount) >= psyLeaves.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 2:
                    silver.stackCount = 7000;
                    requirements.Add(silver);
                    psyLeaves.stackCount = 500;
                    requirements.Add(psyLeaves);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == psyLeaves.def).Sum(t => t.stackCount) >= psyLeaves.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 3:
                    silver.stackCount = 13000;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static string ProstheticslabsTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_ProstheticslabsLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Prosthetics).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeProstheticslabs(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
            Thing comp = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
            Thing compAdvence = ThingMaker.MakeThing(ThingDefOf.ComponentSpacer);

            switch (level)
            {
                case 0:
                    silver.stackCount = 2000;
                    requirements.Add(silver);
                    steel.stackCount = 800;
                    requirements.Add(steel);
                    comp.stackCount = 30;
                    requirements.Add(comp);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == comp.def).Sum(t => t.stackCount) >= comp.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 1:
                    silver.stackCount = 4500;
                    requirements.Add(silver);
                    steel.stackCount = 2000;
                    requirements.Add(steel);
                    compAdvence.stackCount = 15;
                    requirements.Add(comp);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == compAdvence.def).Sum(t => t.stackCount) >= compAdvence.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 2:
                    silver.stackCount = 7000;
                    requirements.Add(silver);
                    steel.stackCount = 2000;
                    requirements.Add(steel);
                    compAdvence.stackCount = 30;
                    requirements.Add(comp);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == compAdvence.def).Sum(t => t.stackCount) >= compAdvence.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 3:
                    silver.stackCount = 10000;
                    requirements.Add(silver);
                    steel.stackCount = 2000;
                    requirements.Add(steel);
                    compAdvence.stackCount = 50;
                    requirements.Add(comp);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == compAdvence.def).Sum(t => t.stackCount) >= compAdvence.stackCount)
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static string FoodTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_FoodLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Food).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeFood(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            Thing hay = ThingMaker.MakeThing(ThingDefOf.Hay);

            switch (level)
            {
                case 0:
                    silver.stackCount = 250;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                case 1:
                    silver.stackCount = 500;
                    requirements.Add(silver);
                    hay.stackCount = 300;
                    requirements.Add(hay);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == hay.def).Sum(t => t.stackCount) >= hay.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 2:
                    silver.stackCount = 1350;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static string ComponentsTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_ComponentsLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Components).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeComponents(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);

            switch (level)
            {
                case 0:
                    silver.stackCount = 2000;
                    requirements.Add(silver);
                    steel.stackCount = 1600;
                    requirements.Add(steel);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 1:
                    silver.stackCount = 4500;
                    requirements.Add(silver);
                    steel.stackCount = 2500;
                    requirements.Add(steel);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 2:
                    silver.stackCount = 7000;
                    requirements.Add(silver);
                    steel.stackCount = 4000;
                    requirements.Add(steel);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount) && TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == steel.def).Sum(t => t.stackCount) >= steel.stackCount)
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static string TradeTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_TradeLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Trade).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeTrade(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);

            switch (level)
            {
                case 0:
                    silver.stackCount = 5000;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                case 1:
                    silver.stackCount = 7500;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                case 2:
                    silver.stackCount = 10000;
                    requirements.Add(silver);
                    if (TradeUtility.ColonyHasEnoughSilver(Find.AnyPlayerHomeMap, silver.stackCount))
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        public static string RelationsTextPerLevel(Faction faction, List<Thing> requirements) => ("FE_RelationsLevel" + Utilities.FactionsWar().GetByFaction(faction).investments.Relations).Translate(faction, GenLabel.ThingsLabel(requirements, string.Empty));

        public static bool CanUpgradeRelations(Faction faction, List<Thing> requirements, int level)
        {
            requirements.Clear();
            Thing gold = ThingMaker.MakeThing(ThingDefOf.Gold);

            switch (level)
            {
                case 0:
                    gold.stackCount = 375;
                    requirements.Add(gold);
                    
                    if (TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where<Thing>(t => t.def == gold.def).Sum<Thing>(t => t.stackCount) >= gold.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 1:
                    gold.stackCount = 500;
                    requirements.Add(gold);
                    if (TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == gold.def).Sum(t => t.stackCount) >= gold.stackCount)
                    {
                        return true;
                    }
                    return false;
                case 2:
                    gold.stackCount = 1000;
                    requirements.Add(gold);
                    if (TradeUtility.AllLaunchableThingsForTrade(Find.AnyPlayerHomeMap).Where(t => t.def == gold.def).Sum(t => t.stackCount) >= gold.stackCount)
                    {
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }
        public static class LevenshteinDistance
        {
            /// <summary>
            ///     Calculate the difference between 2 strings using the Levenshtein distance algorithm
            /// </summary>
            /// <param name="source1">First string</param>
            /// <param name="source2">Second string</param>
            /// <returns></returns>
            public static int Calculate(string source1, string source2) //O(n*m)
            {
                var source1Length = source1.Length;
                var source2Length = source2.Length;

                var matrix = new int[source1Length + 1, source2Length + 1];

                // First calculation, if one entry is empty return full length
                if (source1Length == 0)
                    return source2Length;

                if (source2Length == 0)
                    return source1Length;

                // Initialization of matrix with row size source1Length and columns size source2Length
                for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
                for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

                // Calculate rows and collumns distances
                for (var i = 1; i <= source1Length; i++)
                {
                    for (var j = 1; j <= source2Length; j++)
                    {
                        var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                        matrix[i, j] = Math.Min(
                            Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                            matrix[i - 1, j - 1] + cost);
                    }
                }
                // return result
                return matrix[source1Length, source2Length];
            }
        }
    }
}
