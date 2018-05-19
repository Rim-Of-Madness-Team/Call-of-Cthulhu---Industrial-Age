using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArkhamEstate
{
	public class WaterNetManager : MapComponent
	{
		public WaterNetManager(Map map) : base(map)
		{
			this.map = map;
		}

		public List<WaterNet> AllNetsListForReading => allNets;

		public void Notify_TransmitterSpawned(CompWater newTransmitter)
		{
			//string output = "Transmitter Spawned Called for: " + newTransmitter.parent.Label;
			//WaterUtility.PrintDebugMessage(output);
			delayedActions.Add(new DelayedAction(DelayedActionType.RegisterTransmitter, newTransmitter));
			NotifyDrawersForWireUpdate(newTransmitter.parent.Position);
		}

		public void Notify_TransmitterDespawned(CompWater oldTransmitter)
		{
			delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterTransmitter, oldTransmitter));
			NotifyDrawersForWireUpdate(oldTransmitter.parent.Position);
		}

		public void Notify_TransmitterTransmitsWaterNowChanged(CompWater transmitter)
		{
			if (!transmitter.parent.Spawned)
			{
				return;
			}
			delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterTransmitter, transmitter));
			delayedActions.Add(new DelayedAction(DelayedActionType.RegisterTransmitter, transmitter));
			NotifyDrawersForWireUpdate(transmitter.parent.Position);
		}

		public void Notify_ConnectorWantsConnect(CompWater wantingCon)
		{
			if (Scribe.mode == LoadSaveMode.Inactive && !HasRegisterConnectorDuplicate(wantingCon))
			{
				delayedActions.Add(new DelayedAction(DelayedActionType.RegisterConnector, wantingCon));
			}
			NotifyDrawersForWireUpdate(wantingCon.parent.Position);
		}

		public void Notify_ConnectorDespawned(CompWater oldCon)
		{
			delayedActions.Add(new DelayedAction(DelayedActionType.DeregisterConnector, oldCon));
			NotifyDrawersForWireUpdate(oldCon.parent.Position);
		}

		public void NotifyDrawersForWireUpdate(IntVec3 root)
		{
			map.mapDrawer.MapMeshDirty(root, MapMeshFlag.Things, true, false);
			map.mapDrawer.MapMeshDirty(root, MapMeshFlag.PowerGrid, true, false);
		}

		public void RegisterWaterNet(WaterNet newNet)
		{
			allNets.Add(newNet);
			newNet.waterNetManager = this;
			map.GetComponent<WaterNetGrid>().Notify_WaterNetCreated(newNet);
			WaterNetMaker.UpdateVisualLinkagesFor(newNet);
		}

		public void DeleteWaterNet(WaterNet oldNet)
		{
			allNets.Remove(oldNet);
			map.GetComponent<WaterNetGrid>().Notify_WaterNetDeleted(oldNet);
		}

		public override void MapComponentTick()
		{
			if (Find.TickManager.TicksGame % 100 == 0)
				UpdateWaterNetsAndConnections_First();
			for (int i = 0; i < allNets.Count; i++)
			{
				allNets[i].WaterNetTick();
			}
		}

		public void UpdateWaterNetsAndConnections_First()
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
						WaterConnectionMaker.DisconnectAllFromTransmitterAndSetWantConnect(delayedAction.CompWater, map);
						delayedAction.CompWater.ResetWaterVars();
					}
				}
				else if (delayedAction.position == delayedAction.CompWater.parent.Position)
				{
					ThingWithComps parent = delayedAction.CompWater.parent;
					if (map.GetComponent<WaterNetGrid>().TransmittedWaterNetAt(parent.Position) != null)
					{
						Log.Warning(string.Concat("Tried to register trasmitter ", parent, " at ", parent.Position, ", but there is already a power net here. There can't be two transmitters on the same cell."));
					}
					delayedAction.CompWater.SetUpWaterVars();
					foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(parent))
					{
						TryDestroyNetAt(cell);
					}
				}
			}
			for (int j = 0; j < count; j++)
			{
				DelayedAction delayedAction2 = delayedActions[j];
				if ((delayedAction2.type == DelayedActionType.RegisterTransmitter && delayedAction2.position == delayedAction2.CompWater.parent.Position) || delayedAction2.type == DelayedActionType.DeregisterTransmitter)
				{
					TryCreateNetAt(delayedAction2.position);
					foreach (IntVec3 cell2 in GenAdj.CellsAdjacentCardinal(delayedAction2.position, delayedAction2.rotation, delayedAction2.CompWater.parent.def.size))
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
						WaterConnectionMaker.DisconnectFromWaterNet(delayedAction3.CompWater);
						delayedAction3.CompWater.ResetWaterVars();
					}
				}
				else if (delayedAction3.position == delayedAction3.CompWater.parent.Position)
				{
					delayedAction3.CompWater.SetUpWaterVars();
					WaterConnectionMaker.TryConnectToAnyWaterNet(delayedAction3.CompWater, null);
				}
			}
			delayedActions.RemoveRange(0, count);
			if (DebugViewSettings.drawPower)
			{
				DrawDebugWaterNets();
			}
		}

		private bool HasRegisterConnectorDuplicate(CompWater CompWater)
		{
			for (int i = delayedActions.Count - 1; i >= 0; i--)
			{
				if (delayedActions[i].CompWater == CompWater)
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
			if (map.GetComponent<WaterNetGrid>().TransmittedWaterNetAt(cell) == null)
			{
				Building transmitter = cell.GetWaterTransmitter(map);
				if (transmitter != null && transmitter.TransmitsWaterNow())
				{
					WaterNet waterNet = WaterNetMaker.NewWaterNetStartingFrom(transmitter);
					RegisterWaterNet(waterNet);
					for (int i = 0; i < waterNet.transmitters.Count; i++)
					{
						WaterConnectionMaker.ConnectAllConnectorsToTransmitter(waterNet.transmitters[i]);
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
			WaterNet waterNet = map.GetComponent<WaterNetGrid>().TransmittedWaterNetAt(cell);
			if (waterNet != null)
			{
				DeleteWaterNet(waterNet);
			}
		}

		private void DrawDebugWaterNets()
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
			foreach (WaterNet waterNet in allNets)
			{
				foreach (CompWater CompWater in waterNet.transmitters.Concat(waterNet.connectors))
				{
					foreach (IntVec3 c in GenAdj.CellsOccupiedBy(CompWater.parent))
					{
						CellRenderer.RenderCell(c, num * 0.44f);
					}
				}
				num++;
			}
		}

		public Map map;

		private List<WaterNet> allNets = new List<WaterNet>();

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
			public DelayedAction(DelayedActionType type, CompWater CompWater)
			{
				this.type = type;
				this.CompWater = CompWater;
				position = CompWater.parent.Position;
				rotation = CompWater.parent.Rotation;
			}

			public DelayedActionType type;

			public CompWater CompWater;

			public IntVec3 position;

			public Rot4 rotation;
		}
	}
}
