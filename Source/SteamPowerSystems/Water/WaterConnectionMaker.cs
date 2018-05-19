using System.Collections.Generic;
using RimWorld;
using Verse;
// ReSharper disable RedundantJumpStatement

namespace ArkhamEstate
{
	public static class WaterConnectionMaker
	{
		public static void ConnectAllConnectorsToTransmitter(CompWater newTransmitter)
		{
			foreach (var compWater in PotentialConnectorsForTransmitter(newTransmitter))
			{
				if (compWater.connectParent == null)
				{
					compWater.ConnectToTransmitter(newTransmitter);
				}
			}
		}

		public static void DisconnectAllFromTransmitterAndSetWantConnect(CompWater deadPc, Map map)
		{
			if (deadPc.connectChildren == null)
			{
				return;
			}
			foreach (var compWater in deadPc.connectChildren)
			{
				compWater.connectParent = null;
				if (compWater is CompWaterTrader compWaterTrader)
				{
					compWaterTrader.WaterOn = false;
				}
				map.GetComponent<WaterNetManager>().Notify_ConnectorWantsConnect(compWater);
			}
		}

		public static void TryConnectToAnyWaterNet(CompWater pc, List<WaterNet> disallowedNets = null)
		{
			if (pc.connectParent != null)
			{
				return;
			}
			if (!pc.parent.Spawned)
			{
				return;
			}
			var compWater = BestTransmitterForConnector(pc.parent.Position, pc.parent.Map, disallowedNets);
			if (compWater != null)
			{
				pc.ConnectToTransmitter(compWater);
			}
			else
			{
				pc.connectParent = null;
			}
		}

		public static void DisconnectFromWaterNet(CompWater pc)
		{
			if (pc.connectParent == null)
			{
				return;
			}
			pc.WaterNet?.DeregisterConnector(pc);
			if (pc.connectParent.connectChildren != null)
			{
				pc.connectParent.connectChildren.Remove(pc);
				if (pc.connectParent.connectChildren.Count == 0)
				{
					pc.connectParent.connectChildren = null;
				}
			}
			pc.connectParent = null;
		}

		private static IEnumerable<CompWater> PotentialConnectorsForTransmitter(CompWater b)
		{
			if (!b.parent.Spawned)
			{
				Log.Warning("Can't check potential connectors for " + b + " because it's unspawned.");
				yield break;
			}
			var rect = b.parent.OccupiedRect().ExpandedBy(ConnectMaxDist).ClipInsideMap(b.parent.Map);
			for (var z = rect.minZ; z <= rect.maxZ; z++)
			{
				for (var x = rect.minX; x <= rect.maxX; x++)
				{
					var c = new IntVec3(x, 0, z);
					var thingList = b.parent.Map.thingGrid.ThingsListAt(c);
					foreach (var t in thingList)
					{
						if (t?.TryGetComp<CompWater>() is CompWater w)
						{
							yield return w;
						}
					}
				}
			}
			yield break;
		}

		public static CompWater BestTransmitterForConnector(IntVec3 connectorPos, Map map, List<WaterNet> disallowedNets = null)
		{
			var cellRect = CellRect.SingleCell(connectorPos).ExpandedBy(ConnectMaxDist).ClipInsideMap(map);
			cellRect.ClipInsideMap(map);
			var num = 999999f;
			CompWater result = null;
			for (var i = cellRect.minZ; i <= cellRect.maxZ; i++)
			{
				for (var j = cellRect.minX; j <= cellRect.maxX; j++)
				{
					var c = new IntVec3(j, 0, i);
					var transmitter = c.GetWaterTransmitter(map);
					if (transmitter != null && !transmitter.Destroyed)
					{
						var waterComp = transmitter.TryGetComp<CompWater>();
						if (waterComp != null && waterComp.TransmitsWaterNow && (transmitter.def.building == null || transmitter.def.building.allowWireConnection))
						{
							if (disallowedNets == null || !disallowedNets.Contains(waterComp.transNet))
							{
								var num2 = (float)(transmitter.Position - connectorPos).LengthHorizontalSquared;
								if (num2 < num)
								{
									num = num2;
									result = waterComp;
								}
							}
						}
					}
				}
			}
			return result;
		}

		private const int ConnectMaxDist = 1;
	}
}
