namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;


    public class ItemCordage : Item
    {

        private static BlockPos[] AreaAround(BlockPos pos)
        {
            return new BlockPos[]
{  pos.NorthCopy(),pos.SouthCopy(),pos.EastCopy(),pos.WestCopy(),pos.NorthCopy().EastCopy(), pos.SouthCopy().WestCopy(),pos.SouthCopy().EastCopy(), pos.NorthCopy().WestCopy() };
        }

        public static string BlockHeight(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var heightStr = "small";
            var blockChk = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (blockChk.BlockId > 0)
            {
                var sbs = blockChk.GetSelectionBoxes(blockAccessor, pos);
                if (sbs == null)
                { return heightStr; }
                foreach (var sb in sbs)
                {
                    if (Math.Abs(sb.Y2 - sb.Y1) >= 0.5)
                    { heightStr = "large"; }
                    else if ((Math.Abs(sb.Y2 - sb.Y1) >= 0.3) && heightStr != "large")
                    { heightStr = "medium"; }
                }
            }
            return heightStr;
        }


        public static string BlockWidth(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var widthStr = "small";
            var blockChk = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (blockChk.BlockId > 0)
            {
                if (blockChk.Code.Path.Contains("stake-") || blockChk.Code.Path.Contains("stakeinwater-"))
                { widthStr = "small"; }
                else if (blockChk.Code.GetName().Contains("woodenfence-"))
                { widthStr = "medium"; }
                else
                {
                    var sbs = blockChk.GetSelectionBoxes(blockAccessor, pos);
                    foreach (var sb in sbs)
                    {
                        if (Math.Abs(sb.X2 - sb.X1) > 0.5)
                        { widthStr = "large"; }
                    }
                }
            }
            return widthStr;
        }


        public static bool ValidEndpoint(IBlockAccessor blockAccessor, BlockPos testpos)
        {
            var blockChk = blockAccessor.GetBlock(testpos, BlockLayersAccess.Default);
            bool validEnd;
            if (blockChk.Code.GetName().Contains("woodenfence-") || blockChk.Code.Path.Contains("stake-") || blockChk.Code.Path.Contains("stakeinwater-"))
            { validEnd = true; }
            else if (blockChk.Code.GetName().Contains("limbtrotlinelure"))
            { validEnd = false; }
            else
            {
                validEnd = BlockHeight(blockAccessor, testpos) == "large";
                var aroundPos = new BlockPos(testpos.X, testpos.Y, testpos.Z);
                var around = AreaAround(aroundPos);
                foreach (var neighbor in around)
                {
                    blockChk = blockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                    if (blockChk.BlockId > 0)
                    {
                        if (BlockHeight(blockAccessor, neighbor) != "small" && !blockChk.Code.Path.Contains("limbtrotlinelure"))
                        {
                            if ((!blockChk.Code.GetName().Contains("woodenfence-")) && !blockChk.Code.Path.Contains("stake-") && !blockChk.Code.Path.Contains("stakeinwater-"))
                            { validEnd = false; }
                        }
                    }
                }
            }
            return validEnd;
        }


        public virtual int GetLineLength(IBlockAccessor blockAccessor, BlockSelection blockDest, BlockFacing facing)
        {
            var maxLength = 20;
            var count = 0;
            var foundEnd = false;
            var testpos = blockDest.Position.Copy();
            Block blockChk;
            var testPath = "primitivesurvival:limbtrotlinelure-middle-north";
            var testBlock = blockAccessor.GetBlock(new AssetLocation(testPath));
            do
            {
                count++;
                testpos = testpos.Offset(facing);
                blockChk = blockAccessor.GetBlock(testpos, BlockLayersAccess.Default);
                if (!blockChk.IsReplacableBy(testBlock))
                {
                    if ((BlockHeight(blockAccessor, testpos) != "small") || blockChk.Code.GetName().Contains("limbtrotlinelure-"))
                    {
                        foundEnd = true;
                        if (count == 1)
                        { return 0; }
                        if (count == 2)
                        { return 1; }
                        if (!ValidEndpoint(blockAccessor, testpos))
                        { return 1; }
                    }
                }
            }
            while ((foundEnd == false) && (count < maxLength));
            if (!foundEnd)
            { return 1; }
            return count;
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player)
            { byPlayer = byEntity.World.PlayerByUid(player.PlayerUID); }
            if (blockSel == null || byEntity.World == null || byPlayer == null)
            { return; }

            var facing = byPlayer.CurrentBlockSelection.Face.Opposite;
            var blockAccessor = byEntity.World.BlockAccessor;
            var currPos = blockSel.Position.Copy();


            //quick fix to make cordage play nice with weir traps 1.17
            var blocktemp = blockAccessor.GetBlock(currPos, BlockLayersAccess.Fluid);
            if (blocktemp.Code.Path.Contains("water"))
            { return; }

            var validStart = ValidEndpoint(blockAccessor, currPos);

            if (facing.IsHorizontal && validStart)
            {
                var linelength = this.GetLineLength(blockAccessor, blockSel, facing);
                var stack = slot.Itemstack;
                var tempStr = "";
                if ((slot.StackSize < linelength - 1) && (linelength > 0))
                { linelength = 999; }
                if (linelength > 0)
                {
                    if (linelength > 20)
                    { linelength = 1; } //every once in a while linelength loses its s*&t maybe this prevents that???
                    var blockSize = BlockWidth(blockAccessor, currPos);
                    string newPath;
                    for (var count = 0; count < linelength; count++)
                    {
                        newPath = "primitivesurvival:limbtrotlinelure-";
                        if (count == 0)
                        {
                            currPos = currPos.AddCopy(facing);
                            if (linelength > 1)
                            { tempStr = "withmiddle-"; }
                            newPath += "end-" + blockSize + "-" + tempStr + facing.ToString();
                            var blocknew = byEntity.World.GetBlock(new AssetLocation(newPath));
                            blockAccessor.SetBlock(blocknew.BlockId, currPos);
                        }
                        else if (count < linelength - 1)
                        {
                            currPos = currPos.AddCopy(facing);
                            newPath += "middle-" + facing.ToString();
                            var blocknew = byEntity.World.GetBlock(new AssetLocation(newPath));
                            blockAccessor.SetBlock(blocknew.BlockId, currPos);
                        }
                    }

                    if (linelength > 1) //the last block
                    {
                        var endPos = currPos.AddCopy(facing);
                        blockSize = BlockWidth(blockAccessor, endPos);
                        newPath = "primitivesurvival:limbtrotlinelure-end-" + blockSize + "-withmiddle-" + byPlayer.CurrentBlockSelection.Face.ToString();
                        var blocknew = byEntity.World.GetBlock(new AssetLocation(newPath));
                        blockAccessor.SetBlock(blocknew.BlockId, currPos);
                        linelength -= 1; //fix to ensure we're removing the correct amount of cordage
                    }
                    slot.TakeOut(linelength);
                    slot.MarkDirty();
                }
            }
        }
    }
}
