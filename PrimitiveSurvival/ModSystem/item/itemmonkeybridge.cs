namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using PrimitiveSurvival.ModConfig;
    using System.Collections.Generic;
    using Vintagestory.API.Client;
    using Vintagestory.API.Config;
    //using System.Diagnostics;


    public class ItemMonkeyBridge : Item
    {

        // block highlighter
        bool highlighterEnabled;
        int highlightSlotId1 = 0; //for multiple highlights use a different number
        int highlightSlotId2 = 1; //for multiple highlights use a different number
        List<BlockPos> goodblocks = new List<BlockPos>();
        List<BlockPos> badblocks = new List<BlockPos>();
        List<int> goodcolors = new List<int>();
        List<int> badcolors = new List<int>();
        float scale = 1f;



        string inGameErrorCode;


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            inGameErrorCode = "";
            goodcolors.Add(ColorUtil.ColorFromRgba(94, 215, 94, 64));
            badcolors.Add(ColorUtil.ColorFromRgba(215, 94, 94, 64));
        }


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
            //Debug.WriteLine("Block Width: " + widthStr);
            return widthStr;
        }

        // was static, now virtual
        public virtual bool ValidEndpoint(IBlockAccessor blockAccessor, BlockPos testpos)
        {
            var blockChk = blockAccessor.GetBlock(testpos, BlockLayersAccess.Default);
            bool validEnd;

            //can't stack on a trotline? wtf is this
            if (blockChk.Code.GetName().Contains("limbtrotlinelure")) 
            { 
                validEnd = false;
                badblocks.Add(testpos);
                { inGameErrorCode = "endpoint-invalid"; }
            }
            else
            {
                var sb = blockChk.GetSelectionBoxes(blockAccessor, testpos);
                validEnd = BlockHeight(blockAccessor, testpos) == "large";
                if (validEnd)
                { goodblocks.Add(testpos); }
                else
                { 
                    badblocks.Add(testpos);
                    inGameErrorCode = "endpoint-invalid";
                }

                BlockPos aroundPos;
                BlockPos[] around;

                if (validEnd)
                {
                    aroundPos = new BlockPos(testpos.X, testpos.Y, testpos.Z, 0);
                    around = AreaAround(aroundPos);
                    foreach (var neighbor in around)
                    {
                        blockChk = blockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                        if (blockChk.BlockId > 0)
                        {
                            if (BlockHeight(blockAccessor, neighbor) != "small" && !blockChk.Code.Path.Contains("monkeybridge"))
                            {
                                validEnd = false;
                                inGameErrorCode = "endpoint-neighbor-invalid";
                                badblocks.Add(neighbor);
                            }
                            else
                            { goodblocks.Add(neighbor); }
                        }
                        else
                        { goodblocks.Add(neighbor); }
                    }
                }
                else
                {
                    inGameErrorCode = "endpoint-invalid";
                }
                if (validEnd) //check the block above too
                {
                    testpos = testpos.UpCopy();
                    validEnd = BlockHeight(blockAccessor, testpos) == "large";
                    if (validEnd)
                    { goodblocks.Add(testpos); }
                    else
                    { 
                        badblocks.Add(testpos);
                        inGameErrorCode = "endpoint-invalid";
                    }

                    if (validEnd)
                    {
                        aroundPos = new BlockPos(testpos.X, testpos.Y, testpos.Z, 0);
                        around = AreaAround(aroundPos);
                        foreach (var neighbor in around)
                        {
                            blockChk = blockAccessor.GetBlock(neighbor, BlockLayersAccess.Default);
                            if (blockChk.BlockId > 0)
                            {

                                if (BlockHeight(blockAccessor, neighbor) != "small" && !blockChk.Code.Path.Contains("monkeybridge"))
                                {
                                    validEnd = false;
                                    inGameErrorCode = "endpoint-neighbor-invalid";
                                    badblocks.Add(neighbor);
                                }
                                else
                                { goodblocks.Add(neighbor); }
                            }
                            else
                            { goodblocks.Add(neighbor); }
                        }
                    }
                    else 
                    { inGameErrorCode = "endpoint-invalid"; }
                }
            }
            //Debug.WriteLine("Valid Endpoint: " + validEnd);
            return validEnd;
        }


        public virtual int GetLineLength(IBlockAccessor blockAccessor, BlockSelection blockDest, BlockFacing facing)
        {
            var maxLength = ModConfig.Loaded.MonkeyBridgeMaxLength;
            var count = 0;
            var foundEnd = false;
            var testpos = blockDest.Position.Copy();

            Block blockChk;
            var testPath = "primitivesurvival:monkeybridge-middle-north";
            var testBlock = blockAccessor.GetBlock(new AssetLocation(testPath));
            do
            {
                count++;
                testpos = testpos.Offset(facing);
                blockChk = blockAccessor.GetBlock(testpos, BlockLayersAccess.Default);
                if (!blockChk.IsReplacableBy(testBlock))
                {
                    if ((BlockHeight(blockAccessor, testpos) != "small") || blockChk.Code.GetName().Contains("monkeybridge-"))
                    {
                        foundEnd = true;
                        if (count == 1)
                        {
                            goodblocks.Add(testpos.Copy());
                            return 0; 
                        }
                        else if (count == 2)
                        {
                            inGameErrorCode = "mb-far-endpoint-invalid";
                            badblocks.Add(testpos.Copy());
                            return 1; 
                        }
                        else if (!ValidEndpoint(blockAccessor, testpos))
                        {
                            inGameErrorCode = "mb-far-" + inGameErrorCode;
                            badblocks.Add(testpos.Copy());
                            return 1; 
                        }
                        else 
                        { goodblocks.Add(testpos.Copy()); }
                    }
                    else
                    {
                        inGameErrorCode = "mb-far-" + inGameErrorCode;
                        badblocks.Add(testpos.Copy()); 
                    }
                }
                else
                { goodblocks.Add(testpos.Copy()); }
            }
            while ((foundEnd == false) && (count < maxLength));
            if (!foundEnd)
            {
                inGameErrorCode = "mb-bridge-too-far";
                badblocks.Add(testpos.Copy());
                return 1;
            }
            //Debug.WriteLine("Line Length: " + count);
            return count;
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            //is Kai's issue that byEntity is null? or is it that byPlayer can't be set to null? what the hell
            //I've shored up error reporting and added debug highlighting, but lets also try catch this thing
            try
            {
                highlighterEnabled = false; //set to true to debug this thing

                IPlayer byPlayer = null;
                inGameErrorCode = "";
                goodblocks.Clear();
                badblocks.Clear();

                if (byEntity is EntityPlayer player)
                { byPlayer = byEntity.World.PlayerByUid(player.PlayerUID); }
                if (blockSel == null || byEntity.World == null || byPlayer == null)
                { return; }

                //var facing = byPlayer.CurrentBlockSelection.Face.Opposite;
                var facing = byPlayer?.CurrentBlockSelection?.Face?.Opposite;
                if (facing == null)
                { return; }


                var blockAccessor = byEntity.World.BlockAccessor;
                var currPos = blockSel.Position.Copy();
                var validStart = ValidEndpoint(blockAccessor, currPos);
                var splr = byPlayer as IServerPlayer;

                if (!validStart)
                {
                    inGameErrorCode = "mb-close-" + inGameErrorCode;
                    badblocks.Add(currPos);
                    splr?.SendIngameError("mb-error", Lang.Get("primitivesurvival:ingameerror-" + inGameErrorCode));
                }
                else
                {
                    goodblocks.Add(currPos);
                }

                if (facing.IsHorizontal && validStart)
                {
                    var linelength = this.GetLineLength(blockAccessor, blockSel, facing);
                    var stack = slot.Itemstack;
                    if ((slot.StackSize < linelength - 1) && (linelength > 0))
                    { linelength = 999; }
                    if (linelength > 0)
                    {
                        if (linelength > ModConfig.Loaded.MonkeyBridgeMaxLength)
                        { linelength = 1; } //every once in a while linelength loses its s*&t maybe this prevents that???
                        if (linelength <= 1)
                        {
                            if (inGameErrorCode == "")
                            { inGameErrorCode = "mb-bridge-too-far"; }
                            splr?.SendIngameError("mb-error", Lang.Get("primitivesurvival:ingameerror-" + inGameErrorCode));
                        }

                        //deal with this monkeybridge directly under a monkey bridge business
                        var abovePos = currPos.AddCopy(facing).UpCopy();
                        var blockChk = blockAccessor.GetBlock(abovePos, BlockLayersAccess.Default);
                        if (linelength > 1 && blockChk.Code.GetName().Contains("monkeybridge-"))
                        {
                            inGameErrorCode = "mb-bridge-cant-place";
                            splr?.SendIngameError("mb-error", Lang.Get("primitivesurvival:ingameerror-" + inGameErrorCode));
                        }
                        else if (linelength > 1)
                        {
                            var blockSize = BlockWidth(blockAccessor, currPos);
                            string newPath;
                            Block blocknew;
                            BlockPos nullPos;
                            for (var count = 0; count < linelength; count++)
                            {
                                newPath = "primitivesurvival:monkeybridge-";
                                if (count == 0)
                                {
                                    currPos = currPos.AddCopy(facing);
                                    newPath += "end-" + facing.ToString();
                                    blocknew = byEntity.World.GetBlock(new AssetLocation(newPath));
                                    blockAccessor.SetBlock(blocknew.BlockId, currPos);
                                    nullPos = new BlockPos(currPos.X, currPos.Y + 1, currPos.Z, 0);
                                    blocknew = byEntity.World.GetBlock(new AssetLocation(newPath.Replace("-end", "-null")));
                                    blockAccessor.SetBlock(blocknew.BlockId, nullPos);
                                }
                                else if (count < linelength - 1)
                                {
                                    currPos = currPos.AddCopy(facing);
                                    //goodblocks.Add(currPos);
                                    newPath += "middle-" + facing.ToString();
                                    blocknew = byEntity.World.GetBlock(new AssetLocation(newPath));
                                    blockAccessor.SetBlock(blocknew.BlockId, currPos);
                                }
                            }
                            //the last block
                            var endPos = currPos.AddCopy(facing);
                            blockSize = BlockWidth(blockAccessor, endPos);
                            newPath = "primitivesurvival:monkeybridge-end-" + byPlayer.CurrentBlockSelection.Face.ToString();
                            blocknew = byEntity.World.GetBlock(new AssetLocation(newPath));
                            blockAccessor.SetBlock(blocknew.BlockId, currPos);

                            nullPos = new BlockPos(currPos.X, currPos.Y + 1, currPos.Z, 0);
                            blocknew = byEntity.World.GetBlock(new AssetLocation(newPath.Replace("-end", "-null")));
                            blockAccessor.SetBlock(blocknew.BlockId, nullPos);

                            linelength -= 1; //fix to ensure we're removing the correct amount of cordage
                            slot.TakeOut(linelength);
                            slot.MarkDirty();
                        }
                    }
                    else 
                    {
                        inGameErrorCode = "mb-bridge-cant-place";
                        splr?.SendIngameError("mb-error", Lang.Get("primitivesurvival:ingameerror-" + inGameErrorCode));
                    }
                }
                else
                {
                    if (highlighterEnabled)
                    {
                        byEntity.World.HighlightBlocks(byPlayer, highlightSlotId1, goodblocks, goodcolors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary, scale);
                        byEntity.World.HighlightBlocks(byPlayer, highlightSlotId2, badblocks, badcolors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary, scale);
                    }
                }
            }
            catch
            {
                //Debug.WriteLine(e);
            }
        }
    }
}
