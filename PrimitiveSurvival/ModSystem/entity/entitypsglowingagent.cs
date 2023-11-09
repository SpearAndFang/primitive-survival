namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;

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
    }
}

