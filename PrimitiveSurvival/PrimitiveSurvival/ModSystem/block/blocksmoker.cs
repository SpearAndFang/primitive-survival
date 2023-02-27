namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    //using System.Diagnostics;
    using System.Linq; //1.18
    using Vintagestory.GameContent; //1.18

    //public class BlockSmoker : Block //1.18
    public class BlockSmoker : Block, IIgnitable
    {

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity(selection.Position) as BESmoker;

            if (be?.State == "lit")
            { return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer); }
            else if (be?.State == "closed" && be?.WoodSlot.StackSize == 4 && be?.Inventory[0].Empty == false)
            {
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-forge-ignite",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                },
                new WorldInteraction()
                {
                    ActionLangCode = "primitivesurvival:blockhelp-smoker-rightclick",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                }
                });
            }
            else if (be?.State != "lit")
            {
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "primitivesurvival:blockhelp-smoker-rightclick",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                }
                });
            }
            else
            {
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            }
        }

        //public override EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting) //1.18
        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            var be = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BESmoker;
            if (!be.CanIgnite())
            { return EnumIgniteState.NotIgnitablePreventDefault; }
            return secondsIgniting > 4 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        // public override void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling) //1.18
        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            var be = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BESmoker;
            be?.TryIgnite();
        }

        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, string state, int count)
        {
            Shape shape;
            var tesselator = capi.Tesselator;
            shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();

            var glow = 0;
            if (shapePath.Contains("lit"))
            { glow = 200; }
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0, 0, 0), glow);

            var rotate = this.Shape.rotateY;
            if (state == "open" && shapePath.Contains("door"))
            {
                rotate -= 100;
                mesh.Translate(0.2f, 0f, 0.8f);
            }
            if (shapePath.Contains("log") || shapePath.Contains("lit"))
            {
                if (count == 1)
                { mesh.Translate(-0.06f, -0.08f, -0.12f); }
                else if (count == 2)
                { mesh.Translate(-0.06f, -0.08f, 0.01f); }
                else if (count == 3)
                { mesh.Translate(-0.1f, 0.05f, -0.12f); }
                else
                { mesh.Translate(-0.08f, 0.05f, 0.01f); }
            }
            mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, rotate * GameMath.DEG2RAD, 0); //orient based on direction 
            return mesh;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BESmoker be)
            { be.OnBreak(); } //empty the inventory onto the ground
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
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

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BESmoker be)
            { return be.OnInteract(byPlayer, blockSel); }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }
    }
}
