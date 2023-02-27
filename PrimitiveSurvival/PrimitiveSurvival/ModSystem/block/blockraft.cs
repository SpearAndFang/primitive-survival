namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Linq;
    using Vintagestory.API.Common;
    using Vintagestory.API.Client;
    using PrimitiveSurvival.ModConfig;
    using Vintagestory.API.MathTools;
    //using System.Diagnostics;
    using Vintagestory.API.Client.Tesselation;

    public class BlockRaft : Block, IDrawYAdjustable
    {

        private long handlerId;
        public long lookCount;
        static Random rand = new Random();
        private ILoadedSound wsound;
        private readonly string[] raftTypes = { "raftps", "raftcrab", "raftdolphin", "raftshark", "rafttuna", "raftps" };

        //private AssetLocation splashSound = new AssetLocation("game", "sounds/environment/waterwaves");

        private void GenerateParticles(BlockPos pos)
        {
            // Primary Particles
            var color = ColorUtil.ToRgba(rand.Next(0, 255), 0, 50, rand.Next(80, 140));

            var particles = new SimpleParticleProperties(
                30, 30, // quantity
                color,
                new Vec3d(1, 0.3, 1), //min position
                new Vec3d(), //add position - see below
                new Vec3f(0.5f, 0.5f, 0.5f), //min velocity
                new Vec3f(), //add velocity - see below
                (float)((rand.NextDouble() * 1f) + 1f), //life length
                0f, //gravity effect
                0.2f, 0.8f, //size
                EnumParticleModel.Cube); // model

            particles.MinPos.Add(pos); //add block position
            particles.AddPos.Set(new Vec3d(-1, 0, -1)); //add position
            particles.AddVelocity.Set(new Vec3f(-1f, -0.5f, -1f)); //add velocity
            particles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 1f);
            particles.VertexFlags = 255;

            this.api.World.SpawnParticles(particles);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool placed;
            var facing = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            if (this.isWater(world.BlockAccessor, blockSel.Position.UpCopy()))
            {
                blockSel = blockSel.Clone();
                blockSel.Position = blockSel.Position.Up();
                placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
                if (placed)
                {
                    var block = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
                    var newPath = block.Code.Path;
                    newPath = newPath.Replace("north", facing);
                    block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                    this.api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                }
                return placed;
            }

            //prevent placing raft on wall or in partial water
            var testBlock = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            if (blockSel.Face.IsHorizontal || testBlock.Code.Path.Contains("water"))
            {
                return false;
            }

            placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed)
            {
                var block = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
                var newPath = block.Code.Path;
                newPath = newPath.Replace("north", facing);
                block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                this.api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
            }
            return placed;
        }


        public bool isWater(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var waterblock = blockAccessor.GetBlock(pos.DownCopy(), BlockLayersAccess.Fluid);
            var upblock = blockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            return waterblock.IsLiquid() && waterblock.LiquidLevel == 7 && waterblock.LiquidCode.Contains("water") && upblock.Id == 0;
        }

        public float AdjustYPosition(Block[] chunkExtBlocks, int extIndex3d)
        {
            var nblock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Down]];
            return nblock.BlockId == 0 ? -0.1625f : 0f;
        }

        public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
        {
            sourceMesh.XyzInstanced = true;
            base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, pos, chunkExtBlocks, extIndex3d);

            var below = pos.DownCopy();
            var downSolidBlock = this.api.World.BlockAccessor.GetBlock(below, BlockLayersAccess.Solid);
            var downWaterBlock = this.api.World.BlockAccessor.GetBlock(below, BlockLayersAccess.Fluid);
            int windData = this.VertexFlags.Normal;
            if (downWaterBlock.Code.Path.Contains("water") && downSolidBlock.Id == 0)
            {
                windData = this.VertexFlags.Normal | EnumWindBitModeMask.Water | this.VertexFlags.ZOffset;
                
                //sourceMesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0.11f, 0, 0);
            }
            for (var i = 0; i < sourceMesh.FlagsCount; i++)
            {
                sourceMesh.Flags[i] = windData;
            }
            


        }


        public override void OnBeingLookedAt(IPlayer byPlayer, BlockSelection blockSel, bool firstTick)
        {
            var block = this.api.World.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var newPath = block.Code.Path;
            if (newPath.Contains("raft-"))
            {
                if (this.api.Side == EnumAppSide.Server)
                {
                    if (firstTick)
                    { this.lookCount = 0; }
                    else
                    {
                        this.lookCount++;
                        if (this.lookCount > 200)
                        {
                            if (byPlayer.PlayerName == "KineticKnight") //SpearAndFang KineticKnight
                            { newPath = newPath.Replace("raft", "raftkk"); }
                            else
                            {
                                var rafttype = this.api.World.Rand.Next(this.raftTypes.Count());
                                newPath = newPath.Replace("raft", this.raftTypes[rafttype]);
                            }
                            block = this.api.World.GetBlock(block.CodeWithPath(newPath));
                            this.api.World.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
                            this.api.World.PlaySoundAt(new AssetLocation("game:sounds/block/metaldoor-place"), blockSel.Position.X + 0.5, blockSel.Position.Y + 0.5, blockSel.Position.Z + 0.5, null);
                            this.GenerateParticles(blockSel.Position);
                            this.lookCount = 0;
                        }
                    }
                }
            }
            base.OnBeingLookedAt(byPlayer, blockSel, firstTick);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefault;
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

        public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity.World is IClientWorldAccessor)
            {
                if (this.wsound == null)
                {
                    byEntity.World.Api.ObjectCache["raftSound"] = this.wsound = (byEntity.World as IClientWorldAccessor).LoadSound(new SoundParams()
                    {
                        Location = new AssetLocation("sounds/environment/largesplash1.ogg"),
                        ShouldLoop = true,
                        DisposeOnFinish = false,
                        Volume = 0.3f,
                        Pitch = 1f
                    });
                }
            }

            if (byEntity.World.Side == EnumAppSide.Client)
            {
                byEntity.World.UnregisterCallback(this.handlerId);
                this.handlerId = byEntity.World.RegisterCallback(this.AfterAwhile, 1350);
            }

            if (byEntity.World is IClientWorldAccessor)
            {
                byEntity.StopAnimation("swim");
                if (byEntity.IsEyesSubmerged() || byEntity.FeetInLiquid)
                {
                    //in water
                    byEntity.StartAnimation("swim");

                    this.wsound.SetPosition(byEntity.Pos.XYZ.ToVec3f());
                    if (!this.wsound.IsPlaying)
                    {
                        this.wsound.Start();
                    }
                    this.FpHandTransform.Rotation.X = -140;
                    this.FpHandTransform.Rotation.Y = 45;
                    this.FpHandTransform.Rotation.Z = 180;
                    this.FpHandTransform.Scale = 2f;
                    this.FpHandTransform.Translation.Y = -0.15f;
                    this.FpHandTransform.Translation.Z = -0.1f;

                    this.TpHandTransform.Translation.X = 0.7f;
                    this.TpHandTransform.Translation.Y = -0.35f;
                    this.TpHandTransform.Translation.Z = -0.41f;
                    this.TpHandTransform.Rotation.X = -164;
                    this.TpHandTransform.Rotation.Y = -12;
                    this.TpHandTransform.Rotation.Z = 30;
                    this.TpHandTransform.Origin.X = -0.2f;
                    this.TpHandTransform.Origin.Y = 0.42f;
                    this.TpHandTransform.Origin.Z = 0.3f;
                    this.TpHandTransform.Scale = 1.06f;

                    if (byEntity.IsEyesSubmerged()) //under water
                    {
                        // a bit of forward motion to prevent using waterfalls as elevators
                        // but mostly a floatation device when under water
                        var pos = byEntity.Pos.HorizontalAheadCopy(0.01f).XYZ;
                        var newX = byEntity.Pos.X - pos.X;
                        var newZ = byEntity.Pos.Z - pos.Z;
                        byEntity.Pos.Motion.X -= newX;
                        byEntity.Pos.Motion.Z -= newZ;
                        byEntity.Pos.Motion.Y += ModConfig.Loaded.RaftFlotationModifier / 1.8;
                    }
                    else //feet in water
                    {
                        var pos = byEntity.Pos.HorizontalAheadCopy(0.05f).XYZ;
                        var newX = byEntity.Pos.X - pos.X;
                        var newZ = byEntity.Pos.Z - pos.Z;
                        byEntity.Pos.Motion.X -= newX * ModConfig.Loaded.RaftWaterSpeedModifier;
                        byEntity.Pos.Motion.Z -= newZ * ModConfig.Loaded.RaftWaterSpeedModifier;
                    }

                }
                else //on land
                {
                    this.CancelRaft(byEntity);
                }
            }
        }


        private void CancelRaft(EntityAgent byEntity)
        {
            byEntity.StopAnimation("swim");
            this.FpHandTransform.Rotation.X = -90;
            this.FpHandTransform.Rotation.Y = 73;
            this.FpHandTransform.Rotation.Z = 174;
            this.FpHandTransform.Scale = 1.5f;

            this.TpHandTransform.Translation.X = -0.3f;
            this.TpHandTransform.Translation.Z = 0;
            this.TpHandTransform.Rotation.X = 116;
            this.TpHandTransform.Rotation.Y = -22;
            this.TpHandTransform.Rotation.Z = 163;

            this.TpHandTransform.Origin.X = 0f;
            this.TpHandTransform.Origin.Y = 0.25f;
            this.TpHandTransform.Origin.Z = 0f;
            this.TpHandTransform.Scale = 0.94f;

            if (byEntity.World is IClientWorldAccessor)
            {
                if (this.wsound != null)
                {
                    if (this.wsound.IsPlaying)
                    {
                        this.wsound?.Stop();
                        this.wsound?.Dispose();
                        this.wsound = null;
                    }
                }
            }
        }

        private void AfterAwhile(float dt)
        {
            var capi = this.api as ICoreClientAPI;
            var plr = capi.World.Player;
            var byEntity = plr.Entity;
            var stackname = "null";
            if (byEntity.RightHandItemSlot.Itemstack != null)
            { stackname = byEntity.RightHandItemSlot.Itemstack.GetName(); }
            if (stackname != "Raft")
            {
                this.CancelRaft(byEntity);
            }
        }
    }
}
