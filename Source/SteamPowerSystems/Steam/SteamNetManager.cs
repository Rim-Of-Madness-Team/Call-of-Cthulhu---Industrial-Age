using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArkhamEstate
{
	public class SteamNetManager : MapComponent
	{
		public SteamNetManager(Map map) : base(map)
		{
			this.map = map;
		}

		public List<SteamNet> AllNetsListForReading => allNets;

		public void Notify_TransmitterSpawned(CompSteam newTransmitter)
		{
			//string output = "Transmitter Spawned Called for: " + newTransmitter.parent.Label;
			//SteamUtility.PrintDebugMessage(output);
			delayedActions.Add(new DelayedAction(DelayedActionType.RegisterTransmitter, newTransmitter));
			NotifyDrawersForWireUpdate(newTransmitter.parent.Position);
		}

		public void Notify_TransmitterDespawned(CompSteam oldTransmitter)
		{
			delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterTransmitter, oldTransmitter));
			NotifyDrawersForWireUpdate(oldTransmitter.parent.Position);
		}

		public void Notfiy_TransmitterTransmitsPowerNowChanged(CompSteam transmitter)
		{
			if (!transmitter.parent.Spawned)
			{
				return;
			}
			delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterTransmitter, transmitter));
			delayedActions.Add(new DelayedAction(DelayedActionType.RegisterTransmitter, transmitter));
			NotifyDrawersForWireUpdate(transmitter.parent.Position);
		}

		public void Notify_ConnectorWantsConnect(CompSteam wantingCon)
		{
			if (Scribe.mode == LoadSaveMode.Inactive && !HasRegisterConnectorDuplicate(wantingCon))
			{
				delayedActions.Add(new DelayedAction(DelayedActionType.RegisterConnector, wantingCon));
			}
			NotifyDrawersForWireUpdate(wantingCon.parent.Position);
		}

		public void Notify_ConnectorDespawned(CompSteam oldCon)
		{
			delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterConnector, oldCon));
			NotifyDrawersForWireUpdate(oldCon.parent.Position);
		}

		public void NotifyDrawersForWireUpdate(IntVec3 root)
		{
			map.mapDrawer.MapMeshDirty(root, MapMeshFlag.Things, true, false);
			map.mapDrawer.MapMeshDirty(root, MapMeshFlag.PowerGrid, true, false);
		}

		public void RegisterSteamNet(SteamNet newNet)
		{
			allNets.Add(newNet);
			newNet.steamNetManager = this;
			map.GetComponent<SteamNetGrid>().Notify_SteamNetCreated(newNet);
			SteamNetMaker.UpdateVisualLinkagesFor(newNet);
		}

		public void DeleteSteamNet(SteamNet oldNet)
		{
			allNets.Remove(oldNet);
			map.GetComponent<SteamNetGrid>().Notify_SteamNetDeleted(oldNet);
		}

		public override void MapComponentTick()
		{
			if (Find.TickManager.TicksGame % 100 == 0)
				UpdateSteamNetsAndConnections_First();
			for (int i = 0; i < allNets.Count; i++)
			{
				allNets[i].SteamNetTick();
			}
		}

		public void UpdateSteamNetsAndConnections_First()
		{
			int count = delayedActions.Count;
			for (int i = 0; i < count; i++)
			{
				DelayedAction delayedAction = delayedActions[i];
				DelayedActionType type = delayedActions[i].type;
				if (type != DelayedActionType.RegisterTransmitter)
				{
					if (type == DelayedActionType.DeregisterTransmitter)
					{
						TryDestroyNetAt(delayedAction.position);
						SteamConnectionMaker.DisconnectAllFromTransmitterAndSetWantConnect(delayedAction.CompSteam, map);
						delayedAction.CompSteam.ResetSteamVars();
					}
				}
				else if (delayedAction.position == delayedAction.CompSteam.parent.Position)
				{
					ThingWithComps parent = delayedAction.CompSteam.parent;
					if (map.GetComponent<SteamNetGrid>().TransmittedSteamNetAt(parent.Position) != null)
					{
						//Log.Warning(string.Concat("Tried to register trasmitter ", parent, " at ", parent.Position, ", but there is already a power net here. There can't be two transmitters on the same cell."));
					}
					delayedAction.CompSteam.SetUpSteamVars();
					foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(parent))
					{
						TryDestroyNetAt(cell);
					}
				}
			}
			for (int j = 0; j < count; j++)
			{
				DelayedAction delayedAction2 = delayedActions[j];
				if ((delayedAction2.type == DelayedActionType.RegisterTransmitter && delayedAction2.position == delayedAction2.CompSteam.parent.Position) || delayedAction2.type == DelayedActionType.DeregisterTransmitter)
				{
					TryCreateNetAt(delayedAction2.position);
					foreach (IntVec3 cell2 in GenAdj.CellsAdjacentCardinal(delayedAction2.position, delayedAction2.rotation, delayedAction2.CompSteam.parent.def.size))
					{
						TryCreateNetAt(cell2);
					}
				}
			}
			for (int k = 0; k < count; k++)
			{
				DelayedAction delayedAction3 = delayedActions[k];
				DelayedActionType type2 = delayedActions[k].type;
				if (type2 != DelayedActionType.RegisterConnector)
				{
					if (type2 == DelayedActionType.DeregisterConnector)
					{
						SteamConnectionMaker.DisconnectFromSteamNet(delayedAction3.CompSteam);
						delayedAction3.CompSteam.ResetSteamVars();
					}
				}
				else if (delayedAction3.position == delayedAction3.CompSteam.parent.Position)
				{
					delayedAction3.CompSteam.SetUpSteamVars();
					SteamConnectionMaker.TryConnectToAnySteamNet(delayedAction3.CompSteam, null);
				}
			}
			delayedActions.RemoveRange(0, count);
			if (DebugViewSettings.drawPower)
			{
				DrawDebugSteamNets();
			}
		}

		private bool HasRegisterConnectorDuplicate(CompSteam CompSteam)
		{
			for (int i = delayedActions.Count - 1; i >= 0; i--)
			{
				if (delayedActions[i].CompSteam == CompSteam)
				{
					if (delayedActions[i].type == DelayedActionType.DeregisterConnector)
					{
						return false;
					}
					if (delayedActions[i].type == DelayedActionType.RegisterConnector)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void TryCreateNetAt(IntVec3 cell)
		{
			if (!cell.InBounds(map))
			{
				return;
			}
			if (map.GetComponent<SteamNetGrid>().TransmittedSteamNetAt(cell) == null)
			{
				Building transmitter = cell.GetSteamTransmitter(map);
				if (transmitter != null && transmitter.TransmitsSteamNow())
				{
					SteamNet steamNet = SteamNetMaker.NewSteamNetStartingFrom(transmitter);
					RegisterSteamNet(steamNet);
					for (int i = 0; i < steamNet.transmitters.Count; i++)
					{
						SteamConnectionMaker.ConnectAllConnectorsToTransmitter(steamNet.transmitters[i]);
					}
				}
			}
		}

		private void TryDestroyNetAt(IntVec3 cell)
		{
			if (!cell.InBounds(map))
			{
				return;
			}
			SteamNet steamNet = map.GetComponent<SteamNetGrid>().TransmittedSteamNetAt(cell);
			if (steamNet != null)
			{
				DeleteSteamNet(steamNet);
			}
		}

		private void DrawDebugSteamNets()
		{
			if (Current.ProgramState != ProgramState.Playing)
			{
				return;
			}
			if (Find.VisibleMap != map)
			{
				return;
			}
			int num = 0;
			foreach (SteamNet steamNet in allNets)
			{
				foreach (CompSteam CompSteam in steamNet.transmitters.Concat(steamNet.connectors))
				{
					foreach (IntVec3 c in GenAdj.CellsOccupiedBy(CompSteam.parent))
					{
						CellRenderer.RenderCell(c, num * 0.44f);
					}
				}
				num++;
			}
		}

		public Map map;

		private List<SteamNet> allNets = new List<SteamNet>();

		private List<DelayedAction> delayedActions = new List<DelayedAction>();

		private enum DelayedActionType
		{
			RegisterTransmitter,
			DeregisterTransmitter,
			RegisterConnector,
			DeregisterConnector
		}

		private struct DelayedAction
		{
			public DelayedAction(DelayedActionType type, CompSteam CompSteam)
			{
				this.type = type;
				this.CompSteam = CompSteam;
				position = CompSteam.parent.Position;
				rotation = CompSteam.parent.Rotation;
			}

			public DelayedActionType type;

			public CompSteam CompSteam;

			public IntVec3 position;

			public Rot4 rotation;
		}
	}
}
