namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Collections.Generic;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    using Vintagestory.API.Util;
    //using System.Diagnostics;

    public class BlockMetalBucketFilled : Block
    {
        protected WorldInteraction[] interactions;

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
                    }
                };
            });
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (byPlayer.Entity.Controls.Sneak) //sneak place only
            {
                return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            }
            return false;
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null)
            { return; }
            if (byEntity.Controls.Sneak)
            { return; }
            var bucketPath = slot.Itemstack.Block.Code.Path;
            var pos = blockSel.Position;
            var block = byEntity.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);

            if (byEntity.Controls.Sprint && (this.api.World.Side == EnumAppSide.Server))
            {
                var newblock = byEntity.World.GetBlock(new AssetLocation("primitivesurvival:" + bucketPath.Replace("-filled", "-empty")));
                var newStack = new ItemStack(newblock);
                slot.TakeOut(1);
                slot.MarkDirty();
                if (!byEntity.TryGiveItemStack(newStack))
                {
                    this.api.World.SpawnItemEntity(newStack, byEntity.Pos.XYZ.AddCopy(0, 0.5, 0));
                }
                newblock = byEntity.World.GetBlock(new AssetLocation("lava-still-7"));
                BlockPos targetPos;
                if (block.IsLiquid())
                { targetPos = pos; }
                else
                { targetPos = blockSel.Position.AddCopy(blockSel.Face); }
                this.api.World.BlockAccessor.SetBlock(newblock.BlockId, targetPos); //put lava above
                newblock.OnNeighbourBlockChange(byEntity.World, targetPos, targetPos.NorthCopy());
                this.api.World.BlockAccessor.TriggerNeighbourBlockUpdate(targetPos);
                this.api.World.BlockAccessor.MarkBlockDirty(targetPos); //let the server know the lava's there
            }

            if ((byEntity as EntityPlayer) != null)  //bucket dumping lava animation
            {
                if (((byEntity as EntityPlayer).Player as IClientPlayer) != null)
                { ((byEntity as EntityPlayer).Player as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract); }
            }
            handHandling = EnumHandHandling.PreventDefault;
            return;
        }


        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            var val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (val)
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMetalBucketFilled bect)
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


        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            Dictionary<int, MeshRef> meshrefs = null;
            if (capi.ObjectCache.TryGetValue("bucketMeshRefs", out var obj))
            { meshrefs = obj as Dictionary<int, MeshRef>; }
            else
            { capi.ObjectCache["bucketMeshRefs"] = meshrefs = new Dictionary<int, MeshRef>(); }
        }


        /*
        public int GetBucketHashCode(IClientWorldAccessor world, ItemStack contentStack)
        {
            var s = contentStack.StackSize + "x" + contentStack.Collectible.Code.ToShortString();
            return s.GetHashCode();
        }
        */

        public override void OnUnloaded(ICoreAPI api)
        {
            if (!(api is ICoreClientAPI capi))
            { return; }
            if (capi.ObjectCache.TryGetValue("bucketMeshRefs", out var obj))
            {
                var meshrefs = obj as Dictionary<int, MeshRef>;
                foreach (var val in meshrefs)
                {
                    val.Value.Dispose();
                }
                capi.ObjectCache.Remove("bucketMeshRefs");
            }
        }


        public MeshData GenMesh(ICoreClientAPI capi)
        {
            var shape = capi.Assets.TryGet("primitivesurvival:shapes/block/metalbucket/filled.json").ToObject<Shape>();
            capi.Tesselator.TesselateShape(this, shape, out var bucketmesh);
            return bucketmesh;
        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-empty",
                    HotKeyCode = "sprint",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) => true
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
