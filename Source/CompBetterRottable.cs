using RimWorld;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace IndustrialAge.Objects
{
    public class CompBetterRottable : CompRottable
    {
        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            switch (base.Stage)
            {
                case RotStage.Fresh:
                    sb.AppendLine("RotStateFresh".Translate());
                    break;
                case RotStage.Rotting:
                    sb.AppendLine("RotStateRotting".Translate());
                    break;
                case RotStage.Dessicated:
                    sb.AppendLine("RotStateDessicated".Translate());
                    break;
            }
            float num = (float) this.PropsRot.TicksToRotStart - base.RotProgress;
            if (num > 0f)
            {
                float num2 = GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, this.parent.Map);
                List<Thing> thingList = GridsUtility.GetThingList(this.parent.PositionHeld, this.parent.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i] is Building_Refrigerator)
                    {
                        Building_Refrigerator building_Refrigerator = thingList[i] as Building_Refrigerator;
                        num2 = building_Refrigerator.CurrentTemp;
                        break;
                    }
                }
                num2 = (float) Mathf.RoundToInt(num2);
                float num3 = GenTemperature.RotRateAtTemperature(num2);
                int ticksUntilRotAtCurrentTemp = base.TicksUntilRotAtCurrentTemp;
                if (num3 < 0.001f)
                {
                    sb.Append("CurrentlyFrozen".Translate() + ".");
                }
                else
                {
                    if (num3 < 0.999f)
                    {
                        sb.Append("CurrentlyRefrigerated".Translate(new object[]
                        {
                            ticksUntilRotAtCurrentTemp.ToStringTicksToPeriodVague()
                        }) + ".");
                    }
                    else
                    {
                        sb.Append("NotRefrigerated".Translate(new object[]
                        {
                            ticksUntilRotAtCurrentTemp.ToStringTicksToPeriodVague()
                        }) + ".");
                    }
                }
            }
            return sb.ToString().TrimEndNewlines();
        }

        public override void CompTickRare()
        {
            if (this.parent.MapHeld != null && this.parent.Map != null)
            {
                float rotProgress = this.RotProgress;
                float num = 1f;
                float temperatureForCell =
                    GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, this.parent.MapHeld);
                List<Thing> list = this.parent.MapHeld.thingGrid.ThingsListAtFast(this.parent.PositionHeld);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is Building_Refrigerator)
                    {
                        var bf = list[i] as Building_Refrigerator;
                        temperatureForCell = bf.CurrentTemp;
                        break;
                    }
                }

                num *= GenTemperature.RotRateAtTemperature(temperatureForCell);
                this.RotProgress += Mathf.Round(num * 250f);
                if (this.Stage == RotStage.Rotting && this.PropsRot.rotDestroys)
                {
                    if (this.parent.IsInAnyStorage() && this.parent.SpawnedOrAnyParentSpawned)
                    {
                        Messages.Message("MessageRottedAwayInStorage".Translate(new object[]
                            {
                                this.parent.Label
                            }).CapitalizeFirst(), new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false),
                            MessageTypeDefOf.NegativeEvent, true);
                        LessonAutoActivator.TeachOpportunity(ConceptDefOf.SpoilageAndFreezers,
                            OpportunityType.GoodToKnow);
                    }
                    this.parent.Destroy(DestroyMode.Vanish);
                    return;
                }
                bool flag = Mathf.FloorToInt(rotProgress / 60000f) != Mathf.FloorToInt(this.RotProgress / 60000f);
                if (flag && ShouldTakeRotDamage())
                {
                    if (this.Stage == RotStage.Rotting && this.PropsRot.rotDamagePerDay > 0f)
                    {
                        this.parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting,
                            (float) GenMath.RoundRandom(this.PropsRot.rotDamagePerDay), 0f, -1f, null, null, null,
                            DamageInfo.SourceCategory.ThingOrUnknown, null));
                    }
                    else if (this.Stage == RotStage.Dessicated && this.PropsRot.dessicatedDamagePerDay > 0f)
                    {
                        this.parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting,
                            (float) GenMath.RoundRandom(this.PropsRot.dessicatedDamagePerDay), 0f, -1f, null, null,
                            null, DamageInfo.SourceCategory.ThingOrUnknown, null));
                    }
                }
            }
        }

        private bool ShouldTakeRotDamage()
        {
            Thing thing = this.parent.ParentHolder as Thing;
            return thing == null || thing.def.category != ThingCategory.Building ||
                   !thing.def.building.preventDeteriorationInside;
        }
    }
}