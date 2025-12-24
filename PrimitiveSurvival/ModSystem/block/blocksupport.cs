namespace PrimitiveSurvival.ModSystem
{
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Config;
    using PrimitiveSurvival.ModConfig;
    using Vintagestory.GameContent;
    //using System.Diagnostics;


    public class BlockSupport : Block // BlockDisplayCase
    {
        string dom = "primitivesurvival:";

        //public override void OnLoaded(ICoreAPI api)
        //{}

        //*******************************************************************************************
        // emptyPos is where the invisible pipe side lives
        public void NotifyNeighborsOfBlockChange(BlockPos pos, IWorldAccessor world)
        {
            foreach (var facing in BlockFacing.HORIZONTALS) //was ALLFACES for the Up Down experiment
            {
                var npos = pos.AddCopy(facing);
                var neib = world.BlockAccessor.GetBlock(npos, BlockLayersAccess.Default);
                neib.OnNeighbourBlockChange(world, npos, pos);
            }
        }


        //*******************************************************************************************
        //this handles all the pipe ends
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);

            var ba = world.BlockAccessor;
            if (pos == neibpos) //WTF They can be equal?
            { return; }

            var thisBlock = ba.GetBlock(pos, BlockLayersAccess.Default);
            var neibBlock = ba.GetBlock(neibpos, BlockLayersAccess.Default);
            var sameOrientation = thisBlock.LastCodePart() == neibBlock.LastCodePart();
            var sameElevation = pos.Y == neibpos.Y;


            var thisPipe = false;
            var beBlock = ba.GetBlockEntity(pos) as BESupport;
            if (beBlock != null)
            {
                if (!beBlock.Inventory[0].Empty)
                {
                    //immediately break it if unsupported
                    bool broken = beBlock.BreakIfUnsupported(pos);
                    if (!broken)
                    {
                        thisPipe = true;
                    }
                }
            }

            var neibPipe = false;
            var bebBlock = ba.GetBlockEntity(neibpos) as BESupport;
            if (bebBlock != null)
            {
                if (!bebBlock.Inventory[0].Empty)
                {
                    //immediately break it if unsupported
                    bool broken = bebBlock.BreakIfUnsupported(neibpos);
                    if (!broken)
                    {
                        neibPipe = true;
                    }
                }
            }

            //connect them by changing the current connections of the two - start with the target block
            bool beUpdated = false, bebUpdated = false;


            //hmmm
            //
            // the !sameElevation stuff doesn't account for empty sides
            // only main sides of pipes so basically SW only

            //
            // also adding more connections 
            // IS SURE TO BREAK OTHER SHIT!!!!
            // will need to accomodate that



            if (beBlock != null)
            {
                if (thisPipe && neibPipe && sameElevation)
                {
                    if (sameOrientation)
                    { beUpdated = beBlock.AddConnection(pos, neibpos.FacingFrom(pos)); }
                    else
                    { beUpdated = beBlock.RemoveConnection(pos, neibpos.FacingFrom(pos)); }
                }

                /*else if (thisPipe && neibPipe && !sameElevation) //Up Down experiment
                {
                    if (!sameOrientation)
                    { beUpdated = beBlock.AddConnection(pos, pos.FacingFrom(neibpos)); }
                    else
                    { beUpdated = beBlock.RemoveConnection(pos, pos.FacingFrom(neibpos)); }
                }*/
                else
                {
                    beUpdated = beBlock.RemoveConnection(pos, neibpos.FacingFrom(pos));
                }
            }

            if (bebBlock != null)
            {
                if (thisPipe && neibPipe && sameElevation)
                {
                    if (sameOrientation)
                    { bebUpdated = bebBlock.AddConnection(neibpos, pos.FacingFrom(neibpos)); }
                    else
                    { bebUpdated = bebBlock.RemoveConnection(neibpos, pos.FacingFrom(neibpos)); }
                }
                /*
                else if (thisPipe && neibPipe && !sameElevation) //Up Down experiment
                {
                    if (!sameOrientation)
                    { bebUpdated = bebBlock.AddConnection(neibpos, neibpos.FacingFrom(pos)); }
                    else
                    { bebUpdated = bebBlock.RemoveConnection(neibpos, neibpos.FacingFrom(pos)); }
                }*/
                else
                {
                    bebUpdated = bebBlock.RemoveConnection(neibpos, pos.FacingFrom(neibpos));
                }
            }

            if (beUpdated)
            { beBlock.MarkDirty(true); }

            if (bebUpdated)
            { bebBlock.MarkDirty(true); }
        }



        //*******************************************************************************************
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            //base.OnBlockBroken(world, pos, byPlayer);
            // Broke a support OR a pipe
            var testBlock = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            var direction = testBlock.LastCodePart();
            var material = testBlock.FirstCodePart(1);
            var type = testBlock.FirstCodePart(2);
            BlockPos emptyPos;
            BlockPos mainPos;

            var ba = world.BlockAccessor;

            if (type == "empty")
            {
                emptyPos = pos; // targeting the empty side
                if (direction == "ns")
                { mainPos = pos.SouthCopy(); }
                else // ew
                { mainPos = pos.WestCopy(); }
            }
            else
            {
                mainPos = pos; // targeting the main side
                if (direction == "ns")
                { emptyPos = pos.NorthCopy(); }
                else // ew
                { emptyPos = pos.EastCopy(); }
            }

            if (emptyPos == null || mainPos == null)
            { return; }

            //I commented this out so you could still remove the support
            //even if the game was loaded without the mod installed by accident
            //same thing for the main block
            //if (ba.GetBlockEntity(emptyPos) is BESupport)
            //{ 
            ba.RemoveBlockEntity(emptyPos); // clear empty pos
            //} 
            ba.SetBlock(0, emptyPos);

            testBlock = ba.GetBlock(mainPos, BlockLayersAccess.Default);
            if (testBlock.FirstCodePart(1) == "none")
            {
                if (ba.GetBlockEntity(mainPos) is BESupport be)
                {
                    if (!be.Inventory[0].Empty)
                    {
                        string asset = be.Inventory[0].Itemstack.Collectible.Code.Path;
                        AssetLocation assetloc = new AssetLocation(dom + asset);
                        var dropBlock = world.GetBlock(assetloc);
                        if (dropBlock != null && world.Side == EnumAppSide.Server)
                        {
                            ItemStack stack = new ItemStack(dropBlock);
                            this.api.World.SpawnItemEntity(stack, pos.ToVec3d().AddCopy(0.5, 1.25, 0.5));
                        }
                    }
                    ba.RemoveBlockEntity(mainPos);
                }
                ba.SetBlock(0, mainPos);
            }
            else if (ba.GetBlockEntity(mainPos) is BESupport be)
            {
                string asset = dom + "support-" + material + "-main-ns";
                AssetLocation assetloc = new AssetLocation(asset);
                var dropBlock = world.GetBlock(assetloc);
                if (dropBlock != null && world.Side == EnumAppSide.Server)
                {
                    ItemStack stack = new ItemStack(dropBlock);
                    this.api.World.SpawnItemEntity(stack, pos.ToVec3d().AddCopy(0.5, 1.25, 0.5));
                }
                be.OnBlockBroken();
                base.OnBlockBroken(world, mainPos, byPlayer, 0);
            }
            else
            {
                //this should never happen
                //I added this so you could still remove the support
                //even if the game was loaded without the mod installed by accident
                base.OnBlockBroken(world, mainPos, byPlayer, 0);
            }
            this.NotifyNeighborsOfBlockChange(mainPos, world);
        }


        //*******************************************************************************************
        // This is for placing supports on fertile ground or farmland, NOT pipes - TESTED AOK
        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            // sigh. let's not micromanage this. It will just come back to bite me later
            var ba = world.BlockAccessor;
            var blockBelowPos = blockSel.Position.Copy();
            blockBelowPos.Y -= 1;

            var blockBelow = ba.GetBlock(blockBelowPos, BlockLayersAccess.Default);
            if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
            {
                if (api.World.Side == EnumAppSide.Client)
                {
                    failureCode = Lang.Get("support-ground-unsuitable");
                    return false;
                }
            }

            var material = itemstack.Collectible.FirstCodePart(1);
            var finalFacing = "";
            var targetPos = blockSel.Position;
            if (blockBelow.Code.Path.Contains("crop"))
            { targetPos.Y -= 1; }
            BlockPos[] neibPos = null;

            var playerFacing = SuggestedHVOrientation(byPlayer, blockSel)[0].ToString();
            if (playerFacing == "north")
            {
                blockBelow = ba.GetBlock(targetPos.NorthCopy().DownCopy(), BlockLayersAccess.Default);
                neibPos = new BlockPos[] { targetPos.UpCopy(3), targetPos.UpCopy(3).NorthCopy() };
                finalFacing = "ns";

            }
            else if (playerFacing == "east")
            {
                blockBelow = ba.GetBlock(targetPos.EastCopy().DownCopy(), BlockLayersAccess.Default);
                neibPos = new BlockPos[] { targetPos.UpCopy(2), targetPos.UpCopy(2).EastCopy() };
                finalFacing = "ew";
            }
            else if (playerFacing == "south")
            {
                blockBelow = ba.GetBlock(targetPos.SouthCopy().DownCopy(), BlockLayersAccess.Default);
                neibPos = new BlockPos[] { targetPos.UpCopy(3).SouthCopy(), targetPos.UpCopy(3) };
                finalFacing = "ns";
            }
            else if (playerFacing == "west")
            {
                blockBelow = ba.GetBlock(targetPos.WestCopy().DownCopy(), BlockLayersAccess.Default);
                neibPos = new BlockPos[] { targetPos.UpCopy(2).WestCopy(),
            targetPos.UpCopy(2)};
                finalFacing = "ew";
            }

            if (!blockBelow.Code.Path.Contains("crop") && !(blockBelow.Fertility > 0) && (!blockBelow.Code.Path.Contains("farmland")))
            {
                if (api.World.Side == EnumAppSide.Client)
                {
                    failureCode = Lang.Get("support-ground-unsuitable");
                    return false;
                }
            }
            foreach (var neib in neibPos)
            {
                var testBlock = this.api.World.BlockAccessor.GetBlock(neib, BlockLayersAccess.Default);
                if (testBlock.BlockId != 0)
                { return false; }
            }

            var count = 0;
            foreach (var neib in neibPos)
            {
                var testBlock = this.api.World.BlockAccessor.GetBlock(neib, BlockLayersAccess.Default);
                var asset1 = dom + "support-" + material + "-";
                if (count == 0)
                { asset1 += "main"; }
                else
                { asset1 += "empty"; }
                asset1 += "-" + finalFacing;
                var block = this.api.World.GetBlock(new AssetLocation(asset1));
                if (block != null)
                {
                    this.api.World.BlockAccessor.SetBlock(block.BlockId, neib);
                }
                count++;
            }
            return true;
        }


        //*******************************************************************************************
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }


        //*******************************************************************************************
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            bool result = false;
            var ba = world.BlockAccessor;
            var testBlock = ba.GetBlock(blockSel.Position, BlockLayersAccess.Default);
            var type = testBlock.FirstCodePart(2);

            if (type == "main")
            {
                if (ba.GetBlockEntity(blockSel.Position) is BESupport be)
                {
                    result = be.OnInteract(byPlayer, blockSel.Position);
                }
            }
            else //empty
            {
                var direction = testBlock.LastCodePart();
                var neib = blockSel.Position.WestCopy();
                if (direction == "ns")
                { neib = blockSel.Position.SouthCopy(); }
                if (ba.GetBlockEntity(neib) is BESupport be)
                {
                    result = be.OnInteract(byPlayer, neib);
                }
            }
            return result;
        }


        //*******************************************************************************************
        //genmesh for supports
        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, string supportDir)
        {
            Shape shape = null;
            var tesselator = capi.Tesselator;
            var shapeAsset = capi.Assets.TryGet(shapePath + ".json");
            if (shapeAsset != null)
            {
                shape = shapeAsset.ToObject<Shape>();
                tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0, 0, 0));
                mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, this.Shape.rotateY * GameMath.DEG2RAD, 0); //orient based on direction last
                if (supportDir == "ew")
                { mesh.Translate(0f, 0.35f, 0f); }
                else
                { mesh.Translate(0f, -0.5f, 0f); }
                return mesh;
            }
            return null;
        }

        //*******************************************************************************************
        // Genmesh for other inventory slots
        public MeshData GenInventoryMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, string supportDir)
        {
            Shape shape;
            var tesselator = capi.Tesselator;
            var shapeAsset = capi.Assets.TryGet(shapePath + ".json");
            if (shapeAsset != null)
            {
                shape = shapeAsset.ToObject<Shape>();
                tesselator.TesselateShape(shapePath, shape, out var mesh, texture, null, 0);
                if (mesh != null)
                {
                    if (supportDir == "ew")
                    {
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 90 * GameMath.DEG2RAD, 0);
                        mesh.Translate(0.5f, 0.85f, 0f);
                    }
                    else
                    {
                        mesh.Translate(0f, 0f, -0.5f);
                    }
                    return mesh;
                }
            }
            return null;
        }


        //*******************************************************************************************
        // Genmesh for pipes + sides
        public MeshData GenSideMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture, char side, string supportDir)
        {
            //hijack this to put the new "end" shape in place of the side shape as required
            //check for a barrel or irrigation vessel or water block
            //alternatively, we could do it in addonnection or ontesselation

            Shape shape;
            var tesselator = capi.Tesselator;
            var shapeAsset = capi.Assets.TryGet(shapePath + ".json");
            if (shapeAsset != null)
            {
                shape = shapeAsset.ToObject<Shape>();
                tesselator.TesselateShape(shapePath, shape, out var mesh, texture, null, 0);
                if (mesh != null)
                {
                    mesh.Translate(0f, 0f, 0.5f);
                    switch (side)
                    {
                        case 'e':
                            {
                                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0 * GameMath.DEG2RAD, 0);
                                if (supportDir == "ns")
                                { mesh.Translate(0f, 0f, -1f); }
                                break;
                            }
                        case 'w':
                            {
                                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 180 * GameMath.DEG2RAD, 0);
                                break;
                            }
                        case 'n':
                            {
                                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 90 * GameMath.DEG2RAD, 0);
                                if (supportDir == "ew")
                                { mesh.Translate(0f, 0.85f, 0f); }
                                break;
                            }
                        case 's':
                            {
                                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, -90 * GameMath.DEG2RAD, 0);
                                if (supportDir == "ew")
                                { mesh.Translate(1f, 0.85f, 0f); }
                                break;
                            }
                        case 'd': //blockage
                            {
                                mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 0, 0);
                                mesh.Translate(1f, 0f, 0f);
                                mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.5f, 0.5f, 0.5f);
                                break;
                            }
                        default:
                            { break; }
                    }
                    return mesh;
                }
            }
            return null;
        }

        //*******************************************************************************************
        //genmesh for water
        public MeshData RenderWater(bool pipeEnd, string supportDir, string currentConnections, string currentEndpoints)
        {
            var shapePath = dom + "shapes/block/pipe/wood/basewater";
            if (pipeEnd)
            { shapePath = dom + "shapes/block/pipe/wood/endwater"; }

            var tempBlock = api.World.GetBlock(new AssetLocation(dom + "texturewater"));
            var tmpTextureSource = ((ICoreClientAPI)api).Tesselator.GetTextureSource(tempBlock);
            var mesh = this.GenInventoryMesh((ICoreClientAPI)api, shapePath, tmpTextureSource, supportDir);
            if (pipeEnd && mesh != null)
            {
                if (currentConnections == "e" || currentEndpoints == "w")
                {
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 180 * GameMath.DEG2RAD, 0);
                    mesh.Translate(0f, 0f, -1f);
                }
                if (currentConnections == "n" || currentEndpoints == "s")
                {
                    mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, 180 * GameMath.DEG2RAD, 0);
                    mesh.Translate(1f, 0f, 0f);
                }
            }
            return mesh;
        }


        //*******************************************************************************************
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            string material = inSlot.Itemstack.Collectible.FirstCodePart(1);
            dsc.AppendLine(Lang.GetMatching(dom + "support-iteminfo-" + material));
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }


        //*******************************************************************************************
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            var dsc = new StringBuilder();
            var ba = world.BlockAccessor;
            var testBlock = ba.GetBlock(pos, BlockLayersAccess.Default);
            var direction = testBlock.LastCodePart();
            var material = testBlock.FirstCodePart(1);
            var type = testBlock.FirstCodePart(2);
            BlockPos mainPos = pos;

            if (type == "empty")
            {
                if (direction == "ns")
                { mainPos = pos.SouthCopy(); }
                else // ew
                { mainPos = pos.WestCopy(); }
                testBlock = ba.GetBlock(mainPos, BlockLayersAccess.Default);
                direction = testBlock.LastCodePart();
                material = testBlock.FirstCodePart(1);
                type = testBlock.FirstCodePart(2);
            }

            string heightOffset = "lowered";
            if (direction == "ns")
            { heightOffset = "raised"; }
            if (material != "none")
            {
                dsc.AppendLine(Lang.GetMatching(dom + "support-material") + ": " + Lang.GetMatching(dom + "pipe-support-material-" + material));
                dsc.AppendLine(Lang.GetMatching(dom + "support-heightoffset") + ": " + Lang.GetMatching(dom + "support-pipe-heightoffset-" + heightOffset)).AppendLine();
            }

            if (ba.GetBlockEntity(mainPos) is BESupport be)
            {
                var pipe = be.Inventory[0].Itemstack;
                if (pipe != null)
                {
                    string pipeMaterial = pipe.Collectible.LastCodePart(1);
                    string pipeType = pipe.Collectible.LastCodePart();
                    dsc.AppendLine(Lang.GetMatching(dom + "pipe-type") + ": " + Lang.GetMatching(dom + "pipe-type-" + pipeType));
                    dsc.AppendLine(Lang.GetMatching(dom + "pipe-material") + ": " + Lang.GetMatching(dom + "pipe-support-material-" + pipeMaterial));
                    dsc.AppendLine(Lang.GetMatching(dom + "pipe-heightoffset") + ": " + Lang.GetMatching(dom + "support-pipe-heightoffset-" + heightOffset)).AppendLine();
                    dsc.AppendLine(Lang.GetMatching(dom + "pipe-rightclick-add-pipe")).AppendLine();


                    //*********************
                    //Debug - TO REMOVE
                    string connections = be.GetConnections();
                    string endpoints = be.GetEndpoints();
                    dsc.AppendLine(Lang.GetMatching("Connections: " + connections));
                    dsc.AppendLine(Lang.GetMatching("Endpoints: " + endpoints)).AppendLine();

                    //Inventory debug
                    var stack = be.Inventory[0].Itemstack;
                    if (stack != null)
                    {
                        string contents = stack.Collectible.Code.Path;
                        dsc.AppendLine(Lang.GetMatching("Pipe: " + stack));
                    }
                    else
                    { dsc.AppendLine(Lang.GetMatching("Pipe: none")); }

                    stack = be.Inventory[1].Itemstack;
                    if (stack != null)
                    {
                        string contents = stack.Collectible.Code.Path;
                        dsc.AppendLine(Lang.GetMatching("Water: " + stack));
                    }
                    else
                    { dsc.AppendLine(Lang.GetMatching("Water: none")); }

                    stack = be.Inventory[2].Itemstack;
                    if (stack != null)
                    {
                        string contents = stack.Collectible.Code.Path;
                        dsc.AppendLine(Lang.GetMatching("Other: " + stack));
                    }
                    else
                    { dsc.AppendLine(Lang.GetMatching("Other: none")); }
                    //End Debug
                    //*********************

                }
                else
                {
                    dsc.AppendLine().AppendLine(Lang.GetMatching(dom + "support-rightclick-add-pipe")).AppendLine();
                }
            }
            if (ModConfig.Loaded.ShowModNameInHud)
            {
                dsc.AppendLine("\n<font color=\"#D8EAA3\"><i>" + Lang.GetMatching("game:tabname-primitive") + "</i></font>").AppendLine();
            }

            return dsc.ToString();
        }
    }
}
