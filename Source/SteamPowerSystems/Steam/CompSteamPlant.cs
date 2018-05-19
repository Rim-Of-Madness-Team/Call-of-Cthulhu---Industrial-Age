using RimWorld;
namespace ArkhamEstate
{
    public class CompSteamPlant : CompSteamTrader
    {
        protected virtual float DesiredSteamOutput => -Props.baseSteamConsumption;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelableComp = parent.GetComp<CompRefuelable>();
            breakdownableComp = parent.GetComp<CompBreakdownable>();
            if (Props.baseSteamConsumption < 0f && !parent.IsBrokenDown() && FlickUtility.WantsToBeOn(parent))
            {
                SteamOn = true;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            UpdateDesiredSteamOutput();
        }

        public void UpdateDesiredSteamOutput()
        {
            if ((breakdownableComp != null && breakdownableComp.BrokenDown) || (refuelableComp != null && !refuelableComp.HasFuel) || (flickableComp != null && !flickableComp.SwitchIsOn) || !SteamOn)
            {
                SteamOutput = 0f;
            }
            else
            {
                SteamOutput = DesiredSteamOutput;
            }
        }

        protected CompRefuelable refuelableComp;

        protected CompBreakdownable breakdownableComp;
    }
}
