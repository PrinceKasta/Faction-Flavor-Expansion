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
    class WorldComp_SiteDefense : WorldObjectComp
    {
        private bool active = false, threat=true;
        private int timer = 0, stopTime;
        private IIncidentTarget parms;
        private Faction ally;
        public void StartComp(int stopTime, IncidentParms parms, Faction ally)
        {
            this.parms = parms.target;
            this.stopTime = stopTime;
            this.active = true;
            this.ally = ally;
        }
        public bool IsActive()
        {
            return active;
        }

        public override void CompTick()
        {
            if (!active)
                return;
            base.CompTick();
            MapParent map = (MapParent)this.parent;
            if (!map.HasMap && timer > stopTime)
            {
                if (parent == null)
                    Log.Warning("faction null");
                parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -10);
                Faction enemy;
                if (!Find.FactionManager.AllFactions.Where(x => x.HostileTo(Faction.OfPlayer) &&
                x.HostileTo(ally) && !x.defeated && !x.def.hidden).TryRandomElement(out enemy))
                    return;
                List<Thing> list = new List<Thing>();
                Site site = (Site)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Site);
                site.SetFaction(enemy);
                site.Tile = parent.Tile;
                site.factionMustRemainHostile = true;
                float num1 = StorytellerUtility.DefaultThreatPointsNow(parms);
                site.desiredThreatPoints = num1;
                float myThreatPoints2 = !EndGameDefOf.Outpost_opbase.wantsThreatPoints ? 0.0f : num1;
                site.parts.Add(new SitePart(EndGameDefOf.Outpost_opbase, EndGameDefOf.Outpost_opbase.Worker.GenerateDefaultParams(site, myThreatPoints2)));
                site.core = new SiteCore(SiteCoreDefOf.Nothing, SiteCoreDefOf.Nothing.Worker.GenerateDefaultParams(site, myThreatPoints2));

                site.GetComponent<DefeatAllEnemiesQuestComp>().StartQuest(parent.Faction, 12, list);
                int randomInRange = SiteTuning.QuestSiteTimeoutDaysRange.RandomInRange;
                // Balance
                site.GetComponent<WorldComp_opbase>().StartComp(parms);
                site.GetComponent<TimeoutComp>().StartTimeout(SiteTuning.QuestSiteRefugeeTimeoutDaysRange.RandomInRange);
                Find.WorldObjects.Add(site);
                Find.WorldObjects.Remove(parent);
                active = false;
            }
            else timer++;
            if (threat)
                HostileDefeated();

        }

        private bool HostileDefeated()
        {
            MapParent map = (MapParent)this.parent;
            if (map.HasMap && !GenHostility.AnyHostileActiveThreatTo(map.Map, Faction.OfPlayer))
            {
                Settlement enemy = (from s in Find.WorldObjects.Settlements
                                    where s.Faction.HostileTo(Faction.OfPlayer) && s.Faction.HostileTo(ally) && !s.Faction.def.hidden
                                    select s).RandomElement();
                if (enemy == null)
                {
                    Log.Warning("enemy null");
                }

                /*
                 * A joint attack on enemy settlment
                 */
                // Balance
                parent.Faction.TryAffectGoodwillWith(Faction.OfPlayer, 30);
                var storyComp = Find.Storyteller.storytellerComps.First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
                var threatparms = storyComp.GenerateParms(IncidentCategoryDefOf.Misc, Find.AnyPlayerHomeMap);
                threatparms.faction = enemy.Faction;
                EndGameDefOf.FE_JointRaid.Worker.TryExecute(threatparms);

                threat = false;
                return true;
            }
            return false;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref active, "SiteDefense active", defaultValue: false);
            Scribe_References.Look(ref parms, "sitedefense_parms");
            Scribe_References.Look(ref ally, "sitedefense_ally");
            Scribe_Values.Look(ref timer, "sitedefense_timer", defaultValue : 0);
            Scribe_Values.Look(ref stopTime, "sitedefense_stopTime", defaultValue: 0);
            Scribe_Values.Look(ref threat, "sitedefense_threat", defaultValue: true);
        }
    }
    public class WorldObjectCompProperties_SiteDefense : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SiteDefense()
        {
            this.compClass = typeof(WorldComp_SiteDefense);
        }
    }
}
