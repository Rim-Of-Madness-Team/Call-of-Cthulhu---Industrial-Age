﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace ArkhamEstate
{
    class UtilityWorldObject_ArkhamEstate : Cthulhu.UtilityWorldObject
    {
        private bool CheckedForRecipes = false;
        private bool AreRecipesReady = false;

        public override void Tick()
        {
            base.Tick();

            //Log.Message("UtilityWorldObject Arkham Estate Started");
            if (!CheckedForRecipes)
            {
                GenerateStrangeMeatRecipe();
                CheckedForRecipes = true;
            }
        }


        public void GenerateStrangeMeatRecipe()
        {
            if (Cthulhu.Utility.IsCosmicHorrorsLoaded() && !AreRecipesReady)
            {
                //Not really, but hey, let's get started.
                AreRecipesReady = true;

                //We want to use strange meat to make wax.
                RecipeDef recipeMakeWax = DefDatabase<RecipeDef>.AllDefs.FirstOrDefault((RecipeDef d) => d.defName == "Jecrell_MakeWax");
                if (recipeMakeWax != null)
                {
                    ThingFilter newFilter = new ThingFilter();
                    newFilter.CopyAllowancesFrom(recipeMakeWax.fixedIngredientFilter);
                    newFilter.SetAllow(ThingCategoryDef.Named("CosmicHorror_StrangeMeatRaw"), true);
                    recipeMakeWax.fixedIngredientFilter = newFilter;
                    
                    ThingFilter newFilter2 = new ThingFilter();
                    newFilter2.CopyAllowancesFrom(recipeMakeWax.defaultIngredientFilter);
                    newFilter2.SetAllow(ThingCategoryDef.Named("CosmicHorror_StrangeMeatRaw"), true);
                    recipeMakeWax.defaultIngredientFilter = newFilter;

                    foreach (IngredientCount temp in recipeMakeWax.ingredients)
                    {
                        if (temp.filter != null)
                        {
                            ThingFilter newFilter3 = new ThingFilter();
                            newFilter3.CopyAllowancesFrom(temp.filter);
                            newFilter3.SetAllow(ThingCategoryDef.Named("CosmicHorror_StrangeMeatRaw"), true);
                            temp.filter = newFilter3;
                            Log.Message("Added new filter");
                        }
                    }
                    Log.Message("Strange meat added to wax recipes.");
                }
                
                //I want stoves to be able to cook strange meals too.
                ThingDef stoveDef = DefDatabase<ThingDef>.AllDefs.FirstOrDefault((ThingDef def) => def.defName == "WoodStoveFurnace");
                if (stoveDef != null)
                {
                    if (stoveDef.recipes.FirstOrDefault((RecipeDef def) => def.defName == "CookStrangeMealSimple") == null)
                    {
                        stoveDef.recipes.Add(DefDatabase<RecipeDef>.GetNamed("CookStrangeMealSimple"));
                    }
                    if (stoveDef.recipes.FirstOrDefault((RecipeDef def) => def.defName == "CookStrangeMealFine") == null)
                    {
                        stoveDef.recipes.Add(DefDatabase<RecipeDef>.GetNamed("CookStrangeMealFine"));
                    }
                    if (stoveDef.recipes.FirstOrDefault((RecipeDef def) => def.defName == "CookStrangeMealLavish") == null)
                    {
                        stoveDef.recipes.Add(DefDatabase<RecipeDef>.GetNamed("CookStrangeMealLavish"));
                    }
                    Log.Message("Strange meal recipes added to WoodStoveFurnace defs");
                }
            }
            return;
        }

        public override void ExposeData()
        {
            //Scribe_Collections.LookList<CosmicEntity>(ref this.DeityCache, "Deities", LookMode.Deep, new object[0]);
            //Scribe_Values.LookValue<bool>(ref this.AreRecipesReady, "AreRecipesReady", false, false);
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                GenerateStrangeMeatRecipe();
            }
        }

    }
}
