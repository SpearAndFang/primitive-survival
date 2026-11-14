namespace PrimitiveSurvival.ModSystem
{ 

    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;

    #nullable disable

    public class fishDepletionProbe : CollectibleBehavior
    {
        public float SplashTime; 
        public AssetLocation SplashingSound;
        public string AnimationCode;


        public fishDepletionProbe(CollectibleObject collObj) : base(collObj)
        {
            this.collObj = collObj;
        }


        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            SplashTime = 1f;
            AnimationCode = "heavyimpact";
            SplashingSound = AssetLocation.Create("primitivesurvival:sounds/sticksplash", collObj.Code.Domain);
         }


        private void GenerateWaterParticles(BlockPos pos, IWorldAccessor world)
        {
            var rnd = new Random();
            int loc = rnd.Next(-2, 2);
            int loc2 = rnd.Next(-2, 2);
            float minQuantity = 1;
            float maxQuantity = 1;
            var color = ColorUtil.ToRgba(40, 165, 185, 255);
            var minPos = new Vec3d();
            var addPos = new Vec3d();
            var minVelocity = new Vec3f(0.2f, 0.0f, 0.2f);
            var maxVelocity = new Vec3f(0.6f, 0.4f, 0.6f);
            var lifeLength = 5f;
            var gravityEffect = -0.1f;
            var minSize = 0.1f;
            var maxSize = 0.5f;

            var waterParticles = new SimpleParticleProperties(
                minQuantity, maxQuantity,
                color,
                minPos, addPos,
                minVelocity, maxVelocity,
                lifeLength,
                gravityEffect,
                minSize, maxSize,
                EnumParticleModel.Quad
            );
            waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(0.5, 0.2, 0.5));
            waterParticles.AddPos.Set(new Vec3d(0.1f + loc, 0, 0f + loc2));

            waterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARINCREASE, 0.5f);
            waterParticles.ShouldDieInAir = true;
            waterParticles.SelfPropelled = true;
            world.SpawnParticles(waterParticles);
        }


        BlockSelection GetWaterAtPos(EntityAgent entity, double range)
        {
            Vec3d pos = entity.Pos.XYZ.Add(0, entity.LocalEyePos.Y, 0);
            Vec3f dir = entity.Pos.GetViewVector();
            for (double i = 0; i < range; i += 0.1)
            {
                BlockPos checkPos = new BlockPos((int)(pos.X + dir.X * i), (int)(pos.Y + dir.Y * i), (int)(pos.Z + dir.Z * i));
                Block b = entity.World.BlockAccessor.GetBlock(checkPos);
                if (b.Code.Path == "air") continue;
                if (b.Code.Path.Contains("water"))
                {
                    GenerateWaterParticles(checkPos, entity.World);
                    return new BlockSelection() { Position = checkPos, Face = BlockFacing.UP };
                }
                return null;
            }
            return null;
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            IBlockAccessor blockAcc = byEntity.World.BlockAccessor;

            var waterAtPos = GetWaterAtPos(byEntity, 5.0);

            if (waterAtPos != null)
            {
                //Debug.WriteLine("water at " + waterAtPos.Position);
                handling = EnumHandling.PreventDefault;
                handHandling = EnumHandHandling.PreventDefault;
                if (byEntity.World.Side == EnumAppSide.Client)
                {
                    byEntity.World.PlaySoundAt(SplashingSound, waterAtPos.Position, -0.5, byPlayer, false, 8, 10);
                }
            }
            else base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }


        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            var waterAtPos = GetWaterAtPos(byEntity, 5.0);
            if (waterAtPos != null)
            { 
                handling = EnumHandling.PreventDefault;
                if (byEntity.World is IClientWorldAccessor) byEntity.StartAnimation(AnimationCode);

                return secondsUsed < SplashTime;
            }

            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }


        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            byEntity.StopAnimation(AnimationCode);

            var waterAtPos = GetWaterAtPos(byEntity, 5.0);
            if (waterAtPos != null)
            {

                handling = EnumHandling.PreventDefault;
                //Debug.WriteLine(secondsUsed + " of " + SplashTime);
                if (secondsUsed < SplashTime - 0.05f)
                { return; }

                if (byEntity.World.Side == EnumAppSide.Client)
                { return; }
                var splr = (byEntity as EntityPlayer)?.Player as IServerPlayer;
                if (splr == null)
                { return; }

                waterAtPos = GetWaterAtPos(byEntity, 5.0);
                if (waterAtPos != null)
                {
                    var rate = PrimitiveSurvivalSystem.FishDepletedPercent(byEntity.Api as ICoreServerAPI, waterAtPos.Position);
                    
                    
                    var msg = "<strong><font size=16 color=yellow>" + Lang.Get("primitivesurvival:fishingstocksat", rate) + "</font></strong>";
                    splr.SendIngameError("depleted", msg);
                }
                return;
                
            }
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }


        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handling)
        {
            byEntity.StopAnimation(AnimationCode);
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handling);
        }
    }
}