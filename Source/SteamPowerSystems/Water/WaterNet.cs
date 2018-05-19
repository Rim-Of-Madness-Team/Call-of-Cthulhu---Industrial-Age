using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArkhamEstate
{
	public class WaterNet
	{
		public WaterNet(IEnumerable<CompWater> newTransmitters)
		{
			foreach (CompWater compWater in newTransmitters)
			{
				this.transmitters.Add(compWater);
				compWater.transNet = this;
				this.RegisterAllComponentsOf(compWater.parent);
				if (compWater.connectChildren != null)
				{
					List<CompWater> connectChildren = compWater.connectChildren;
					for (int i = 0; i < connectChildren.Count; i++)
					{
						this.RegisterConnector(connectChildren[i]);
					}
				}
			}
			this.hasWaterSource = false;
			for (int j = 0; j < this.transmitters.Count; j++)
			{
				if (this.IsWaterSource(this.transmitters[j]))
				{
					this.hasWaterSource = true;
					break;
				}
			}
		}

		public Map Map
		{
			get
			{
				return this.waterNetManager.map;
			}
		}

		public bool HasActiveWaterSource
		{
			get
			{
				if (!this.hasWaterSource)
				{
					return false;
				}
				for (int i = 0; i < this.transmitters.Count; i++)
				{
					if (this.IsActiveWaterSource(this.transmitters[i]))
					{
						return true;
					}
				}
				return false;
			}
		}

		private bool IsWaterSource(CompWater cp)
		{
			return cp is CompWaterTank || (cp is CompWaterTrader && cp.Props.baseWaterConsumption < 0f);
		}

		private bool IsActiveWaterSource(CompWater cp)
		{
			CompWaterTank CompWaterTank = cp as CompWaterTank;
			if (CompWaterTank != null && CompWaterTank.StoredWater > 0f)
			{
				return true;
			}
			CompWaterTrader compWaterTrader = cp as CompWaterTrader;
			return compWaterTrader != null && compWaterTrader.WaterOutput > 0f;
		}

		public void RegisterConnector(CompWater b)
		{
			//WaterUtility.PrintDebugMessage("RegisterConnector Called");
			if (this.connectors.Contains(b))
			{
				Log.Error("WaterNet registered connector it already had: " + b);
				return;
			}
			this.connectors.Add(b);
			this.RegisterAllComponentsOf(b.parent);
		}

		public void DeregisterConnector(CompWater b)
		{
			this.connectors.Remove(b);
			this.DeregisterAllComponentsOf(b.parent);
		}

		private void RegisterAllComponentsOf(ThingWithComps parentThing)
		{
			CompWaterTrader comp = parentThing.GetComp<CompWaterTrader>();
			if (comp != null)
			{
				if (this.WaterComps.Contains(comp))
				{
					//Log.Error("WaterNet adding WaterComp " + comp + " which it already has.");
				}
				else
				{
					this.WaterComps.Add(comp);
				}
			}			
			CompWaterTank comp2 = parentThing.GetComp<CompWaterTank>();
			if (comp2 != null)
			{
				if (this.batteryComps.Contains(comp2))
				{
					//Log.Error("WaterNet adding WaterTankComp " + comp2 + " which it already has.");
				}
				else
				{
					this.batteryComps.Add(comp2);
				}
			}
		}

		private void DeregisterAllComponentsOf(ThingWithComps parentThing)
		{
			CompWaterTrader comp = parentThing.GetComp<CompWaterTrader>();
			if (comp != null)
			{
				this.WaterComps.Remove(comp);
			}
			CompWaterTank comp2 = parentThing.GetComp<CompWaterTank>();
			if (comp2 != null)
			{
				this.batteryComps.Remove(comp2);
			}
		}

		public float CurrentWaterGainRate()
		{
			if (DebugSettings.unlimitedPower)
			{
				return 100000f;
			}
			float num = 0f;
			for (int i = 0; i < this.WaterComps.Count; i++)
			{
				if (this.WaterComps[i].WaterOn)
				{
					num += this.WaterComps[i].WaterOutputPerTick;
				}
			}
			return num;
		}

		public float CurrentStoredWater()
		{
			float num = 0f;
			for (int i = 0; i < this.batteryComps.Count; i++)
			{
				num += this.batteryComps[i].StoredWater;
			}
			return num;
		}

		public void WaterNetTick()
		{
			float num = this.CurrentWaterGainRate();
			float num2 = this.CurrentStoredWater();
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
					this.debugLastApparentStoredWater = num3;
					this.debugLastCreatedWater = num;
					this.debugLastRawStoredWater = num2;
				}
				if (num3 + num >= 0f)
				{
					WaterNet.partsWantingWaterOn.Clear();
					for (int i = 0; i < this.WaterComps.Count; i++)
					{
						if (!this.WaterComps[i].WaterOn && FlickUtility.WantsToBeOn(this.WaterComps[i].parent) && !this.WaterComps[i].parent.IsBrokenDown())
						{
							WaterNet.partsWantingWaterOn.Add(this.WaterComps[i]);
						}
					}
					if (WaterNet.partsWantingWaterOn.Count > 0)
					{
						int num4 = 200 / WaterNet.partsWantingWaterOn.Count;
						if (num4 < 30)
						{
							num4 = 30;
						}
						if (Find.TickManager.TicksGame % num4 == 0)
						{
							int num5 = Mathf.Max(1, Mathf.RoundToInt((float)WaterNet.partsWantingWaterOn.Count * 0.05f));
							for (int j = 0; j < num5; j++)
							{
								CompWaterTrader compWaterTrader = WaterNet.partsWantingWaterOn.RandomElement<CompWaterTrader>();
								if (!compWaterTrader.WaterOn)
								{
									if (num + num2 >= -(compWaterTrader.WaterOutputPerTick + 1E-07f))
									{
										compWaterTrader.WaterOn = true;
										num += compWaterTrader.WaterOutputPerTick;
									}
								}
							}
						}
					}
				}
				this.ChangeStoredWater(num);
			}
			else if (Find.TickManager.TicksGame % 20 == 0)
			{
				WaterNet.potentialShutdownParts.Clear();
				for (int k = 0; k < this.WaterComps.Count; k++)
				{
					if (this.WaterComps[k].WaterOn && this.WaterComps[k].WaterOutputPerTick < 0f)
					{
						WaterNet.potentialShutdownParts.Add(this.WaterComps[k]);
					}
				}
				if (WaterNet.potentialShutdownParts.Count > 0)
				{
					int num6 = Mathf.Max(1, Mathf.RoundToInt((float)WaterNet.potentialShutdownParts.Count * 0.05f));
					for (int l = 0; l < num6; l++)
					{
						WaterNet.potentialShutdownParts.RandomElement<CompWaterTrader>().WaterOn = false;
					}
				}
			}
		}

		private void ChangeStoredWater(float extra)
		{
			if (extra > 0f)
			{
				this.DistributeWaterAmongTanks(extra);
			}
			else
			{
				float num = -extra;
				this.givingBats.Clear();
				for (int i = 0; i < this.batteryComps.Count; i++)
				{
					if (this.batteryComps[i].StoredWater > 1E-07f)
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
						float num3 = Mathf.Min(a, this.givingBats[j].StoredWater);
						this.givingBats[j].DrawWater(num3);
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
					Log.Warning("Drew water from a WaterNet that didn't have it.");
				}
			}
		}

		private void DistributeWaterAmongTanks(float water)
		{
			if (water <= 0f || !this.batteryComps.Any<CompWaterTank>())
			{
				return;
			}
			WaterNet.tanksShuffled.Clear();
			WaterNet.tanksShuffled.AddRange(this.batteryComps);
			WaterNet.tanksShuffled.Shuffle<CompWaterTank>();
			int num = 0;
			for (;;)
			{
				num++;
				if (num > 10000)
				{
					break;
				}
				float num2 = float.MaxValue;
				for (int i = 0; i < WaterNet.tanksShuffled.Count; i++)
				{
					num2 = Mathf.Min(num2, WaterNet.tanksShuffled[i].AmountCanAccept);
				}
				if (water < num2 * (float)WaterNet.tanksShuffled.Count)
				{
					goto IL_128;
				}
				for (int j = WaterNet.tanksShuffled.Count - 1; j >= 0; j--)
				{
					float amountCanAccept = WaterNet.tanksShuffled[j].AmountCanAccept;
					bool flag = amountCanAccept <= 0f || amountCanAccept == num2;
					if (num2 > 0f)
					{
						WaterNet.tanksShuffled[j].AddWater(num2);
						water -= num2;
					}
					if (flag)
					{
						WaterNet.tanksShuffled.RemoveAt(j);
					}
				}
				if (water < 0.0005f || !WaterNet.tanksShuffled.Any<CompWaterTank>())
				{
					goto IL_18F;
				}
			}
			Log.Error("Too many iterations.");
			goto IL_199;
			IL_128:
			float amount = water / (float)WaterNet.tanksShuffled.Count;
			for (int k = 0; k < WaterNet.tanksShuffled.Count; k++)
			{
				WaterNet.tanksShuffled[k].AddWater(amount);
			}
			water = 0f;
			IL_18F:
			IL_199:
			WaterNet.tanksShuffled.Clear();
		}

		public string DebugString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("WaterNET:");
			stringBuilder.AppendLine("  Created water: " + this.debugLastCreatedWater);
			stringBuilder.AppendLine("  Raw stored water: " + this.debugLastRawStoredWater);
			stringBuilder.AppendLine("  Apparent stored water: " + this.debugLastApparentStoredWater);
			stringBuilder.AppendLine("  hasWaterSource: " + this.hasWaterSource);
			stringBuilder.AppendLine("  Connectors: ");
			foreach (CompWater compWater in this.connectors)
			{
				stringBuilder.AppendLine("      " + compWater.parent);
			}
			stringBuilder.AppendLine("  Transmitters: ");
			foreach (CompWater compWater2 in this.transmitters)
			{
				stringBuilder.AppendLine("      " + compWater2.parent);
			}
			stringBuilder.AppendLine("  WaterComps: ");
			foreach (CompWaterTrader compWaterTrader in this.WaterComps)
			{
				stringBuilder.AppendLine("      " + compWaterTrader.parent);
			}
			stringBuilder.AppendLine("  batteryComps: ");
			foreach (CompWaterTank CompWaterTank in this.batteryComps)
			{
				stringBuilder.AppendLine("      " + CompWaterTank.parent);
			}
			return stringBuilder.ToString();
		}

		public WaterNetManager waterNetManager;

		public bool hasWaterSource;

		public List<CompWater> connectors = new List<CompWater>();

		public List<CompWater> transmitters = new List<CompWater>();

		public List<CompWaterTrader> WaterComps = new List<CompWaterTrader>();

		public List<CompWaterTank> batteryComps = new List<CompWaterTank>();

		private float debugLastCreatedWater;

		private float debugLastRawStoredWater;

		private float debugLastApparentStoredWater;

		private const int MaxRestartTryInterval = 200;

		private const int MinRestartTryInterval = 30;

		private const float RestartMinFraction = 0.05f;

		private const int ShutdownInterval = 20;

		private const float ShutdownMinFraction = 0.05f;

		private const float MinStoredWaterToTurnOn = 5f;

		private static List<CompWaterTrader> partsWantingWaterOn = new List<CompWaterTrader>();

		private static List<CompWaterTrader> potentialShutdownParts = new List<CompWaterTrader>();

		private List<CompWaterTank> givingBats = new List<CompWaterTank>();

		private static List<CompWaterTank> tanksShuffled = new List<CompWaterTank>();
	}
}
