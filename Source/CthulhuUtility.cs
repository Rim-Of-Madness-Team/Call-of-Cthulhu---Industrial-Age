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
using System.Reflection;
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

/// <summary>
/// Utility File for use between Cthulhu mods.
/// Last Update: 5/5/2017
/// </summary>
namespace Cthulhu
{
    public static class ModProps
    {
        public const string main = "Industrial Age";
        public const string mod = "Objects and Furniture";
        public const string version = "1.19.0";
    }

    public static class SanityLossSeverity
    {
        public const float Initial = 0.1f;
        public const float Minor = 0.25f;
        public const float Major = 0.5f;
        public const float Severe = 0.7f;
        public const float Extreme = 0.95f;
    }

    static public class Utility
    {
        public enum SanLossSev { None = 0, Hidden, Initial, Minor, Major, Extreme };
        public const string SanityLossDef = "ROM_SanityLoss";
        public const string AltSanityLossDef = "Cults_SanityLoss";

        public static bool modCheck = false;
        public static bool loadedCosmicHorrors = false;
        public static bool loadedIndustrialAge = false;
        public static bool loadedCults = false;
        public static bool loadedFactions = false;


        public static bool IsMorning(Map map) =>
            GenLocalDate.HourInteger(map) > 6 && GenLocalDate.HourInteger(map) < 10; public static bool IsEvening(Map map) => GenLocalDate.HourInteger(map) > 18 && GenLocalDate.HourInteger(map) < 22; public static bool IsNight(Map map) => GenLocalDate.HourInteger(map) > 22;
        public static T GetMod<T>(string s) where T : Mod
        {
            //Call of Cthulhu - Cosmic Horrors
            T result = default(T);
            foreach (Mod ResolvedMod in LoadedModManager.ModHandles)
            {
                if (ResolvedMod.Content.Name == s) result = ResolvedMod as T;
            }
            return result;
        }

        public static bool IsCosmicHorror(Pawn thing)
        {
            if (!IsCosmicHorrorsLoaded()) return false;

            Type type = Type.GetType("CosmicHorror.CosmicHorrorPawn");
            if (type != null)
            {
                if (thing.GetType() == type)
                {
                    return true;
                }
            }
            return false;
        }

        //public static float GetSanityLossRate(PawnKindDef kindDef)
        //{
        //    float sanityLossRate = 0f;
        //    if (kindDef.ToString() == "ROM_StarVampire")
        //        sanityLossRate = 0.04f;
        //    if (kindDef.ToString() == "StarSpawnOfCthulhu")
        //        sanityLossRate = 0.02f;
        //    if (kindDef.ToString() == "DarkYoung")
        //        sanityLossRate = 0.004f;
        //    if (kindDef.ToString() == "DeepOne")
        //        sanityLossRate = 0.008f;
        //    if (kindDef.ToString() == "DeepOneGreat")
        //        sanityLossRate = 0.012f;
        //    if (kindDef.ToString() == "MiGo")
        //        sanityLossRate = 0.008f;
        //    if (kindDef.ToString() == "Shoggoth")
        //        sanityLossRate = 0.012f;
        //    return sanityLossRate;
        //}

        public static bool CapableOfViolence(Pawn pawn, bool allowDowned = false)
        {
            if (pawn == null) return false;
            if (pawn.Dead) return false;
            if (pawn.Downed && !allowDowned) return false;
            if (pawn.story.WorkTagIsDisabled(WorkTags.Violent)) return false;
            return true;
        }

        public static bool IsActorAvailable(Pawn preacher, bool downedAllowed = false)
        {
            StringBuilder s = new StringBuilder();
            s.Append("ActorAvailble Checks Initiated");
            s.AppendLine();
            if (preacher == null)
                return ResultFalseWithReport(s);
            s.Append("ActorAvailble: Passed null Check");
            s.AppendLine();
            //if (!preacher.Spawned)
            //    return ResultFalseWithReport(s);
            //s.Append("ActorAvailble: Passed not-spawned check");
            //s.AppendLine();
            if (preacher.Dead)
                return ResultFalseWithReport(s);
            s.Append("ActorAvailble: Passed not-dead");
            s.AppendLine();
            if (preacher.Downed && !downedAllowed)
                return ResultFalseWithReport(s);
            s.Append("ActorAvailble: Passed downed check & downedAllowed = " + downedAllowed.ToString());
            s.AppendLine();
            if (preacher.Drafted)
                return ResultFalseWithReport(s);
            s.Append("ActorAvailble: Passed drafted check");
            s.AppendLine();
            if (preacher.InAggroMentalState)
                return ResultFalseWithReport(s);
            s.Append("ActorAvailble: Passed drafted check");
            s.AppendLine();
            if (preacher.InMentalState)
                return ResultFalseWithReport(s);
            s.Append("ActorAvailble: Passed InMentalState check");
            s.AppendLine();
            s.Append("ActorAvailble Checks Passed");
            Cthulhu.Utility.DebugReport(s.ToString());
            return true;
        }

        public static bool ResultFalseWithReport(StringBuilder s)
        {
            s.Append("ActorAvailble: Result = Unavailable");
            Cthulhu.Utility.DebugReport(s.ToString());
            return false;
        }

        public static float CurrentSanityLoss(Pawn pawn)
        {
            string sanityLossDef;
            sanityLossDef = AltSanityLossDef;
            if (IsCosmicHorrorsLoaded()) sanityLossDef = SanityLossDef;

            Hediff pawnSanityHediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed(sanityLossDef));
            if (pawnSanityHediff != null)
            {
                return pawnSanityHediff.Severity;
            }
            return 0f;
        }


        public static void ApplyTaleDef(string defName, Map map)
        {
            Pawn randomPawn = map.mapPawns.FreeColonists.RandomElement<Pawn>();
            TaleDef taleToAdd = TaleDef.Named(defName);
            TaleRecorder.RecordTale(taleToAdd, new object[]
                    {
                        randomPawn,
                    });
        }

        public static void ApplyTaleDef(string defName, Pawn pawn)
        {
            TaleDef taleToAdd = TaleDef.Named(defName);
            if ((pawn.IsColonist || pawn.HostFaction == Faction.OfPlayer) && taleToAdd != null)
            {
                TaleRecorder.RecordTale(taleToAdd, new object[]
                {
                    pawn,
                });
            }
        }


        public static bool HasSanityLoss(Pawn pawn)
        {
            string sanityLossDef = (!IsCosmicHorrorsLoaded()) ? AltSanityLossDef : SanityLossDef;
            var pawnSanityHediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed(sanityLossDef));

            return pawnSanityHediff != null;
        }

        public static void ApplySanityLoss(Pawn pawn, float sanityLoss = 0.3f, float sanityLossMax = 1.0f)
        {
            if (pawn == null) return;
            string sanityLossDef = (!IsCosmicHorrorsLoaded()) ? AltSanityLossDef : SanityLossDef;

            var pawnSanityHediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed(sanityLossDef));
            if (pawnSanityHediff != null)
            {
                if (pawnSanityHediff.Severity > sanityLossMax) sanityLossMax = pawnSanityHediff.Severity;
                float result = pawnSanityHediff.Severity;
                result += sanityLoss;
                result = Mathf.Clamp(result, 0.0f, sanityLossMax);
                pawnSanityHediff.Severity = result;
            }
            else if (sanityLoss > 0)
            {
                Hediff sanityLossHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed(sanityLossDef), pawn, null);

                sanityLossHediff.Severity = sanityLoss;
                pawn.health.AddHediff(sanityLossHediff, null, null);

            }
        }


        public static int GetSocialSkill(Pawn p) => p.skills.GetSkill(SkillDefOf.Social).Level;

        public static int GetResearchSkill(Pawn p) => p.skills.GetSkill(SkillDefOf.Intellectual).Level;

        public static bool IsCosmicHorrorsLoaded()
        {

            if (!modCheck) ModCheck();
            return loadedCosmicHorrors;
        }


        public static bool IsIndustrialAgeLoaded()
        {
            if (!modCheck) ModCheck();
            return loadedIndustrialAge;
        }



        public static bool IsCultsLoaded()
        {
            if (!modCheck) ModCheck();
            return loadedCults;
        }

        public static bool IsRandomWalkable8WayAdjacentOf(IntVec3 cell, Map map, out IntVec3 resultCell)
        {
            if (cell != IntVec3.Invalid)
            {
                IntVec3 temp = cell.RandomAdjacentCell8Way();
                if (map != null)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        temp = cell.RandomAdjacentCell8Way();
                        if (temp.Walkable(map))
                        {
                            resultCell = temp;
                            return true;
                        }
                    }
                }
            }
            resultCell = IntVec3.Invalid;
            return false;
        }

        public static void ModCheck()
        {
            loadedCosmicHorrors = false;
            loadedIndustrialAge = false;
            foreach (ModContentPack ResolvedMod in LoadedModManager.RunningMods)
            {
                if (loadedCosmicHorrors && loadedIndustrialAge && loadedCults) break; //Save some loading
                if (ResolvedMod.Name.Contains("Call of Cthulhu - Cosmic Horrors"))
                {
                    DebugReport("Loaded - Call of Cthulhu - Cosmic Horrors");
                    loadedCosmicHorrors = true;
                }
                if (ResolvedMod.Name.Contains("Call of Cthulhu - Industrial Age"))
                {
                    DebugReport("Loaded - Call of Cthulhu - Industrial Age");
                    loadedIndustrialAge = true;
                }
                if (ResolvedMod.Name.Contains("Call of Cthulhu - Cults"))
                {
                    DebugReport("Loaded - Call of Cthulhu - Cults");
                    loadedCults = true;
                }
                if (ResolvedMod.Name.Contains("Call of Cthulhu - Factions"))
                {
                    DebugReport("Loaded - Call of Cthulhu - Factions");
                    loadedFactions = true;
                }
            }
            modCheck = true;
            return;
        }

        public static string Prefix => ModProps.main + " :: " + ModProps.mod + " " + ModProps.version + " :: ";

        public static void DebugReport(string x)
        {
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                Log.Message(Prefix + x);
            }
        }

        public static void ErrorReport(string x) => Log.Error(Prefix + x);


    }
}
