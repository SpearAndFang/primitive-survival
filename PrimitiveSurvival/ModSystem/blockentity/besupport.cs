namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using PrimitiveSurvival.ModConfig;
    using System.Diagnostics;
    using Vintagestory.API.Util;


    public class BESupport : BlockEntityDisplayCase, ITexPositionSource
    {
        private readonly int PipeUpdateFrequency = ModConfig.Loaded.PipeUpdateFrequency;
        private readonly double PipeBlockageChancePercent = ModConfig.Loaded.PipeBlockageChancePercent;
        private readonly double PipeMinMoisture = ModConfig.Loaded.PipeMinMoisture;

        private readonly int tickSeconds = 4;
        private readonly int maxSlots = 3; //pipe, liquid, blockage
        private readonly string[] pipeTypes = { "pipe" };
        private readonly string[] blockageTypes = { "rot", "drygrass" };
        protected static readonly Random Rnd = new Random();
        private readonly BlockPos tmpOffPos = new BlockPos(0);
        private readonly BlockPos tmpEndPos = new BlockPos(0);
        private readonly BlockPos tmpDistPos0 = new BlockPos(0);
        private readonly BlockPos tmpDistPos1 = new BlockPos(0);
        private readonly BlockPos tmpDistPos2 = new BlockPos(0);
        private readonly BlockPos tmpNeibPos0 = new BlockPos(0);
        private readonly BlockPos tmpNeibPos1 = new BlockPos(0);
        private readonly BlockPos[] distBlocks1;
        private readonly BlockPos[] distBlocks2;
        private readonly BlockPos[] distBlocks3;

        //the shape file(s)
        private readonly string endShape = "block/pipe/wood/end";
        private readonly string sideShape = "block/pipe/wood/side";

        //tree attributes
        private string currentConnections = ""; //i.e. "" for none, or  "nsewd" for all - can only have 3 max though - i.e. "nsu"
        private string currentEndpoints = ""; //endpoints are attached to water suppliers

        //tickers - remove particleTick and add it to pipeTick
        private long particleTick;
        private long pipeTick;

        //private AssetLocation wetPickupSound;
        private AssetLocation dryPickupSound;

        BlockSupport ownBlock;
        string[] sourceTypes = { "water-", "barrel", "irrigationvessel-" }; //irrigationvessel-normal
        string dom = "primitivesurvival:";

        public BESupport()
        {
            this.inventory = new InventoryGeneric(this.maxSlots, null, null);
            var meshes = new MeshData[this.maxSlots];
            this.distBlocks1 = new[] { this.tmpDistPos0 };
            this.distBlocks2 = new[] { this.tmpDistPos0, this.tmpDistPos1 };
            this.distBlocks3 = new[] { this.tmpDistPos0, this.tmpDistPos1, this.tmpDistPos2 };
        }


        //*******************************************************************************************
        //inventory setup
        public override string InventoryClassName => "support";
        protected new InventoryGeneric inventory;

        public override InventoryBase Inventory => this.inventory;
        public ItemSlot PipeSlot => this.inventory[0];
        public ItemSlot LiquidSlot => this.inventory[1];
        public ItemSlot OtherSlot => this.inventory[2];

        public ItemStack PipeStack
        {
            get => this.inventory[0].Itemstack;
            set => this.inventory[0].Itemstack = value;
        }

        public ItemStack LiquidStack
        {
            get => this.inventory[1].Itemstack;
            set => this.inventory[1].Itemstack = value;
        }

        public ItemStack OtherStack
        {
            get => this.inventory[2].Itemstack;
            set => this.inventory[2].Itemstack = value;
        }


        //*******************************************************************************************
        //main
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.ownBlock = Block as BlockSupport;
            if (api.Side.IsServer())
            {
                this.particleTick = this.RegisterGameTickListener(this.OnParticleTick, this.tickSeconds * 400); //1000
                //this.pipeTick = this.RegisterGameTickListener(this.OnPipeTick, this.PipeUpdateFrequency * 1000);
                this.pipeTick = this.RegisterGameTickListener(this.OnPipeTick, this.PipeUpdateFrequency * 10);
            }
            //this.wetPickupSound = new AssetLocation("game", "sounds/environment/smallsplash");
            this.dryPickupSound = new AssetLocation("game", "sounds/block/cloth");
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            this.UnregisterGameTickListener(this.particleTick);
            this.UnregisterGameTickListener(this.pipeTick);
        }


        //*******************************************************************************************
        //listeners + listener related
        private void GenerateWaterParticles(BlockPos pos, string dir)
        {
            float minQuantity = 1f;
            float maxQuantity = 1f;
            var color = ColorUtil.ToRgba(80, 125, 185, 225); //40
            var minPos = new Vec3d();
            var addPos = new Vec3d();
            var minVelocity = new Vec3f(0f, 0.0f, 0f);
            var lifeLength = 2f;
            var gravityEffect = 0.5f;
            var minSize = 0.3f;
            var maxSize = 0.3f;

            float dx, dy, dz;
            Vec3f maxVelocity;

            if (dir == "ew")
            {
                dx = 0.95f; dy = 0.9f; dz = 0.725f;
                var vel = 1f;
                var rando = Rnd.Next(2);
                if (rando == 0)
                { vel *= -1f; dz = 0.825f; }
                maxVelocity = new Vec3f(vel / 2, 0.4f, 0f);
                rando = Rnd.Next(2);
                if (rando == 0)
                { dz -= 0.5f; }
            }
            else
            {
                dx = 0.8f; dy = 0.08f; dz = 0.05f;
                var vel = 1f;
                var rando = Rnd.Next(2);
                if (rando == 0)
                { vel *= -1f; dx = 0.735f; }
                maxVelocity = new Vec3f(0f, 0.4f, vel / 2);
                rando = Rnd.Next(2);
                if (rando == 0)
                { dx -= 0.5f; }
            }

            var waterParticles = new SimpleParticleProperties(
                minQuantity, maxQuantity, color, minPos, addPos, minVelocity, maxVelocity, lifeLength, gravityEffect, minSize, maxSize, EnumParticleModel.Quad
            );
            waterParticles.MinPos.Set(pos.ToVec3d().AddCopy(dx, dy, dz));
            waterParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 0.2f);
            waterParticles.ShouldDieInAir = false;
            waterParticles.Bounciness = 0.25f;
            waterParticles.SelfPropelled = true;
            this.Api.World.SpawnParticles(waterParticles);
        }


        public void OnParticleTick(float par)
        {
            if (this.PipeStack != null)
            {
                this.AdjustWaterFromSourceOrNeib();

                var dir = this.Block.LastCodePart();
                if (this.PipeStack.Collectible.FirstCodePart(2) == "aerated")
                {
                    if (!this.LiquidSlot.Empty)
                    {
                        var rando = 0; //overall frequency of drips
                        if (rando == 0)
                        {
                            this.GenerateWaterParticles(this.Pos, dir);
                            //water farmland here
                            this.TryWaterFarmland(this.Pos, dir);
                        }
                    }
                }
                //can't do this in a listener
                //this.BreakIfUnsupported(this.Pos);
            }
        }


        public void OnPipeTick(float par)
        {
            //if stacksize of water is ever 0 we need to clear it completely
            var ba = this.Api.World.BlockAccessor;
            var dim = this.Pos.dimension;
            var offpos = this.tmpOffPos;
            var endpos = this.tmpEndPos;
            offpos.Set(this.Pos, dim);
            endpos.Set(this.Pos, dim);
            this.tmpDistPos0.dimension = dim;
            this.tmpDistPos1.dimension = dim;
            this.tmpDistPos2.dimension = dim;
            this.tmpNeibPos0.dimension = dim;
            this.tmpNeibPos1.dimension = dim;


            if (ba.GetBlockEntity(this.Pos) is BESupport be)
            {
                if (!be.LiquidSlot.Empty)
                {
                    if (be.LiquidStack.StackSize == 0)
                    {
                        be.LiquidSlot.TakeOutWhole();
                        be.MarkDirty();
                    }
                }
            }

            //NEW WATER SOURCE DETECTOR
            string possibleConnections;
            var blocka = ba.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockSupport;
            var supportDir = blocka?.LastCodePart();
            if (supportDir == "ns")
            { possibleConnections = "ew"; }
            else
            { possibleConnections = "ns"; }

            var oldEndpoints = this.currentEndpoints;
            this.currentEndpoints = ""; //clear all endpoints, then add them back

            foreach (var side in possibleConnections)
            {
                if (!this.currentConnections.Contains(side))
                {
                    this.CheckForWaterSource(side, supportDir);
                }
            }
            if (oldEndpoints != currentEndpoints)
            { this.MarkDirty(true); }

            //get some temporary fake water in the endpoint
            if (this.currentEndpoints != "")
            {
                if (this.LiquidSlot.Empty)
                {

                    var asset = new AssetLocation("game:waterportion");
                    var waterItem = Api.World.GetItem(asset);
                    if (waterItem != null)
                    {
                        ItemStack stack = new ItemStack(waterItem, 500); //500 = 50L maybe?
                        this.LiquidSlot.Itemstack = stack;
                        this.LiquidSlot.MarkDirty();
                    }

                }
            }


            // Water flow time - applies to endpoints only
            if (this.currentEndpoints == "")
            { return; } //not an endpoint
            int x = 0, z = 0;

            if (this.currentConnections == "n")
            { z = 1; }
            else if (this.currentConnections == "s")
            { z = -1; }
            else if (this.currentConnections == "e")
            { x = -1; }
            else if (this.currentConnections == "w")
            { x = 1; }

            int pipeLength = 1;

            Block block;

            if (x != 0 || z != 0)
            {
                // Debug.WriteLine("...scanning in X:" + x + "  Z:" + z);
                bool endfound = false;
                do
                {
                    offpos.X += x;
                    offpos.Z += z;
                    block = ba.GetBlock(offpos);
                    if (ba.GetBlockEntity(offpos) is BESupport beBlock)
                    {
                        if (beBlock.currentConnections.Length == 1)
                        {
                            // far endpoint found!
                            endfound = true;
                            //pipeLength--;
                        }
                        if (!beBlock.LiquidSlot.Empty)
                        {
                            // water found!
                            //endfound = true;
                        }
                    }
                    pipeLength++;

                } while (!endfound && pipeLength < 50);
            }
            //Debug.WriteLine("Pipelength:" + pipeLength);

            //now. 
            // start at the far end and work towards near end

            block = ba.GetBlock(endpos);
            //Debug.WriteLine("ENDPOINT " + block.Code.Path + " " + endpos.X + " " + endpos.Z);

            for (int i = pipeLength - 1; i >= 0; i--)
            {
                offpos.Set(endpos);
                offpos.X = endpos.X + (x * i);
                offpos.Z = endpos.Z + (z * i);
                block = ba.GetBlock(offpos);
                //Debug.WriteLine(" i:" + i + "  " + block.Code.Path + " " + offpos.X + " " + offpos.Z);
                if (ba.GetBlockEntity(offpos) is BESupport beBlock)
                {
                    //check neighbor
                    if (beBlock.LiquidSlot.Empty)
                    {
                        offpos.X = endpos.X + (x * (i - 1));
                        offpos.Z = endpos.Z + (z * (i - 1));
                        Block neibblock = ba.GetBlock(offpos);
                        //Debug.WriteLine("neib:" + i + "  " + neibblock.Code.Path + " " + offpos.X + " " + offpos.Z);

                        //distribute water from the source
                        if (ba.GetBlockEntity(offpos) is BESupport beBlockNeib)
                        {
                            if (!beBlockNeib.LiquidSlot.Empty)
                            {
                                var toMove = beBlockNeib.LiquidStack;
                                //Debug.WriteLine("moving");
                                //beBlockNeib.MarkDirty();

                                beBlock.LiquidStack = toMove;
                                beBlock.MarkDirty();
                            }
                        }
                    }
                    else if (i > 0 && i < pipeLength) //dont muck with the endpoints here yet
                    {
                        //distribute water evenly
                        int connectionCount = this.currentConnections.Length + 1;
                        BlockPos[] blocks;
                        if (connectionCount == 1)
                        {
                            blocks = this.distBlocks1;
                        }
                        else if (connectionCount == 2)
                        {
                            blocks = this.distBlocks2;
                        }
                        else if (connectionCount == 3)
                        {
                            blocks = this.distBlocks3;
                        }
                        else
                        {
                            blocks = new BlockPos[connectionCount];
                        }

                        if (connectionCount <= 3)
                        {
                            blocks[0].Set(offpos, dim);

                            if (connectionCount > 1)
                            {
                                offpos.X = endpos.X + (x * (i - 1));
                                offpos.Z = endpos.Z + (z * (i - 1));
                                blocks[1].Set(offpos, dim);
                            }
                            if (connectionCount > 2)
                            {
                                offpos.X = endpos.X + (x * (i + 1));
                                offpos.Z = endpos.Z + (z * (i + 1));
                                blocks[2].Set(offpos, dim);
                            }
                        }
                        else
                        {
                            blocks[0] = offpos.Copy();

                            if (connectionCount > 1)
                            {
                                offpos.X = endpos.X + (x * (i - 1));
                                offpos.Z = endpos.Z + (z * (i - 1));
                                blocks[1] = offpos.Copy();
                            }
                            if (connectionCount > 2)
                            {
                                offpos.X = endpos.X + (x * (i + 1));
                                offpos.Z = endpos.Z + (z * (i + 1));
                                blocks[2] = offpos.Copy();
                            }
                        }
                        DistributeWater(blocks);
                    }
                }
            }
            //see bepipe's OnPipeTick for clues
            //PipeBlockageChancePercent
            //PipeMinMoisture
        }


        //*******************************************************************************************
        // Water/Water Related
        private void TryWaterFarmland(BlockPos pos, string dir)
        {
            /*
              For each of the two positions, search down until we find a block
              if the block if farmland, water it
            */
            BlockPos neibPos;
            int maxDistance;
            var ba = Api.World.BlockAccessor;

            //find neighbor
            if (dir == "ns")
            {
                neibPos = pos.NorthCopy();
                maxDistance = 6; //only check for 6 blocks
            }
            else
            {
                neibPos = pos.EastCopy();
                maxDistance = 5; //ew blocks are one lower
            }
            BlockPos[] positions = { pos, neibPos };
            foreach (var position in positions)
            {
                var found = false;
                var distance = 0;
                var downPos = position.DownCopy();
                do
                {
                    var block = ba.GetBlock(downPos, BlockLayersAccess.Default);
                    if (block.Id != 0 && block.FirstCodePart() != "support")
                    {
                        found = true;
                        if (block.Class == "BlockCrop" || block.Class == "BlockTallGrass" || block.Class == "BlockPlant")
                        {
                            //might have hit the crop or plant of some kind, go down once more
                            downPos = downPos.DownCopy();
                            block = ba.GetBlock(downPos, BlockLayersAccess.Default);
                        }
                        if (ba.GetBlockEntity(downPos) is BlockEntityFarmland be)
                        {
                            //Debug.WriteLine("watering:" + block.Code.Path);
                            if (be.MoistureLevel <= 0.99)
                            {
                                // add 0.1 to it
                                be.WaterFarmland(0.1f, false); //no neighbors until nearby watered
                            }
                        }
                    }
                    distance++;
                    downPos = downPos.DownCopy();
                }
                while (!found && distance < maxDistance);
            }
        }


        private void AdjustWaterFromSourceOrNeib()
        {
            //look for barrel or irrigation vessel or water
            //do I even need supportDir here? ns or ew

            var ba = Api.World.BlockAccessor;
            var dir = this.Block.LastCodePart();
            //is an endpoint?
            if (this.currentEndpoints != null)
            {
                foreach (var side in dir)
                {
                    if (this.currentEndpoints.Contains(side))
                    {
                        Debug.WriteLine("Adjust Water From Source or Neib: ENDPOINT");
                        var waterBlock = this.Api.World.GetBlock(new AssetLocation("game:water-still-7"));
                        if (waterBlock != null)
                        {
                            var newStack = new ItemStack(waterBlock, 1);
                            this.LiquidStack = newStack;
                            this.MarkDirty();
                        }
                    }
                }
            }

            //Drain the pipe TEMP for testing
            if (ba.GetBlockEntity(this.Pos) is BESupport be)
            {
                if (!be.PipeSlot.Empty)
                {
                    var pipeType = be.PipeStack.Collectible.FirstCodePart(2);
                    if (pipeType == "aerated")
                    {
                        if (!be.LiquidSlot.Empty)
                        {
                            be.LiquidSlot.TakeOut(1);
                            be.MarkDirty();
                        }
                    }
                }
            }


            /*

            var isWaterSource = false;
            BlockPos[] neibPositions = null;
            switch (side)
            {
                case 'e':
                {
                    if (supportDir == "ns")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.WestCopy(), this.Pos.WestCopy().NorthCopy()};
                    }
                    break;
                }
                case 'w':
                {
                    if (supportDir == "ns")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.EastCopy(), this.Pos.EastCopy().NorthCopy()};
                    }
                    break;
                }
                case 'n':
                {
                    if (supportDir == "ew")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.SouthCopy().UpCopy(), this.Pos.SouthCopy().EastCopy().UpCopy()};
                    }
                    break;
                }
                case 's':
                {
                    if (supportDir == "ew")
                    {
                        neibPositions = new BlockPos[]
                        { this.Pos.NorthCopy().UpCopy(), this.Pos.NorthCopy().EastCopy().UpCopy()};
                    }
                    break;
                }
                default:
                { break; }
            }
            if (neibPositions != null)
            {
                foreach (var neibPos in neibPositions)
                {
                    var block = this.Api.World.BlockAccessor.GetBlock(neibPos, BlockLayersAccess.Default);
                    //Debug.WriteLine(side + ": " + block.Code.Path);
                    foreach (var sourceType in sourceTypes)
                    {
                        if (block.Code.Path.StartsWith(sourceType))
                        { isWaterSource = true; }
                    }
                }
            }
            */
        }


        private void DistributeWater(BlockPos[] blocks)
        {
            var ba = this.Api.World.BlockAccessor;
            int blockCount = blocks.Length;
            int waterCount = 0;

            //get the total water count
            foreach (var blockpos in blocks)
            {
                if (ba.GetBlockEntity(blockpos) is BESupport be)
                {
                    if (!be.LiquidSlot.Empty)
                    { waterCount += be.LiquidStack.StackSize; }
                }
            }
            var toDist = waterCount / blockCount - 1; //might need the -1 loss

            //distribute the water
            foreach (var blockpos in blocks)
            {
                if (ba.GetBlockEntity(blockpos) is BESupport be)
                {
                    if (toDist < 1)
                    {
                        if (!be.LiquidSlot.Empty)
                        { be.LiquidSlot.TakeOutWhole(); }
                    }
                    else
                    {
                        var asset = new AssetLocation("game:waterportion");
                        var waterItem = Api.World.GetItem(asset);
                        if (waterItem != null)
                        {
                            ItemStack stack = new ItemStack(waterItem, toDist);
                            be.LiquidStack = stack;
                        }
                    }
                    be.MarkDirty();
                }
            }
        }



        private void CheckForWaterSource(char side, string supportDir)
        {
            //look for barrel or irrigation vessel or water
            //do I even need supportDir here? ns or ew
            bool hasNeibPositions = false;
            var baseX = this.Pos.X;
            var baseY = this.Pos.Y;
            var baseZ = this.Pos.Z;
            var neibPos0 = this.tmpNeibPos0;
            var neibPos1 = this.tmpNeibPos1;
            switch (side)
            {
                case 'e':
                    {
                        if (supportDir == "ns")
                        {
                            neibPos0.Set(baseX - 1, baseY, baseZ);
                            neibPos1.Set(baseX - 1, baseY, baseZ - 1);
                            hasNeibPositions = true;
                        }
                        break;
                    }
                case 'w':
                    {
                        if (supportDir == "ns")
                        {
                            neibPos0.Set(baseX + 1, baseY, baseZ);
                            neibPos1.Set(baseX + 1, baseY, baseZ - 1);
                            hasNeibPositions = true;
                        }
                        break;
                    }
                case 'n':
                    {
                        if (supportDir == "ew")
                        {
                            neibPos0.Set(baseX, baseY + 1, baseZ + 1);
                            neibPos1.Set(baseX + 1, baseY + 1, baseZ + 1);
                            hasNeibPositions = true;
                        }
                        break;
                    }
                case 's':
                    {
                        if (supportDir == "ew")
                        {
                            neibPos0.Set(baseX, baseY + 1, baseZ - 1);
                            neibPos1.Set(baseX + 1, baseY + 1, baseZ - 1);
                            hasNeibPositions = true;
                        }
                        break;
                    }
                default:
                    { break; }
            }
            if (hasNeibPositions)
            {
                foreach (var sourceType in sourceTypes)
                {
                    int layer = BlockLayersAccess.Default;
                    if (sourceType.StartsWith("water"))
                    {
                        layer = BlockLayersAccess.Fluid;
                    }

                    var block = this.Api.World.BlockAccessor.GetBlock(neibPos0, layer);
                    if (block.BlockId != 0)
                    {
                        //Debug.WriteLine(side + ": " + block.Code.Path);
                        if (block.Code.Path.StartsWith(sourceType))
                        {
                            this.AddEndpoint(side);
                        }
                    }
                    block = this.Api.World.BlockAccessor.GetBlock(neibPos1, layer);
                    if (block.BlockId != 0)
                    {
                        //Debug.WriteLine(side + ": " + block.Code.Path);
                        if (block.Code.Path.StartsWith(sourceType))
                        {
                            this.AddEndpoint(side);
                        }
                    }
                }
            }
            return;
        }


        //*******************************************************************************************
        // Interaction Methods
        public bool OnInteract(IPlayer byPlayer, BlockPos pos)
        {
            var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (!slot.Empty)
            {
                if (this.TryPut(byPlayer, slot, pos))
                {
                    this.Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                    this.Api.World.PlaySoundAt(this.dryPickupSound, pos.X, pos.Y, pos.Z);
                    return true;
                }
            }

            // try remove blockage
            if (this.OtherStack?.Collectible != null)
            {
                var result = this.TryTake(byPlayer, this.OtherSlot);
                if (result)
                {
                    this.Api.World.PlaySoundAt(this.dryPickupSound, pos.X, pos.Y, pos.Z, byPlayer);
                    return true;
                }
            }
            return false;
        }



        private bool TryPut(IPlayer byPlayer, ItemSlot playerSlot, BlockPos pos)
        {
            Debug.WriteLine("Try put a pipe");
            var ba = Api.World.BlockAccessor;
            var index = -1;
            var moved = 0;
            var playerStack = playerSlot.Itemstack;
            if (this.inventory != null)
            {
                var stacks = this.inventory.Where(slot => !slot.Empty).Select(slot => slot.Itemstack).ToArray();
                if (stacks.Count() >= this.maxSlots)
                { return false; }
            }

            if (playerStack.Collectible != null)
            {
                //Debug.WriteLine("Putting a " + playerStack.Item.FirstCodePart());
                if (Array.IndexOf(this.pipeTypes, playerStack.Collectible.FirstCodePart()) >= 0 && this.PipeSlot.Empty)
                { index = 0; }
                else
                {
                    if (this.LiquidSlot.Empty)
                    { index = 1; }
                    else if (this.OtherSlot.Empty)
                    {
                        index = 2;
                        Debug.WriteLine("WHOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO");
                    }
                }
            }
            if (index == 0) //only accept something in the pipe slot for now
            {
                Debug.WriteLine("...on an empty support");
                moved = playerSlot.TryPutInto(this.Api.World, this.inventory[index], 1);
                if (moved > 0)
                {
                    Debug.WriteLine("......success");

                    if (index == 0)
                    {
                        // why the fuck would I clear these
                        //this.currentConnections = "";
                        //this.currentEndpoints = "";
                    }

                    this.MarkDirty(true);

                    //what's the occasional hangup here with pipe ends                   

                    if (ba.GetBlockEntity(pos) is BESupport beBlock)
                    {
                        beBlock.Inventory[0].MarkDirty();
                        ba.MarkBlockDirty(pos);
                        ba.TriggerNeighbourBlockUpdate(pos);
                    }
                    //end occasional hangup
                    return moved > 0;
                }
            }
            else
            {
                /**************
                 * 
                 * 
                 * I think this is causing a rendering crash on middle click
                 * maybe only in creative mode, but still
                 * 
                //hijack this for adding pipes
                Debug.WriteLine("...on a pipe");
                if (Array.IndexOf(this.pipeTypes, playerStack.Collectible.FirstCodePart()) < 0)
                {
                    Debug.WriteLine("...hold your horses, that's not a pipe you're holding");
                    return false;
                }

                var block = ba.GetBlock(pos, BlockLayersAccess.Default);
                var finalFacing = block.LastCodePart();
                if (block.LastCodePart() != finalFacing)
                { return false; } //restrict direction for now

                BlockPos targetPos;
                BlockPos neibPos;
                var placeDir = "ew";
                var isFacing = byPlayer.CurrentBlockSelection?.Face.ToString();
                if (isFacing == null)
                { return false; }
                if (isFacing == "south")
                {
                    targetPos = pos.SouthCopy();
                    neibPos = targetPos.EastCopy();
                }
                else if (isFacing == "north")
                {
                    targetPos = pos.NorthCopy();
                    neibPos = targetPos.EastCopy();
                }
                else if (isFacing == "west")
                {
                    targetPos = pos.WestCopy();
                    neibPos = targetPos.NorthCopy();
                    placeDir = "ns";
                }
                else if (isFacing == "east")
                {
                    targetPos = pos.EastCopy();
                    neibPos = targetPos.NorthCopy();
                    placeDir = "ns";
                }
                else
                {
                    return false; // cant put a pipe above or below a pipe
                }

                if (finalFacing != placeDir)
                {
                    return false; // cant put a pipe on the side of a pipe
                }

                // this is the end of a pipe...
                var testBlock = ba.GetBlock(targetPos, BlockLayersAccess.Default);
                var testBlock2 = ba.GetBlock(neibPos, BlockLayersAccess.Default);
                if (testBlock.BlockId != 0 || testBlock2.BlockId != 0)
                {
                    if (!testBlock.IsLiquid() || !testBlock2.IsLiquid())
                    {
                        return false;  // something in the way
                    }
                }

                if (!HasSupport(targetPos, finalFacing))
                {
                    if (this.Api.World.Side == EnumAppSide.Client)
                    {
                        (Api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("game:placefailure-support-needed"));
                    }
                    return false;
                }

                var material = playerStack.Collectible.FirstCodePart(1);
                var type = playerStack.Collectible.FirstCodePart(2);
                var neibAsset = dom + "support-none-empty-" + finalFacing; //none
                block = ba.GetBlock(new AssetLocation(neibAsset));
                if (block != null)
                {
                    ba.SetBlock(block.BlockId, neibPos);
                    ba.MarkBlockDirty(neibPos);

                    neibAsset = neibAsset.Replace("empty", "main");
                    block = ba.GetBlock(new AssetLocation(neibAsset));
                    ba.SetBlock(block.BlockId, targetPos);

                    if (ba.GetBlockEntity(targetPos) is BESupport beBlock)
                    {
                        var moveStack = byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                        beBlock.Inventory[0].Itemstack = moveStack;
                        beBlock.Inventory[0].MarkDirty();
                        ba.MarkBlockDirty(targetPos);

                        //we need this here to update connections
                        ownBlock.NotifyNeighborsOfBlockChange(targetPos, Api.World);
                        // and maybe this as well - not
                        //ba.TriggerNeighbourBlockUpdate(targetPos);
                        return true;
                    }
                    else
                    {
                        if (this.Api.World.Side == EnumAppSide.Client)
                        {
                            (Api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("game:placefailure-support-needed"));
                        }
                    }
                }*/
            }
            return false;
        }


        //for removing blockages probably - maybe pipes from supports...
        private bool TryTake(IPlayer byPlayer, ItemSlot sourceSlot)
        {
            if (!sourceSlot.Empty)
            {
                var stackCode = sourceSlot.Itemstack.Collectible.Code.Path;
                var newAsset = new AssetLocation(stackCode);
                var item = this.Api.World.GetItem(newAsset);
                if (item != null)
                {
                    var tempStack = new ItemStack(item, sourceSlot.StackSize);
                    var takeOK = byPlayer.InventoryManager.TryGiveItemstack(tempStack);
                    if (!takeOK) //player has no free slots
                    {
                        this.Api.World.SpawnItemEntity(tempStack, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
                    }
                    sourceSlot.Itemstack = null;
                    this.MarkDirty(true);
                    return true;
                }
            }
            return false;
        }


        //*******************************************************************************************
        // Helper methods
        public BlockPos RelativeToSpawn(BlockPos pos)
        {
            var worldSpawn = this.Api.World.DefaultSpawnPosition.XYZ.AsBlockPos;
            var blockPos = pos.SubCopy(worldSpawn);
            return new BlockPos(blockPos.X, pos.Y, blockPos.Z, 0);
        }


        private bool isSupported(BlockPos pos, string dir) // is a pipe supported by a support?  watch out for the hardcoded 99
        {
            var maxLength = 3;
            var supported = true;
            var count = 0;
            var ba = Api.World.BlockAccessor;
            do
            {
                if (dir == "n")
                { pos = pos.NorthCopy(); }
                else if (dir == "s")
                { pos = pos.SouthCopy(); }
                else if (dir == "e")
                { pos = pos.EastCopy(); }
                else
                { pos = pos.WestCopy(); }



                if (ba.GetBlockEntity(pos) is BESupport be)
                {
                    //Debug.WriteLine(" " + count + ": " + be.Block.Code);
                    //Debug.WriteLine(" " + be.PipeStack.Collectible.Code);
                    //Debug.WriteLine(dir + "  at Position:" + RelativeToSpawn(pos));

                    if (be.PipeStack == null)
                    { supported = false; }
                    else
                    {
                        if (be.Block.FirstCodePart(1) != "none") //support
                        { return true; }
                    }
                }
                else
                { supported = false; }
                count++;
            }
            while (supported && (count < maxLength));
            if (count == maxLength && supported)
            { supported = false; }
            return supported;
        }


        public bool BreakIfUnsupported(BlockPos pos)
        {
            //check for enough support and break pipe if there isn't - as in an actual support 4 blocks in either direction
            var ba = Api.World.BlockAccessor;
            var block = ba.GetBlock(pos, BlockLayersAccess.Default) as BlockSupport;
            var dir = block.LastCodePart();
            var type = block.FirstCodePart(1);
            var rSupported = true;
            var lSupported = true;

            // debug
            // var relPos = RelativeToSpawn(pos);
            // Debug.WriteLine("--------" + relPos.Z);

            if (type == "none")
            {
                if (dir == "ew")
                {
                    rSupported = this.isSupported(pos, "n");
                    lSupported = this.isSupported(pos, "s");
                }
                else  //ns
                {
                    rSupported = this.isSupported(pos, "e");
                    lSupported = this.isSupported(pos, "w");
                }
            }
            if (!rSupported && !lSupported)
            {
                if (ba.GetBlockEntity(pos) is BESupport be)
                {
                    ownBlock.OnBlockBroken(Api.World, Pos, null);
                    if (dir == "ns")
                    { ba.SetBlock(0, pos.NorthCopy()); }
                    else
                    { ba.SetBlock(0, pos.EastCopy()); }
                    return true;
                }
            }
            return false;
        }


        private bool HasSupport(BlockPos pos, string dir)
        {
            //check for support - as in an actual support 4 blocks in either direction
            var rSupported = false;
            var lSupported = false;

            Debug.WriteLine("HasSupport: " + dir);

            if (dir == "ew")
            {
                rSupported = this.isSupported(pos, "n");
                lSupported = this.isSupported(pos, "s");
            }
            else  //ns
            {
                rSupported = this.isSupported(pos, "e");
                lSupported = this.isSupported(pos, "w");
            }

            if (rSupported || lSupported)
            {
                return true;
            }
            return false;
        }



        internal string GetConnections()
        { return this.currentConnections; }

        internal string GetEndpoints()
        { return this.currentEndpoints; }


        internal bool AddConnection(BlockPos pos, BlockFacing facing)
        {
            var face = char.ToLower(facing.Opposite.ToString()[0]);
            if (!this.currentConnections.Contains(face))
            {
                this.currentConnections += face;
                return true;
            }
            return false;
        }


        internal bool RemoveConnection(BlockPos pos, BlockFacing facing)
        {
            var face = char.ToLower(facing.Opposite.ToString()[0]).ToString();
            if (this.currentConnections.Contains(face))
            {
                this.currentConnections = this.currentConnections.Replace(face, string.Empty);
                return true;
            }
            return false;
        }


        internal bool AddEndpoint(char face)
        {
            if (!this.currentEndpoints.Contains(face))
            {
                this.currentEndpoints += face;
                return true;
            }
            return false;
        }


        internal bool RemoveEndpoint(char face)
        {
            if (this.currentEndpoints.Contains(face))
            {
                this.currentEndpoints = this.currentEndpoints.Replace(face.ToString(), string.Empty);
                return true;
            }
            return false;
        }


        //*******************************************************************************************
        // Shapes, Textures, Rendering, Tesselation
        public virtual string BuildShapePath(string partialPath)
        {
            if (partialPath == "")
            { return ""; }
            var fullPath = "";
            if (!partialPath.Contains(":"))
            { fullPath = this.Block.Code.Domain + ":"; }
            if (!partialPath.Contains("shapes/"))
            { fullPath += "shapes/"; }
            fullPath += partialPath;
            return fullPath;
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            MeshData mesh;
            string shapePath;
            var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default) as BlockSupport;
            var supportDir = block?.LastCodePart();
            string possibleConnections;

            if (block != null)
            {
                if (block.FirstCodePart(1) != "none")
                {
                    var texture = tesselator.GetTextureSource(block);
                    shapePath = dom + "shapes/" + block.Shape.Base.Path;
                    mesh = block.GenMesh(this.Api as ICoreClientAPI, shapePath, texture, supportDir);
                    mesher.AddMeshData(mesh);
                }
            }

            if (this.inventory != null)
            {
                if (!this.PipeSlot.Empty)
                {
                    if (supportDir == "ns")
                    { possibleConnections = "ew"; }
                    else
                    { possibleConnections = "ns"; }
                    Block tempBlock;
                    if (this.PipeStack.Block != null)
                    {
                        tempBlock = this.Api.World.GetBlock(this.PipeStack.Block.Id);
                        var texture = ((ICoreClientAPI)this.Api).Tesselator.GetTextureSource(tempBlock);
                        shapePath = dom + "shapes/" + tempBlock.Shape.Base.Path;
                        mesh = block.GenInventoryMesh(this.capi, shapePath, texture, supportDir);
                        mesher.AddMeshData(mesh);

                        bool pipeEnd = false;
                        //pipe ends
                        foreach (var side in possibleConnections)
                        {
                            mesh = null;
                            if (!this.currentConnections.Contains(side) && shapePath != "")
                            {
                                shapePath = this.BuildShapePath(this.sideShape); //this uses a single down facing side to render all sides

                                if (this.currentEndpoints.Contains(side))
                                {
                                    shapePath = this.BuildShapePath(this.endShape);
                                    pipeEnd = true;
                                }
                                mesh = block.GenSideMesh(this.capi, shapePath, texture, side, supportDir);
                            }
                            if (mesh != null)
                            {
                                mesher.AddMeshData(mesh);
                            }
                        }


                        if (!this.LiquidSlot.Empty)
                        {
                            //Debug.WriteLine("HERE " + supportDir);
                            var watermesh = block.RenderWater(pipeEnd, supportDir, currentConnections, currentEndpoints);
                            if (watermesh != null)
                            {
                                //why did this null exception once when I placed a chest? Because I was using renderpass:liquid in the shape file itself FUCK ME WITH A RUSTY SPOON
                                //plan B
                                watermesh.CustomInts = new CustomMeshDataPartInt(watermesh.FlagsCount);
                                watermesh.CustomInts.Values.Fill(0x4000000); // light foam only
                                watermesh.CustomInts.Count = watermesh.FlagsCount;
                                watermesh.CustomFloats = new CustomMeshDataPartFloat(watermesh.FlagsCount * 2);
                                watermesh.CustomFloats.Count = watermesh.FlagsCount * 2;
                                watermesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.Liquid);

                                mesher.AddMeshData(watermesh);
                            }
                        }
                    }
                }

                //Blockage For future use
                /*
                if (!this.OtherSlot.Empty)
                {
                    var tmpTextureSource = tesselator.GetTextureSource(this.Block);
                    shapePath = this.BuildShapePath("block/pipe/soil/blockage-" + this.OtherStack.Collectible.Code.Path);
                    mesh = block.GenInventoryMesh(this.capi, shapePath, tmpTextureSource, supportDir);
                    if (mesh != null)
                    {
                        mesh.Translate(new Vec3f(0f, 0.85f - (this.OtherSlot.StackSize * 0.05f), 0f));
                        mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.85f, 0.85f, 0.85f);
                        mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, this.OtherSlot.StackSize * 45 * GameMath.DEG2RAD, 0);
                        mesher.AddMeshData(mesh);
                    }
                }
                */
            }
            return true;
        }


        //*******************************************************************************************
        // Tree Attributes
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolve)
        {
            base.FromTreeAttributes(tree, worldForResolve);
            this.currentConnections = tree.GetString("currentConnections");
            this.currentEndpoints = tree.GetString("currentEndpoints");

            /*
            if (this.Api != null)
            {
                if (this.Api.Side == EnumAppSide.Client)
                { this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos); }
            }
            */
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("currentConnections", this.currentConnections);
            tree.SetString("currentEndpoints", this.currentEndpoints);
        }
    }
}
