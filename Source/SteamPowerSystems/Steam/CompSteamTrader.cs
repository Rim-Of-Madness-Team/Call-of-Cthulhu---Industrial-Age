using System;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;

namespace ArkhamEstate
{
	public class CompSteamTrader : CompSteam
	{
		public float SteamOutput
		{
			get
			{
				return this.steamOutputInt;
			}
			set
			{
				this.steamOutputInt = value;
				if (this.steamOutputInt > 0f)
				{
					this.steamLastOutputted = true;
				}
				if (this.steamOutputInt < 0f)
				{
					this.steamLastOutputted = false;
				}
			}
		}

		public float SteamOutputPerTick
		{
			get
			{
				return this.SteamOutput * CompSteam.PsiPerTick;
			}
		}

		public bool SteamOn
		{
			get
			{
				return this.steamOnInt;
			}
			set
			{
				if (this.steamOnInt == value)
				{
					return;
				}
				this.steamOnInt = value;
				if (this.steamOnInt)
				{
					if (!FlickUtility.WantsToBeOn(this.parent))
					{
						Log.Warning("Tried to steam on " + this.parent + " which did not desire it.");
						return;
					}
					if (this.parent.IsBrokenDown())
					{
						Log.Warning("Tried to steam on " + this.parent + " which is broken down.");
						return;
					}
					if (this.steamStartedAction != null)
					{
						this.steamStartedAction();
					}
					this.parent.BroadcastCompSignal("SteamTurnedOn");
					SoundDef soundDef = ((CompProperties_Steam)this.parent.def.CompDefForAssignableFrom<CompSteamTrader>()).soundSteamOn;
					if (soundDef.NullOrUndefined())
					{
						soundDef = SoundDef.Named("Estate_FlipSwitch");
					}
					soundDef.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
					this.StartSustainerSteamedIfInactive();
				}
				else
				{
					if (this.steamStoppedAction != null)
					{
						this.steamStoppedAction();
					}
					this.parent.BroadcastCompSignal("SteamTurnedOff");
					SoundDef soundDef2 = ((CompProperties_Steam)this.parent.def.CompDefForAssignableFrom<CompSteamTrader>()).soundSteamOff;
					if (soundDef2.NullOrUndefined())
					{
						soundDef2 = SoundDef.Named("Estate_FlipSwitch");
					}
					if (this.parent.Spawned)
					{
						soundDef2.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
					}
					this.EndSustainerSteamedIfActive();
				}
			}
		}

		public string DebugString
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine(this.parent.LabelCap + " CompSteam:");
				stringBuilder.AppendLine("   SteamOn: " + this.SteamOn);
				stringBuilder.AppendLine("   steamProduction: " + this.SteamOutput);
				return stringBuilder.ToString();
			}
		}

		public override void ReceiveCompSignal(string signal)
		{
			if (signal == "FlickedOff" || signal == "ScheduledOff" || signal == "Breakdown")
			{
				this.SteamOn = false;
			}
			if (signal == "RanOutOfFuel" && this.steamLastOutputted)
			{
				this.SteamOn = false;
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
			this.EndSustainerSteamedIfActive();
			this.steamOutputInt = 0f;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref this.steamOnInt, "steamOn", true, false);
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
					if (!this.SteamOn)
					{
						this.parent.Map.overlayDrawer.DrawOverlay(this.parent, OverlayTypes.NeedsPower); //TODO
					}
				}
			}
		}

		public override void SetUpSteamVars()
		{
			base.SetUpSteamVars();
			CompProperties_Steam props = base.Props;
			this.SteamOutput = props.baseWaterConsumption > 0f ? -1f * props.baseWaterConsumption : -1f * props.baseSteamConsumption;
			this.steamLastOutputted = props.baseWaterConsumption > 0f ? (props.baseWaterConsumption <= 0f) : (props.baseSteamConsumption <= 0f);
		}

		public override void ResetSteamVars()
		{
			base.ResetSteamVars();
			this.steamOnInt = false;
			this.steamOutputInt = 0f;
			this.steamLastOutputted = false;
			this.sustainerSteamed = null;
			if (this.flickableComp != null)
			{
				this.flickableComp.ResetToOn();
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			//this.SteamNet?.AdjustToxicGas(Props.toxicGasVentRate);
		}

		protected override void LostConnectParent()
		{
			base.LostConnectParent();
			this.SteamOn = false;
		}

		public override string CompInspectStringExtra()
		{
			string str;
			if (this.steamLastOutputted)
			{
				str = "Estate_SteamOutput".Translate() + ": " + this.SteamOutput.ToString("#####0") + " Psi";
			}
			else
			{
				str = "Estate_SteamNeeded".Translate() + ": " + (-this.SteamOutput).ToString("#####0") + " Psi";
			}
			return str + "\n" + base.CompInspectStringExtra();
		}

		private void StartSustainerSteamedIfInactive()
		{
			CompProperties_Steam props = base.Props;
			if (!props.soundAmbientSteamed.NullOrUndefined() && this.sustainerSteamed == null)
			{
				SoundInfo info = SoundInfo.InMap(this.parent, MaintenanceType.None);
				this.sustainerSteamed = props.soundAmbientSteamed.TrySpawnSustainer(info);
			}
		}

		private void EndSustainerSteamedIfActive()
		{
			if (this.sustainerSteamed != null)
			{
				this.sustainerSteamed.End();
				this.sustainerSteamed = null;
			}
		}

		public Action steamStartedAction;

		public Action steamStoppedAction;

		private bool steamOnInt;

		public float steamOutputInt;

		private bool steamLastOutputted;

		private Sustainer sustainerSteamed;

		protected CompFlickable flickableComp;

		public const string SteamTurnedOnSignal = "SteamTurnedOn";

		public const string SteamTurnedOffSignal = "SteamTurnedOff";
	}
}
