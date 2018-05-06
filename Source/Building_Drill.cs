using System.Linq;
using RimWorld;
using Verse;

namespace ArkhamEstate
{
    public class Building_Drill : Building_WorkTable_HeatPush
    {
        private const int TicksUntilDamage = 2200;

        private Thing Vein => PositionHeld.GetThingList(MapHeld).FirstOrDefault(x =>
            x.def.defName == "Estate_VeinCoal" || x.def.defName == "Estate_VeinCopper");

        private bool IsEfficient => def.defName.Contains("Advanced");
        
        private int DamageRate => IsEfficient ? 10 : 30;

        private int FilthRate => IsEfficient ? 500 : 120;
        
        private ThingDef FilthDef => Vein.def.defName == "Estate_VeinCoal" ? ThingDefOf.FilthAsh : ThingDefOf.FilthDirt;
        
        private int lastStartedTick = -1;
        private int lastUsedTick = -1;
        private Effecter effecter = null;

        public override bool UsableNow => !Vein?.DestroyedOrNull() ?? false;

        public override void Tick()
        {
            if (lastUsedTick + 200 <= Find.TickManager.TicksGame)
            {
                if (effecter != null)
                {
                    effecter.Cleanup();
                    effecter = null;
                }
            }
            base.Tick();
        }

        public override void UsedThisTick()
        {
            base.UsedThisTick();
            lastUsedTick = Find.TickManager.TicksGame;
            if (effecter == null)
            {
                EffecterDef effecterDef = EffecterDef.Named("Drill");
                if (effecterDef == null)
                {
                    return;
                }
                effecter = effecterDef.Spawn();
            }
            else
            {
                effecter.EffectTick(this, this);
            }
            
            if (Find.TickManager.TicksGame % FilthRate == 0)
            {
                FilthMaker.MakeFilth(this.RandomAdjacentCell8Way(), MapHeld, FilthDef);
            }
            if (lastStartedTick + TicksUntilDamage < Find.TickManager.TicksGame)
            {
                lastStartedTick = Find.TickManager.TicksGame;
                Vein.TakeDamage(new DamageInfo(DamageDefOf.Crush, DamageRate));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastStartedTick, "lastStartedTick", -1);
            Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", -1);
        }
    }
}