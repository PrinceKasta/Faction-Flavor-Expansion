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
            if (Find.AnyPlayerHomeMap == null)
            {
                return;
            }
            List<Thing> payment = new List<Thing>();
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            
            string factionList = "";
            bool vassalPay = false, TributePay = false;

            foreach (LE_FactionInfo f in Utilities.FactionsWar().factionInfo.Where(i=>i.vassalage!=0))
            {
                if (f.vassalage == 2)
                {
                    if (f.faction.PlayerRelationKind == FactionRelationKind.Hostile)
                    {
                        f.vassalage = 0;
                        return;
                    }
                    // Goodwill decay
                    else if (Find.TickManager.TicksGame % (Global.DayInTicks * 7) == 0)
                    {
                        f.faction.TryAffectGoodwillWith(Faction.OfPlayer, RelationsInvestment(f, payment), false, true, "FactionVassalageGoodWillDecay".Translate());
                    }
                    // Vassal Tribute
                    if (GenLocalDate.Year(Find.AnyPlayerHomeMap) == year && GenLocalDate.DayOfYear(Find.AnyPlayerHomeMap) == 0)
                    {
                        silver.stackCount = new IntRange(800 + (f.faction.PlayerGoodwill * 5), 1700 + (f.faction.PlayerGoodwill * 5)).RandomInRange;
                        factionList += f.faction + ",";
                        vassalPay = true;
                        payment.Add(silver);
                    }
                    // Investment returns
                    if (GenLocalDate.DayOfYear(Find.AnyPlayerHomeMap) == dayOfMonth)
                    {
                        ArmoryInvestment(f, payment, ref vassalPay);
                        WeaponryInvestment(f, payment, ref vassalPay);
                        RawMaterialsInvestment(f, payment, ref vassalPay);
                        MedicineInvestment(f, payment, ref vassalPay);
                        DruglabsInvestment(f, payment, ref vassalPay);
                        ProstheticslabsInvestment(f, payment, ref vassalPay);
                        FoodInvestment(f, payment, ref vassalPay);
                        ComponentInvestment(f, payment, ref vassalPay);
                        TradeInvestment(f, payment, ref vassalPay);

                        if (vassalPay)
                        {
                            factionList += f.faction + ",";
                        }
                    }
                }

                #region Tribute
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
                        TributePay = true;
                    }
                }
                #endregion Tribute
            }
            Payment(payment,vassalPay, TributePay ,factionList);
        }

        private void ArmoryInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        {
            if (!DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel && !x.apparel.tags.NullOrEmpty() && (x.apparel.tags.Contains("IndustrialMilitary") || x.apparel.tags.Contains("MedievalMilitary"))).TryRandomElement(out ThingDef def))
                return;
            Thing armor = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.RandomStuffByCommonalityFor(def, info.faction.def.techLevel) : null);
            switch (info.investments.Armory)
            {
                case 0:
                    return;
                case 1:
                    
                    armor.TryGetComp<CompQuality>().SetQuality(QualityCategory.Poor, ArtGenerationContext.Outsider);
                    payment.Add(armor);
                    vassalPay = true;
                    return;
                case 2:
                    armor.TryGetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                    payment.Add(armor);
                    vassalPay = true;
                    return;
                case 3:
                    armor.TryGetComp<CompQuality>().SetQuality(QualityCategory.Good, ArtGenerationContext.Outsider);
                    payment.Add(armor);
                    vassalPay = true;
                    return;
                case 4:
                    if (!DefDatabase<ThingDef>.AllDefs.Where(x => x.IsApparel && !x.apparel.tags.NullOrEmpty() && ((x.apparel.tags.Contains("IndustrialMilitary") || x.apparel.tags.Contains("MedievalMilitary") || x.apparel.tags.Contains("SpacerMilitary")))).TryRandomElement(out def))
                        return;
                    armor = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.RandomStuffByCommonalityFor(def, info.faction.def.techLevel) : null);
                    armor.TryGetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Outsider);
                    payment.Add(armor);
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private void WeaponryInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        {
            if (!DefDatabase<ThingDef>.AllDefs.Where(x => x.defName.Contains("Gun") && x.weaponTags !=null && !x.weaponTags.Contains("SimpleGun") && !x.weaponTags.Contains("SpacerGun")).TryRandomElement(out ThingDef def))
                return;
            Thing weapon = ThingMaker.MakeThing(def,def.MadeFromStuff ? GenStuff.RandomStuffByCommonalityFor(def, info.faction.def.techLevel) : null);
            switch (info.investments.Weaponry)
            {
                case 0:
                    return;
                case 1:
                    weapon.TryGetComp<CompQuality>().SetQuality(QualityCategory.Poor, ArtGenerationContext.Outsider);
                    payment.Add(weapon);
                    vassalPay = true;
                    return;
                case 2:
                    weapon.TryGetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                    payment.Add(weapon);
                    vassalPay = true;
                    return;
                case 3:
                    weapon.TryGetComp<CompQuality>().SetQuality(QualityCategory.Good, ArtGenerationContext.Outsider);
                    payment.Add(weapon);
                    vassalPay = true;
                    return;
                case 4:
                    if (!DefDatabase<ThingDef>.AllDefs.Where(x => x.defName.Contains("Gun") && x.weaponTags != null && !x.weaponTags.Contains("SimpleGun")).TryRandomElement(out def))
                        return;
                    weapon = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.RandomStuffByCommonalityFor(def, info.faction.def.techLevel) : null);
                    weapon.TryGetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Outsider);
                    payment.Add(weapon);
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private void RawMaterialsInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        {
            ThingCategoryDefOf.StoneBlocks.childThingDefs.TryRandomElement(out ThingDef stoneDef);
            Thing wood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
            Thing stone = ThingMaker.MakeThing(stoneDef);
            Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
            Thing jade = ThingMaker.MakeThing(ThingCategoryDefOf.ResourcesRaw.childThingDefs.Find(x=> x.defName.Contains("Jade")));
            Thing gold = ThingMaker.MakeThing(ThingDefOf.Gold);
            Thing plasteel = ThingMaker.MakeThing(ThingDefOf.Plasteel);
            Thing uranium = ThingMaker.MakeThing(ThingDefOf.Uranium);

            switch (info.investments.Mining)
            {
                case 0:
                    return;
                case 1:
                    wood.stackCount = wood.def.stackLimit;
                    payment.Add(wood);
                    stone.stackCount = stone.def.stackLimit;
                    payment.Add(stone);
                    vassalPay = true;
                    return;
                case 2:
                    wood.stackCount = wood.def.stackLimit * 2;
                    payment.Add(wood);
                    stone.stackCount = stone.def.stackLimit * 2;
                    payment.Add(stone);
                    steel.stackCount = steel.def.stackLimit;
                    payment.Add(steel);
                    vassalPay = true;
                    return;
                case 3:
                    wood.stackCount = wood.def.stackLimit * 3;
                    payment.Add(wood);
                    stone.stackCount = stone.def.stackLimit * 3;
                    payment.Add(stone);
                    steel.stackCount = steel.def.stackLimit * 2;
                    payment.Add(steel);
                    jade.stackCount = jade.def.stackLimit;
                    payment.Add(jade);
                    gold.stackCount = gold.def.stackLimit;
                    payment.Add(gold);
                    vassalPay = true;
                    return;
                case 4:
                    wood.stackCount = wood.def.stackLimit * 3;
                    payment.Add(wood);
                    stone.stackCount = stone.def.stackLimit * 3;
                    payment.Add(stone);
                    steel.stackCount = steel.def.stackLimit * 2;
                    payment.Add(steel);
                    jade.stackCount = jade.def.stackLimit;
                    payment.Add(jade);
                    gold.stackCount = gold.def.stackLimit;
                    payment.Add(gold);
                    plasteel.stackCount = plasteel.def.stackLimit;
                    payment.Add(plasteel);
                    uranium.stackCount = uranium.def.stackLimit;
                    payment.Add(uranium);
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private void MedicineInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        {
            Thing herbal = ThingMaker.MakeThing(ThingDefOf.MedicineHerbal);
            Thing medicine = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
            Thing glitter = ThingMaker.MakeThing(ThingDefOf.MedicineUltratech);

            switch (info.investments.Medicine)
            {
                case 0:
                    return;
                case 1:
                    herbal.stackCount = herbal.def.stackLimit;
                    payment.Add(herbal);
                    vassalPay = true;
                    return;
                case 2:
                    herbal.stackCount = herbal.def.stackLimit * 2;
                    payment.Add(herbal);
                    medicine.stackCount = medicine.def.stackLimit;
                    payment.Add(medicine);
                    vassalPay = true;
                    return;
                case 3:
                    herbal.stackCount = herbal.def.stackLimit * 2;
                    payment.Add(herbal);
                    medicine.stackCount = medicine.def.stackLimit * 3;
                    payment.Add(medicine);
                    glitter.stackCount = glitter.def.stackLimit/2;
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private void DruglabsInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        {
            Thing beer = ThingMaker.MakeThing(ThingDefOf.Beer);
            Thing smokeleaf = ThingMaker.MakeThing(ThingDefOf.SmokeleafJoint);
            Thing peno = ThingMaker.MakeThing(ThingCategoryDefOf.Drugs.childThingDefs.Find(x => x.defName == "Penoxycyline"));
            Thing tea = ThingMaker.MakeThing(ThingCategoryDefOf.Drugs.childThingDefs.Find(x => x.defName == "PsychiteTea"));
            Thing yayo = ThingMaker.MakeThing(ThingCategoryDefOf.Drugs.childThingDefs.Find(x => x.defName == "Yayo"));
            Thing flake = ThingMaker.MakeThing(ThingCategoryDefOf.Drugs.childThingDefs.Find(x => x.defName == "Flake"));
            Thing wakeup = ThingMaker.MakeThing(ThingCategoryDefOf.Drugs.childThingDefs.Find(x => x.defName == "WakeUp"));
            Thing gogo = ThingMaker.MakeThing(ThingCategoryDefOf.Drugs.childThingDefs.Find(x => x.defName == "GoJuice"));
            Thing luciferium = ThingMaker.MakeThing(ThingDefOf.Luciferium);

            switch (info.investments.Druglabs)
            {
                case 0:
                    return;
                case 1:
                    beer.stackCount = beer.def.stackLimit;
                    payment.Add(beer);
                    smokeleaf.stackCount = smokeleaf.def.stackLimit;
                    payment.Add(smokeleaf);
                    vassalPay = true;
                    return;
                case 2:
                    beer.stackCount = beer.def.stackLimit * 2;
                    payment.Add(beer);
                    smokeleaf.stackCount = smokeleaf.def.stackLimit * 2;
                    payment.Add(smokeleaf);
                    peno.stackCount = peno.def.stackLimit;
                    payment.Add(peno);
                    tea.stackCount = tea.def.stackLimit;
                    payment.Add(tea);
                    vassalPay = true;
                    return;
                case 3:
                    beer.stackCount = beer.def.stackLimit * 3;
                    payment.Add(beer);
                    smokeleaf.stackCount = smokeleaf.def.stackLimit * 3;
                    payment.Add(smokeleaf);
                    peno.stackCount = peno.def.stackLimit;
                    payment.Add(peno);
                    tea.stackCount = tea.def.stackLimit;
                    payment.Add(tea);
                    yayo.stackCount = yayo.def.stackLimit;
                    payment.Add(yayo);
                    flake.stackCount = flake.def.stackLimit;
                    payment.Add(flake);
                    wakeup.stackCount = wakeup.def.stackLimit;
                    payment.Add(wakeup);
                    gogo.stackCount = gogo.def.stackLimit;
                    payment.Add(gogo);
                    vassalPay = true;
                    return;
                case 4:
                    beer.stackCount = beer.def.stackLimit * 3;
                    payment.Add(beer);
                    smokeleaf.stackCount = smokeleaf.def.stackLimit * 3;
                    payment.Add(smokeleaf);
                    peno.stackCount = peno.def.stackLimit;
                    payment.Add(peno);
                    tea.stackCount = tea.def.stackLimit;
                    payment.Add(tea);
                    yayo.stackCount = yayo.def.stackLimit;
                    payment.Add(yayo);
                    flake.stackCount = flake.def.stackLimit;
                    payment.Add(flake);
                    wakeup.stackCount = wakeup.def.stackLimit;
                    payment.Add(wakeup);
                    gogo.stackCount = gogo.def.stackLimit;
                    payment.Add(gogo);
                    luciferium.stackCount = luciferium.def.stackLimit;
                    payment.Add(luciferium);
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private void ProstheticslabsInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        {
            ThingCategoryDef artifical = ThingCategoryDefOf.BodyParts.childCategories.Find(p => p.defName == "BodyPartsArtificial");
            Thing prothesticLeg = ThingMaker.MakeThing(artifical.childThingDefs.Find(x => x.defName == "SimpleProstheticLeg"));
            Thing prothesticArm = ThingMaker.MakeThing(artifical.childThingDefs.Find(x => x.defName == "SimpleProstheticArm"));
            Thing bionicLeg = ThingMaker.MakeThing(artifical.childThingDefs.Find(x => x.defName == "BionicLeg"));
            Thing bionicArm = ThingMaker.MakeThing(artifical.childThingDefs.Find(x => x.defName == "BionicArm"));
            Thing bionicEye = ThingMaker.MakeThing(artifical.childThingDefs.Find(x => x.defName == "BionicEye"));
            Thing bionicEar = ThingMaker.MakeThing(artifical.childThingDefs.Find(x => x.defName == "BionicEar"));
            Thing bionicSpine = ThingMaker.MakeThing(artifical.childThingDefs.Find(x => x.defName == "BionicSpine"));
            Thing bionicStomach = ThingMaker.MakeThing(artifical.childThingDefs.Find(x => x.defName == "BionicStomach"));

            switch (info.investments.Prosthetics)
            {
                case 0:
                    return;
                case 1:
                    prothesticLeg.stackCount = 1;
                    payment.Add(prothesticLeg);
                    prothesticArm.stackCount = 1;
                    payment.Add(prothesticArm);
                    vassalPay = true;
                    return;
                case 2:
                    bionicLeg.stackCount = 1;
                    payment.Add(bionicLeg);
                    bionicArm.stackCount = 1;
                    payment.Add(bionicArm);
                    vassalPay = true;
                    return;
                case 3:
                    bionicLeg.stackCount = 1;
                    payment.Add(bionicLeg);
                    bionicArm.stackCount = 1;
                    payment.Add(bionicArm);
                    bionicEye.stackCount = 1;
                    payment.Add(bionicEye);
                    bionicEar.stackCount = 1;
                    payment.Add(bionicEar);
                    vassalPay = true;
                    return;
                case 4:
                    bionicLeg.stackCount = 1;
                    payment.Add(bionicLeg);
                    bionicArm.stackCount = 1;
                    payment.Add(bionicArm);
                    bionicEye.stackCount = 1;
                    payment.Add(bionicEye);
                    bionicEar.stackCount = 1;
                    payment.Add(bionicEar);
                    bionicSpine.stackCount = 1;
                    payment.Add(bionicSpine);
                    bionicStomach.stackCount = 1;
                    payment.Add(bionicStomach);
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private void FoodInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        { 
            Thing meat = ThingMaker.MakeThing(ThingCategoryDefOf.MeatRaw.childThingDefs.RandomElement());
            Thing plantMatter = ThingMaker.MakeThing(ThingCategoryDefOf.PlantFoodRaw.childThingDefs.RandomElement());

            switch (info.investments.Food)
            {
                case 0:
                    return;
                case 1:
                    plantMatter.stackCount = plantMatter.def.stackLimit;
                    payment.Add(plantMatter);
                    vassalPay = true;
                    return;
                case 2:
                    plantMatter.stackCount = plantMatter.def.stackLimit * 2;
                    payment.Add(plantMatter);
                    meat.stackCount = meat.def.stackLimit;
                    payment.Add(meat);
                    vassalPay = true;
                    return;
                case 3:
                    plantMatter.stackCount = plantMatter.def.stackLimit * 5;
                    payment.Add(plantMatter);
                    meat.stackCount = meat.def.stackLimit * 5;
                    payment.Add(meat);
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private void TradeInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        {
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);

            switch (info.investments.Trade)
            {
                case 0:
                    return;
                case 1:
                    silver.stackCount = silver.def.stackLimit * 3;
                    payment.Add(silver);
                    vassalPay = true;
                    return;
                case 2:
                    silver.stackCount = silver.def.stackLimit * 6;
                    payment.Add(silver);
                    vassalPay = true;
                    return;
                case 3:
                    silver.stackCount = silver.def.stackLimit * 10;
                    payment.Add(silver);
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private void ComponentInvestment(LE_FactionInfo info, List<Thing> payment, ref bool vassalPay)
        {
            Thing componentIndustrial = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
            Thing componentSpacer = ThingMaker.MakeThing(ThingDefOf.ComponentSpacer);

            switch (info.investments.Components)
            {
                case 0:
                    return;
                case 1:

                    componentIndustrial.stackCount = 30;
                    payment.Add(componentIndustrial);
                    vassalPay = true;
                    return;
                case 2:
                    componentIndustrial.stackCount = 60;
                    payment.Add(componentIndustrial);
                    vassalPay = true;
                    return;
                case 3:
                    componentIndustrial.stackCount = 60;
                    payment.Add(componentIndustrial);
                    componentSpacer.stackCount = 10;
                    payment.Add(componentSpacer);
                    vassalPay = true;
                    return;
                default:
                    return;
            }
        }

        private int RelationsInvestment(LE_FactionInfo info, List<Thing> payment)
        {
            switch (info.investments.Relations)
            {
                case 0:
                    return -10;
                case 1:
                    return 0;
                case 2:
                    return 10;
                case 3:
                    return 15;
                default:
                    return -2;
            }
        }


        private void Payment(List<Thing> payment, bool vassalPay, bool TributePay, string factionList)
        {
            if (GenLocalDate.Year(Find.AnyPlayerHomeMap) >= year)
            {
                year = GenLocalDate.Year(Find.AnyPlayerHomeMap) + 1;
            }
            if (GenLocalDate.DayOfYear(Find.AnyPlayerHomeMap) >= dayOfMonth)
            {
                dayOfMonth = ClosestNumberOf15(GenLocalDate.DayOfYear(Find.AnyPlayerHomeMap) + 1);

            }
            if (!payment.NullOrEmpty())
            {
                string text = "";
                if (vassalPay && TributePay)
                {
                    text += "FactionVassalSilverRecivedBoth".Translate(GenThing.GetMarketValue(payment).ToStringMoney(), GenLabel.ThingsLabel(payment, string.Empty));
                }
                else if (vassalPay)
                {
                    text += "FactionVassalSilverRecivedVassals".Translate(GenThing.GetMarketValue(payment).ToStringMoney(), GenLabel.ThingsLabel(payment, string.Empty));
                }
                else if (TributePay)
                {
                    text += "FactionVassalSilverRecivedTrivutaries".Translate(GenThing.GetMarketValue(payment).ToStringMoney(), GenLabel.ThingsLabel(payment, string.Empty));
                }

                factionList.Remove(factionList.Count() - 1);
                IntVec3 intVec3 = DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap);
                DropPodUtility.DropThingsNear(intVec3, Find.AnyPlayerHomeMap, payment, 110, false, true, true);
                Find.LetterStack.ReceiveLetter("LetterFactionVassalSilverRecived".Translate(), text + factionList, LetterDefOf.PositiveEvent, null);
            }
        }
    

        private static int ClosestNumberOf15(int num)
        {
            
            int q = num / 15;
            int n2 = 15 * (q + 1);

            return n2;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref year, "year");
            Scribe_Values.Look(ref dayOfMonth, "dayOfMonth");
        }
    }
}
