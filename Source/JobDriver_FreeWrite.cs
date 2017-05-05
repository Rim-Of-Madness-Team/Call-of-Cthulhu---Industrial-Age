using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using UnityEngine;
//using VerseBase;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;

namespace ArkhamEstate
{
    public class JobDriver_FreeWrite : JobDriver
    {
        private HediffDef sanityLossHediff;
        private string sanityLossString = "CosmicHorror_SanityLoss";
        private float sanityRestoreRate = 0.1f; 

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);
            yield return Toils_Reserve.Reserve(TargetIndex.A, base.CurJob.def.joyMaxParticipants);
            if (this.TargetB != null)
                yield return Toils_Reserve.Reserve(TargetIndex.B, 1);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            Toil toil = new Toil();
            toil.PlaySustainerOrSound(DefDatabase<SoundDef>.GetNamed("Estate_SoundManualTypewriter"));
            toil.tickAction = delegate
            {
                this.pawn.Drawer.rotator.FaceCell(this.TargetA.Cell);
                this.pawn.GainComfortFromCellIfPossible();
                float statValue = this.TargetThingA.GetStatValue(StatDefOf.EntertainmentStrengthFactor, true);
                float extraJoyGainFactor = statValue;
                JoyUtility.JoyTickCheckEnd(this.pawn, JoyTickFullJoyAction.EndJob, extraJoyGainFactor);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.defaultDuration = base.CurJob.def.joyDuration;
            toil.AddFinishAction(delegate
            {
                if (Cthulhu.Utility.IsCosmicHorrorsLoaded())
                {
                    try
                    {
                        sanityLossHediff = HediffDef.Named(sanityLossString);
                        if (pawn.health.hediffSet.HasHediff(sanityLossHediff))
                        {
                            HealthUtility.AdjustSeverity(this.pawn, sanityLossHediff, -sanityRestoreRate);
                            Messages.Message(this.pawn.ToString() + " has restored some sanity using the " + this.TargetA.Thing.def.label + ".", new TargetInfo(this.pawn.Position,this.pawn.Map), MessageSound.Standard);
                        }
                    }
                    catch
                    {
                        Log.Message("Error loading Sanity Hediff.");    
                    }
                }

                JoyUtility.TryGainRecRoomThought(this.pawn);
            });
            yield return toil;
            yield break;
        }
    }

}
