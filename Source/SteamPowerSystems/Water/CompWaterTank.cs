using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArkhamEstate
{
	public class CompWaterTank : CompWater, ILeakable, IVentable
	{
		public float AmountCanAccept
		{
			get
			{
				if (this.parent.IsBrokenDown())
				{
					return 0f;
				}
				CompProperties_WaterTank props = this.Props;
				return (props.storedWaterMax - this.storedWater) / props.efficiency;
			}
		}

		public float StoredWater
		{
			get
			{
				return this.storedWater;
			}
		}

		public float StoredWaterPct
		{
			get
			{
				return this.storedWater / this.Props.storedWaterMax;
			}
		}

		public new CompProperties_WaterTank Props
		{
			get
			{
				return (CompProperties_WaterTank)this.props;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<float>(ref this.storedWater, "storedWater", 0f, false);
			Scribe_Values.Look(ref this.leakRate, "leakRate", 1f);
			Scribe_Values.Look(ref this.lastRepairTick, "lastRepairTick", 1f);
			CompProperties_WaterTank props = this.Props;
			if (this.storedWater > props.storedWaterMax)
			{
				this.storedWater = props.storedWaterMax;
			}
		}

		public float LeakGainFactor = 1f;

		public override void CompTick()
		{
			base.CompTick();
			this.DrawWater(Mathf.Min(LeakRate * CompWater.LitersPerTick, this.storedWater));
			if (LastRepairTick + (GenDate.TicksPerDay * LeakGainFactor) < Find.TickManager.TicksGame)
			{
				LastRepairTick = Find.TickManager.TicksGame;
				LeakRate += 1f;
			}
		}

		public void AddWater(float amount)
		{
			if (amount < 0f)
			{
				Log.Error("Cannot add negative water " + amount);
				return;
			}
			if (amount > this.AmountCanAccept)
			{
				amount = this.AmountCanAccept;
			}
			amount *= this.Props.efficiency;
			this.storedWater += amount;
		}

		public bool CanDrawWater(float amount)
		{
			var tempResult = this.storedWater;
			tempResult -= amount;
			if (tempResult < 0f)
				return false;
			return true;
		}
		
		public void DrawWater(float amount)
		{
			this.storedWater -= amount;
			if (this.storedWater < 0f)
			{
				Log.Error("Drawing Water we don't have from " + this.parent);
				this.storedWater = 0f;
			}
		}

		public void SetStoredWaterPct(float pct)
		{
			pct = Mathf.Clamp01(pct);
			this.storedWater = this.Props.storedWaterMax * pct;
		}

		public override void ReceiveCompSignal(string signal)
		{
			if (signal == "Breakdown")
			{
				this.DrawWater(this.StoredWater);
			}
		}

		public override string CompInspectStringExtra()
		{
			CompProperties_WaterTank props = this.Props;
			string text = string.Concat(new string[]
			{
				"PowerBatteryStored".Translate(),
				": ",
				this.storedWater.ToString("F0"),
				" / ",
				props.storedWaterMax.ToString("F0"),
				" L"
			});
			string text2 = text;
			text = string.Concat(new string[]
			{
				text2,
				"\n",
				"PowerBatteryEfficiency".Translate(),
				": ",
				(props.efficiency * 100f).ToString("F0"),
				"%"
			});
			if (this.storedWater > 0f)
			{
				text2 = text;
				text = string.Concat(new string[]
				{
					text2,
					"\n",
					"Estate_WaterLeakRate".Translate(),
					": ",
					LeakRate.ToString("F0"),
					" L"
				});
			}
			return text + "\n" + base.CompInspectStringExtra();
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo c in base.CompGetGizmosExtra())
			{
				yield return c;
			}
			yield return new Command_Toggle
			{
				defaultLabel = "Estate_VentWater".Translate(),
				defaultDesc = "Estate_VentWaterDesc".Translate(storedWater.ToString("#####0") + " L"),
				icon = TexButton.Estate_VentWater,
				isActive = () => ShouldVentNow,
				toggleAction = () => shouldVentNow = !shouldVentNow,
				activateSound = SoundDefOf.TickTiny
			};			
			if (Prefs.DevMode)
			{
				yield return new Command_Action
				{
					defaultLabel = "DEBUG: Fill",
					action = delegate
					{
						SetStoredWaterPct(1f);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEBUG: Empty",
					action = delegate
					{
						SetStoredWaterPct(0f);
					}
				};
			}
			yield break;
		}

		private float storedWater;
		private float leakRate = 1f;
		private float lastRepairTick = -1f;

		public float LastRepairTick
		{
			get => lastRepairTick < 0f ? lastRepairTick = Find.TickManager.TicksGame : lastRepairTick;
			set => lastRepairTick = value;
		}

		public float LeakRate
		{
			get => leakRate;
			set => leakRate = value;
		}

		private const float SelfDischargingWatts = 5f;
		public float CurLeakRate()
		{
			return LeakRate;
		}

		public void AdjustLeakRate(float amt)
		{
			LeakRate += amt;
		}

		public bool ShouldVentNow
		{
			get => shouldVentNow;
			set => shouldVentNow = value;
		}

		private bool shouldVentNow = false;
		public void Vent()
		{
			//AddWaterTicks(Rand.Range(600, 800));
			foreach (var c in parent.OccupiedRect())
			{
				MoteMaker.ThrowAirPuffUp(c.ToVector3(), this.parent.Map);
				FilthMaker.MakeFilth(c, parent.MapHeld, ThingDefOf.FilthSlime);
			}
			SoundDef.Named("Estate_SteamHiss").PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
			SetStoredWaterPct(0f);
		}

	}
}
