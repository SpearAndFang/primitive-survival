namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Collections.Generic;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;
    //using System.Diagnostics;


    public class BlockMetalBucket : BlockLiquidContainerTopOpened
    {
        public override float CapacityLitres => 10;
        protected new WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client)
            { return; }
            var capi = api as ICoreClientAPI;

            this.interactions = ObjectCacheUtil.GetOrCreate(api, "metalbucketfilled", () =>
            {
                var liquidContainerStacks = new List<ItemStack>();
                foreach (var obj in api.World.Collectibles)
                {
                    if (obj is ILiquidSource || obj is ILiquidSink || obj is BlockWateringCan)
                    {
                        var stacks = obj.GetHandBookStacks(capi);
                        if (stacks == null)
                        { continue; }

                        foreach (var stack in stacks)
                        {
                            stack.StackSize = 1;
                            liquidContainerStacks.Add(stack);
                        }
                    }
                }
                var lcstacks = liquidContainerStacks.ToArray();
                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-behavior-rightclickpickup",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-bucket-rightclick",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = lcstacks
                    }
                };
            });
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (byPlayer.Entity.Controls.Sneak) //sneak place only
            { return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode); }
            return false;
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity.Controls.Sneak)
            { return; }
            var bucketPath = slot.Itemstack.Block.Code.Path;
            var pos = blockSel.Position;
            var block = byEntity.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);

            var contentStack = this.GetContent(slot.Itemstack);

            if (block.Code.Path.Contains("lava-") && bucketPath.Contains("-empty") && contentStack == null) //lava block and empty bucket?
            {
                if (block.Code.Path.Contains("-7")) //lots of lava?
                {
                    if (this.api.World.Side == EnumAppSide.Server)
                    {
                        var newblock = byEntity.World.GetBlock(new AssetLocation("primitivesurvival:" + bucketPath.Replace("-empty", "-filled")));
                        var newStack = new ItemStack(newblock);
                        slot.TakeOut(1);
                        slot.MarkDirty();
                        if (!byEntity.TryGiveItemStack(newStack))
                        { this.api.World.SpawnItemEntity(newStack, byEntity.Pos.XYZ.AddCopy(0, 0.5, 0)); }
                        newblock = byEntity.World.GetBlock(new AssetLocation("lava-still-3"));

                        this.api.World.BlockAccessor.SetBlock(newblock.BlockId, pos); //replace lava with less lava
                        newblock.OnNeighbourBlockChange(byEntity.World, pos, pos.NorthCopy());
                        this.api.World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                        this.api.World.BlockAccessor.MarkBlockDirty(pos); //let the server know the lava's gone
                    }
                    handHandling = EnumHandHandling.PreventDefault;
                    if ((byEntity as EntityPlayer) != null)  //bucket scooping lava animation
                    {
                        if (((byEntity as EntityPlayer).Player as IClientPlayer) != null)
                        { ((byEntity as EntityPlayer).Player as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract); }
                    }
                    return;
                }
            }
            else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
            }
        }


        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            var val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (val)
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMetalBucket bect)
                {
                    var targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    var dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    var dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    var angleHor = (float)Math.Atan2(dx, dz);
                    var deg22dot5rad = GameMath.PIHALF / 4;
                    var roundRad = ((int)Math.Round(angleHor / deg22dot5rad)) * deg22dot5rad;
                    bect.MeshAngle = roundRad;
                }
            }
            return val;
        }


        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture)
        {
            var tesselator = capi.Tesselator;
            var shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();
            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0, 0, 0));
            return mesh;

        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return this.interactions;
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-fill",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) => this.GetCurrentLitres(inSlot.Itemstack) < this.CapacityLitres
                },
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-place",
                    HotKeyCode = "sneak",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) => true
                }
            };
        }
    }
}
