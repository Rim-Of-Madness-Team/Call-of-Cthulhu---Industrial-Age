using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

// ReSharper disable RedundantJumpStatement

namespace ArkhamEstate
{
	public abstract class CompWater : ThingComp
	{
		public bool TransmitsWaterNow => ((Building)parent).TransmitsWaterNow();

		public WaterNet WaterNet => transNet ?? connectParent?.transNet;

		public CompProperties_Water Props => (CompProperties_Water)props;

		public virtual void ResetWaterVars()
		{
			transNet = null;
			connectParent = null;
			connectChildren = null;
			recentlyConnectedNets.Clear();
			lastManualReconnector = null;
		}

		public virtual void SetUpWaterVars()
		{
		}

		public override void PostExposeData()
		{
			Thing thing = null;
			if (Scribe.mode == LoadSaveMode.Saving && connectParent != null)
			{
				thing = connectParent.parent;
			}
			Scribe_References.Look(ref thing, "parentThing", false);
			if (thing != null)
			{
				connectParent = ((ThingWithComps)thing).GetComp<CompWater>();
			}
			if (Scribe.mode == LoadSaveMode.PostLoadInit && connectParent != null)
			{
				ConnectToTransmitter(connectParent, true);
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (Props.transmitsWater || parent.def.ConnectsToWater()) //|| this.parent.def.ConnectsToWater())
			{
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, true, false);
				if (Props.transmitsWater)
				{
					parent.Map.GetComponent<WaterNetManager>().Notify_TransmitterSpawned(this);
				}
				if (parent.def.ConnectsToWater())
				{
					parent.Map.GetComponent<WaterNetManager>().Notify_ConnectorWantsConnect(this);
				}
				SetUpWaterVars();
			}
			
			parent.Map.GetComponent<WaterNetManager>().UpdateWaterNetsAndConnections_First();
		}

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			if (Props.transmitsWater || parent.def.ConnectsToWater())
			{
				if (Props.transmitsWater)
				{
					if (connectChildren != null)
					{
						foreach (var t in connectChildren)
						{
							t.LostConnectParent();
						}
					}
					map.GetComponent<WaterNetManager>().Notify_TransmitterDespawned(this);
				}
				if (parent.def.ConnectsToWater())
				{
					map.GetComponent<WaterNetManager>().Notify_ConnectorDespawned(this);
				}
				map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, true, false);
			}
		}

		protected virtual void LostConnectParent()
		{
			connectParent = null;
			if (parent.Spawned)
			{
				parent.Map.GetComponent<WaterNetManager>().Notify_ConnectorWantsConnect(this);
			}
		}

		public override void PostPrintOnto(SectionLayer layer)
		{
			base.PostPrintOnto(layer);
//			if (this.connectParent != null)
//			{
//				PowerNetGraphics.PrintWirePieceConnecting(layer, this.parent, this.connectParent.parent, false);
//			}
		}

		private int firstTicks = 100;
		public override void CompTick()
		{
			base.CompTick();
			if (firstTicks > 0)
			{
				firstTicks--;
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things, true, false);				
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, true, false);				
			}
		}

		public override void CompPrintForPowerGrid(SectionLayer layer)
		{
			if (TransmitsWaterNow)
			{
				WaterOverlayMats.LinkedOverlayGraphic.Print(layer, parent);
			}
			if (parent.def.ConnectsToWater())
			{
				WaterOverlayMats.PrintOverlayConnectorBaseFor(layer, parent);
			}
//			if (this.connectParent != null)
//			{
//				PowerNetGraphics.PrintWirePieceConnecting(layer, this.parent, this.connectParent.parent, true);
//			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (var c in base.CompGetGizmosExtra())
			{
				yield return c;
			}
			if (connectParent != null && parent.Faction == Faction.OfPlayer)
			{
				yield return new Command_Action
				{
					action = delegate
					{
						SoundDefOf.TickTiny.PlayOneShotOnCamera();
						TryManualReconnect();
					},
					hotKey = KeyBindingDefOf.Misc1,
					defaultDesc = "CommandTryReconnectDesc".Translate(),
					icon = ContentFinder<Texture2D>.Get("UI/Commands/TryReconnect"),
					defaultLabel = "CommandTryReconnectLabel".Translate()
				};
			}
			yield break;
		}

		private void TryManualReconnect()
		{
			if (lastManualReconnector != this)
			{
				recentlyConnectedNets.Clear();
				lastManualReconnector = this;
			}
			if (WaterNet != null)
			{
				recentlyConnectedNets.Add(WaterNet);
			}
			var compWater = WaterConnectionMaker.BestTransmitterForConnector(parent.Position, parent.Map, recentlyConnectedNets);
			if (compWater == null)
			{
				recentlyConnectedNets.Clear();
				compWater = WaterConnectionMaker.BestTransmitterForConnector(parent.Position, parent.Map);
			}
			if (compWater != null)
			{
				WaterConnectionMaker.DisconnectFromWaterNet(this);
				ConnectToTransmitter(compWater);
				for (var i = 0; i < 5; i++)
				{
					MoteMaker.ThrowMetaPuff(compWater.parent.Position.ToVector3Shifted(), compWater.parent.Map);
				}
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid);
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);
			}
		}

		public void ConnectToTransmitter(CompWater transmitter, bool reconnectingAfterLoading = false)
		{
			//WaterUtility.PrintDebugMessage("ConnectToTransmitter Called");
			if (connectParent != null && (!reconnectingAfterLoading || connectParent != transmitter))
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to connect ",
					this,
					" to transmitter ",
					transmitter,
					" but it's already connected to ",
					connectParent,
					"."
				}));
				return;
			}
			connectParent = transmitter;
			if (connectParent.connectChildren == null)
			{
				connectParent.connectChildren = new List<CompWater>();
			}
			transmitter.connectChildren.Add(this);
			//WaterUtility.PrintDebugMessage(transmitter.parent.Label);
			//foreach (var v in transmitter.connectChildren)
				//WaterUtility.PrintDebugMessage("->" + v.parent.Label);
			WaterNet?.RegisterConnector(this);
		}

		public override string CompInspectStringExtra()
		{
			if (WaterNet == null)
			{
				return "Estate_WaterNotConnected".Translate();
			}
			var text = (WaterNet.CurrentWaterGainRate() / LitersPerTick).ToString("F0");
			var text2 = WaterNet.CurrentStoredWater().ToString("F0");
			return "Estate_WaterConnectedRateStored".Translate(new object[]
			{
				text,
				text2
			});
		}

		public WaterNet transNet;

		public CompWater connectParent;

		public List<CompWater> connectChildren;

		private static List<WaterNet> recentlyConnectedNets = new List<WaterNet>();

		private static CompWater lastManualReconnector = null;

		public static readonly float LitersPerTick = 0.000012f;
	}
}
