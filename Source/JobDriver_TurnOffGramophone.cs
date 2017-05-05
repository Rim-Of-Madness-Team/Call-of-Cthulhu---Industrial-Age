using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
//using VerseBase;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
//using RimWorld.Planet;
//using RimWorld.SquadAI;


namespace ArkhamEstate
{

    public class JobDriver_TurnOffGramophone : JobDriver
    {

        private string report = "";
        public override string GetReport()
        {
            if (report != "")
            {
                return base.ReportStringProcessed(report);
            }
            return base.GetReport();
        }

        //What should we do?
        protected override IEnumerable<Toil> MakeNewToils()
        {
            //Check it out. Can we go there?
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            if (this.CurJob.targetA.Thing is Building_Radio) report = "Turning off radio."; 

            // Toil 1:
            // Reserve Target (TargetPack A is selected (It has the info where the target cell is))
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1);

            // Toil 2:
            // Go to the thing.
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            
            // Toil 3:
            // Turn off music.

            Toil toilStopMusic = new Toil();
            toilStopMusic.defaultCompleteMode = ToilCompleteMode.Instant;
            toilStopMusic.initAction = delegate
            {
                Building_Gramophone gramophone = this.CurJob.targetA.Thing as Building_Gramophone;
                gramophone.StopMusic();
            };
            yield return toilStopMusic;

            yield break;
        }
    }
}