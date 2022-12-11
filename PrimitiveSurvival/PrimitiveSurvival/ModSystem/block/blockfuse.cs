namespace PrimitiveSurvival.ModSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using Vintagestory.API.Client;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;

    public class BlockFuse : Block
    {
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client)
            { return; }
            var capi = api as ICoreClientAPI;

            this.interactions = ObjectCacheUtil.GetOrCreate(api, "fuseInteractions", () =>
            {
                var canIgniteStacks = new List<ItemStack>();

                foreach (var obj in api.World.Collectibles)
                {
                    if (obj is Block && (obj as Block).HasBehavior<BlockBehaviorCanIgnite>())
                    {
                        var stacks = obj.GetHandBookStacks(capi);
                        if (stacks != null)
                        { canIgniteStacks.AddRange(stacks); }
                    }
                }

                return new WorldInteraction[] {
                new WorldInteraction()
                {
                    MouseButton = EnumMouseButton.Right,
                    ActionLangCode = "primitivesurvival:blockhelp-firework-ignite",
                    Itemstacks = canIgniteStacks.ToArray(),
                    GetMatchingStacks = (wi, bs, es) => !(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BEFuse befuse) || befuse.IsLit ? null : wi.Itemstacks }
                };
            });
        }


        public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) is BEFuse befuse) || befuse.IsLit)
            { return EnumIgniteState.NotIgnitablePreventDefault; }

            if (secondsIgniting > 0.75f)
            {
                return EnumIgniteState.IgniteNow;
            }
            return EnumIgniteState.Ignitable;
        }

        public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            if (secondsIgniting < 0.7f)
            { return; }

            handling = EnumHandling.PreventDefault;

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player)
            { byPlayer = byEntity.World.PlayerByUid(player.PlayerUID); }
            if (byPlayer == null)
            { return; }

            var befuse = byPlayer.Entity.World.BlockAccessor.GetBlockEntity(pos) as BEFuse;
            befuse?.OnIgnite(byPlayer);
        }


        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
        {
            //IMPORTANT NOTICE FOR MODDERS: If you override Block.OnBlockExploded() and don't call the base method you now must manually delete the block with "world.BulkBlockAccessor.SetBlock(0, pos);" or your block will become a source of infinite drops
            var befuse = world.BlockAccessor.GetBlockEntity(pos) as BEFuse;
            befuse?.OnBlockExploded(pos);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return this.interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }


        public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
        {
            return;  // no windwave
        }

        public string GetOrientations(IWorldAccessor world, BlockPos pos)
        {
            var orientations =
                this.GetFuseCode(world, pos, BlockFacing.NORTH) +
                this.GetFuseCode(world, pos, BlockFacing.EAST) +
                this.GetFuseCode(world, pos, BlockFacing.SOUTH) +
                this.GetFuseCode(world, pos, BlockFacing.WEST);
            if (orientations.Length == 0)
            { orientations = "empty"; }
            return orientations;
        }


        private string GetFuseCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
        {
            if (this.ShouldConnectAt(world, pos, facing) || this.ShouldConnectAt(world, pos.UpCopy(), facing) || this.ShouldConnectAt(world, pos.DownCopy(), facing))
            { return "" + facing.Code[0]; }
            return "";
        }


        /*
         * Fuse is an item - this is completely unneccessary
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            bool placed;
            bool inwater;
            var pos = blockSel.Position;
            var block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            inwater = block.LiquidCode == "water";
            var blockSelBelow = blockSel.Clone();
            blockSelBelow.Position.Y -= 1;
            var blockBelow = world.BlockAccessor.GetBlock(blockSelBelow.Position, BlockLayersAccess.Default);
            if (blockBelow.LiquidCode == "water")
            {
                failureCode = Lang.Get("you cannot place a fuse in water");
                return false;
            }
            var orientations = this.GetOrientations(world, pos);
            block = world.BlockAccessor.GetBlock(this.CodeWithVariant("dir", orientations));
            if (block == null)
            { block = this; }

            placed = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            if (placed)
            {
                return true;
            }
            return false;
        }
        */


        private static BlockPos[] AboveBelow(BlockPos pos)
        {
            return new BlockPos[]
            {  pos, pos.UpCopy(), pos.DownCopy() };
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            var around = AboveBelow(pos);
            var orientations = this.GetOrientations(world, pos);
            var newBlockCode = this.CodeWithVariant("dir", orientations);
            if (!this.Code.Equals(newBlockCode))
            {
                var block = world.BlockAccessor.GetBlock(newBlockCode);
                if (block == null)
                { return; }

                if (block.Code.Path.Contains("fuse"))
                {
                    world.BlockAccessor.SetBlock(block.BlockId, pos);
                }

                foreach (var neighbor in around)
                {
                    world.BlockAccessor.TriggerNeighbourBlockUpdate(neighbor);

                    //Disabling this seems to help a lot with the breaking block issue
                    //base.OnNeighbourBlockChange(world, neighbor, neibpos);

                    // although there's still some weirdness, I was able to stack fuses two high
                    // for example when there was a neighbor at the stacked level
                    // also clicking on a rock with a fuse should just abort, not blow up


                }
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }


        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
        }

        public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
        {
            var block = world.BlockAccessor.GetBlock(ownPos.AddCopy(side), BlockLayersAccess.Default);
            return block.FirstCodePart() == this.FirstCodePart() || block.FirstCodePart() == "firework" || block.FirstCodePart() == "oreblastingbomb"; //|| block.SideSolid[side.Opposite.Index]
        }


        private static readonly string[] OneDir = new string[] { "n", "e", "s", "w" };
        private static readonly string[] TwoDir = new string[] { "ns", "ew" };
        private static readonly string[] AngledDir = new string[] { "ne", "es", "sw", "nw" };
        private static readonly string[] ThreeDir = new string[] { "nes", "new", "nsw", "esw" };
        private static readonly string[] GateLeft = new string[] { "egw", "ngs" };
        private static readonly string[] GateRight = new string[] { "gew", "gns" };
        private static readonly Dictionary<string, KeyValuePair<string[], int>> AngleGroups = new Dictionary<string, KeyValuePair<string[], int>>();


        static BlockFuse()
        {
            AngleGroups["n"] = new KeyValuePair<string[], int>(OneDir, 0);
            AngleGroups["e"] = new KeyValuePair<string[], int>(OneDir, 1);
            AngleGroups["s"] = new KeyValuePair<string[], int>(OneDir, 2);
            AngleGroups["w"] = new KeyValuePair<string[], int>(OneDir, 3);

            AngleGroups["ns"] = new KeyValuePair<string[], int>(TwoDir, 0);
            AngleGroups["ew"] = new KeyValuePair<string[], int>(TwoDir, 1);

            AngleGroups["ne"] = new KeyValuePair<string[], int>(AngledDir, 0);
            AngleGroups["nw"] = new KeyValuePair<string[], int>(AngledDir, 1);
            AngleGroups["es"] = new KeyValuePair<string[], int>(AngledDir, 2);
            AngleGroups["sw"] = new KeyValuePair<string[], int>(AngledDir, 3);

            AngleGroups["nes"] = new KeyValuePair<string[], int>(ThreeDir, 0);
            AngleGroups["new"] = new KeyValuePair<string[], int>(ThreeDir, 1);
            AngleGroups["nsw"] = new KeyValuePair<string[], int>(ThreeDir, 2);
            AngleGroups["esw"] = new KeyValuePair<string[], int>(ThreeDir, 3);


            AngleGroups["egw"] = new KeyValuePair<string[], int>(GateLeft, 0);
            AngleGroups["ngs"] = new KeyValuePair<string[], int>(GateLeft, 1);

            AngleGroups["gew"] = new KeyValuePair<string[], int>(GateRight, 0);
            AngleGroups["gns"] = new KeyValuePair<string[], int>(GateRight, 1);
        }


        public override AssetLocation GetRotatedBlockCode(int angle)
        {
            var type = this.Variant["dir"];
            if (type == "empty" || type == "nesw")
            { return this.Code; }
            var angleIndex = angle / 90;
            var val = AngleGroups[type];
            var newFacing = val.Key[(angleIndex + val.Value) % val.Key.Length];
            return this.CodeWithVariant("dir", newFacing);
        }

        /*
         * Not even worth it
         *
        private static BlockPos[] AreaAround(BlockPos pos)
        {
            return new BlockPos[]
            {  pos.WestCopy().UpCopy(), pos.SouthCopy().UpCopy(), pos.EastCopy().UpCopy(), pos.NorthCopy().UpCopy() };
        }



        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var selBoxes = base.GetSelectionBoxes(blockAccessor, pos);
            Block blockChk;
            var around = AreaAround(pos);
            var rot = 0;
            foreach (var neighbor in around)
            {
                blockChk = this.api.World.BlockAccessor.GetBlock(neighbor);
                if (blockChk != null)
                {
                    if (blockChk.Class == "blockfuse")
                    {
                        selBoxes = selBoxes.Append(new Cuboidf(0, 0.22f, 0.4f, 0.22f, 1f, 0.6f).RotatedCopy(0, rot, 0, new Vec3d(0.5, 0, 0.5)));
                    }
                }
                rot += 90;
            }
            return selBoxes;
        }
        */


        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, int rot, float updown)
        {
            var tesselator = capi.Tesselator;
            var shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0f, 0, 0f));
            if (shapePath.Contains("lead"))
            {
                mesh.Translate(-1f, updown, 0f);
            }
            else
            {
                if (updown == -1f)
                {
                    mesh.Translate(-0.02f, -1f, 0f);
                }
            }
            mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, rot * GameMath.DEG2RAD, 0);
            return mesh;
        }
    }
}
