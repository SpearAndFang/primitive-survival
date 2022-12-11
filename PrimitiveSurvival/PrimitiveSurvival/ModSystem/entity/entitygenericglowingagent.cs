namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    //using System.Diagnostics; 

    public class EntityGenericGlowingAgent : EntityAgent
    {
        private float time;
        private double offset;
        private float strobeFrequency;
        private float minLightLevel;
        private float minLightValue;
        private byte[] lightHsv;

        public override byte[] LightHsv => this.lightHsv;

        public override void Initialize(EntityProperties properties, ICoreAPI api, long inChunkIndex3d)
        {
            base.Initialize(properties, api, inChunkIndex3d);
            this.offset = this.SidedPos.X + this.SidedPos.Y + this.SidedPos.Z;
            this.strobeFrequency = properties.Attributes["strobeFrequency"].AsFloat();
            this.minLightLevel = properties.Attributes["minLightLevel"].AsFloat();
            this.lightHsv = properties.Attributes["lightHsv"].AsObject<byte[]>();
            if (this.lightHsv == null)
            { this.lightHsv = new byte[] { 1, 0, 4 }; } //arbitrarily give it some light
            this.minLightValue = this.lightHsv[2];
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (this.strobeFrequency > 0f)
            {
                this.time += dt * this.strobeFrequency;
                var timeoff = (float)Math.Abs(Math.Sin(this.time + this.offset));
                this.lightHsv[2] = (byte)((timeoff * this.minLightValue) + this.minLightLevel);
            }
        }
    }
}

