using RimWorld;
using RimWorld.Planet;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Verse;


namespace Flavor_Expansion
{
    
    class FactionHistoryDialog
    {
        public static DiaOption RequestFactionInfoOption(
        Faction faction,
         Pawn negotiator)
        {
            
            string text = "FactionInfo".Translate();
            DiaOption diaOption1 = new DiaOption(text);

            int disposition = Utilities.FactionsWar().GetByFaction(faction).disposition;
            DiaNode diaNode1 = new DiaNode("FactionChose".Translate(disposition == 0 ? "FactionNeutral".Translate() : disposition > 0 ? (disposition> 4 ? "FactionGenocidal".Translate() : "FactionWarlike".Translate()) : disposition<-4 ? "FactionPacifistic".Translate() : "FactionPeacelike".Translate()) + (Prefs.DevMode ? " (Debug): (" + disposition + ")\n" : "\n") + "FactionDispositionInfo".Translate());
            
            #region History
            // Faction History
            if (EndGame_Settings.FactionHistory)
            {
                DiaNode diaNode2 = new DiaNode((Utilities.FactionsWar().GetByFaction(faction).ancientHistory).CapitalizeFirst() + (Utilities.FactionsWar().GetByFaction(faction).history).CapitalizeFirst());

                diaNode1.options.Add(new DiaOption("FactionHistory".Translate())
                {
                    link = diaNode2

                });

                diaNode2.options.Add(new DiaOption("GoBack".Translate())
                {
                    link = diaNode1
                });
            }
            #endregion History

            #region War
            
            if (EndGame_Settings.FactionWar)
            {
                foreach (War war in Find.World.GetComponent<FE_WorldComp_FactionsWar>().GetWars().Where(x=> x.AttackerFaction()==faction || x.DefenderFaction()==faction))
                {
                    // Faction Wars
                    DiaNode diaNode3 = new DiaNode(HistoryDialogDataBase.GetWarInfo(faction,war));

                    diaNode1.options.Add(new DiaOption("FactionWar".Translate(war.AttackerFaction() == faction ? war.DefenderFaction() : war.AttackerFaction()))
                    {
                        link = diaNode3

                    });
                    diaNode3.options.Add(new DiaOption("GoBack".Translate())
                    {
                        link = diaNode1
                    });
                }
            }
            #endregion War

            DiaNode vassalInfo = new DiaNode("FactionVasalageInfo".Translate(faction));

            vassalInfo.options.Add(new DiaOption("Disconnect".Translate())
            {
                resolveTree = true
            });

            DiaNode tributaryInfo = new DiaNode("FactionTributaryInfo".Translate(faction));

            tributaryInfo.options.Add(new DiaOption("\""+"Disconnect".Translate()+"\"")
            {
                resolveTree = true
            });

            #region Vassal

            if (Utilities.FactionsWar().GetByFaction(faction).vassalage == 0)
            {
                DiaOption vassalage = new DiaOption("FactionVassalage".Translate())
                {
                    link = vassalInfo,

                    action = new Action(() =>
                    {
                        Utilities.FactionsWar().GetByFaction(faction).vassalage = 2;
                        faction.TryAffectGoodwillWith(Faction.OfPlayer, 100);
                    })
                };

                if (Prefs.DevMode || faction.def.permanentEnemy || (Utilities.FactionsWar().GetResouceAmount(faction) < 2000 && Utilities.FactionsWar().GetResouceAmount(faction) >= Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal || Utilities.FactionsWar().GetResouceAmount(faction) / Math.Min(Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal, 10000) > 0.25f - Utilities.FactionsWar().GetByFaction(faction).disposition/100))
                {
                    DiaOption vassalagediaOption = new DiaOption("FactionVassalage".Translate());
                    if (faction.def.permanentEnemy)
                        vassalagediaOption.Disable("FactionVassalageDisabledEnemy".Translate());
                    else if (Prefs.DevMode)
                        vassalagediaOption.Disable("DevMode");
                    else vassalagediaOption.Disable("FactionVassalageDisabled".Translate());
                    diaNode1.options.Add(vassalagediaOption);
                }
                else
                {
                    diaNode1.options.Add(vassalage);
                }

                DiaOption tribute = new DiaOption("FactionTributary".Translate())
                {
                    link = tributaryInfo,

                    action = new Action(() =>
                    {
                        Utilities.FactionsWar().GetByFaction(faction).vassalage = 1;
                        faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Neutral);
                    })
                };

                if (Prefs.DevMode || faction.def.permanentEnemy || (Utilities.FactionsWar().GetResouceAmount(faction) < 3000 && Utilities.FactionsWar().GetResouceAmount(faction) >= Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal || Utilities.FactionsWar().GetResouceAmount(faction) / Math.Min(Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal, 10000) > 0.5f - Utilities.FactionsWar().GetByFaction(faction).disposition / 100))
                {
                    DiaOption tributarydiaOption = new DiaOption("FactionTributary".Translate());
                    if (faction.def.permanentEnemy)
                        tributarydiaOption.Disable("FactionTributaryDisabledEnemy".Translate());
                    else if (Prefs.DevMode)
                        tributarydiaOption.Disable("DevMode");
                    else tributarydiaOption.Disable("FactionTributaryDisabled".Translate());
                    diaNode1.options.Add(tributarydiaOption);
                }
                else
                {
                    diaNode1.options.Add(tribute);
                }

            }
            else if (Utilities.FactionsWar().GetByFaction(faction).vassalage == 2)
            {
                if (Find.TickManager.TicksGame - Utilities.FactionsWar().GetByFaction(faction).vassalageResourseCooldown < Global.DayInTicks * 3)
                {
                    DiaOption vassalage = new DiaOption("FactionVassalDemandResources".Translate())
                    {
                        link = diaNode1,

                        action = new Action(() =>
                        {
                            ThingDef def;
                            if (!DefDatabase<ThingDef>.AllDefs.Where(x => x.CountAsResource && x.IsStuff && x.BaseMarketValue >= 5 && x.PlayerAcquirable && !x.CanHaveFaction).TryRandomElement(out def))
                            {
                                Log.Error("No def found");
                                return;
                            }

                            Thing thing = ThingMaker.MakeThing(def);
                            thing.stackCount = new IntRange(75, 100).RandomInRange;
                            IntVec3 intVec3 = DropCellFinder.TradeDropSpot(Find.AnyPlayerHomeMap);
                            DropPodUtility.DropThingsNear(intVec3, Find.AnyPlayerHomeMap, new List<Thing>() { thing }, 110, false, false, false);
                            Utilities.FactionsWar().GetByFaction(faction).vassalageResourseCooldown = Find.TickManager.TicksGame;
                        })
                    };
                    diaNode1.options.Add(vassalage);
                }
                else
                {
                    DiaOption vassalage = new DiaOption("FactionVassalDemandResources".Translate());
                    vassalage.Disable("FactionVassalDemandResourcesDisabled".Translate());
                    diaNode1.options.Add(vassalage);
                }
            }
            
            
            #endregion Vassal

            diaNode1.options.Add(new DiaOption("GoBack".Translate())
            {
                linkLateBind = (Func<DiaNode>)(() => FactionDialogMaker.FactionDialogFor(negotiator, faction))

            });

            diaOption1.link = diaNode1;
            return diaOption1;
        }
    };
    


    class HistoryDialogDataBase
    {
        private readonly List<string> HistoryOptions2 = new List<string>();
        private readonly IntRange leapInYears2 = new IntRange(7, 50);

        public HistoryDialogDataBase()
        {

        }
        public static List<string> GetOptions()
        {
            List<string> HistoryOptions = new List<string>();
            string text;
            if ("HistoryFactions".TryTranslate(out text))
            {
                HistoryOptions.Add("HistoryFactions");
                int y = 2;
                while (("HistoryFactions" + y).TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryFactions" + y);
                    y++;
                }
                y = 1;
                while (("HistoryFactions" + y + "_Dead").TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryFactions" + y + "_Dead");
                    y++;
                }
            }
            if ("HistoryTown".TryTranslate(out text))
            {
                HistoryOptions.Add("HistoryTown");
                int y = 2;
                while (("HistoryTown" + y).TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryTown" + y);
                    y++;
                }
                y = 2;
                while (("HistoryTown" + y + "_Destroyed").TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryTown" + y + "_Destroyed");
                    y++;
                }
            }
            if ("HistoryPolitic".TryTranslate(out text))
            {
                HistoryOptions.Add("HistoryPolitic");
                int y = 2;
                while (("HistoryPolitic" + y).TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryPolitic" + y);
                    y++;
                }
            }

            if ("HistoryGeneric".TryTranslate(out text))
            {
                HistoryOptions.Add("HistoryGeneric");
                int y = 2;
                while (("HistoryGeneric" + y).TryTranslate(out text))
                {
                    HistoryOptions.Add("HistoryGeneric" + y);
                    y++;
                }
            }

            return HistoryOptions;
        }
        public static string GenerateHistory(Faction subject, ref int disposition)
        {
            
            IntRange leapInYears = new IntRange(7, 40);
            List<string> HistoryOptions = GetOptions();

            if (HistoryOptions.NullOrEmpty())
            {
                Log.Error("HistoryDialogDataBase not constracted correctly, HistoryOptions count: " + HistoryOptions.Count());
                return "";
            }
            string text = "";
            
            int year = Find.TickManager.StartingYear-300;
            while(year< Find.TickManager.StartingYear)
            {
                string option = "";
                if (!HistoryOptions.TryRandomElement(out option))
                {
                    Log.Error("TopHistory is empty");
                    return "";
                }
                text += "HistoryDate".Translate(year);

                if (option.Contains("HistoryFactions"))
                {
                    HistoryOptions.Remove(option);

                    if (option.Contains("DeadFaction"))
                    {
                        List<string> extantNames = new List<string>();
                         
                        text += TrimKeys(option.Translate(subject, NameGenerator.GenerateName(subject.def.factionNameMaker, Find.FactionManager.AllFactionsVisible.Select<Faction, string>((Func<Faction, string>)(fac => fac.Name)))), subject, ref disposition);
                    }
                    else text += TrimKeys( option.Translate(subject, Find.FactionManager.AllFactionsVisible.Where(fac=> fac!=subject && !fac.def.hidden).RandomElement().Name),subject, ref disposition);
                }
                if(option.Contains("HistoryTown"))
                {
                    HistoryOptions.Remove(option);
                    text += TrimKeys(option.Translate(option.Contains("Destroyed") ? SettlementNameGenerator.GenerateSettlementName(Find.WorldObjects.Settlements.FindAll(x => x.Faction == subject).RandomElement()) : Find.WorldObjects.Settlements.FindAll(x => x.Faction == subject).RandomElement().Name), subject, ref disposition);
                }
                if (option.Contains("HistoryPolitic"))
                {
                    HistoryOptions.Remove(option);
                    text += TrimKeys( TranslatorFormattedStringExtensions.Translate(option,PawnGenerator.GeneratePawn(subject.RandomPawnKind(), subject)), subject,ref disposition);
                }
                if (option.Contains("HistoryGeneric"))
                {
                    HistoryOptions.Remove(option);
                    text += TrimKeys(option.Translate(), subject, ref disposition);
                }
                year += leapInYears.RandomInRange;
            }
            return text;
        }

        private static string TrimKeys(string option, Faction subject, ref int disposition)
        {
            if (option.NullOrEmpty() || option == "")
            {
                Log.Warning("null");
                return "";
            }
            if (option.Contains("_!Tribal"))
            {
                if (subject.def.techLevel.IsNeolithicOrWorse())
                {
                    option = "";
                    option += "HistoryTribalFallback".Translate();
                }
                else option = option.Replace("_!Tribal", "");
            }
            if (option.Contains("_Warlike"))
            {
                disposition++;
                option=option.Replace("_Warlike", "");
            }
            if (option.Contains("_Peacelike"))
            {
                disposition--;
                option=option.Replace("_Peacelike", "");
            }
            return option;
        }

        public static string GetWarInfo(Faction subject, War war)
        {
            string text = "";
            List<War> wars = Utilities.FactionsWar().GetWars();

            while (wars.Count() > 20)
                wars.Remove(wars.First());

            float resourceAtt = Utilities.FactionsWar().GetResouceAmount(war.AttackerFaction());
            float resourceDefe = Utilities.FactionsWar().GetResouceAmount(war.DefenderFaction());
            if (resourceAtt / resourceDefe == 0 && resourceDefe / resourceAtt == 0)
                text += "FactionWarInfoSettlemate".Translate(subject, (subject == war.DefenderFaction() ? war.AttackerFaction() : war.DefenderFaction()));
            else text += "FactionWarInfo".Translate(subject, (subject == war.DefenderFaction() ? war.AttackerFaction() : war.DefenderFaction()), war.AttackerFaction(), resourceAtt > resourceDefe ? war.AttackerFaction() : war.DefenderFaction(), resourceAtt > resourceDefe ? Math.Floor((1-(float)resourceDefe / (float)resourceAtt) * 100) : Math.Floor((1-(float)resourceAtt / (float)resourceDefe) * 100)) + "\n\n";
            if (Prefs.DevMode)
                text += resourceAtt + ", " + resourceDefe + "\n\n";
            text += war.warHistory;

            if (text == "")
            {
                text = "FactionAtPeace".Translate(subject);
            }
            return text;
        }
    };
}
