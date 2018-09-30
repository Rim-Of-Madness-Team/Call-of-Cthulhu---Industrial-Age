using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace IndustrialAge.Objects
{
    /*
     * 
     * Nandonalt's CompHeatPusherRefuelable
     * 
     */
    public class CompHeatPusherRefuelable : ThingComp
    {
        private const int HeatPushInterval = 60;

        public CompProperties_HeatPusher Props
        {
            get
            {
                return (CompProperties_HeatPusher)this.props;
            }
        }

        protected virtual bool ShouldPushHeatNow
        {
            get
            {
                CompRefuelable b = this.parent.GetComp<CompRefuelable>();
                CompFlickable f = this.parent.GetComp<CompFlickable>();

                if ((f != null && f.SwitchIsOn))
                {
                    if ((b != null && b.HasFuel))
                        return true;
                }
                return false;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.IsHashIntervalTick(60) && this.ShouldPushHeatNow)
            {
                CompProperties_HeatPusher props = this.Props;
                float temperature = this.parent.Position.GetTemperature(this.parent.Map);
                if (temperature < props.heatPushMaxTemperature && temperature > props.heatPushMinTemperature)
                {
                    GenTemperature.PushHeat(this.parent.Position, this.parent.Map, props.heatPerSecond);
                }
            }
        }
    }
}
