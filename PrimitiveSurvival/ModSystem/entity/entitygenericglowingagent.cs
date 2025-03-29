namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using PrimitiveSurvival.ModConfig;


    public class EntityGenericGlowingAgent : EntityAgent
    {
        private float time;
        private double offset;
        private float strobeFrequency;
        private float minLightLevel;
        private float minLightValue;
        private byte[] lightHsv;
        private float strobeFrequencyWithOffset;
        Vec3d tmp = new Vec3d();

        public override byte[] LightHsv => this.lightHsv;

        public override void Initialize(EntityProperties properties, ICoreAPI api, long inChunkIndex3d)
        {
            base.Initialize(properties, api, inChunkIndex3d);
            var rnd = api.World.Rand.Next(0, 3);
            float d = rnd / 10;
            this.offset = this.SidedPos.X + this.SidedPos.Y + this.SidedPos.Z;
            this.strobeFrequency = properties.Attributes["strobeFrequency"].AsFloat();
            this.strobeFrequencyWithOffset = this.strobeFrequency + d;
            this.minLightLevel = properties.Attributes["minLightLevel"].AsFloat();
            this.lightHsv = properties.Attributes["lightHsv"].AsObject<byte[]>();
            if (this.lightHsv == null)
            { this.lightHsv = new byte[] { 1, 0, 4 }; } //arbitrarily give it some light
            this.minLightValue = this.lightHsv[2];
            
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

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            this.tmp.Set(this.ServerPos.X, this.ServerPos.Y + this.SelectionBox.Y1 + (this.SelectionBox.Y2 / 2), this.ServerPos.Z);
            var entities = this.World.GetEntitiesAround(this.tmp, 5.0f, 5.0f, null);
            if (entities.Length <= 1 && this.FirstCodePart() == "fireflies")
            { return; }
            if (this.strobeFrequency > 0f)
            {
                var rnd = this.Api.World.Rand.Next(0, entities.Length);
                float d = rnd / 10;

                this.time += dt * this.strobeFrequencyWithOffset - d;
                var timeoff = (float)Math.Abs(Math.Sin(this.time + this.offset));
                this.lightHsv[2] = (byte)((timeoff * this.minLightValue) + this.minLightLevel);
            }
        }
    }
}

