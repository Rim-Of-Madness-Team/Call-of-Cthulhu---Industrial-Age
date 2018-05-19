﻿using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArkhamEstate
{
	[StaticConstructorOnStartup]
	public class Building_WaterTank : Building
	{
		public CompProperties_WaterTank Props => this.GetComp<CompWaterTank>().Props;
		
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<int>(ref this.ticksToExplode, "ticksToExplode", 0, false);
		}

		public override void Draw()
		{
			base.Draw();
			CompWaterTank comp = base.GetComp<CompWaterTank>();
			GenDraw.FillableBarRequest r = default(GenDraw.FillableBarRequest);
			r.center = (this.DrawPos + Vector3.up * 0.1f) + Props.indicatorOffset;
			r.size = new Vector2
				(Building_WaterTank.BarSize.x * Props.indicatorDrawSize.x,
				Building_WaterTank.BarSize.y * Props.indicatorDrawSize.y);
			r.fillPercent = comp.StoredWater / comp.Props.storedWaterMax;
			r.filledMat = Building_WaterTank.BatteryBarFilledMat;
			r.unfilledMat = Building_WaterTank.BatteryBarUnfilledMat;
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

		public override void Tick()
		{
			base.Tick();
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
					GenExplosion.DoExplosion(randomCell, base.Map, radius, DamageDefOf.Flame, null, -1, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
					base.GetComp<CompWaterTank>().DrawWater(400f);
				}
			}
		}

		public override void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
			base.PostApplyDamage(dinfo, totalDamageDealt);
			if (!base.Destroyed && this.ticksToExplode == 0 && dinfo.Def == DamageDefOf.Flame && Rand.Value < 0.05f && base.GetComp<CompWaterTank>().StoredWater > 500f)
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

		private const float MinWaterToExplode = 500f;

		private const float WaterToLoseWhenExplode = 400f;

		private const float ExplodeChancePerDamage = 0.05f;

		private static readonly Material BatteryBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0.8f, 1f), false);

		private static readonly Material BatteryBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f), false);
	}
}
