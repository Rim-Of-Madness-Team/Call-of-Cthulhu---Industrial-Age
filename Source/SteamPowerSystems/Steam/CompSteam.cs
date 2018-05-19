using System;
using System.Collections.Generic;
using System.Security.Policy;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

// ReSharper disable RedundantJumpStatement

namespace ArkhamEstate
{
	public abstract class CompSteam : ThingComp
	{
		public bool TransmitsSteamNow => ((Building)parent).TransmitsSteamNow();

		public SteamNet SteamNet => transNet ?? connectParent?.transNet;

		public CompProperties_Steam Props => (CompProperties_Steam)props;

		public virtual void ResetSteamVars()
		{
			transNet = null;
			connectParent = null;
			connectChildren = null;
			recentlyConnectedNets.Clear();
			lastManualReconnector = null;
		}

		public virtual void SetUpSteamVars()
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
				connectParent = ((ThingWithComps)thing).GetComp<CompSteam>();
			}
			if (Scribe.mode == LoadSaveMode.PostLoadInit && connectParent != null)
			{
				ConnectToTransmitter(connectParent, true);
			}
		}



		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			if (Props.transmitsSteam || parent.def.ConnectsToSteam())
			{
				if (Props.transmitsSteam)
				{
					if (connectChildren != null)
					{
						foreach (var t in connectChildren)
						{
							t.LostConnectParent();
						}
					}
					map.GetComponent<SteamNetManager>().Notify_TransmitterDespawned(this);
				}
				if (parent.def.ConnectsToSteam())
				{
					map.GetComponent<SteamNetManager>().Notify_ConnectorDespawned(this);
				}
				map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, true, false);
			}
		}

		protected virtual void LostConnectParent()
		{
			connectParent = null;
			if (parent.Spawned)
			{
				parent.Map.GetComponent<SteamNetManager>().Notify_ConnectorWantsConnect(this);
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
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			if (Props.transmitsSteam || parent.def.ConnectsToSteam()) //|| this.parent.def.ConnectsToSteam())
			{
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, true, false);
				if (Props.transmitsSteam)
				{
					parent.Map.GetComponent<SteamNetManager>().Notify_TransmitterSpawned(this);
				}
				if (parent.def.ConnectsToSteam())
				{
					parent.Map.GetComponent<SteamNetManager>().Notify_ConnectorWantsConnect(this);
				}
				SetUpSteamVars();
			}
			parent.Map.GetComponent<SteamNetManager>().UpdateSteamNetsAndConnections_First();
			//this.steamSprayer = new IntermittentSteamSprayer(this.parent);
			//this.steamSprayer.startSprayCallback = new Action(this.StartSpray);
			//this.steamSprayer.endSprayCallback = new Action(this.EndSpray);
		}

		private int firstTicks = 100;
		public override void CompTick()
		{
			if (firstTicks > 0)
			{
				firstTicks--;
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things, true, false);				
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid, true, false);				
			}
			if (this?.SteamNet?.HasActiveSteamSource ?? false)
			{
				if (Props.ticksOnTime.RandomInRange > 0 && ticksOn < 0 && ticksOff < 0)
				{
					ticksOn = Props.ticksOnTime.RandomInRange;
					ticksOff = Props.ticksOffTime;
					this.spraySustainer = SoundDefOf.GeyserSpray.TrySpawnSustainer(new TargetInfo(this.parent.PositionHeld, this.parent.MapHeld, false));
					if (ticksOff > 100) 
						SoundDef.Named("Estate_SteamHiss").PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
					
				}
				if (ticksOn >= 0)
				{
					if (Rand.Value > 0.6f)
						MoteMaker.ThrowAirPuffUp(this.parent.TrueCenter() + Props.steamOffset, this.parent.Map);
					ticksOn--;
				}
				if (ticksOff >= 0)
				{
					ticksOff--;
					if (this.spraySustainer != null)
					{
						this.spraySustainer.End();
						this.spraySustainer = null;
					}
				}
			}
		}

		public void AddSteamTicks(int amt)
		{
			ticksOn += amt;
		}

		private int ticksOn = -1;
		private int ticksOff = -1;
		private Sustainer spraySustainer;

		public override void CompPrintForPowerGrid(SectionLayer layer)
		{
			if (TransmitsSteamNow)
			{
				SteamOverlayMats.LinkedOverlayGraphic.Print(layer, parent);
			}
			if (parent.def.ConnectsToSteam())
			{
				SteamOverlayMats.PrintOverlayConnectorBaseFor(layer, parent);
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
			if (SteamNet != null)
			{
				recentlyConnectedNets.Add(SteamNet);
			}
			var compSteam = SteamConnectionMaker.BestTransmitterForConnector(parent.Position, parent.Map, recentlyConnectedNets);
			if (compSteam == null)
			{
				recentlyConnectedNets.Clear();
				compSteam = SteamConnectionMaker.BestTransmitterForConnector(parent.Position, parent.Map);
			}
			if (compSteam != null)
			{
				SteamConnectionMaker.DisconnectFromSteamNet(this);
				ConnectToTransmitter(compSteam);
				for (var i = 0; i < 5; i++)
				{
					MoteMaker.ThrowMetaPuff(compSteam.parent.Position.ToVector3Shifted(), compSteam.parent.Map);
				}
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.PowerGrid);
				parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);
			}
		}

		public void ConnectToTransmitter(CompSteam transmitter, bool reconnectingAfterLoading = false)
		{
			//SteamUtility.PrintDebugMessage("ConnectToTransmitter Called");
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
				connectParent.connectChildren = new List<CompSteam>();
			}
			transmitter.connectChildren.Add(this);
			//SteamUtility.PrintDebugMessage(transmitter.parent.Label);
			//foreach (var v in transmitter.connectChildren)
				//SteamUtility.PrintDebugMessage("->" + v.parent.Label);
			SteamNet?.RegisterConnector(this);
		}

		public override string CompInspectStringExtra()
		{
			if (SteamNet == null)
			{
				return "Estate_SteamNotConnected".Translate();
			}
			var text = (SteamNet.CurrentSteamGainRate() / PsiPerTick).ToString("F0");
			var text2 = SteamNet.CurrentStoredSteam().ToString("F0");
			var text3 = "Estate_SteamConnectedRateStored".Translate(new object[]
			{
				text,
				text2
			});
			if (ToxicGasGenPct > 0f)
			{
				var text4 = "Estate_ToxicGasPercentStored".Translate(new object[]
				{
					ToxicGasGenPct.ToStringPercent(),
					SteamNet.CurrentStoredToxicGas().ToString("F0")
				});
				return text3 + "\n" + text4;
			}
			if (ToxicGasVentRate > 0f)
			{
				var text4 = "Estate_ToxicGasConnectedRateStored".Translate(new object[]
				{
					(SteamNet.CurrentToxicGasRate() / ToxicGasVentRate).ToString("F0"),
					SteamNet.CurrentStoredToxicGas().ToString("F0")
				});
				return text3 + "\n" + text4;
			}
			return text3;

		}

		public SteamNet transNet;

		public CompSteam connectParent;

		public List<CompSteam> connectChildren;

		private static List<SteamNet> recentlyConnectedNets = new List<SteamNet>();

		private static CompSteam lastManualReconnector = null;

		public static readonly float PsiPerTick = 0.00015f;

		public float ToxicGasVentRate => Props.toxicGasVentRate;

		public float ToxicGasGenPct => Props.toxicGasGenPct;
	}
}
