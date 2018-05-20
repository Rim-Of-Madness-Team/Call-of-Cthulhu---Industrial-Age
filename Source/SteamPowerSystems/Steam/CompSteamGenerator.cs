using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArkhamEstate
{
    public enum BoilerSetting : int
    {
        Safe = 0,
        Acceptable = 1,
        Caution = 2,
        Dangerous = 3,
        SetToBlow = 4
    }
    
    public class CompSteamGenerator : CompSteamTrader
    {
        protected virtual float DesiredSteamOutput => -Props.baseSteamConsumption * (1 + (int)curBoilerSetting / 2);
        protected virtual float WaterInput => Props.baseWaterConsumption * (1 + (int)curBoilerSetting / 2);


        public void SetBoilerSettingFromPressure(PressureLevel pressure)
        {
            var newBoilerSetting = BoilerSetting.Safe;
            switch (pressure)
            {
                case PressureLevel.Off:
                    newBoilerSetting = BoilerSetting.Safe;
                    break;
                case PressureLevel.Nominal:
                    newBoilerSetting = BoilerSetting.Acceptable;
                    break;
                case PressureLevel.Caution:
                    newBoilerSetting = BoilerSetting.Caution;
                    break;
                case PressureLevel.Danger:
                    newBoilerSetting = BoilerSetting.Dangerous;
                    break;
                case PressureLevel.Maximum:
                    newBoilerSetting = BoilerSetting.SetToBlow;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pressure), pressure, null);
            }
            CurBoilerSetting = newBoilerSetting;
        }
        
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelableComp = parent.GetComp<CompRefuelable>();
            breakdownableComp = parent.GetComp<CompBreakdownable>();
            waterTankComp = parent.GetComp<CompWaterTank>();
            if ((Props.baseSteamConsumption < 0f || Props.baseWaterConsumption < 0f) && !parent.IsBrokenDown() && FlickUtility.WantsToBeOn(parent))
            {
                SteamOn = true;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            UpdateDesiredSteamOutput();
            SteamBurstEffect();
        }

        private void SteamBurstEffect()
        {
            if (waterTankComp is CompWaterTank waterTank)
            {
                int damageRate = 9999999;
                if (waterTank.LeakRate < 5f)
                    damageRate = 9999999;
                else if (waterTank.LeakRate < 10f)
                    damageRate = 2500;
                else if (waterTank.LeakRate < 20f)
                    damageRate = 1000;
                else if (waterTank.LeakRate < 99f)
                    damageRate = 750;
                else
                {
                    this.parent.TryGetComp<CompExplosive>()?.StartWick();
                }
                if (Find.TickManager.TicksGame % damageRate == 0)
                {
                    waterTank.AdjustLeakRate(1);
                    this.parent?.TakeDamage(new DamageInfo(DamageDefOf.Crush, Rand.Range(1, 3)));
                    MoteMaker.ThrowExplosionCell(this.parent.OccupiedRect().RandomCell, this.parent.MapHeld, ThingDefOf.Mote_Smoke, Color.white);
                }                
            }

        }

        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra();
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            return base.CompFloatMenuOptions(selPawn);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref curBoilerSetting, "curBoilerSetting", BoilerSetting.Safe);
        }

        public void UpdateDesiredSteamOutput()
        {
            if ((waterTankComp != null && !waterTankComp.CanDrawWater(WaterInput * LitersPerTick)) || 
                (breakdownableComp != null && breakdownableComp.BrokenDown) || 
                (refuelableComp != null && !refuelableComp.HasFuel) || 
                (flickableComp != null && !flickableComp.SwitchIsOn) || 
                !SteamOn)
            {
                SteamOutput = 0f;
            }
            else
            {
                SteamOutput = DesiredSteamOutput;
                waterTankComp?.DrawWater(WaterInput * LitersPerTick);
            }
        }

        protected CompWaterTank waterTankComp;
        
        protected CompRefuelable refuelableComp;

        protected CompBreakdownable breakdownableComp;

        public BoilerSetting CurBoilerSetting
        {
            get => curBoilerSetting;
            set
            {
                if (value != curBoilerSetting)
                {
                    curBoilerSetting = value;
                    if (waterTankComp != null)
                    {
                        switch (curBoilerSetting)
                        {
                            case BoilerSetting.Safe:
                            case BoilerSetting.Acceptable:
                                waterTankComp.LeakGainFactor = 1f;
                                break;
                            case BoilerSetting.Caution:
                                waterTankComp.LeakGainFactor = 0.05f;
                                break;
                            case BoilerSetting.Dangerous:
                                waterTankComp.LeakGainFactor = 0.01f;
                                break;
                            case BoilerSetting.SetToBlow:
                                waterTankComp.LeakGainFactor = 0.001f;
                                break;
                        }
                        //waterTankComp.LastRepairTick = -1;
                    }
                }
                
            }
        }

        private BoilerSetting curBoilerSetting = BoilerSetting.Safe;
        
        public static readonly float LitersPerTick = 0.00006f;
    }
}
