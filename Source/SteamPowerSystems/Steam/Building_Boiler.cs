using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArkhamEstate
{
    [StaticConstructorOnStartup]
    public class Building_Boiler : Building
    {
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.ticksToExplode, "ticksToExplode", 0, false);
        }

        public override void Draw()
        {
            base.Draw();
            if (compWaterTank == null)
                compWaterTank = base.GetComp<CompWaterTank>();
            GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
            r.center = (this.DrawPos + Vector3.up * 0.1f) + compWaterTank.Props.indicatorOffset;
            r.size = new Vector2
            (Building_Boiler.WaterBarSize.x * compWaterTank.Props.indicatorDrawSize.x,
                Building_Boiler.WaterBarSize.y * compWaterTank.Props.indicatorDrawSize.y);
            r.fillPercent = compWaterTank.StoredWater / compWaterTank.Props.storedWaterMax;
            r.filledMat = Building_Boiler.WaterFilledMat;
            r.unfilledMat = Building_Boiler.WaterUnfilledMat;
            r.margin = 0.15f;
            Rot4 rotation = base.Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            r.rotation = rotation;
            GenDraw.DrawFillableBar(r);
            if (this.ticksToExplode > 0 && base.Spawned)
            {
                base.Map.overlayDrawer.DrawOverlay(this, OverlayTypes.BurningWick);
            }
        }
       
        private static readonly Vector2 WaterBarSize = new Vector2(1.3f, 0.4f);
        private static readonly Material WaterFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0.8f, 1f), false);
        private static readonly Material WaterUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);

        private static readonly Texture2D SetTargetFuelLevelCommand =
            ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel", true);
        private static readonly Texture2D SteamWhistle =
            ContentFinder<Texture2D>.Get("UI/Commands/Estate_SteamWhistle", true);
        private static readonly Texture2D DischargeFuel =
            ContentFinder<Texture2D>.Get("UI/Commands/Estate_DischargeFuel", true);

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos()) yield return g;

            if (compRefuelable != null)
            {
                yield return new ArkhamEstate.Command_SetTargetFuelLevel()
                {
                    refuelable = compRefuelable,
                    defaultLabel = "CommandSetTargetFuelLevel".Translate(),
                    defaultDesc = "CommandSetTargetFuelLevelDesc".Translate(),
                    icon = Building_Boiler.SetTargetFuelLevelCommand
                };

                yield return new Command_Action
                {
                    defaultLabel = "Estate_EmptyFuelContents".Translate(),
                    defaultDesc = "Estate_EmptyFuelContentsDesc".Translate(),
                    icon = DischargeFuel,
                    disabled = (this?.CompSteam?.TransmitsSteamNow ?? false) == false,
                    action = EmptyBoilerContents
                };
                yield return new Command_Action
                {
                    defaultLabel = "Estate_BlowSteamWhistle".Translate(),
                    defaultDesc = "Estate_BlowSteamWhistleDesc".Translate(),
                    icon = SteamWhistle,
                    action = BlowSteamWhistle,
                    disabled = (this?.CompSteam?.TransmitsSteamNow ?? false) == false,
                    disabledReason = "Estate_NoSteamAvailable".Translate()
                };
                yield return new Command_Action
                {
                    defaultLabel = curPressureLevel.GetString(),
                    icon = GetCurPressureIcon(),
                    action = null
                };
            }
        }

        private void EmptyBoilerContents()
        {
            if (!this.compRefuelable.HasFuel)
            {
                Messages.Message("Estate_EmptyFuelContentsNoFuel".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            ThingDef thingDef = ThingDef.Named("Estate_Coal");
            float num = 1f;
            var curFuel = this.compRefuelable.Fuel;
            this.compRefuelable.Refuel(-this.compRefuelable.Fuel);
            int i = GenMath.RoundRandom(num * curFuel);
            IntVec3 loc = this.OccupiedRect().AdjacentCellsCardinal.RandomElement();
            while (i > 0)
            {
                Thing thing = ThingMaker.MakeThing(thingDef, null);
                thing.stackCount = Mathf.Min(i, thingDef.stackLimit);
                i -= thing.stackCount;
                loc = this.OccupiedRect().AdjacentCellsCardinal.RandomElement();
                GenPlace.TryPlaceThing(thing, loc, this.MapHeld, ThingPlaceMode.Near, null);
            }
            if (CompSteam != null && CompSteam.TransmitsSteamNow)
                //for (int i = 0; i < Rand.Range(1, 2); i++)
                    FireUtility.TryStartFireIn(loc, MapHeld, Rand.Range(0.33f, 0.55f));
        }

        private void BlowSteamWhistle()
        {
            MoteMaker.ThrowAirPuffUp(this.TrueCenter() + CompSteam.Props.steamOffset, this.MapHeld);
            SoundDef.Named("Estate_SteamPressureWhistle").PlayOneShot(this);
            compWaterTank.DrawWater(1);
        }

        private static readonly Texture2D PressureOff =
            ContentFinder<Texture2D>.Get("UI/Commands/PressureOff", true);
        private static readonly Texture2D PressureNominal =
            ContentFinder<Texture2D>.Get("UI/Commands/PressureNominal", true);
        private static readonly Texture2D PressureCaution =
            ContentFinder<Texture2D>.Get("UI/Commands/PressureCaution", true);
        private static readonly Texture2D PressureDanger =
            ContentFinder<Texture2D>.Get("UI/Commands/PressureDanger", true);
        private static readonly Texture2D PressureMaximum =
            ContentFinder<Texture2D>.Get("UI/Commands/PressureMaximum", true);
        Texture2D GetCurPressureIcon()
        {
            var result = PressureOff;
            switch (curPressureLevel)
            {
                case PressureLevel.Off:
                    break;
                case PressureLevel.Nominal:
                    result = PressureNominal;
                    break;
                case PressureLevel.Caution:
                    result = PressureCaution;
                    break;
                case PressureLevel.Danger:
                    result = PressureDanger;
                    break;
                case PressureLevel.Maximum:
                    result = PressureMaximum;
                    break;
            }
            return result;
        }
        
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            compRefuelable = this.GetComp<CompRefuelable>();
            CompSteam = this.GetComp<CompSteam>();
            compWaterTank = base.GetComp<CompWaterTank>();
        }

        private PressureLevel curPressureLevel = PressureLevel.Off;

        public PressureLevel CurPressureLevel
        {
            get => curPressureLevel;
            set
            {
                if (curPressureLevel != value)
                {
                    if (value == PressureLevel.Maximum)
                        SoundDef.Named("Estate_SteamPressureWhistle").PlayOneShot(this);
                    else if (curPressureLevel < value)
                        SoundDef.Named("Estate_SteamPressureUp").PlayOneShot(this);
                    else if (curPressureLevel > value)
                        SoundDef.Named("Estate_SteamPressureDown").PlayOneShot(this);
                    if (this.TryGetComp<CompSteamGenerator>() is CompSteamGenerator steamGen)
                    {
                        steamGen.SetBoilerSettingFromPressure(curPressureLevel);
                    }
                }
                curPressureLevel = value;
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 100 == 0)
            {
                if (CompSteam != null && compRefuelable != null && compRefuelable.FuelPercentOfMax is float curFuelLevel)
                {
                    CurPressureLevel = SteamUtility.GetCurPressureLevel(this.CompSteam, curFuelLevel);
                }
            }
            if (this.ticksToExplode > 0)
            {
                if (this.wickSustainer == null)
                {
                    this.StartWickSustainer();
                }
                else
                {
                    this.wickSustainer.Maintain();
                }
                this.ticksToExplode--;
                if (this.ticksToExplode == 0)
                {
                    IntVec3 randomCell = this.OccupiedRect().RandomCell;
                    float radius = Rand.Range(0.5f, 1f) * 3f;
                    GenExplosion.DoExplosion(randomCell, base.Map, radius, DamageDefOf.Flame, null, -1, null, null,
                        null, null, 0f, 1, false, null, 0f, 1, 0f, false);
                    base.GetComp<CompSteamTank>().DrawSteam(400f);
                }
            }
        }

        public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostApplyDamage(dinfo, totalDamageDealt);
            if (!base.Destroyed && this.ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < 0.05f &&
                base.GetComp<CompSteamTank>().StoredSteam > 500f)
            {
                this.ticksToExplode = Rand.Range(70, 150);
                this.StartWickSustainer();
            }
        }

        private void StartWickSustainer()
        {
            SoundInfo info = SoundInfo.InMap(this, MaintenanceType.PerTick);
            this.wickSustainer = SoundDefOf.HissSmall.TrySpawnSustainer(info);
        }

        private int ticksToExplode;

        private Sustainer wickSustainer;

        private static readonly Vector2 BarSize = new Vector2(1.3f, 0.4f);

        private const float MinSteamToExplode = 500f;

        private const float SteamToLoseWhenExplode = 400f;

        private const float ExplodeChancePerDamage = 0.05f;

        private static readonly Material BatteryBarFilledMat =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f), false);

        private static readonly Material BatteryBarUnfilledMat =
            SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);

        private CompRefuelable compRefuelable;
        private CompWaterTank compWaterTank;
        public CompSteam CompSteam { get; private set; }
    }

}