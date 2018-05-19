using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace ArkhamEstate
{
	public class WorkGiver_PatchLeaks : WorkGiver_Scanner
	{
		public override ThingRequest PotentialWorkThingRequest
		{
			get
			{
				return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
			}
		}

		public override PathEndMode PathEndMode
		{
			get
			{
				return PathEndMode.Touch;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			var tc = t as ThingWithComps;
			if (t == null) return false;
			var leakables = tc.AllComps.Where(x => x is ILeakable);
			var thingComps = leakables as ThingComp[] ?? leakables.ToArray();
			if (!thingComps.Any()) return false;
			var biggestLeak = thingComps.Max(y => ((ILeakable) y).CurLeakRate());
			if (biggestLeak < 5f)
			{
				return false;
			}
			bool flag = !forced;
			if (flag && biggestLeak < 1f)
			{
				return false;
			}
			if (!t.IsForbidden(pawn))
			{
				LocalTargetInfo target = t;
				if (pawn.CanReserve(target, 1, -1, null, forced))
				{
					if (t.Faction != pawn.Faction)
					{
						return false;
					}
					ThingWithComps thingWithComps = t as ThingWithComps;
					if (thingWithComps != null)
					{
						CompFlickable comp = thingWithComps.GetComp<CompFlickable>();
						if (comp != null && !comp.SwitchIsOn)
						{
							return false;
						}
					}
/*					if (this.FindBestFuel(pawn, t) == null)
					{
						ThingFilter fuelFilter = t.TryGetComp<CompRefuelable>().Props.fuelFilter;
						JobFailReason.Is("NoFuelToRefuel".Translate(new object[]
						{
							fuelFilter.Summary
						}));
						return false;
					}*/
					return true;
				}
			}
			return false;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return new Job(DefDatabase<JobDef>.GetNamedSilentFail("Estate_PatchLeaks"), t);
		}
	}
}
