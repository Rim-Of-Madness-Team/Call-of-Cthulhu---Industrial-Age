using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArkhamEstate
{
	public class SteamNet
	{
		public SteamNet(IEnumerable<CompSteam> newTransmitters)
		{
			foreach (CompSteam compSteam in newTransmitters)
			{
				this.transmitters.Add(compSteam);
				compSteam.transNet = this;
				this.RegisterAllComponentsOf(compSteam.parent);
				if (compSteam.connectChildren != null)
				{
					List<CompSteam> connectChildren = compSteam.connectChildren;
					for (int i = 0; i < connectChildren.Count; i++)
					{
						this.RegisterConnector(connectChildren[i]);
					}
				}
			}
			this.hasSteamSource = false;
			for (int j = 0; j < this.transmitters.Count; j++)
			{
				if (this.IsSteamSource(this.transmitters[j]))
				{
					this.hasSteamSource = true;
					break;
				}
			}
		}

		public Map Map
		{
			get
			{
				return this.steamNetManager.map;
			}
		}

		public bool HasActiveSteamSource
		{
			get
			{
				if (!this.hasSteamSource)
				{
					return false;
				}
				for (int i = 0; i < this.transmitters.Count; i++)
				{
					if (this.IsActiveSteamSource(this.transmitters[i]))
					{
						return true;
					}
				}
				return false;
			}
		}

		private bool IsSteamSource(CompSteam cp)
		{
			return cp is CompSteamTank || (cp is CompSteamTrader && (cp.Props.baseSteamConsumption < 0f || cp.Props.baseWaterConsumption < 0f));
		}

		private bool IsActiveSteamSource(CompSteam cp)
		{
			CompSteamTank CompSteamTank = cp as CompSteamTank;
			if (CompSteamTank != null && CompSteamTank.StoredSteam > 0f)
			{
				return true;
			}
			CompSteamTrader compSteamTrader = cp as CompSteamTrader;
			return compSteamTrader != null && compSteamTrader.SteamOutput > 0f;
		}

		public void RegisterConnector(CompSteam b)
		{
			//SteamUtility.PrintDebugMessage("RegisterConnector Called");
			if (this.connectors.Contains(b))
			{
				Log.Error("SteamNet registered connector it already had: " + b);
				return;
			}
			this.connectors.Add(b);
			this.RegisterAllComponentsOf(b.parent);
		}

		public void DeregisterConnector(CompSteam b)
		{
			this.connectors.Remove(b);
			this.DeregisterAllComponentsOf(b.parent);
		}

		private void RegisterAllComponentsOf(ThingWithComps parentThing)
		{
			CompSteamTrader comp = parentThing.GetComp<CompSteamTrader>();
			if (comp != null)
			{
				if (this.SteamComps.Contains(comp))
				{
					//Log.Error("SteamNet adding SteamComp " + comp + " which it already has.");
				}
				else
				{
					this.SteamComps.Add(comp);
				}
			}			
			CompSteamTank comp2 = parentThing.GetComp<CompSteamTank>();
			if (comp2 != null)
			{
				if (this.batteryComps.Contains(comp2))
				{
					//Log.Error("SteamNet adding SteamTankComp " + comp2 + " which it already has.");
				}
				else
				{
					this.batteryComps.Add(comp2);
				}
			}
		}

		private void DeregisterAllComponentsOf(ThingWithComps parentThing)
		{
			CompSteamTrader comp = parentThing.GetComp<CompSteamTrader>();
			if (comp != null)
			{
				this.SteamComps.Remove(comp);
			}
			CompSteamTank comp2 = parentThing.GetComp<CompSteamTank>();
			if (comp2 != null)
			{
				this.batteryComps.Remove(comp2);
			}
		}

		public float CurrentSteamGainRate()
		{
			if (DebugSettings.unlimitedPower)
			{
				return 100000f;
			}
			float num = 0f;
			for (int i = 0; i < this.SteamComps.Count; i++)
			{
				if (this.SteamComps[i].SteamOn)
				{
					num += this.SteamComps[i].SteamOutputPerTick;
				}
			}
			return num;
		}

		public float CurrentStoredSteam()
		{
			float num = 0f;
			for (int i = 0; i < this.batteryComps.Count; i++)
			{
				num += this.batteryComps[i].StoredSteam;
			}
			return num;
		}
		
		
		public float CurrentToxicGasRate()
		{
			if (DebugSettings.unlimitedPower)
			{
				return 0f;
			}
			float num = 0f;
			for (int i = 0; i < this.SteamComps.Count; i++)
			{
				if (this.SteamComps[i].SteamOn)
				{
					num += this.SteamComps[i].SteamOutputPerTick * this.SteamComps[i].ToxicGasGenPct;
					num -= this.SteamComps[i].ToxicGasVentRate;
				}
			}
			return num;
		}
		
		public float CurrentStoredToxicGas()
		{
			float num = 0f;
			for (int i = 0; i < this.batteryComps.Count; i++)
			{
				num += this.batteryComps[i].StoredToxicGas;
			}
			return num;
		}

		public void AdjustToxicGas(float amt)
		{
			for (int i = 0; i < this.batteryComps.Count; i++)
			{
				var storedToxicGas = this.batteryComps[i].StoredToxicGas;
				this.batteryComps[i].StoredToxicGas = Mathf.Clamp(storedToxicGas - amt, 0f, storedToxicGas);
			}
		}

		public void SteamNetTick()
		{
			float num = this.CurrentSteamGainRate();
			float num2 = this.CurrentStoredSteam();
			if (num2 + num >= -1E-07f && !this.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.SolarFlare))
			{
				float num3;
				if (this.batteryComps.Count > 0 && num2 >= 0.1f)
				{
					num3 = num2 - 5f;
				}
				else
				{
					num3 = num2;
				}
				if (UnityData.isDebugBuild)
				{
					this.debugLastApparentStoredSteam = num3;
					this.debugLastCreatedSteam = num;
					this.debugLastRawStoredSteam = num2;
				}
				if (num3 + num >= 0f)
				{
					SteamNet.partsWantingSteamOn.Clear();
					for (int i = 0; i < this.SteamComps.Count; i++)
					{
						if (!this.SteamComps[i].SteamOn && FlickUtility.WantsToBeOn(this.SteamComps[i].parent) && !this.SteamComps[i].parent.IsBrokenDown())
						{
							SteamNet.partsWantingSteamOn.Add(this.SteamComps[i]);
						}
					}
					if (SteamNet.partsWantingSteamOn.Count > 0)
					{
						int num4 = 200 / SteamNet.partsWantingSteamOn.Count;
						if (num4 < 30)
						{
							num4 = 30;
						}
						if (Find.TickManager.TicksGame % num4 == 0)
						{
							int num5 = Mathf.Max(1, Mathf.RoundToInt((float)SteamNet.partsWantingSteamOn.Count * 0.05f));
							for (int j = 0; j < num5; j++)
							{
								CompSteamTrader compSteamTrader = SteamNet.partsWantingSteamOn.RandomElement<CompSteamTrader>();
								if (!compSteamTrader.SteamOn)
								{
									if (num + num2 >= -(compSteamTrader.SteamOutputPerTick + 1E-07f))
									{
										compSteamTrader.SteamOn = true;
										num += compSteamTrader.SteamOutputPerTick;
									}
								}
							}
						}
					}
				}
				this.ChangeStoredSteam(num);
			}
			else if (Find.TickManager.TicksGame % 20 == 0)
			{
				SteamNet.potentialShutdownParts.Clear();
				for (int k = 0; k < this.SteamComps.Count; k++)
				{
					if (this.SteamComps[k].SteamOn && this.SteamComps[k].SteamOutputPerTick < 0f)
					{
						SteamNet.potentialShutdownParts.Add(this.SteamComps[k]);
					}
				}
				if (SteamNet.potentialShutdownParts.Count > 0)
				{
					int num6 = Mathf.Max(1, Mathf.RoundToInt((float)SteamNet.potentialShutdownParts.Count * 0.05f));
					for (int l = 0; l < num6; l++)
					{
						SteamNet.potentialShutdownParts.RandomElement<CompSteamTrader>().SteamOn = false;
					}
				}
			}
		}

		private void ChangeStoredSteam(float extra)
		{
			if (extra > 0f)
			{
				this.DistributeSteamAmongTanks(extra);
			}
			else
			{
				float num = -extra;
				this.givingBats.Clear();
				for (int i = 0; i < this.batteryComps.Count; i++)
				{
					if (this.batteryComps[i].StoredSteam > 1E-07f)
					{
						this.givingBats.Add(this.batteryComps[i]);
					}
				}
				float a = num / (float)this.givingBats.Count;
				int num2 = 0;
				while (num > 1E-07f)
				{
					for (int j = 0; j < this.givingBats.Count; j++)
					{
						float num3 = Mathf.Min(a, this.givingBats[j].StoredSteam);
						this.givingBats[j].DrawSteam(num3);
						num -= num3;
						if (num < 1E-07f)
						{
							return;
						}
					}
					num2++;
					if (num2 > 10)
					{
						break;
					}
				}
				if (num > 1E-07f)
				{
					Log.Warning("Drew steam from a SteamNet that didn't have it.");
				}
			}
		}

		private void DistributeSteamAmongTanks(float steam)
		{
			if (steam <= 0f || !this.batteryComps.Any<CompSteamTank>())
			{
				return;
			}
			SteamNet.tanksShuffled.Clear();
			SteamNet.tanksShuffled.AddRange(this.batteryComps);
			SteamNet.tanksShuffled.Shuffle<CompSteamTank>();
			int num = 0;
			for (;;)
			{
				num++;
				if (num > 10000)
				{
					break;
				}
				float num2 = float.MaxValue;
				for (int i = 0; i < SteamNet.tanksShuffled.Count; i++)
				{
					num2 = Mathf.Min(num2, SteamNet.tanksShuffled[i].AmountCanAccept);
				}
				if (steam < num2 * (float)SteamNet.tanksShuffled.Count)
				{
					goto IL_128;
				}
				for (int j = SteamNet.tanksShuffled.Count - 1; j >= 0; j--)
				{
					float amountCanAccept = SteamNet.tanksShuffled[j].AmountCanAccept;
					bool flag = amountCanAccept <= 0f || amountCanAccept == num2;
					if (num2 > 0f)
					{
						SteamNet.tanksShuffled[j].AddSteam(num2);
						steam -= num2;
					}
					if (flag)
					{
						SteamNet.tanksShuffled.RemoveAt(j);
					}
				}
				if (steam < 0.0005f || !SteamNet.tanksShuffled.Any<CompSteamTank>())
				{
					goto IL_18F;
				}
			}
			Log.Error("Too many iterations.");
			goto IL_199;
			IL_128:
			float amount = steam / (float)SteamNet.tanksShuffled.Count;
			for (int k = 0; k < SteamNet.tanksShuffled.Count; k++)
			{
				SteamNet.tanksShuffled[k].AddSteam(amount);
			}
			steam = 0f;
			IL_18F:
			IL_199:
			SteamNet.tanksShuffled.Clear();
		}

		public string DebugString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("SteamNET:");
			stringBuilder.AppendLine("  Created steam: " + this.debugLastCreatedSteam);
			stringBuilder.AppendLine("  Raw stored steam: " + this.debugLastRawStoredSteam);
			stringBuilder.AppendLine("  Apparent stored steam: " + this.debugLastApparentStoredSteam);
			stringBuilder.AppendLine("  hasSteamSource: " + this.hasSteamSource);
			stringBuilder.AppendLine("  Connectors: ");
			foreach (CompSteam compSteam in this.connectors)
			{
				stringBuilder.AppendLine("      " + compSteam.parent);
			}
			stringBuilder.AppendLine("  Transmitters: ");
			foreach (CompSteam compSteam2 in this.transmitters)
			{
				stringBuilder.AppendLine("      " + compSteam2.parent);
			}
			stringBuilder.AppendLine("  SteamComps: ");
			foreach (CompSteamTrader compSteamTrader in this.SteamComps)
			{
				stringBuilder.AppendLine("      " + compSteamTrader.parent);
			}
			stringBuilder.AppendLine("  batteryComps: ");
			foreach (CompSteamTank CompSteamTank in this.batteryComps)
			{
				stringBuilder.AppendLine("      " + CompSteamTank.parent);
			}
			return stringBuilder.ToString();
		}

		public SteamNetManager steamNetManager;

		public bool hasSteamSource;

		public List<CompSteam> connectors = new List<CompSteam>();

		public List<CompSteam> transmitters = new List<CompSteam>();

		public List<CompSteamTrader> SteamComps = new List<CompSteamTrader>();

		public List<CompSteamTank> batteryComps = new List<CompSteamTank>();

		private float debugLastCreatedSteam;

		private float debugLastRawStoredSteam;

		private float debugLastApparentStoredSteam;

		private const int MaxRestartTryInterval = 200;

		private const int MinRestartTryInterval = 30;

		private const float RestartMinFraction = 0.05f;

		private const int ShutdownInterval = 20;

		private const float ShutdownMinFraction = 0.05f;

		private const float MinStoredSteamToTurnOn = 5f;

		private static List<CompSteamTrader> partsWantingSteamOn = new List<CompSteamTrader>();

		private static List<CompSteamTrader> potentialShutdownParts = new List<CompSteamTrader>();

		private List<CompSteamTank> givingBats = new List<CompSteamTank>();

		private static List<CompSteamTank> tanksShuffled = new List<CompSteamTank>();
	}
}
