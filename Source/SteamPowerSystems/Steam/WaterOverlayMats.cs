using UnityEngine;
using Verse;

namespace ArkhamEstate
{
    [StaticConstructorOnStartup]
    public static class SteamOverlayMats
    {
        static SteamOverlayMats()
        {
            if (Estate_SteamAtlasGraphic == null)
            {
                var graphicData = new GraphicData();
                graphicData.graphicClass = typeof(Graphic_Single);
                graphicData.texPath = "Things/Special/Power/Estate_SteamTransmitterAtlas";
                graphicData.linkFlags = LinkFlags.Custom2;
                graphicData.shaderType = ShaderType.MetaOverlay;
                Estate_SteamAtlasGraphic = graphicData.Graphic;
            }
            Graphic graphic = Estate_SteamAtlasGraphic;
            SteamOverlayMats.LinkedOverlayGraphic = GraphicUtility.WrapLinked(graphic, LinkDrawerType.TransmitterOverlay);
            graphic.data.linkFlags = LinkFlags.Custom2;
            graphic.MatSingle.renderQueue = 3600;
            SteamOverlayMats.MatConnectorBase.renderQueue = 3600;
            SteamOverlayMats.MatConnectorLine.renderQueue = 3600;
        }
        public static readonly Texture2D Estate_SteamAtlas = ContentFinder<Texture2D>.Get("Things/Special/Power/Estate_SteamTransmitterAtlas", true);


        private static Graphic Estate_SteamAtlasGraphic = null; 
        
        public static void PrintOverlayConnectorBaseFor(SectionLayer layer, Thing t)
        {
            Vector3 center = t.TrueCenter();
            center.y = Altitudes.AltitudeFor(AltitudeLayer.MapDataOverlay);
            Printer_Plane.PrintPlane(layer, center, new Vector2(1f, 1f), SteamOverlayMats.MatConnectorBase, 0f, false, null, null, 0.01f);
        }


        private const string TransmitterAtlasPath = "Things/Special/Power/Estate_SteamTransmitterAtlas";

        private static readonly Shader TransmitterShader = ShaderDatabase.MetaOverlay;

        public static readonly Graphic LinkedOverlayGraphic;

        public static readonly Material MatConnectorBase = MaterialPool.MatFrom("Things/Special/Power/Estate_SteamOverlayBase", ShaderDatabase.MetaOverlay);

        public static readonly Material MatConnectorLine = MaterialPool.MatFrom("Things/Special/Power/OverlayWire", ShaderDatabase.MetaOverlay);

        public static readonly Material MatConnectorAnticipated = MaterialPool.MatFrom("Things/Special/Power/OverlayWireAnticipated", ShaderDatabase.MetaOverlay);

        public static readonly Material MatConnectorBaseAnticipated = MaterialPool.MatFrom("Things/Special/Power/OverlayBaseAnticipated", ShaderDatabase.MetaOverlay);
    }
}
