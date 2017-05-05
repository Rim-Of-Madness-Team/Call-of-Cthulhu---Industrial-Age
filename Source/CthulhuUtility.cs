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
/// Last Update: 1/21/2017
/// </summary>
namespace Cthulhu
{
    public static class ModProps
    {
        public const string main = "Cthulhu";
        public const string mod = "Industrial Age";
        public const string version = "1.1.7";
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
        public const string SanityLossDef = "CosmicHorror_SanityLoss";
        public const string AltSanityLossDef = "Cults_SanityLoss";

        public static bool modCheck = false;
        public static bool loadedCosmicHorrors = false;
        public static bool loadedIndustrialAge = false;
        public static bool loadedCults = false;
        public static bool loadedFactions = false;


        public static bool IsMorning(Map map) { return GenLocalDate.HourInt(map) > 6 && GenLocalDate.HourInt(map) < 10; }
        public static bool IsEvening(Map map) { return GenLocalDate.HourInt(map) > 18 && GenLocalDate.HourInt(map) < 22; }
        public static bool IsNight(Map map) { return GenLocalDate.HourInt(map) > 22; }

        //[DefOf]
        //public static class PawnKindDefOf
        //{
        //    public static PawnKindDef DarkYoung;
        //}

        public static bool isCosmicHorror(Pawn thing)
        {
            if (!IsCosmicHorrorsLoaded()) return false;

            var type = Type.GetType("CosmicHorror.CosmicHorrorPawn");
            if (type != null)
            {
                if (thing.GetType() == type)
                {
                    return true;
                }
            }
            return false;
        }

        public static float GetSanityLossRate(PawnKindDef kindDef)
        {
            float sanityLossRate = 0f;
            if (kindDef.ToString() == "CosmicHorror_StarVampire")
                sanityLossRate = 0.04f;
            if (kindDef.ToString() == "StarSpawnOfCthulhu")
                sanityLossRate = 0.02f;
            if (kindDef.ToString() == "DarkYoung")
                sanityLossRate = 0.004f;
            if (kindDef.ToString() == "DeepOne")
                sanityLossRate = 0.008f;
            if (kindDef.ToString() == "DeepOneGreat")
                sanityLossRate = 0.012f;
            if (kindDef.ToString() == "MiGo")
                sanityLossRate = 0.008f;
            if (kindDef.ToString() == "Shoggoth")
                sanityLossRate = 0.012f;
            return sanityLossRate;
        }

        public static bool CapableOfViolence(Pawn pawn, bool allowDowned = false)
        {
            if (pawn == null) return false;
            if (pawn.Dead) return false;
            if (pawn.Downed && !allowDowned) return false;
            List<WorkTags> list = pawn.story.DisabledWorkTags.ToList<WorkTags>();
            if (list.Count == 0)
            {
                return true;
            }
            else
            {
                foreach (WorkTags current in list)
                {
                    if (current == WorkTags.Violent) return false;
                }
            }
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

        public static bool TryFindSpawnCell(ThingDef def, IntVec3 nearLoc, Map map, int maxDist, out IntVec3 pos)
        {
            return CellFinder.TryFindRandomCellNear(nearLoc, map, maxDist, delegate (IntVec3 x)
            {
                foreach (IntVec3 current in GenAdj.OccupiedRect(x, Rot4.North, new IntVec2(def.size.x + 2, def.size.z + 2)))
                {
                    if (!current.InBounds(map) || current.Fogged(map) || !current.Standable(map) || (current.Roofed(map) && current.GetRoof(map).isThickRoof))
                    {
                        return false;
                    }
                    if (!current.SupportsStructureType(map, def.terrainAffordanceNeeded))
                    {
                        return false;
                    }
                    bool canBeReached = true;
                    foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                    {
                        if (!colonist.CanReach(current, PathEndMode.ClosestTouch, Danger.Deadly)) canBeReached = false;
                    }
                    if (!canBeReached) return false;
                    List<Thing> thingList = current.GetThingList(map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        Thing thing = thingList[i];
                        if (thing.def.category != ThingCategory.Plant && GenSpawn.SpawningWipes(def, thing.def))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }, out pos);
        }

        public static BodyPartRecord GetHeart(HediffSet set)
        {
            foreach (BodyPartRecord current in set.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined))
            {
                for (int i = 0; i < current.def.Activities.Count; i++)
                {
                    if (current.def.Activities[i].First == PawnCapacityDefOf.BloodPumping)
                    {
                        return current;
                    }
                }
            }
            return null;
        }





        public static void SpawnThingDefOfCountAt(ThingDef of, int count, TargetInfo target)
        {
            while (count > 0)
            {
                Thing thing = ThingMaker.MakeThing(of, null);

                thing.stackCount = Math.Min(count, of.stackLimit);
                GenPlace.TryPlaceThing(thing, target.Cell, target.Map, ThingPlaceMode.Near);
                count -= thing.stackCount;
            }
        }

        public static void SpawnPawnsOfCountAt(PawnKindDef kindDef, IntVec3 at, Map map, int count, out Pawn returnable, Faction fac = null, bool berserk = false, bool target = false)
        {
            Pawn result = null;
            for (int i = 1; i <= count; i++)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(new TargetInfo(at, map))
                     where at.Walkable(map)
                     select cell).TryRandomElement(out at))
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(kindDef, fac);
                    if (result == null) result = pawn;
                    if (GenPlace.TryPlaceThing(pawn, at, map, ThingPlaceMode.Near, null))
                    {
                        //if (target) Map.GetComponent<MapComponent_SacrificeTracker>().lastLocation = at;
                        //continue;
                    }
                    //Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    if (berserk) pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                }
            }
            returnable = result;
        }

        public static void SpawnPawnsOfCountAt(PawnKindDef kindDef, IntVec3 at, Map map, int count, Faction fac = null, bool berserk = false, bool target = false)
        {
            for (int i = 1; i <= count; i++)
            {
                if ((from cell in GenAdj.CellsAdjacent8Way(new TargetInfo(at, map))
                     where at.Walkable(map)
                     select cell).TryRandomElement(out at))
                {

                    Pawn pawn = PawnGenerator.GeneratePawn(kindDef, fac);
                    if (GenPlace.TryPlaceThing(pawn, at, map, ThingPlaceMode.Near, null))
                    {
                        //if (target) Map.GetComponent<MapComponent_SacrificeTracker>().lastLocation = at;
                        //continue;
                    }
                    //Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    if (berserk) pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                }
            }
        }

        public static bool TryGetUnreservedPewSpot(Thing pew, Pawn claimer, out IntVec3 loc)
        {
            loc = IntVec3.Invalid;

            Map map = pew.Map;
            Rot4 currentDirection = pew.Rotation;

            IntVec3 CellNorth = pew.Position + GenAdj.CardinalDirections[Rot4.North.AsInt];
            IntVec3 CellSouth = pew.Position + GenAdj.CardinalDirections[Rot4.South.AsInt];
            IntVec3 CellEast = pew.Position + GenAdj.CardinalDirections[Rot4.East.AsInt];
            IntVec3 CellWest = pew.Position + GenAdj.CardinalDirections[Rot4.West.AsInt];

            if (!map.reservationManager.IsReserved(pew.Position, Faction.OfPlayer)) { loc = pew.Position; return true; }

            if (currentDirection == Rot4.North ||
                currentDirection == Rot4.South)
            {
                if (!map.reservationManager.IsReserved(CellWest, Faction.OfPlayer)) { loc = CellWest; return true; }
                if (!map.reservationManager.IsReserved(CellEast, Faction.OfPlayer)) { loc = CellEast; return true; }
            }
            if (currentDirection == Rot4.East ||
                currentDirection == Rot4.West)
            {
                if (!map.reservationManager.IsReserved(CellNorth, Faction.OfPlayer)) { loc = CellNorth; return true; }
                if (!map.reservationManager.IsReserved(CellSouth, Faction.OfPlayer)) { loc = CellSouth; return true; }
            }
            //map.reservationManager.Reserve(claimer, pew);
            return false;
        }


        public static void ChangeResearchProgress(ResearchProjectDef projectDef, float progressValue, bool deselectCurrentResearch = false)
        {
            FieldInfo researchProgressInfo = typeof(ResearchManager).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic);
            var researchProgress = researchProgressInfo.GetValue(Find.ResearchManager);
            PropertyInfo itemPropertyInfo = researchProgress.GetType().GetProperty("Item");
            itemPropertyInfo.SetValue(researchProgress, progressValue, new[] { projectDef });
            if (deselectCurrentResearch) Find.ResearchManager.currentProj = null;
            Find.ResearchManager.ReapplyAllMods();
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
        public static void ApplySanityLoss(Pawn pawn, float sanityLoss = 0.3f, float sanityLossMax = 1.0f)
        {
            if (pawn == null) return;
            string sanityLossDef;
            sanityLossDef = SanityLossDef;
            if (!IsCosmicHorrorsLoaded()) sanityLossDef = AltSanityLossDef;
            Hediff pawnSanityHediff = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed(sanityLossDef));
            float trueMax = sanityLossMax;
            if (pawnSanityHediff != null)
            {
                if (pawnSanityHediff.Severity > trueMax) trueMax = pawnSanityHediff.Severity;
                float result = pawnSanityHediff.Severity;
                result += sanityLoss;
                result = Mathf.Clamp(result, 0.0f, trueMax);
                pawnSanityHediff.Severity = result;
            }
            else
            {
                Hediff sanityLossHediff = HediffMaker.MakeHediff(DefDatabase<HediffDef>.GetNamed(sanityLossDef), pawn, null);

                sanityLossHediff.Severity = sanityLoss;
                pawn.health.AddHediff(sanityLossHediff, null, null);

            }
        }

        public static int GetSocialSkill(Pawn p)
        {
            return p.skills.GetSkill(SkillDefOf.Social).Level;
        }

        public static int GetResearchSkill(Pawn p)
        {
            return p.skills.GetSkill(SkillDefOf.Research).Level;
        }

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

        public static void TemporaryGoodwill(Faction faction, bool reset = false)
        {
            Faction playerFaction = Faction.OfPlayer;
            if (!reset)
            {
                if (faction.GoodwillWith(playerFaction) == 0f)
                {
                    faction.RelationWith(playerFaction, false).goodwill = faction.PlayerGoodwill;
                }

                faction.RelationWith(playerFaction, false).goodwill = 100f;
                faction.RelationWith(playerFaction, false).hostile = false;
            }
            else
            {
                faction.RelationWith(playerFaction, false).goodwill = 0f;
                faction.RelationWith(playerFaction, false).hostile = true;
            }
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

        public static string Prefix
        {
            get
            {
                return ModProps.main + " :: " + ModProps.mod + " " + ModProps.version + " :: ";
            }
        }

        public static void DebugReport(string x)
        {
            if (Prefs.DevMode && DebugSettings.godMode)
            {
                Log.Message(Prefix + x);
            }
        }

        public static void ErrorReport(string x)
        {
            Log.Error(Prefix + x);
        }


    }
}
