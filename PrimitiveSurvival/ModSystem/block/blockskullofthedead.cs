namespace PrimitiveSurvival.ModSystem
{
    //using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Util;
    using Vintagestory.API.Client;
    using Vintagestory.API.Config;

    //using System.Diagnostics;

    public class BlockSkullOfTheDead : Block
    {
        //private static readonly Random Rnd = new Random();
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
        {
            return base.GetHeldTpUseAnimation(activeHotbarSlot, byEntity);
            //return null;
        }


        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!this.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            { return false; }

            var blockSrc = blockSel.Clone();
            blockSrc.Position.Y--;
            var block = world.BlockAccessor.GetBlock(blockSrc.Position, BlockLayersAccess.Default);
            if (block.Fertility <= 0)
            {
                failureCode = Lang.Get("softer-ground-needed");
                return false;
            }

            var location = new AssetLocation(this.Code.Domain, this.Code.Path);
            //Debug.WriteLine(location);
            var type = byPlayer.WorldData.EntityPlayer.World.GetEntityType(location);
            var entity = byPlayer.WorldData.EntityPlayer.World.ClassRegistry.CreateEntity(type);
            if (entity != null)
            {
                entity.ServerPos.X = blockSel.Position.X + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.X) + 0.5f;
                entity.ServerPos.Y = blockSel.Position.Y + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.Y);
                entity.ServerPos.Z = blockSel.Position.Z + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.Z) + 0.5f;

                entity.ServerPos.Yaw = 0f;
                entity.Pos.SetFrom(entity.ServerPos);
                entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
                entity.Attributes.SetString("origin", "playerplaced");
                byPlayer.WorldData.EntityPlayer.World.SpawnEntity(entity);

                if (this.api.Side == EnumAppSide.Server)
                {
                    byPlayer.WorldData.EntityPlayer.World.PlaySoundAt(new AssetLocation("game:sounds/player/buildhigh"), entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z, null, true, 32f, 1f);
                }
            }
            return true;
        }


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            var playerSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!playerSlot.Empty)
            {
                var playerStack = playerSlot.Itemstack;
                if (playerStack.Block != null)
                {
                    if (playerStack.Block.FirstCodePart() == "fruit")
                    {
                        Block blockToPlace = this;
                        var newPath = blockToPlace.Code.Path;
                        if (newPath.Contains("-empty"))
                        {
                            newPath = newPath.Replace("-empty", "-baited");
                            blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                            world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                            world.PlaySoundAt(new AssetLocation("sounds/player/buildhigh"), blockSel.Position.X + 0.5f, blockSel.Position.Y + 0.5f, blockSel.Position.Z + 0.5f, byPlayer);
                            playerSlot.TakeOut(1);
                            playerSlot.MarkDirty();
                            return true;
                        }
                    }
                }
                else if (playerStack.Item != null)
                {
                    if (playerStack.Item.FirstCodePart() == "fruit")
                    {
                        Block blockToPlace = this;
                        var newPath = blockToPlace.Code.Path;
                        if (newPath.Contains("-empty"))
                        {
                            newPath = newPath.Replace("-empty", "-baited");
                            blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                            world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                            world.PlaySoundAt(new AssetLocation("sounds/player/buildhigh"), blockSel.Position.X + 0.5f, blockSel.Position.Y + 0.5f, blockSel.Position.Z + 0.5f, byPlayer);
                            playerSlot.TakeOut(1);
                            playerSlot.MarkDirty();
                            return true;
                        }
                    }
                }
            }
            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[] {
        new WorldInteraction()
            {
                ActionLangCode = "heldhelp-place",
                MouseButton = EnumMouseButton.Right,
            }
        }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}
