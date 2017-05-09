using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;
using RimWorld;

namespace ArkhamEstate
{
    public class Building_Refrigerator : Building_Storage
    {
        public CompPowerTrader powerComp;

        public float Temp = 0f;

        public override void SpawnSetup(Map map, bool bla)
        {
            base.SpawnSetup(map, bla);
            this.powerComp = base.GetComp<CompPowerTrader>();
        }

        public override void TickRare()
        {
			base.TickRare();
            if (this.powerComp != null && !this.powerComp.PowerOn)
            {
                return;
            }
            foreach (var thing in Position.GetThingList(Map))
            {
                var rottable = thing.TryGetComp<CompRottable>();
                if (rottable != null && !(rottable is CompBetterRottable))
                {
                    var li = thing as ThingWithComps;
                    var newRot = new CompBetterRottable();
                    li.AllComps.Remove(rottable);
                    li.AllComps.Add(newRot);
                    newRot.props = rottable.props;
                    newRot.parent = li;
                    newRot.RotProgress = rottable.RotProgress;
                }
            }
        }
        
    }
}
