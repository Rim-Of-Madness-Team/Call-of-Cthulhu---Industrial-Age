using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using Verse;
using RimWorld;

namespace ArkhamEstate
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("jecrell.arkhamestate");
            harmony.Patch(AccessTools.Method(typeof(BuildableDef), "ForceAllowPlaceOver"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_ForceAllowPlaceOver))));
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), "CanPlaceBlueprintOver"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_CanPlaceBlueprintOver))));
            //harmony.Patch(AccessTools.Method(typeof(GenConstruct), "BlocksConstruction"),
            //    null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_BlocksConstruction))));
            harmony.Patch(AccessTools.Method(typeof(GenConstruct), "FirstBlockingThing"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_FirstBlockingThing))));
            harmony.Patch(AccessTools.Method(typeof(GenSpawn), "SpawningWipes"),
                null, new HarmonyMethod(AccessTools.Method(typeof(HarmonyPatches), nameof(Chandeliers_SpawningWipes))));
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
