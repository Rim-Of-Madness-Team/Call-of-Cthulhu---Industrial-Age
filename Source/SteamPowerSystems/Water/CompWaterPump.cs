using RimWorld;
using CompRefuelable = ArkhamEstate.SteamPowerSystems.Steam.CompRefuelable;

namespace ArkhamEstate
{
    public class CompWaterPlant : CompWaterTrader
    {
        protected virtual float DesiredWaterOutput => -Props.baseWaterConsumption;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelableComp = parent.GetComp<CompRefuelable>();
            breakdownableComp = parent.GetComp<CompBreakdownable>();
            if (Props.baseWaterConsumption < 0f && !parent.IsBrokenDown() && FlickUtility.WantsToBeOn(parent))
            {
                WaterOn = true;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            UpdateDesiredWaterOutput();
        }

        public void UpdateDesiredWaterOutput()
        {
            if ((breakdownableComp != null && breakdownableComp.BrokenDown) || (refuelableComp != null && !refuelableComp.HasFuel) || (flickableComp != null && !flickableComp.SwitchIsOn) || !WaterOn)
            {
                WaterOutput = 0f;
            }
            else
            {
                WaterOutput = DesiredWaterOutput;
            }
        }

        protected CompRefuelable refuelableComp;

        protected CompBreakdownable breakdownableComp;
    }
}
