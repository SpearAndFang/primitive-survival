namespace PrimitiveSurvival.ModSystem
{
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    //using System.Diagnostics;

    internal class BlockParticulator : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!playerSlot.Empty)
            {
                var playerStack = playerSlot.Itemstack;
                if (playerStack.Item != null)
                {
                    if (playerStack.Item.Code.Path.Contains("linktool"))
                    { return base.OnBlockInteractStart(world, byPlayer, blockSel); }
                }
            }

            var pos = blockSel.Position;
            if (world.BlockAccessor.GetBlockEntity(pos) is BEParticulator be)
            {
                if (!byPlayer.Entity.Controls.Sneak) //interact
                {
                    be.OnBlockInteract();
                    return true;
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }


        public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ItemStack stack)
        {
            base.OnBlockPlaced(world, pos, stack);

            if (stack != null)
            {
                if (world.BlockAccessor.GetBlockEntity(pos) is BEParticulator be)
                {
                    be.Link(pos);
                }
            }
        }


        public byte[] GetSettings(ItemStack stack)
        {
            return stack.Attributes.GetBytes("thisData", null);
        }


        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            //Drop the block in creative mode too so we don't lose a configured block
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                if (world.BlockAccessor.GetBlockEntity(pos) is BEParticulator be)
                {
                    var stack = new ItemStack(this);
                    stack.Attributes.SetBytes("thisData", SerializerUtil.Serialize(be.Data));
                    world.SpawnItemEntity(stack, new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                    be.OnBlockBroken(byPlayer);
                    world.BlockAccessor.SetBlock(0, pos);
                }
            }
            base.OnBlockBroken(world, pos, byPlayer, 1);
        }


        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new ItemStack[] { this.OnPickBlock(world, pos) };
        }



        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BEParticulator be)
            {
                var stack = new ItemStack(this);
                stack.Attributes.SetBytes("thisData", SerializerUtil.Serialize(be.Data));
                return stack;
            }
            return base.OnPickBlock(world, pos);
        }

        private static BlockPos[] AreaAround(BlockPos pos)
        {
            return new BlockPos[] { pos.NorthCopy(), pos.SouthCopy(), pos.EastCopy(), pos.WestCopy(), pos.UpCopy(), pos.DownCopy() };
        }

        public static void ScanAround(IWorldAccessor world, BlockPos pos)
        {
            //Debug.WriteLine("Scanning " + pos);
            if (world.BlockAccessor.GetBlockEntity(pos) is BEParticulator bes)
            {
                bes.UpdateNeib(new BlockPos(-1, -1, -1), "Death");
                bes.UpdateNeib(new BlockPos(-1, -1, -1), "Secondary");
                var around = AreaAround(pos);
                foreach (var neighbor in around)
                {
                    if (world.BlockAccessor.GetBlockEntity(neighbor) is BEParticulator be)
                    {
                        var tempData = be.Data;
                        if (tempData != null)
                        {
                            if (tempData.particleType == "Death" || tempData.particleType == "Secondary")
                            { bes.UpdateNeib(neighbor, tempData.particleType); }
                        }
                    }
                }
            }
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            //bidirectional scanning
            if (world.BlockAccessor.GetBlockEntity(pos) is BEParticulator be)
            {
                ScanAround(world, pos);
            }
            if (world.BlockAccessor.GetBlockEntity(neibpos) is BEParticulator ben)
            {
                ScanAround(world, neibpos);
            }
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            var data = inSlot.Itemstack.Attributes.GetBytes("thisData", null);
            if (data != null)
            {
                var pdata = SerializerUtil.Deserialize<BEParticleData>(data);
                var name = pdata.name;
                if (name == "" || name == null)
                { name = "-"; }
                dsc.AppendLine(Lang.GetMatching("primitivesurvival:particle-name") + ": " + name);
                dsc.AppendLine(Lang.GetMatching("primitivesurvival:particle-type") + ": " + Lang.Get(pdata.particleType));
            }
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "primitivesurvival:blockhelp-particulator-configure",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null,
                }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
