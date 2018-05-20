using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArkhamEstate
{
	public class CompSteamTank : CompSteam, ILeakable, IVentable
	{
		public float AmountCanAccept
		{
			get
			{
				if (parent.IsBrokenDown())
				{
					return 0f;
				}
				CompProperties_SteamTank props = Props;
				return (props.storedSteamMax - storedSteam) / props.efficiency;
			}
		}
		
		public float StoredSteam => storedSteam;
		public float StoredToxicGas
		{
			get { return storedToxicGas; }
			set { storedSteam = value; }
		}

		public float StoredSteamPct => (storedSteam + storedToxicGas) / Props.storedSteamMax;
		public new CompProperties_SteamTank Props => (CompProperties_SteamTank)props;

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<float>(ref storedSteam, "storedSteam", 0f, false);
			Scribe_Values.Look<float>(ref storedToxicGas, "storedToxicGas", 0f, false);
			Scribe_Values.Look(ref leakRate, "leakRate", 1f);
			Scribe_Values.Look(ref lastRepairTick, "lastRepairTick", -1f);
			Scribe_Values.Look(ref lastToxicTick, "lastToxicTick", -1);
			CompProperties_SteamTank props = Props;
			if (storedSteam > props.storedSteamMax)
			{
				storedSteam = props.storedSteamMax;
			}
		}

		public override void CompTick()
		{
			base.CompTick();
			DrawSteam(Mathf.Min(LeakRate * PsiPerTick, storedSteam));
			
			if (LastRepairTick + GenDate.TicksPerDay < Find.TickManager.TicksGame)
			{
				LastRepairTick = Find.TickManager.TicksGame;
				LeakRate += 1f;
			}
			if (LastToxicTick + (GenDate.TicksPerDay / 2) < Find.TickManager.TicksGame)
			{
				LastToxicTick = Find.TickManager.TicksGame;
				StoredToxicGas += Rand.Range(0, 3);
			}
		}

		public int LastToxicTick { get => lastToxicTick; set => lastToxicTick = value; }

		public bool CanDrawSteam(float amount)
		{
			var tempResult = this.storedSteam;
			tempResult -= amount;
			if (tempResult < 0f)
				return false;
			return true;
		}

		public void AddSteam(float amount)
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
			this.storedSteam += amount;
		}

		public void DrawSteam(float amount)
		{
			storedSteam -= amount;
			if (storedSteam < 0f)
			{
				Log.Error("Drawing Steam we don't have from " + parent);
				storedSteam = 0f;
			}
		}
		
		public void SetStoredSteamPct(float pct)
		{
			pct = Mathf.Clamp01(pct);
			storedSteam = Props.storedSteamMax * pct;
			storedToxicGas = 0f;
		}

		public override void ReceiveCompSignal(string signal)
		{
			if (signal == "Breakdown")
			{
				DrawSteam(StoredSteam);
				VentToxicGas(StoredToxicGas);	
			}
		}

		public void VentToxicGas(float amt)
		{
			storedToxicGas -= amt;
			if (storedToxicGas < 0f)
			{
				storedToxicGas = 0f;
			}
		}

		public override string CompInspectStringExtra()
		{
			CompProperties_SteamTank props = Props;
			string text = string.Concat(new string[]
			{
				"PowerBatteryStored".Translate(),
				": ",
				storedSteam.ToString("F0"),
				" / ",
				props.storedSteamMax.ToString("F0"),
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
			if (storedSteam > 0f)
			{
				text2 = text;
				text = string.Concat(new string[]
				{
					text2,
					"\n",
					"Estate_SteamLeakRate".Translate(),
					": ",
					LeakRate.ToString("F0"),
					"%"
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
				defaultLabel = "Estate_VentSteam".Translate(),
				defaultDesc = "Estate_VentSteamDesc".Translate(storedSteam.ToString("#####0") + " Psi") + (StoredToxicGas > 1f ? "\n" + "Estate_VentToxicGasWarnDesc".Translate(storedToxicGas.ToString("#####0") + " Psi") : ""),
				icon = TexButton.Estate_VentSteam,
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
						SetStoredSteamPct(1f);
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "DEBUG: Empty",
					action = delegate
					{
						SetStoredSteamPct(0f);
					}
				};
			}
			yield break;
		}

		public bool ShouldVentNow
		{
			get => shouldVentNow;
			set => shouldVentNow = value;
		}

		private bool shouldVentNow = false;
		public void Vent()
		{
			AddSteamTicks(Rand.Range(600, 800));
			foreach (var c in parent.OccupiedRect())
			{
				MoteMaker.ThrowAirPuffUp(c.ToVector3(), this.parent.Map);	
			}
			SoundDef.Named("Estate_SteamHiss").PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
			VentToxicGas(this.storedToxicGas);
			SetStoredSteamPct(0f);
		}

		private float storedSteam;
		private float storedToxicGas;
		private float leakRate = 1f;
		private float lastRepairTick = -1f;
		private int lastToxicTick = -1;

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
	}
}
