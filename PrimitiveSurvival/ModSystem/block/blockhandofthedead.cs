namespace PrimitiveSurvival.ModSystem
{
    using System;
    //using System.Diagnostics;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;

    public class BlockHandOfTheDead : Block
    {
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            //if (!this.HasSolidGround(world.BlockAccessor, blockSel.Position))
            //{ return false; }

            var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var face = blockSel.Face.ToString();
            string newPath;
            Block blockToPlace = this;
            if (face != "up" && face != "down") //wall
            {
                newPath = blockToPlace.Code.Path;
                face = blockSel.Face.Opposite.ToString(); //rotate 180
                newPath = newPath.Replace("north", face);
                blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                if (blockToPlace != null)
                {
                    world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                    return true;
                }
                return false;
            }

            if (blockToPlace != null)
            {
                string facing;
                var targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                var dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                var dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                var angle = Math.Atan2(dx, dz);
                angle += Math.PI;
                angle /= Math.PI / 4;
                var halfQuarter = Convert.ToInt32(angle);
                halfQuarter %= 8;

                if (halfQuarter == 4)
                { facing = "south"; }
                else if (halfQuarter == 6)
                { facing = "east"; }
                else if (halfQuarter == 2)
                { facing = "west"; }
                else if (halfQuarter == 7)
                { facing = "northeast"; }
                else if (halfQuarter == 1)
                { facing = "northwest"; }
                else if (halfQuarter == 5)
                { facing = "southeast"; }
                else if (halfQuarter == 3)
                { facing = "southwest"; }
                else
                { facing = "north"; }

                newPath = blockToPlace.Code.Path;
                newPath = newPath.Replace("north", facing);
                newPath = newPath.Replace("candle-", "candleplaced-");
                blockToPlace = this.api.World.GetBlock(blockToPlace.CodeWithPath(newPath));
                if (blockToPlace != null)
                {
                    world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
                    return true;
                }
            }
            return false;
        }

        internal virtual bool HasSolidGround(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var block = blockAccessor.GetBlock(pos.DownCopy(), BlockLayersAccess.Default);
            return block.CanAttachBlockAt(blockAccessor, this, pos, BlockFacing.UP);
        }
    }
}
