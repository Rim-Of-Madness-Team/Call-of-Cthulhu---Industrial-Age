using System.Collections.Generic;
using Verse;

namespace ArkhamEstate
{
    
        
    public enum PressureLevel : int
    {
        Off = 0,
        Nominal = 1,
        Caution = 2,
        Danger = 3,
        Maximum = 4
    }
    
    public static class SteamUtility
    {
        
        
        public const float fuelLevelNominal = 0.26f;
        public const float fuelLevelCaution = 0.51f;
        public const float fuelLevelDanger = 0.76f;
                    
        
        public static PressureLevel GetCurPressureLevel(CompSteam compSteam, float curFuelLevel)
        {
            var curPressureLevel = curFuelLevel * ((compSteam.TransmitsSteamNow) ? 1f : 0f);
            if (curPressureLevel <= 0.01f)
                return PressureLevel.Off;
            else if (curPressureLevel <= fuelLevelNominal)
                return PressureLevel.Nominal;
            else if (curPressureLevel <= fuelLevelCaution)
                return PressureLevel.Caution;
            else if (curPressureLevel <= fuelLevelDanger)
                return PressureLevel.Danger;
            else
                return PressureLevel.Maximum;
        }
        
        public static string GetString(this PressureLevel level, bool showPrefix = true)
        {
            string prefix = "Estate_Pressure".Translate();
            string result = "Estate_PressureOff".Translate();
            switch (level)
            {
                case PressureLevel.Off:
                    break;
                case PressureLevel.Nominal:
                    result = "Estate_PressureNominal".Translate();
                    break;
                case PressureLevel.Caution:
                    result = "Estate_PressureCaution".Translate();
                    break;
                case PressureLevel.Danger:
                    result = "Estate_PressureDanger".Translate();
                    break;
                case PressureLevel.Maximum:
                    result = "Estate_PressureMaximum".Translate();
                    break;
            }
            return (showPrefix) ? prefix + " : " + result : result;
        }
        
        public static Building GetSteamTransmitter(this IntVec3 c, Map map)
        {
            List<Thing> list = map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].def.EverTransmitsSteam())
                {
                    return (Building)list[i];
                }
            }
            return null;
        }
        
        
        public static bool EverTransmitsSteam(this ThingDef def)
        {
            for (int i = 0; i < def.comps.Count; i++)
                {
                    CompProperties_Steam compProperties_Steam = def.comps[i] as CompProperties_Steam;
                    if (compProperties_Steam != null && compProperties_Steam.transmitsSteam)
                    {
                        return true;
                    }
                }
                return false;
        }
        
        public static SteamNetManager SteamNetManager(this Map map)
        {
            return map.GetComponent<SteamNetManager>();
        }
        
        public static bool ConnectsToSteam(this ThingDef def)
        {
            if (def.EverTransmitsSteam())
            {
                return false;
            }
            for (int i = 0; i < def.comps.Count; i++)
            {
                if (def.comps[i].compClass == typeof(CompSteamTank))
                {
                    return true;
                }
                if (def.comps[i].compClass == typeof(CompSteamTrader))
                {
                    return true;
                }
            }
            return false;
        }
        
        public static bool TransmitsSteamNow(this Building b)
        {
            CompSteam steamComp = b.TryGetComp<CompSteam>();
            return steamComp != null && steamComp.Props.transmitsSteam;
        }

        public static void PrintDebugMessage(string output)
        {
            if (DebugSettings.godMode)
            {
                Log.Message(output);
            }
        }

    }
}