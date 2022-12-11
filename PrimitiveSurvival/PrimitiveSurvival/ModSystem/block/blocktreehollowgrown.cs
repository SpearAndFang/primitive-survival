namespace PrimitiveSurvival.ModSystem
{
    //using System.Collections.Generic;
    using System.Linq;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    //using System.Diagnostics;
    //using Vintagestory.GameContent;

    public class BlockTreeHollowGrown : Block
    {
        private WorldInteraction[] interactions;
        public override void OnLoaded(ICoreAPI api)
        {
            if (api.Side != EnumAppSide.Client)
            { return; }
            var capi = api as ICoreClientAPI;
            this.interactions = ObjectCacheUtil.GetOrCreate(api, "treehollowInteractions", () => new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-behavior-rightclickpickup",
                    MouseButton = EnumMouseButton.Right,
                    RequireFreeHand = true
                }
            });
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            var facing = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            bool placed;
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

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BETreeHollowGrown bedc)
            {
                Block blockToBreak = this;
                bedc.OnBreak(); //  byPlayer, pos);

                var newPath = "primitivesurvival:treehollowplaced-" + blockToBreak.FirstCodePart(2) + "-north";
                var newBlock = this.api.World.GetBlock(new AssetLocation(newPath)) as BlockTreeHollowPlaced;
                world.BlockAccessor.SetBlock(newBlock.BlockId, pos);
                if (world.BlockAccessor.GetBlockEntity(pos) is BETreeHollowPlaced be)
                {
                    be.Initialize(this.api);
                    be.type = blockToBreak.FirstCodePart(1);
                    be.MarkDirty();
                    world.BlockAccessor.BreakBlock(pos, null, 1);
                }
            }
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BETreeHollowGrown bedc)
            { return bedc.OnInteract(byPlayer); } //, blockSel); }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, ITesselatorAPI tesselator = null)
        {
            MeshData mesh = null;
            var shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            if (shape != null && texture != null)
            {
                tesselator.TesselateShape(shapePath, shape, out mesh, texture, new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ));
            }
            return mesh;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (world.BlockAccessor.GetBlockEntity(selection.Position) is BETreeHollowGrown bedc)
            {
                if (!bedc.Inventory.Empty)
                {
                    return this.interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
                }
            }
            return null;
        }
    }
}
