using RimWorld;

namespace ArkhamEstate
{
    public class CompSteamElectricPlant : CompPowerTrader
    {
        public CompProperties_SteamTank SteamProps => this.parent?.GetComp<CompSteamTank>()?.Props;

        protected virtual float DesiredPowerOutput
        {
            get
            {
                //if (SteamProps.baseSteamConsumption < 0f)
                //{
                //    return -SteamProps.baseSteamConsumption;
                //}
                return -Props.basePowerConsumption;
            }
        }


        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelableComp = parent.GetComp<CompRefuelable>();
            breakdownableComp = parent.GetComp<CompBreakdownable>();
            steamTankComp = parent.GetComp<CompSteamTank>();
            if ((Props.basePowerConsumption < 0f || SteamProps.baseSteamConsumption < 0f) && !parent.IsBrokenDown() &&
                FlickUtility.WantsToBeOn(parent))
            {
                PowerOn = true;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            UpdateDesiredSteamOutput();
        }

        public void UpdateDesiredSteamOutput()
        {
            if ((steamTankComp != null && !steamTankComp.CanDrawSteam(SteamProps.baseSteamConsumption)) ||
                (breakdownableComp != null && breakdownableComp.BrokenDown) ||
                (refuelableComp != null && !refuelableComp.HasFuel) ||
                (flickableComp != null && !flickableComp.SwitchIsOn) ||
                !PowerOn)
            {
                PowerOutput = 0f;
            }
            else
            {
                PowerOutput = DesiredPowerOutput;
                steamTankComp?.DrawSteam(SteamProps.baseSteamConsumption * LitersPerTick);
            }
        }

        private CompSteamTank steamTankComp;

        private CompRefuelable refuelableComp;

        private CompBreakdownable breakdownableComp;

        private static readonly float LitersPerTick = 0.01f;
    }
}