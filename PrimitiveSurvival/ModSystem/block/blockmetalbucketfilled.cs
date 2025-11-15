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

    public class BlockMetalBucketFilled : BlockLiquidContainerTopOpened

    {
        public override float CapacityLitres => 10;
        protected WorldInteraction[] newinteractions;
        public ItemStack[] lcdstacks;
        protected override string meshRefsCacheKey => Code.ToShortString() + "meshRefs";

        public override bool AllowHeldLiquidTransfer => false; //we need this to prevent lava from transferring to wood buckets for example

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client)
            { return; }
  
            List<ItemStack> liquidContainerStacks = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj is BlockMetalBucket)
                    liquidContainerStacks.Add(new ItemStack(obj));
            }
            lcdstacks = liquidContainerStacks.ToArray();
         }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            this.newinteractions = ObjectCacheUtil.GetOrCreate(api, "blockmetalbucketfilled", () =>
            {
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
                        Itemstacks = lcdstacks
                    },
                };
            });
            var allinteractions = newinteractions;
            return allinteractions;
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (byPlayer.Entity.Controls.Sneak) //sneak place only
            {
                return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
            }
            return false;
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

            // https://github.com/SpearAndFang/primitive-survival/issues/39
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }

            var activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (activeSlot.Empty)
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }

            var activeSlotPath = activeSlot.Itemstack.Collectible.Code.Path;
            if (activeSlotPath.StartsWith("metalbucket-"))
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }
            return false;
        }


        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var beb = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMetalBucketFilled;

            var activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (activeSlot.Empty)
            {
                base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
                return;
            }

            var placedBlockPath = block.Code.Path;
            var activeSlotPath = activeSlot.Itemstack.Collectible.Code.Path;

            if (placedBlockPath.StartsWith("metalbucket-filled") && activeSlotPath.StartsWith("metalbucket-empty"))
            {
                //if the player just scooped lava out of this bucket swap out the one on the ground
                var newAssetLocation = new AssetLocation("primitivesurvival:" + block.Code.Path.Replace("-filled", "-empty"));
                var newblock = world.GetBlock(newAssetLocation);
                world.BlockAccessor.SetBlock(newblock.BlockId, blockSel.Position); //put lava above
                var beb2 = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMetalBucket;
                beb2.MeshAngle = beb.MeshAngle;
                this.api.World.BlockAccessor.MarkBlockDirty(blockSel.Position); //let the server know the lava's there


                //now swap out the one in the inventory
                var newblock2 = world.GetBlock(new AssetLocation("primitivesurvival:" + activeSlotPath.Replace("-empty", "-filled")));
                if (newblock2 != null)
                {
                    var newStack = new ItemStack(newblock2);
                    //activeSlot.TakeOut(1);
                    activeSlot.Itemstack = newStack;
                    activeSlot.MarkDirty();
                }
            }
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
            
            if (block.Code.Path.Contains("metalbucket-empty"))
            { 
                var beb = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BEMetalBucket;

                if (beb.Inventory.Empty)
                {
                    var newAssetLocation = new AssetLocation("primitivesurvival:" + block.Code.Path.Replace("-empty", "-filled"));
                    var newblock = byEntity.World.GetBlock(newAssetLocation);

                    byEntity.World.BlockAccessor.SetBlock(newblock.BlockId, blockSel.Position); //put lava above
                    this.api.World.BlockAccessor.MarkBlockDirty(pos); //let the server know the lava's there
                    var beb2 = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BEMetalBucketFilled;
                    beb2.MeshAngle = beb.MeshAngle;
                    this.api.World.BlockAccessor.MarkBlockDirty(pos); //let the server know the lava's there
                                                                      //now remove the lava from the slot block

                    var activeSlotPath = slot.Itemstack.Collectible.Code.Path;
                    //now swap out the one in the inventory
                    var newblock2 = byEntity.World.GetBlock(new AssetLocation("primitivesurvival:" + activeSlotPath.Replace("-filled", "-empty")));
                    if (newblock2 != null)
                    {
                        var newStack = new ItemStack(newblock2);
                        //activeSlot.TakeOut(1);
                        slot.Itemstack = newStack;
                        slot.MarkDirty();
                    }
                }

            }
            else if (byEntity.Controls.Sprint && (this.api.World.Side == EnumAppSide.Server))
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
