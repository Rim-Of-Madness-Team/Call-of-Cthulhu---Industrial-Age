// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
using Verse.AI;          // Needed when you do something with the AI
using Verse.AI.Group;
using Verse.Sound;       // Needed when you do something with Sound
using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

namespace ArkhamEstate
{
    class MapComponent_ArkhamEstate : MapComponent
    {
        //public List<CosmicEntity> DeityCache = new List<CosmicEntity>();
        public bool startedWorldObject = false;
        private Map mapRecord = null;

        public MapComponent_ArkhamEstate(Map map) : base(map)
        {
            this.map = map;
            this.mapRecord = map;
            if (startedWorldObject) return;
            Cthulhu.UtilityWorldObjectManager.GetUtilityWorldObject<UtilityWorldObject_ArkhamEstate>();
            Cthulhu.UtilityWorldObjectManager.GetUtilityWorldObject<UtilityWorldObject_Tunes>();
        }

        public static MapComponent_ArkhamEstate GetComponent(Map map)
        {
            MapComponent_ArkhamEstate result = map.components.OfType<MapComponent_ArkhamEstate>().FirstOrDefault<MapComponent_ArkhamEstate>();
            if (result == null)
            {
                result = new MapComponent_ArkhamEstate(map);
                map.components.Add(result);
            }
            return result;
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            //if (!CheckedForRecipes)
            //{
            //    GenerateStrangeMeatRecipe(map);
            //    CheckedForRecipes = true;
            //}
        }

        //public static void GenerateStrangeMeatRecipe(Map map)
        //{
        //    if (Cthulhu.Utility.IsCosmicHorrorsLoaded() && !MapComponent_ArkhamEstate.GetComponent(map).AreRecipesReady)
        //    {
        //        foreach (RecipeDef current in DefDatabase<RecipeDef>.AllDefs)
        //        {
        //            if (current.defName.Contains("Jecrell_MakeWax"))
        //            {
        //                Log.Message("Found MakeWax");
        //                //CosmicHorror_StrangeMeatRaw

        //                ThingFilter newFilter = new ThingFilter();
        //                newFilter.CopyAllowancesFrom(current.fixedIngredientFilter);
        //                newFilter.SetAllow(ThingCategoryDef.Named("CosmicHorror_StrangeMeatRaw"), true);
        //                current.fixedIngredientFilter = newFilter;


        //                ThingFilter newFilter2 = new ThingFilter();
        //                newFilter2.CopyAllowancesFrom(current.defaultIngredientFilter);
        //                newFilter2.SetAllow(ThingCategoryDef.Named("CosmicHorror_StrangeMeatRaw"), true);
        //                current.defaultIngredientFilter = newFilter;

        //                foreach (IngredientCount temp in current.ingredients)
        //                {
        //                    if (temp.filter != null)
        //                    {
        //                        ThingFilter newFilter3 = new ThingFilter();
        //                        newFilter3.CopyAllowancesFrom(temp.filter);
        //                        newFilter3.SetAllow(ThingCategoryDef.Named("CosmicHorror_StrangeMeatRaw"), true);
        //                        temp.filter = newFilter3;
        //                        Log.Message("Added new filter");
        //                    }
        //                }
        //                //foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        //                //{
        //                //    if (def.defName.Contains("RawCHFood_"))
        //                //    {
        //                //IngredientCount ingredientCount = new IngredientCount();
        //                //ingredientCount.filter.SetAllow(def, true);
        //                //ingredientCount.SetBaseCount((float)2);
        //                //current.fixedIngredientFilter.SetAllow(def, true);

        //                //current.defaultIngredientFilter.SetAllow(def, true);
        //                //current.ingredients.Add(ingredientCount);
        //                //       Log.Message("Added " + def.defName);
        //                //   }
        //                //}
        //            }
        //        }
        //        MapComponent_ArkhamEstate.GetComponent(map).AreRecipesReady = true;
        //        Log.Message("Strange meat added to wax recipes.");
        //    }
        //    return;
        //}

        
        //public override void ExposeData()
        //{
        //    //Scribe_Collections.LookList<CosmicEntity>(ref this.DeityCache, "Deities", LookMode.Deep, new object[0]);
        //    //Scribe_Values.LookValue<bool>(ref this.AreRecipesReady, "AreRecipesReady", false, false);
        //    base.ExposeData();
        //    if (Scribe.mode == LoadSaveMode.PostLoadInit)
        //    {
        //        GenerateStrangeMeatRecipe(map);
        //    }
        //}
    }
}
