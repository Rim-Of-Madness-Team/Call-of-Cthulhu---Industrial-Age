using System;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ArkhamEstate
{
	public class CompWaterTrader : CompWater
	{
		public float WaterOutput
		{
			get
			{
				return this.waterOutputInt;
			}
			set
			{
				this.waterOutputInt = value;
				if (this.waterOutputInt > 0f)
				{
					this.waterLastOutputted = true;
				}
				if (this.waterOutputInt < 0f)
				{
					this.waterLastOutputted = false;
				}
			}
		}

		public float WaterOutputPerTick
		{
			get
			{
				return this.WaterOutput * CompWater.LitersPerTick;
			}
		}

		public bool WaterOn
		{
			get
			{
				return this.waterOnInt;
			}
			set
			{
				if (this.waterOnInt == value)
				{
					return;
				}
				this.waterOnInt = value;
				if (this.waterOnInt)
				{
					if (!FlickUtility.WantsToBeOn(this.parent))
					{
						Log.Warning("Tried to water on " + this.parent + " which did not desire it.");
						return;
					}
					if (this.parent.IsBrokenDown())
					{
						Log.Warning("Tried to water on " + this.parent + " which is broken down.");
						return;
					}
					if (this.waterStartedAction != null)
					{
						this.waterStartedAction();
					}
					this.parent.BroadcastCompSignal("WaterTurnedOn");
					SoundDef soundDef = ((CompProperties_Water)this.parent.def.CompDefForAssignableFrom<CompWaterTrader>()).soundWaterOn;
					if (soundDef.NullOrUndefined())
					{
						soundDef = SoundDef.Named("Estate_FlipSwitch");
					}
					soundDef.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
					this.StartSustainerWateredIfInactive();
				}
				else
				{
					if (this.waterStoppedAction != null)
					{
						this.waterStoppedAction();
					}
					this.parent.BroadcastCompSignal("WaterTurnedOff");
					SoundDef soundDef2 = ((CompProperties_Water)this.parent.def.CompDefForAssignableFrom<CompWaterTrader>()).soundWaterOff;
					if (soundDef2.NullOrUndefined())
					{
						soundDef2 = SoundDef.Named("Estate_FlipSwitch");
					}
					if (this.parent.Spawned)
					{
						soundDef2.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
					}
					this.EndSustainerWateredIfActive();
				}
			}
		}

		public string DebugString
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(this.parent.LabelCap + " CompWater:");
				stringBuilder.AppendLine("   WaterOn: " + this.WaterOn);
				stringBuilder.AppendLine("   waterProduction: " + this.WaterOutput);
				return stringBuilder.ToString();
			}
		}

		public override void ReceiveCompSignal(string signal)
		{
			if (signal == "FlickedOff" || signal == "ScheduledOff" || signal == "Breakdown")
			{
				this.WaterOn = false;
			}
			if (signal == "RanOutOfFuel" && this.waterLastOutputted)
			{
				this.WaterOn = false;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			this.flickableComp = this.parent.GetComp<CompFlickable>();
		}

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			this.EndSustainerWateredIfActive();
			this.waterOutputInt = 0f;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref this.waterOnInt, "waterOn", true, false);
		}

		public override void PostDraw()
		{
			base.PostDraw();
			if (!this.parent.IsBrokenDown())
			{
				if (this.flickableComp != null && !this.flickableComp.SwitchIsOn)
				{
					this.parent.Map.overlayDrawer.DrawOverlay(this.parent, OverlayTypes.PowerOff); //TODO
				}
				else if (FlickUtility.WantsToBeOn(this.parent))
				{
					if (!this.WaterOn)
					{
						this.parent.Map.overlayDrawer.DrawOverlay(this.parent, OverlayTypes.NeedsPower); //TODO
					}
				}
			}
		}

		public override void SetUpWaterVars()
		{
			base.SetUpWaterVars();
			CompProperties_Water props = base.Props;
			this.WaterOutput = -1f * props.baseWaterConsumption;
			this.waterLastOutputted = (props.baseWaterConsumption <= 0f);
		}

		public override void ResetWaterVars()
		{
			base.ResetWaterVars();
			this.waterOnInt = false;
			this.waterOutputInt = 0f;
			this.waterLastOutputted = false;
			this.sustainerWatered = null;
			if (this.flickableComp != null)
			{
				this.flickableComp.ResetToOn();
			}
		}

		protected override void LostConnectParent()
		{
			base.LostConnectParent();
			this.WaterOn = false;
		}

		public override string CompInspectStringExtra()
		{
			string str;
			if (this.waterLastOutputted)
			{
				str = "Estate_PumpOutput".Translate() + ": " + this.WaterOutput.ToString("#####0") + " L";
			}
			else
			{
				str = "Estate_WaterNeeded".Translate() + ": " + (-this.WaterOutput).ToString("#####0") + " L";
			}
			return str + "\n" + base.CompInspectStringExtra();
		}

		private void StartSustainerWateredIfInactive()
		{
			CompProperties_Water props = base.Props;
			if (!props.soundAmbientWatered.NullOrUndefined() && this.sustainerWatered == null)
			{
				SoundInfo info = SoundInfo.InMap(this.parent, MaintenanceType.None);
				this.sustainerWatered = props.soundAmbientWatered.TrySpawnSustainer(info);
			}
		}

		private void EndSustainerWateredIfActive()
		{
			if (this.sustainerWatered != null)
			{
				this.sustainerWatered.End();
				this.sustainerWatered = null;
			}
		}

		public Action waterStartedAction;

		public Action waterStoppedAction;

		private bool waterOnInt;

		public float waterOutputInt;

		private bool waterLastOutputted;

		private Sustainer sustainerWatered;

		protected CompFlickable flickableComp;

		public const string WaterTurnedOnSignal = "WaterTurnedOn";

		public const string WaterTurnedOffSignal = "WaterTurnedOff";
	}
}
