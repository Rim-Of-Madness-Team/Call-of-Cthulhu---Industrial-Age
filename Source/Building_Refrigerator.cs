using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArkhamEstate
{
    public class Building_Refrigerator : Building_Storage, IStoreSettingsParent
    {
        private const float IdealTempDefault = -10f;
	    
        private float currentTemp = float.MinValue;
        private float idealTemp = float.MinValue;
	    private bool operatingAtHighPower;
	    private StorageSettings curStorageSettings;

        private CompPowerTrader powerTrader;
        private CompGlower glower;

        public CompPowerTrader PowerTrader
        {
            get => powerTrader;
            set => powerTrader = value;
        }

        public CompGlower Glower
        {
            get => glower;
            set => glower = value;
        }

        public float IdealTemp
        {
            get
            {
                if (idealTemp == float.MinValue)
                    idealTemp = IdealTempDefault;
                return idealTemp;
            }
            set => idealTemp = value;
        }

        public float CurrentTemp
        {
            get
            {
                if (currentTemp == float.MinValue)
                    currentTemp = PositionHeld.GetTemperature(MapHeld);
                ;
                return currentTemp;
            }
            set => currentTemp = value;
        }

        public override void SpawnSetup(Map map, bool bla)
        {
            base.SpawnSetup(map, bla);
            this.powerTrader = GetComp<CompPowerTrader>();
            this.glower = GetComp<CompGlower>();
            curStorageSettings = new StorageSettings();
            curStorageSettings.CopyFrom(def.building.fixedStorageSettings);
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefs.Where(x => x.HasComp(typeof(CompRottable))))
            {
                if (!curStorageSettings.filter.Allows(thingDef))
                    curStorageSettings.filter.SetAllow(thingDef, true);
            }
        }

        StorageSettings IStoreSettingsParent.GetParentStoreSettings() => curStorageSettings;

        public override void TickRare()
        {
            base.TickRare();
            MakeAllHeldThingsBetterCompRottable();
            ResolveTemperature();
        }
	    

	    const float lowPowerConsumptionFactor = 0.1f;
	    const float temperatureChangeRate = 0.116923077f;
	    const float energyPerSecond = 12f;

	    public float BasePowerConsumption => -powerTrader.Props.basePowerConsumption;
	    
	    private void ResolveTemperature()
        {
	        if (!this.Spawned || powerTrader == null || !powerTrader.PowerOn)
	        {
		        EqualizeWithRoomTemperature();
		        return;
	        }

	        this.glower.UpdateLit(MapHeld);

            IntVec3 intVec = PositionHeld;
	        float moddedTemperatureChangeRate = temperatureChangeRate;
	        float energyUsed = 0f;
	        float energyLimit = energyPerSecond * moddedTemperatureChangeRate * 4.16666651f;
	        var usingHighPower = IsUsingHighPower(energyLimit, out energyUsed);
            if (usingHighPower)
            {
	            GenTemperature.PushHeat(intVec, MapHeld, -energyLimit * 1.25f);
	            energyUsed += BasePowerConsumption;
	            moddedTemperatureChangeRate *= 0.8f;
            }
            else
            {
	            energyUsed =
                    BasePowerConsumption * lowPowerConsumptionFactor;
	            moddedTemperatureChangeRate *= 1.1f;
            }
	        if (!Mathf.Approximately(CurrentTemp, IdealTemp))
	        {
		        CurrentTemp += CurrentTemp > IdealTemp ? -moddedTemperatureChangeRate : moddedTemperatureChangeRate;
	        }
		    if (CurrentTemp.ToStringTemperature("F0") == IdealTemp.ToStringTemperature("F0"))
		        usingHighPower = false;
	        
            operatingAtHighPower = usingHighPower;
	        powerTrader.PowerOutput = energyUsed;
        }

	    private void EqualizeWithRoomTemperature()
	    {
		    float roomTemperature = PositionHeld.GetTemperature(MapHeld);
		    if (CurrentTemp > roomTemperature)
		    {
			    CurrentTemp += -temperatureChangeRate;
		    }
		    else if (CurrentTemp < roomTemperature)
		    {
			    CurrentTemp += temperatureChangeRate;
		    }
	    }

	    private bool IsUsingHighPower(float energyLimit, out float energyUsed)
	    {
		    float b = energyLimit;
		    float a = IdealTemp - CurrentTemp;
		    energyUsed = 0f;
		    if (energyLimit > 0f)
		    {
			    energyUsed = Mathf.Min(a, b);
			    energyUsed = Mathf.Max(energyUsed, 0f);
		    }
		    else
		    {
			    energyUsed = Mathf.Max(a, b);
			    energyUsed = Mathf.Min(energyUsed, 0f);
		    }
		    return Mathf.Approximately(energyUsed, 0f);
	    }

	    private void MakeAllHeldThingsBetterCompRottable()
        {
            foreach (var thing in PositionHeld.GetThingList(Map))
            {
                if (thing is ThingWithComps thingWithComps)
                {
                    var rottable = thing.TryGetComp<CompRottable>();
                    if (rottable != null && !(rottable is CompBetterRottable))
                    {
                        var newRot = new CompBetterRottable();
                        thingWithComps.AllComps.Remove(rottable);
                        thingWithComps.AllComps.Add(newRot);
                        newRot.props = rottable.props;
                        newRot.parent = thingWithComps;
                        newRot.RotProgress = rottable.RotProgress;
                    }
                }
            }
        }
        
        
		private float RoundedToCurrentTempModeOffset(float celsiusTemp)
		{
			float num = GenTemperature.CelsiusToOffset(celsiusTemp, Prefs.TemperatureMode);
			num = Mathf.RoundToInt(num);
			return GenTemperature.ConvertTemperatureOffset(num, Prefs.TemperatureMode, TemperatureDisplayMode.Celsius);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo c in base.GetGizmos())
			{
				yield return c;
			}
			float offset2 = RoundedToCurrentTempModeOffset(-10f);
			yield return new Command_Action
			{
				action = delegate
				{
					InterfaceChangeTargetTemperature(offset2);
				},
				defaultLabel = offset2.ToStringTemperatureOffset("F0"),
				defaultDesc = "CommandLowerTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc5,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
			};
			float offset3 = RoundedToCurrentTempModeOffset(-1f);
			yield return new Command_Action
			{
				action = delegate
				{
					InterfaceChangeTargetTemperature(offset3);
				},
				defaultLabel = offset3.ToStringTemperatureOffset("F0"),
				defaultDesc = "CommandLowerTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc4,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
			};
			yield return new Command_Action
			{
				action = delegate
				{
					idealTemp = 21f;
					SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
					ThrowCurrentTemperatureText();
				},
				defaultLabel = "CommandResetTemp".Translate(),
				defaultDesc = "CommandResetTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc1,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset", true)
			};
			float offset4 = RoundedToCurrentTempModeOffset(1f);
			yield return new Command_Action
			{
				action = delegate
				{
					InterfaceChangeTargetTemperature(offset4);
				},
				defaultLabel = "+" + offset4.ToStringTemperatureOffset("F0"),
				defaultDesc = "CommandRaiseTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc2,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
			};
			float offset = RoundedToCurrentTempModeOffset(10f);
			yield return new Command_Action
			{
				action = delegate
				{
					InterfaceChangeTargetTemperature(offset);
				},
				defaultLabel = "+" + offset.ToStringTemperatureOffset("F0"),
				defaultDesc = "CommandRaiseTempDesc".Translate(),
				hotKey = KeyBindingDefOf.Misc3,
				icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
			};
		}

		private void InterfaceChangeTargetTemperature(float offset)
		{
			if (offset > 0f)
			{
				SoundDefOf.AmountIncrement.PlayOneShotOnCamera(null);
			}
			else
			{
				SoundDefOf.AmountDecrement.PlayOneShotOnCamera(null);
			}
			idealTemp += offset;
			idealTemp = Mathf.Clamp(idealTemp, -270f, 2000f);
			ThrowCurrentTemperatureText();
		}

		private void ThrowCurrentTemperatureText()
		{
			MoteMaker.ThrowText(this.TrueCenter() + new Vector3(0.5f, 0f, 0.5f), MapHeld, idealTemp.ToStringTemperature("F0"), Color.white, -1f);
		}

		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Temperature".Translate() + ": ");
			stringBuilder.AppendLine(CurrentTemp.ToStringTemperature("F0"));
			stringBuilder.Append("TargetTemperature".Translate() + ": ");
			stringBuilder.AppendLine(IdealTemp.ToStringTemperature("F0"));
			stringBuilder.Append("PowerConsumptionMode".Translate() + ": ");
			if (operatingAtHighPower)
			{
				stringBuilder.Append("PowerConsumptionHigh".Translate());
			}
			else
			{
				stringBuilder.Append("PowerConsumptionLow".Translate());
			}
			return stringBuilder.ToString();
		}


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentTemp, "currentTemp", float.MinValue);
            Scribe_Values.Look(ref idealTemp, "idealTemp", float.MinValue);
        }
    }
}