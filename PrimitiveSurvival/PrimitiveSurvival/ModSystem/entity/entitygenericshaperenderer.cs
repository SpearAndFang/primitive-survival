namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Config;
    using Vintagestory.GameContent;
    //using System.Diagnostics;


    public class EntityGenericShapeRenderer : EntityShapeRenderer
    {
        private readonly PrimitiveSurvivalSystem modSystem;
        private double time = 0;
        private readonly float strobeFrequency;
        private readonly double offset;

        public EntityGenericShapeRenderer(Entity entity, ICoreClientAPI api) : base(entity, api)
        {
            api.Event.ReloadTextures += () =>
            this.capi = api;
            this.modSystem = api.ModLoader.GetModSystem<PrimitiveSurvivalSystem>();


            //initialize lighting
            this.offset = entity.SidedPos.X + entity.SidedPos.Y + entity.SidedPos.Z;
            this.strobeFrequency = this.entity.Properties.Attributes["strobeFrequency"].AsFloat();
        }

        public override void DoRender3DOpaqueBatched(float dt, bool isShadowPass)
        {
            // Do nothing
            // base.DoRender3DOpaqueBatched(dt, isShadowPass);
        }


        public override void DoRender3DOpaque(float dt, bool isShadowPass)
        {
            base.DoRender3DOpaque(dt, isShadowPass);


            
            if (isShadowPass)
            { return; }

            var prevProg = this.capi.Render.CurrentActiveShader;
            IShaderProgram prog = null;

            if (prevProg != null)
            { prevProg.Stop(); }
            prog = this.modSystem.EntityGenericShaderProgram;
            prog.Use();

            var lightrgbs = this.capi.World.BlockAccessor.GetLightRGBs((int)(this.entity.Pos.X + this.entity.SelectionBox.X1 - this.entity.OriginSelectionBox.X1), (int)this.entity.Pos.Y, (int)(this.entity.Pos.Z + this.entity.SelectionBox.Z1 - this.entity.OriginSelectionBox.Z1));

            if (this.strobeFrequency > 0f)
            {
                this.time += dt * this.strobeFrequency;
                var timeoff = (float)Math.Abs(Math.Sin(this.time + this.offset));
                //Debug.WriteLine("glow: " + timeoff);
                lightrgbs.R = (lightrgbs.R * 0.2f) + (timeoff * 0.8f);
                lightrgbs.G = (lightrgbs.G * 0.2f) + (timeoff * 0.8f);
                lightrgbs.B = (lightrgbs.B * 0.2f) + (timeoff * 0.8f);
                lightrgbs.A = (lightrgbs.A * 0.2f) + (timeoff * 0.8f);
                prog.Uniform("extraGlow", (int)timeoff);
            }
            else
            {
                prog.Uniform("extraGlow", this.entity.Properties.Client.GlowLevel);
            }

            var rapi = this.capi.Render;

            prog.Uniform("rgbaAmbientIn", rapi.AmbientColor);
            prog.Uniform("rgbaLightIn", lightrgbs);
            prog.Uniform("rgbaFogIn", rapi.FogColor);
            prog.Uniform("fogMinIn", rapi.FogMin);
            prog.Uniform("fogDensityIn", rapi.FogDensity);
            prog.BindTexture2D("entityTex", this.capi.EntityTextureAtlas.AtlasTextureIds[0], 0);
            
            prog.UniformMatrix("modelMatrix", this.ModelMat);

            //prog.UniformMatrix("viewMatrix", capi.Render.CurrentModelviewMatrix);
            prog.UniformMatrix("viewMatrix", this.capi.Render.CameraMatrixOriginf);

            prog.Uniform("addRenderFlags", this.AddRenderFlags);
            prog.Uniform("windWaveIntensity", (float)this.WindWaveIntensity);
            prog.Uniform("skipRenderJointId", -2);
            prog.Uniform("skipRenderJointId2", -2);
            prog.Uniform("entityId", (int)this.entity.EntityId);
            prog.Uniform("waterWaveCounter", this.capi.Render.ShaderUniforms.WaterWaveCounter);

            this.color[0] = ((this.entity.RenderColor >> 16) & 0xff) / 255f;
            this.color[1] = ((this.entity.RenderColor >> 8) & 0xff) / 255f;
            this.color[2] = ((this.entity.RenderColor >> 0) & 0xff) / 255f;
            this.color[3] = ((this.entity.RenderColor >> 24) & 0xff) / 255f;

            prog.Uniform("renderColor", this.color);
            prog.UniformMatrix("projectionMatrix", this.capi.Render.CurrentProjectionMatrix);

            var stab = this.entity.WatchedAttributes.GetDouble("temporalStability", 1);
            var plrStab = this.capi.World.Player.Entity.WatchedAttributes.GetDouble("temporalStability", 1);
            var stabMin = Math.Min(stab, plrStab);

            var strength = (float)(this.glitchAffected ? Math.Max(0, 1 - (1 / 0.4f * stabMin)) : 0);
            prog.Uniform("glitchEffectStrength", strength);

            prog.UniformMatrices(
               "elementTransforms",
               GlobalConstants.MaxAnimatedElements,
               this.entity.AnimManager.Animator.Matrices
           );

            if (this.meshRefOpaque != null)
            { this.capi.Render.RenderMesh(this.meshRefOpaque); }

            if (this.meshRefOit != null)
            { this.capi.Render.RenderMesh(this.meshRefOit); }
            prog.Stop();

            
        }


    }
}

