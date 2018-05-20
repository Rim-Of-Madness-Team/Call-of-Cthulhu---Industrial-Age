using System.Collections.Generic;
using System.Linq;
using RimWorld;

namespace ArkhamEstate
{
    public class CompSteamElectricPlant : CompPowerTrader
    {
        public CompProperties_SteamTank SteamProps => this.parent?.GetComp<CompSteamTank>()?.Props;

        public CompSteamTank SteamTankComp => steamTankComp;

        protected virtual float DesiredPowerOutput
        {
            get
            {
                //if (SteamProps.baseSteamConsumption < 0f)
                //{
                //    return -SteamProps.baseSteamConsumption;
                //})
                var result = -Props.basePowerConsumption;
                var modifier = 1f;
                if (steamTankComp != null && steamTankComp?.SteamNet?.GetSteamBoilers != null)
                {
                    HashSet<Building_Boiler> boilers =
                        new HashSet<Building_Boiler>(steamTankComp?.SteamNet?.GetSteamBoilers);
                    if (boilers.Any())
                    {
                        foreach (var boiler in boilers)
                        {
                            var boilerPressure = ((int)boiler.CurPressureLevel) * 0.5f;
                            modifier += boilerPressure;
                        }
                    }
                }
                return result * modifier;
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
            if ((steamTankComp != null && !steamTankComp.CanDrawSteam(SteamProps.baseSteamConsumption * LitersPerTick)) ||
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

        private static readonly float LitersPerTick = 0.0001f;
    }
}