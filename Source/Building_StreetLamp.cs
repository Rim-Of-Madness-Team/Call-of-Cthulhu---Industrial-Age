using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ArkhamEstate
{
    class Building_StreetLamp : Building
    {
        private CompBreakdownable compBreakdownable = null;
        private ThingWithComps_Glower glower;
        private ThingDef glowerDef = ThingDef.Named("Jecrell_GasLampGlower");

        private void SpawnGlower()
        {
            Thing thing = ThingMaker.MakeThing(glowerDef, null);
            IntVec3 position = this.Position + GenAdj.CardinalDirections[0]
                                             + GenAdj.CardinalDirections[0];
            GenPlace.TryPlaceThing(thing, position, this.Map, ThingPlaceMode.Near);
            glower = thing as ThingWithComps_Glower;
            glower.master = this;
        }

        private void DespawnGlower()
        {
            glower.master = null;
            glower.DeSpawn();
            glower = null;
        }

        private void ResolveGlower()
        {
            if (compBreakdownable != null)
            {
                if (compBreakdownable.BrokenDown)
                {
                    if (glower != null) DespawnGlower();
                    return;
                }
                if (glower == null)
                {
                    SpawnGlower();
                    return;
                }
            }
        }

        public override void SpawnSetup(Map map)
        {
            base.SpawnSetup(map);
            compBreakdownable = this.TryGetComp<CompBreakdownable>();
        }

        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(60))
            {
                ResolveGlower();
            }
        }

        public override void DeSpawn()
        {
            base.DeSpawn();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.LookReference<ThingWithComps_Glower>(ref this.glower, "glower", false);
        }
    }
}
