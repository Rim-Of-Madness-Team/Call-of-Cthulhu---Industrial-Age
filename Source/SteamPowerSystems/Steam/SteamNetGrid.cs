using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ArkhamEstate
{
	public class SteamNetGrid : MapComponent
	{
		public SteamNetGrid(Map map) : base(map)
		{
			this.map = map;
			this.netGrid = new SteamNet[map.cellIndices.NumGridCells];
		}

		public SteamNet TransmittedSteamNetAt(IntVec3 c)
		{
			return this.netGrid[this.map.cellIndices.CellToIndex(c)];
		}

		public void Notify_SteamNetCreated(SteamNet newNet)
		{
			if (this.steamNetCells.ContainsKey(newNet))
			{
				Log.Warning("Net " + newNet + " is already registered in SteamNetGrid.");
				this.steamNetCells.Remove(newNet);
			}
			List<IntVec3> list = new List<IntVec3>();
			this.steamNetCells.Add(newNet, list);
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
								"Two steam nets on the same cell (",
								k,
								", ",
								j,
								"). First transmitters: ",
								newNet.transmitters[0].parent.LabelCap,
								" and ",
								(!this.netGrid[num].transmitters.NullOrEmpty<CompSteam>()) ? this.netGrid[num].transmitters[0].parent.LabelCap : "[none]",
								"."
							}));
						}
						this.netGrid[num] = newNet;
						list.Add(new IntVec3(k, 0, j));
					}
				}
			}
		}

		public void Notify_SteamNetDeleted(SteamNet deadNet)
		{
			List<IntVec3> list;
			if (!this.steamNetCells.TryGetValue(deadNet, out list))
			{
				Log.Warning("Net " + deadNet + " does not exist in SteamNetGrid's dictionary.");
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
			this.steamNetCells.Remove(deadNet);
		}

		public void DrawDebugSteamNetGrid()
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
				SteamNet steamNet = this.netGrid[this.map.cellIndices.CellToIndex(c)];
				if (steamNet != null)
				{
					Rand.Seed = steamNet.GetHashCode();
					CellRenderer.RenderCell(c, Rand.Value);
				}
			}
			Rand.PopState();
		}

		private Map map;

		private SteamNet[] netGrid;

		private Dictionary<SteamNet, List<IntVec3>> steamNetCells = new Dictionary<SteamNet, List<IntVec3>>();
	}
}
