using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ArkhamEstate
{
	public class WaterNetGrid : MapComponent
	{
		public WaterNetGrid(Map map) : base(map)
		{
			this.map = map;
			this.netGrid = new WaterNet[map.cellIndices.NumGridCells];
		}

		public WaterNet TransmittedWaterNetAt(IntVec3 c)
		{
			return this.netGrid[this.map.cellIndices.CellToIndex(c)];
		}

		public void Notify_WaterNetCreated(WaterNet newNet)
		{
			if (this.waterNetCells.ContainsKey(newNet))
			{
				Log.Warning("Net " + newNet + " is already registered in WaterNetGrid.");
				this.waterNetCells.Remove(newNet);
			}
			List<IntVec3> list = new List<IntVec3>();
			this.waterNetCells.Add(newNet, list);
			for (int i = 0; i < newNet.transmitters.Count; i++)
			{
				CellRect cellRect = newNet.transmitters[i].parent.OccupiedRect();
				for (int j = cellRect.minZ; j <= cellRect.maxZ; j++)
				{
					for (int k = cellRect.minX; k <= cellRect.maxX; k++)
					{
						int num = this.map.cellIndices.CellToIndex(k, j);
						if (this.netGrid[num] != null)
						{
							Log.Warning(string.Concat(new object[]
							{
								"Two water nets on the same cell (",
								k,
								", ",
								j,
								"). First transmitters: ",
								newNet.transmitters[0].parent.LabelCap,
								" and ",
								(!this.netGrid[num].transmitters.NullOrEmpty<CompWater>()) ? this.netGrid[num].transmitters[0].parent.LabelCap : "[none]",
								"."
							}));
						}
						this.netGrid[num] = newNet;
						list.Add(new IntVec3(k, 0, j));
					}
				}
			}
		}

		public void Notify_WaterNetDeleted(WaterNet deadNet)
		{
			List<IntVec3> list;
			if (!this.waterNetCells.TryGetValue(deadNet, out list))
			{
				Log.Warning("Net " + deadNet + " does not exist in WaterNetGrid's dictionary.");
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				int num = this.map.cellIndices.CellToIndex(list[i]);
				if (this.netGrid[num] == deadNet)
				{
					this.netGrid[num] = null;
				}
				else
				{
					Log.Warning("Multiple nets on the same cell " + list[i] + ". This is probably a result of an earlier error.");
				}
			}
			this.waterNetCells.Remove(deadNet);
		}

		public void DrawDebugWaterNetGrid()
		{
			if (!DebugViewSettings.drawPowerNetGrid)
			{
				return;
			}
			if (Current.ProgramState != ProgramState.Playing)
			{
				return;
			}
			if (this.map != Find.VisibleMap)
			{
				return;
			}
			Rand.PushState();
			foreach (IntVec3 c in Find.CameraDriver.CurrentViewRect.ClipInsideMap(this.map))
			{
				WaterNet waterNet = this.netGrid[this.map.cellIndices.CellToIndex(c)];
				if (waterNet != null)
				{
					Rand.Seed = waterNet.GetHashCode();
					CellRenderer.RenderCell(c, Rand.Value);
				}
			}
			Rand.PopState();
		}

		private Map map;

		private WaterNet[] netGrid;

		private Dictionary<WaterNet, List<IntVec3>> waterNetCells = new Dictionary<WaterNet, List<IntVec3>>();
	}
}
