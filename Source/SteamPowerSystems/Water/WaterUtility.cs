using System.Collections.Generic;
using Verse;

namespace ArkhamEstate
{
    public static class WaterUtility
    {
        public static bool IsWater(this TerrainDef td)
        {
            if (td.defName.ToLowerInvariant().Contains("water") ||
                td.defName.ToLowerInvariant().Contains("ocean") ||
                td.defName.ToLowerInvariant().Contains("marsh"))
                return true;
            return false;
        }
        
        public static Building GetWaterTransmitter(this IntVec3 c, Map map)
        {
            List<Thing> list = map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].def.EverTransmitsWater())
                {
                    return (Building)list[i];
                }
            }
            return null;
        }
        
        public static bool EverTransmitsWater(this ThingDef def)
        {
            for (int i = 0; i < def.comps.Count; i++)
                {
                    CompProperties_Water compProperties_Water = def.comps[i] as CompProperties_Water;
                    if (compProperties_Water != null && compProperties_Water.transmitsWater)
                    {
                        return true;
                    }
                }
                return false;
        }
        
        public static WaterNetManager WaterNetManager(this Map map)
        {
            return map.GetComponent<WaterNetManager>();
        }
        
        public static bool ConnectsToWater(this ThingDef def)
        {
            if (def.EverTransmitsWater())
            {
                return false;
            }
            for (int i = 0; i < def.comps.Count; i++)
            {
                if (def.comps[i].compClass == typeof(CompWaterTank))
                {
                    return true;
                }
                if (def.comps[i].compClass == typeof(CompWaterTrader))
                {
                    return true;
                }
            }
            return false;
        }
        
        public static bool TransmitsWaterNow(this Building b)
        {
            CompWater waterComp = b.TryGetComp<CompWater>();
            return waterComp != null && waterComp.Props.transmitsWater;
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