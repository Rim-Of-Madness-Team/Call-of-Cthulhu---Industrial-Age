using System.Collections.Generic;
using Verse;

namespace ArkhamEstate
{
    public static class SteamUtility
    {
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