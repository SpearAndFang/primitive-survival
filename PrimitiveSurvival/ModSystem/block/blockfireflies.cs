namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Util;
    using Vintagestory.API.Client;
    //using System.Diagnostics;

    public class BlockFireflies : Block
    {
        //private static readonly Random Rnd = new Random();
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
        {
            return base.GetHeldTpUseAnimation(activeHotbarSlot, byEntity);
            //return null;
        }

        private string Facing(IPlayer byPlayer, BlockSelection blockSel)
        {
            var targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            var dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
            var dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
            var angle = Math.Atan2(dx, dz);
            angle += Math.PI;
            angle /= Math.PI / 4;
            var halfQuarter = Convert.ToInt32(angle);
            halfQuarter %= 8;
            if (halfQuarter % 2 == 0)
            { return "straight"; }
            else
            { return "angled"; }
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            if (!this.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            { return false; }

            if (this.Code.Path.Contains("-empty") || this.Code.Path.Contains("-baited"))
            {
                var blockBelow = world.BlockAccessor.GetBlock(blockSel.Position.DownCopy(), BlockLayersAccess.Default);
                if (blockBelow.BlockId == 0)
                {
                    failureCode = "requiresolidground";
                    return false;
                }

                Block blockToPlace = this;
                var newPath = blockToPlace.Code.Path;
                newPath = newPath.Replace("straight", this.Facing(byPlayer, blockSel));

                blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                return true;
            }
            else
            {
                var location = new AssetLocation(this.Code.Domain, this.CodeWithoutParts(1));
                //Debug.WriteLine(location);
                var type = byPlayer.WorldData.EntityPlayer.World.GetEntityType(location);
                var entity = byPlayer.WorldData.EntityPlayer.World.ClassRegistry.CreateEntity(type);
                if (entity != null)
                {
                    entity.ServerPos.X = blockSel.Position.X + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.X) + 0.5f;
                    entity.ServerPos.Y = blockSel.Position.Y + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.Y);
                    entity.ServerPos.Z = blockSel.Position.Z + (blockSel.DidOffset ? 0 : blockSel.Face.Normali.Z) + 0.5f;

                    if (this.Facing(byPlayer, blockSel) == "angled")
                    { entity.ServerPos.Yaw = 0.78f; }
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
