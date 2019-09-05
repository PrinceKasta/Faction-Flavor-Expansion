using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Flavor_Expansion
{
    class FE_MapComponent_Bombardment : MapComponent
    {
        // Balance
        private Settlement bomber;
        private int length=0;
        private int startTimer=0;
        private IntVec3 direction = new IntVec3();
        
        private static readonly IntRange bombardmentDamage = new IntRange(10, 20);
        private static readonly IntRange cellVariation = new IntRange(-20, 20);

        public FE_MapComponent_Bombardment(Map map) : base(map)
        {
            
        }

        public void ForceStart(int length)
        {
            this.length = length;
        }

        public override void MapComponentTick()
        {
            if (!(map.ParentFaction == Faction.OfPlayer) || !EndGame_Settings.Bombardment || bomber == null)
                return;
            if (bomber != null && !Find.WorldObjects.Settlements.Contains(bomber))
                bomber = null;
            if (length < 0 || Find.TickManager.TicksGame < startTimer)
            {
                return;
            }
            if (Find.TickManager.TicksGame == startTimer)
                Find.LetterStack.ReceiveLetter("LetterLabelBombardmentThreatStarted".Translate(), "BombardmentThreatStarted".Translate(bomber.Name), LetterDefOf.ThreatBig,new LookTargets(direction, map) ,bomber.Faction);

            length--;

            if (length == -1)
                Find.LetterStack.ReceiveLetter("LetterLabelBombardmentThreatStopped".Translate(), "BombardmentThreatStopped".Translate(bomber.Name), LetterDefOf.PositiveEvent, new LookTargets(direction, map), bomber.Faction);

            if ( length != 0 && length % 200==0)
            {
                Projectile_Explosive shell = (Projectile_Explosive)ThingMaker.MakeThing(EndGameDefOf.Bullet_Shell_HighExplosive);
                IntVec3 intVec3= CellFinder.RandomNotEdgeCell(20, map);  
                GenSpawn.Spawn(shell, direction, map);
                shell.Launch(null, intVec3, intVec3, ProjectileHitFlags.IntendedTarget, shell);
                    
            }
        }

        public void StartComp(int length, Settlement bomber, int startTimer)
        {
            this.startTimer = startTimer + Find.TickManager.TicksGame;
            this.bomber = bomber;
            this.length = length;
            this.direction= GetDir(Find.WorldGrid.GetDirection8WayFromTo(map.Tile, bomber.Tile));

        }

        private IntVec3 GetDir(Direction8Way dir)
        {
            switch (dir)
            {
                case Direction8Way.North:
                    return new IntVec3(map.Center.x + cellVariation.RandomInRange, 1, map.Size.z-1);
                case Direction8Way.NorthEast:
                    return new IntVec3(map.Size.x-1 , 1, map.Size.z-1);
                case Direction8Way.East:
                    return new IntVec3(map.Size.x-1, 1, map.Center.z + cellVariation.RandomInRange);
                case Direction8Way.SouthEast:
                    return new IntVec3(map.Size.x-1, 1, 0) ;
                case Direction8Way.South:
                    return new IntVec3(map.Center.x + cellVariation.RandomInRange, 1, 0);
                case Direction8Way.SouthWest:
                    return new IntVec3(0, 1, 0);
                case Direction8Way.West:
                    return new IntVec3(0, 1, map.Center.z + cellVariation.RandomInRange);
                case Direction8Way.NorthWest:
                    return new IntVec3(0, 1, map.Center.z);
                default:
                    return new IntVec3(0, 1, 0);
            }
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref bomber, "bomber");
            Scribe_Values.Look(ref length, "length", defaultValue : 0);
            Scribe_Values.Look(ref startTimer, "startTimer", defaultValue : 0);
            Scribe_Values.Look(ref direction, "direction", defaultValue: new IntVec3());
        }


    }
}