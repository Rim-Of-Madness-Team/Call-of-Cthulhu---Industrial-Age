using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld;
using UnityEngine;

namespace ArkhamEstate
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
//            <li Class="CompProperties_AffectedByFacilities">
//                <linkableFacilities>
//                <li>ToolCabinet</li>
//                </linkableFacilities>
//                </li>
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading.Where(x => x.comps.Any(y => y is CompProperties_AffectedByFacilities)))
            {
                def.GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities
                    .Add(ThingDef.Named("Estate_SteamDrivePlant"));
            }
            
            HarmonyInstance harmony = HarmonyInstance.Create("jecrell.arkhamestate");
            harmony.Patch(AccessTools.Method(typeof(BuildableDef), "ForceAllowPlaceOver"),
                null,
                new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_ForceAllowPlaceOver))));
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), "CanPlaceBlueprintOver"),
                null,
                new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches),
                    nameof(Chandeliers_CanPlaceBlueprintOver))));
            //harmony.Patch(AccessTools.Method(typeof(GenConstruct), "BlocksConstruction"),
            //    null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_BlocksConstruction))));
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), "FirstBlockingThing"),
                null,
                new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_FirstBlockingThing))));
            harmony.Patch(AccessTools.Method(typeof(GenSpawn), "SpawningWipes"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_SpawningWipes))));
            harmony.Patch(AccessTools.Method(typeof(Graphic_LinkedTransmitterOverlay), "ShouldLinkWith"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(ShouldLinkWith))));
            harmony.Patch(AccessTools.Method(typeof(Graphic_LinkedTransmitter), "ShouldLinkWith"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(ShouldLinkWithTrans))));
            harmony.Patch(AccessTools.Method(typeof(CompFlickable), "get_CommandTex"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(flickWaterCommandTex))));
            harmony.Patch(AccessTools.Method(typeof(CompFacility), "get_CanBeActive"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(SteamFacilityCanBeActive))));
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), "TerrainCanSupport"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(TerrainCanSupport))));
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), "CanBuildOnTerrain"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(CanBuildOnTerrain))));
        }

        //GenConstruct
        public static void CanBuildOnTerrain(BuildableDef entDef, IntVec3 c, Map map, Rot4 rot,
            Thing thingToIgnore, ref bool __result)
        {
            if (entDef.defName == "Estate_WaterPump" ||
                entDef.defName == "Estate_WaterConduit")
            {
                CellRect cellRect = GenAdj.OccupiedRect(c, rot, entDef.Size);
                cellRect.ClipInsideMap(map);
                CellRect.CellRectIterator iterator = cellRect.GetIterator();
                while (!iterator.Done())
                {
                    TerrainDef terrainDef2 = map.terrainGrid.TerrainAt(iterator.Current);
                    if (terrainDef2.defName != "Mud" &&
                        !terrainDef2.affordances.Contains(entDef.terrainAffordanceNeeded))
                    {
                        __result = false;
                        return;
                    }
                    iterator.MoveNext();
                }
                __result = true;         
            }
        }

        //GenConstruct
        public static void TerrainCanSupport(CellRect rect, Map map, ThingDef thing, ref bool __result)
        {
            if (thing.defName == "Estate_WaterPump" ||
                thing.defName == "Estate_WaterConduit")
            {
                CellRect.CellRectIterator iterator = rect.GetIterator();
                while (!iterator.Done())
                {
                    if (!iterator.Current.SupportsStructureType(map, thing.terrainAffordanceNeeded) && iterator.Current.GetTerrain(map).defName != "Mud")
                    {
                        __result = false;
                        return;
                    }
                    iterator.MoveNext();
                }
                __result = true;
            }

        }
        
        //CompFacility : ThingComp
        //{
        public static void SteamFacilityCanBeActive(CompFacility __instance, ref bool __result)
        {
            if (!__result && __instance.parent.GetComp<CompSteamTrader>() is CompSteamTrader t)
            {
                __result = t.SteamOn;
            }
        }

        private static Texture2D flickWaterTexture = null;
        public static void flickWaterCommandTex(CompFlickable __instance, ref Texture2D __result)
        {
            if (__instance?.parent?.GetComp<CompWater>() != null)
            {
                var Props = (CompProperties_Flickable)__instance.props;
                if (flickWaterTexture == null)
                    flickWaterTexture = TexButton.Estate_DesireWater;
                __result = flickWaterTexture;   
            }
        }

        
        public static bool bShouldLinkWith(IntVec3 c, Thing parent)
        {
            if (!parent.Spawned)
            {
                return false;
            }
            if (!c.InBounds(parent.Map))
            {
                return (parent.def.graphicData.linkFlags & LinkFlags.MapEdge) != LinkFlags.None;
            }
            return (parent.Map.linkGrid.LinkFlagsAt(c) & parent.def.graphicData.linkFlags) != LinkFlags.None;
        }
        
        public static void ShouldLinkWithTrans(Graphic_LinkedTransmitter __instance, IntVec3 c, Thing parent, ref bool __result)
        {
            parent.Map.GetComponent<WaterNetManager>().UpdateWaterNetsAndConnections_First();
            parent.Map.GetComponent<SteamNetManager>().UpdateSteamNetsAndConnections_First();
           __result = __result ||
                (c.InBounds(parent.Map) && (bShouldLinkWith(c, parent) || (parent.def.EverTransmitsWater() && parent.Map.GetComponent<WaterNetGrid>()?.TransmittedWaterNetAt(c) != null))) ||
                (c.InBounds(parent.Map) && (bShouldLinkWith(c, parent) || (parent.def.EverTransmitsSteam() && parent.Map.GetComponent<SteamNetGrid>()?.TransmittedSteamNetAt(c) != null)));
           // __result = __result || (c.InBounds(parent.Map) && parent.Map.GetComponent<WaterNetGrid>()?.TransmittedWaterNetAt(c) != null);
        }

        public static void ShouldLinkWith(IntVec3 c, Thing parent, ref bool __result)
        {
            __result = __result || 
                       (c.InBounds(parent.Map) && parent.Map.GetComponent<WaterNetGrid>()?.TransmittedWaterNetAt(c) != null) ||
                       (c.InBounds(parent.Map) && parent.Map.GetComponent<SteamNetGrid>()?.TransmittedSteamNetAt(c) != null);
        }

        public static List<ThingDef> GetSpecialCases()
        {
            return new List<ThingDef>
            { ThingDef.Named("Jecrell_ChandelierRoyal"),
              ThingDef.Named("Jecrell_Chandelier")};
        }

        // Verse.GenSpawn
        public static void Chandeliers_SpawningWipes(BuildableDef newEntDef, BuildableDef oldEntDef, ref bool __result)
        {
            //Log.Message("NewEntDef :" + newEntDef.LabelCap);
            //Log.Message("OldEntDef :" + oldEntDef.LabelCap);
            if (newEntDef is ThingDef d && (GetSpecialCases().Contains(d.entityDefToBuild as ThingDef) ||
                GetSpecialCases().Contains(d as ThingDef)))
            {
                __result = false;
            }
        }

            // RimWorld.GenConstruct
            public static void Chandeliers_FirstBlockingThing(Thing constructible, Pawn pawnToIgnore, ref Thing __result)
        {
            //Log.Message("FirstBlockingThing Called");
            if (__result != null)
            {
                //Log.Message("Result = " + constructible.def.LabelCap);
                    if (__result.def == constructible.def) return;
                    foreach (ThingDef tDef in GetSpecialCases())
                    {
                        if (constructible.def.entityDefToBuild == tDef ||
                        constructible.def == tDef)
                        {
                            //Log.Message("Does not block.");
                            __result = null;
                            return;
                        }
                    }
            }

        }

        //// RimWorld.GenConstruct
        //public static void Chandeliers_BlocksConstruction(Thing constructible, Thing t, ref bool __result)
        //{
        //    Log.Message("BlocksConstruction Called");

        //    if (GetSpecialCases().Contains(constructible.def))
        //    {
        //        if (GetSpecialCases().Contains(t.def))
        //        {
        //            Log.Message("Blocks");
        //            __result = true;
        //            return;
        //        }
        //        Log.Message("Does not block");
        //        __result = false;
        //    }
        //}

        // RimWorld.GenConstruct
        public static void Chandeliers_CanPlaceBlueprintOver(
        BuildableDef newDef, ThingDef oldDef, ref bool __result)
        {
            List<ThingDef> specialCases = GetSpecialCases();
            if (newDef.ForceAllowPlaceOver(oldDef))
            {
                __result = true;
            }
        }

        public static void Chandeliers_ForceAllowPlaceOver(BuildableDef __instance, BuildableDef other, ref bool __result)
        {
            List<ThingDef> specialCases = GetSpecialCases();
            if (specialCases.Contains(__instance as ThingDef) && !specialCases.Contains(other as ThingDef))
            {
                //Log.Message("Success");
                __result = true;
                return;
            }
            //Log.Message("Failure");
        }

    }
}
