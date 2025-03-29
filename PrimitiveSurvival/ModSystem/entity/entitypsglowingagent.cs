namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Config;
    using PrimitiveSurvival.ModConfig;


    public class EntityPSGlowingAgent : EntityAgent
    {
        /*
        public override bool ApplyGravity => false;

        public override bool IsInteractable => false;
        */


        private byte[] lightHsv = new byte[] { 0, 0, 0 };

        public override byte[] LightHsv => this.lightHsv;

        public override void Initialize(EntityProperties properties, ICoreAPI api, long inChunkIndex3d)
        {
            base.Initialize(properties, api, inChunkIndex3d);
            this.lightHsv = properties.Attributes["lightHsv"].AsObject<byte[]>();
        }

        public override string GetInfoText()
        {
            var result = base.GetInfoText();
            if (ModConfig.Loaded.ShowModNameInHud)
            {
                result += "\n<font color=\"#D8EAA3\"><i>" + Lang.GetMatching("game:tabname-primitive") + "</i></font>\n\n";
            }
            return result;
        }
    }
}

