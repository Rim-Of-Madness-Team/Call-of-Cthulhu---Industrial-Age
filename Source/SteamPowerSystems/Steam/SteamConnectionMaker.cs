using System.Collections.Generic;
using RimWorld;
using Verse;
// ReSharper disable RedundantJumpStatement

namespace ArkhamEstate
{
	public static class SteamConnectionMaker
	{
		public static void ConnectAllConnectorsToTransmitter(CompSteam newTransmitter)
		{
			foreach (var compSteam in PotentialConnectorsForTransmitter(newTransmitter))
			{
				if (compSteam.connectParent == null)
				{
					compSteam.ConnectToTransmitter(newTransmitter);
				}
			}
		}

		public static void DisconnectAllFromTransmitterAndSetWantConnect(CompSteam deadPc, Map map)
		{
			if (deadPc.connectChildren == null)
			{
				return;
			}
			foreach (var compSteam in deadPc.connectChildren)
			{
				compSteam.connectParent = null;
				if (compSteam is CompSteamTrader compSteamTrader)
				{
					compSteamTrader.SteamOn = false;
				}
				map.GetComponent<SteamNetManager>().Notify_ConnectorWantsConnect(compSteam);
			}
		}

		public static void TryConnectToAnySteamNet(CompSteam pc, List<SteamNet> disallowedNets = null)
		{
			if (pc.connectParent != null)
			{
				return;
			}
			if (!pc.parent.Spawned)
			{
				return;
			}
			var compSteam = BestTransmitterForConnector(pc.parent.Position, pc.parent.Map, disallowedNets);
			if (compSteam != null)
			{
				pc.ConnectToTransmitter(compSteam);
			}
			else
			{
				pc.connectParent = null;
			}
		}

		public static void DisconnectFromSteamNet(CompSteam pc)
		{
			if (pc.connectParent == null)
			{
				return;
			}
			pc.SteamNet?.DeregisterConnector(pc);
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

		private static IEnumerable<CompSteam> PotentialConnectorsForTransmitter(CompSteam b)
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
						if (t?.TryGetComp<CompSteam>() is CompSteam w)
						{
							yield return w;
						}
					}
				}
			}
			yield break;
		}

		public static CompSteam BestTransmitterForConnector(IntVec3 connectorPos, Map map, List<SteamNet> disallowedNets = null)
		{
			var cellRect = CellRect.SingleCell(connectorPos).ExpandedBy(ConnectMaxDist).ClipInsideMap(map);
			cellRect.ClipInsideMap(map);
			var num = 999999f;
			CompSteam result = null;
			for (var i = cellRect.minZ; i <= cellRect.maxZ; i++)
			{
				for (var j = cellRect.minX; j <= cellRect.maxX; j++)
				{
					var c = new IntVec3(j, 0, i);
					var transmitter = c.GetSteamTransmitter(map);
					if (transmitter != null && !transmitter.Destroyed)
					{
						var steamComp = transmitter.TryGetComp<CompSteam>();
						if (steamComp != null && steamComp.TransmitsSteamNow && (transmitter.def.building == null || transmitter.def.building.allowWireConnection))
						{
							if (disallowedNets == null || !disallowedNets.Contains(steamComp.transNet))
							{
								var num2 = (float)(transmitter.Position - connectorPos).LengthHorizontalSquared;
								if (num2 < num)
								{
									num = num2;
									result = steamComp;
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
