namespace PrimitiveSurvival.ModSystem
{
    using System.Collections.Generic;
    using System.Diagnostics;
    //using System.Diagnostics;
    using System.Linq; //1.18
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;

    public class BlockSmoker : Block, IIgnitable
    {

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client)
            { return; }

            
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity(selection.Position) as BESmoker;

            if (be?.State == "lit")
            { return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer); }

            List<ItemStack> torchStacklist = BlockBehaviorCanIgnite.CanIgniteStacks(api, false);
            
            if (be?.State == "closed" && be?.WoodSlot.StackSize == 4 && be?.Inventory[0].Empty == false)
            {
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-forge-ignite",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = torchStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                              return wi.Itemstacks;
                        }
                },
                new WorldInteraction()
                {
                    ActionLangCode = "primitivesurvival:blockhelp-smoker-rightclickopen",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                }
                });
            }

            if (be?.State == "closed")
            {
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "primitivesurvival:blockhelp-smoker-rightclickopen",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                }
                });
            }

            if (be?.State == "open" && be?.Inventory[0].Empty == false)
            {
                if (be.Inventory[0].Itemstack.Collectible.FirstCodePart(2) == "smoked")
                {
                    return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "primitivesurvival:blockhelp-smoker-smokedmeat",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = null
                    }
                    });
                }
            }

            List<ItemStack> trussedMeatStacklist = new List<ItemStack>();
            List<ItemStack> firewoodStacklist = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj.Code.FirstCodePart() == "trussedmeat")
                {
                    List<ItemStack> stacks = obj.GetHandBookStacks(api as ICoreClientAPI);
                    if (stacks != null) trussedMeatStacklist.AddRange(stacks);
                }
                else
                {
                    if (obj.Code.FirstCodePart().Contains("firewood")) //regular or smoked or other I suppose
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(api as ICoreClientAPI);
                        if (stacks != null) firewoodStacklist.AddRange(stacks);
                    }
                }
            }
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction[] {
            new WorldInteraction()
            {
                ActionLangCode = "primitivesurvival:blockhelp-smoker-rightclickclose",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = null
            },
            new WorldInteraction()
            {
                ActionLangCode = "primitivesurvival:blockhelp-smoker-trussedmeat",
                HotKeyCode = null,
                MouseButton = EnumMouseButton.Right,
                Itemstacks = trussedMeatStacklist.ToArray(),
                GetMatchingStacks = getMatchingStacks
            },
            new WorldInteraction()
                {
                    ActionLangCode = "primitivesurvival:blockhelp-smoker-firewood",
                    HotKeyCode = null,
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = firewoodStacklist.ToArray(),
                    GetMatchingStacks = getMatchingStacks
                }
            });
        }



        private ItemStack[] getMatchingStacks(WorldInteraction wi, BlockSelection selection, EntitySelection entitySelection)
        {
            var be = api.World.BlockAccessor.GetBlockEntity(selection.Position) as BESmoker;

            if (be == null || wi.Itemstacks.Length == 0) return null;

            List<ItemStack> matchStacks = new List<ItemStack>();
            foreach (ItemStack stack in wi.Itemstacks)
            {
                if (stack.Collectible.Code.FirstCodePart().Contains("firewood"))
                {
                    if (be.Inventory[4].StackSize < 4) matchStacks.Add(stack);
                }
                if (stack.Collectible.Code.FirstCodePart() == "trussedmeat")
                {
                    if (be.Inventory[3].Empty) matchStacks.Add(stack);
                }
            }
            Debug.WriteLine("hoo");
            return matchStacks.ToArray();

        }


        // 1.19
        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }


        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            var be = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BESmoker;
            if (!be.CanIgnite())
            { return EnumIgniteState.NotIgnitablePreventDefault; }
            return secondsIgniting > 4 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }


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
                { mesh.Translate(0.04f, -0.25f, -0.26f); }
                else if (count == 2)
                { mesh.Translate(0.06f, -0.25f, 0f); }
                else if (count == 3)
                { mesh.Translate(0.01f, 0.0f, -0.26f); }
                else
                { mesh.Translate(0.02f, 0.0f, 0f); }
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
