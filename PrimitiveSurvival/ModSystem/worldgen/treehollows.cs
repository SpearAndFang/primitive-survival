namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Collections.Generic;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using PrimitiveSurvival.ModConfig;
    using Vintagestory.API.Config;
    using System.Diagnostics;

    //using System.Diagnostics;


    public class TreeHollows : ModSystem
    {
        private const int MinItems = 1;
        private const int MaxItems = 8;
        private ICoreServerAPI sapi; //The main interface we will use for interacting with Vintage Story
        private int chunkSize; //Size of chunks. Chunks are cubes so this is the size of the cube.
        private ISet<string> treeTypes; //Stores tree types that will be used for detecting trees for placing our tree hollows
        private IBlockAccessor chunkGenBlockAccessor; //Used for accessing blocks during chunk generation
        private IBlockAccessor worldBlockAccessor; //Used for accessing blocks after chunk generation

        private readonly string[] dirs = { "north", "south", "east", "west" };
        private readonly string[] woods = { "acacia", "birch", "kapok", "larch", "maple", "oak", "pine", "walnut" };

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            this.worldBlockAccessor = api.World.BlockAccessor;

            //   v3.7.9 performance   
            //this.chunkSize = this.worldBlockAccessor.ChunkSize;
            this.chunkSize = GlobalConstants.ChunkSize;

            this.treeTypes = new HashSet<string>();
            this.LoadTreeTypes(this.treeTypes);

            //Registers our command with the system's command registry.
            var devToolsEnabled = ModConfig.Loaded.TreeHollowsEnableDeveloperTools;
            //devToolsEnabled = true;
            if (devToolsEnabled)
            {
                this.sapi.ChatCommands.GetOrCreate("hollow")
                        .WithDescription("Place a tree hollow with random items")
                        .RequiresPrivilege(Privilege.controlserver)
                        .HandleWith(PlaceTreeHollowInFrontOfPlayer); //1.19

                //this.sapi.RegisterCommand("hollow", "Place a tree hollow with random items", "", this.PlaceTreeHollowInFrontOfPlayer, Privilege.controlserver); //1.18
            }

            //Registers a delegate to be called so we can get a reference to the chunk gen block accessor
            this.sapi.Event.GetWorldgenBlockAccessor(this.OnWorldGenBlockAccessor);

            //Registers a delegate to be called when a chunk column is generating in the Vegetation phase of generation
            this.sapi.Event.ChunkColumnGeneration(this.OnChunkColumnGeneration, EnumWorldGenPass.PreDone, "standard");
        }


        // Delegate for /hollow command. Places a treehollow 2 blocks in front of the player
        private TextCommandResult PlaceTreeHollowInFrontOfPlayer(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;
            this.PlaceTreeHollow(this.sapi.World.BlockAccessor, player.Entity.Pos.HorizontalAheadCopy(2).AsBlockPos);
            return TextCommandResult.Success("");
        }


        // Our mod only needs to be loaded by the server
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }

        private void LoadTreeTypes(ISet<string> treeTypes)
        {
            //var treeTypesFromFile = this.sapi.Assets.TryGet("worldproperties/block/wood.json").ToObject<StandardWorldProperty>();
            foreach (var variant in this.woods)
            {
                treeTypes.Add($"log-grown-" + variant + "-ud");
            }
        }

        /// <summary>
        /// Stores the chunk gen thread's IBlockAccessor for use when generating tree hollows during chunk gen. This callback
        /// is necessary because chunk loading happens in a separate thread and it's important to use this block accessor
        /// when placing tree hollows during chunk gen.
        /// </summary>
        private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
        {
            this.chunkGenBlockAccessor = chunkProvider.GetBlockAccessor(true);
        }

        /// <summary>
        /// Called when a number of chunks have been generated. For each chunk we first determine if we should place a tree hollow
        /// and if we should we then loop through each block to find a tree. When one is found we place the block.
        /// </summary>


        //1.17pre.6 look like this
        //private void OnChunkColumnGeneration(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkgenparams)

        //1.17pre.7 like this
        private void OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
        {
            var chunks = request.Chunks;
            var chunkX = request.ChunkX;
            var chunkZ = request.ChunkZ;
            //END of 1.17pre.7 like this

            //Moved from PlaceTreeHollow to hopefully speed things up...a lot
            if (!this.ShouldPlaceHollow())
            { return; }

            //Debug.WriteLine("Entering the death loop for chunk " + chunkX + " " + chunkZ);
            var hollowsPlacedCount = 0;
            for (var i = 0; i < chunks.Length-2; i=i+2)
            {
                //var blockPos = new BlockPos();
                var blockPos = new BlockPos(0,0,0,0);
                for (var x = 0; x < this.chunkSize/2; x = x + 2)
                {
                    for (var z = 0; z < this.chunkSize/2; z = z + 2)
                    {
                        //for (var y = 0; y < this.worldBlockAccessor.MapSizeY; y++)
                        //VINTER'S PERFORMANCE PATCH
                        var terrainHeight = this.worldBlockAccessor.GetTerrainMapheightAt(blockPos);
                        for (var y = terrainHeight; y < terrainHeight + 6; y++) //only scan 6 blocks high
                        {
                            if (hollowsPlacedCount < ModConfig.Loaded.TreeHollowsMaxPerChunk)
                            {
                                blockPos.X = (chunkX * this.chunkSize) + x;
                                blockPos.Y = y;
                                blockPos.Z = (chunkZ * this.chunkSize) + z;

                                var hollowLocation = this.TryGetHollowLocation(blockPos);
                                if (hollowLocation != null)
                                {
                                    var hollowWasPlaced = this.PlaceTreeHollow(this.chunkGenBlockAccessor, hollowLocation);
                                    if (hollowWasPlaced)
                                    {
                                        hollowsPlacedCount++;
                                    }
                                }
                            }
                            else //Max hollows have been placed for this chunk
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }

        // Returns the location to place the hollow if the given world coordinates is a tree, null if it's not a tree.
        private BlockPos TryGetHollowLocation(BlockPos pos)
        {
            var block = this.chunkGenBlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            if (this.IsTreeLog(block))
            {
                for (var posY = pos.Y; posY >= 0; posY--)
                {
                    while (pos.Y-- > 0)
                    {
                        var underBlock = this.chunkGenBlockAccessor.GetBlock(pos, BlockLayersAccess.Default);
                        if (this.IsTreeLog(underBlock))
                        {
                            continue;
                        }
                        return pos.UpCopy();
                    }
                }
            }
            return null;
        }

        private bool IsTreeLog(Block block)
        {
            return this.treeTypes.Contains(block.Code.Path);
        }

        

        // Places a tree hollow filled with random items at the given world coordinates using the given IBlockAccessor
        private bool PlaceTreeHollow(IBlockAccessor blockAccessor, BlockPos pos)
        {
            //Moved this to chunk gen to hopefully speed things up...a lot
            /*
            if (!this.ShouldPlaceHollow())
            {
                //Debug.WriteLine("cancelled!");
                return true;
            }
            */

            //consider moving it upwards
            var upCount = this.sapi.World.Rand.Next(4);
            var upCandidateBlock = blockAccessor.GetBlock(pos.UpCopy(upCount), BlockLayersAccess.Default);

            if (upCandidateBlock.FirstCodePart() == "log")
            { pos = pos.Up(upCount); }

            var treeBlock = blockAccessor.GetBlock(pos, BlockLayersAccess.Default);
            //Debug.WriteLine("Will replace:" + treeBlock.Code.Path);
            var woodType = "pine";

            if (treeBlock.FirstCodePart() == "log")
            {
                woodType = treeBlock.FirstCodePart(2);
            }

            var hollowType = "up";
            if (this.sapi.World.Rand.Next(2) == 1)
            { hollowType = "up2"; }
            var belowBlock = blockAccessor.GetBlock(pos.DownCopy(), BlockLayersAccess.Default);
            if (belowBlock.Fertility > 0) //fertile ground below?
            {
                if (this.sapi.World.Rand.Next(2) == 1)
                { hollowType = "base"; }
                else
                { hollowType = "base2"; }
            }

            var withPath = "primitivesurvival:treehollowgrown-" + hollowType + "-" + woodType + "-" + this.dirs[this.sapi.World.Rand.Next(4)];
            //Debug.WriteLine("With: " + withPath);
            var withBlockID = this.sapi.WorldManager.GetBlockId(new AssetLocation(withPath));
            var withBlock = blockAccessor.GetBlock(withBlockID);
            blockAccessor.SetBlock(0, pos);
            if (withBlock.TryPlaceBlockForWorldGen(blockAccessor, pos, BlockFacing.UP, null))
            {
                var block = blockAccessor.GetBlock(pos, BlockLayersAccess.Default) as BlockTreeHollowGrown;
                if (block.EntityClass != null)
                {
                    if (block.EntityClass == withBlock.EntityClass)
                    {
                        blockAccessor.SpawnBlockEntity(block.EntityClass, pos);
                        var be = blockAccessor.GetBlockEntity(pos);
                        if (be is BETreeHollowGrown)
                        {
                            var hollow = blockAccessor.GetBlockEntity(pos) as BETreeHollowGrown;
                            //hollow.Initialize(this.sapi); //SpawnBlockEntity does this already

                            // JHR
                            //var makeStacks = this.MakeItemStacks(hollowType, this.sapi);
                            if (block != null)
                            {
                                var makeStacks = this.MakeItemStacks(block, this.sapi);
                            
                            // END JHR

                                if (makeStacks != null)
                                { this.AddItemStacks(hollow, makeStacks); }
                            }
                        }
                    }
                }
                return true;
            }
            else
            { return false; }
        }

        private bool ShouldPlaceHollow()
        {
            var randomNumber = this.sapi.World.Rand.Next(0, 100);
            return randomNumber > 0 && randomNumber <= ModConfig.Loaded.TreeHollowsSpawnProbability * 100;
        }

        // Makes a list of random ItemStacks to be placed inside our tree hollow
        //JHR
        //public IEnumerable<ItemStack> MakeItemStacks(string hollowType, ICoreServerAPI sapi)
        public IEnumerable<ItemStack> MakeItemStacks(Block block, ICoreServerAPI sapi)
        {
            if (sapi == null)
            { return null; }
            //var shuffleBag = this.MakeShuffleBag(hollowType, sapi);
            var shuffleBag = this.MakeShuffleBag(block, sapi);
            // END JHR
            var itemStacks = new Dictionary<string, ItemStack>();
            var minDrops = MinItems;
            if (ModConfig.Loaded.TreeHollowsMaxItems < MinItems)
            { minDrops = 0; }
            var maxDrops = MaxItems;
            if (ModConfig.Loaded.TreeHollowsMaxItems < MaxItems)
            { maxDrops = ModConfig.Loaded.TreeHollowsMaxItems; }
            var grabCount = sapi.World.Rand.Next(minDrops, maxDrops);
            for (var i = 0; i < grabCount; i++)
            {
                var nextItem = shuffleBag.Next();
                if (nextItem.Contains("item-"))
                {
                    nextItem = nextItem.Replace("item-", "");

                    AssetLocation aloc = new AssetLocation(nextItem);
                    if (aloc != null)
                    {
                        var item = sapi.World.GetItem(aloc);
                        if (itemStacks.ContainsKey(nextItem))
                        { itemStacks[nextItem].StackSize++; }
                        else
                        { itemStacks.Add(nextItem, new ItemStack(item)); }
                    }
                }
                else //block
                {
                    nextItem = nextItem.Replace("block-", "");
                    AssetLocation aloc = new AssetLocation(nextItem);
                    if (aloc != null)
                    {
                        var item = sapi.World.GetBlock(aloc);
                        if (itemStacks.ContainsKey(nextItem))
                        { itemStacks[nextItem].StackSize++; }
                        else
                        { itemStacks.Add(nextItem, new ItemStack(item)); }
                    }
                }
            }
            return itemStacks.Values;
        }

        //Adds the given list of ItemStacks to the first slots in the given hollow.
        public void AddItemStacks(IBlockEntityContainer hollow, IEnumerable<ItemStack> itemStacks)
        {
            var slotNumber = 0;
            if (itemStacks != null)
            {
                foreach (var itemStack in itemStacks)
                {
                    slotNumber = Math.Min(slotNumber, hollow.Inventory.Count - 1);
                    var slot = hollow.Inventory[slotNumber];
                    slot.Itemstack = itemStack;
                    slotNumber++;
                }
            }
        }

        // Creates our ShuffleBag to pick from when generating items for the tree hollow

        // JHR
        //private ShuffleBag<string> MakeShuffleBag(string hollowType, ICoreServerAPI sapi)
        private ShuffleBag<string> MakeShuffleBag(Block block, ICoreServerAPI sapi)
        // END JHR
        {
            var shuffleBag = new ShuffleBag<string>(100, sapi.World.Rand);
            if (block == null)
            { return shuffleBag; }

            var psAttributes = block.Attributes?["primitivesurvival"];
            if (psAttributes == null)
            { return shuffleBag; }

            var hollowType = psAttributes["treeHollowType"].AsString("all");
            var contentsByHollowType = psAttributes["treeHollowContentsByHollowType"];

            if (hollowType != "all")
            {
                this.AddItemsToShuffleBag(shuffleBag, contentsByHollowType[hollowType]);
            }
            this.AddItemsToShuffleBag(shuffleBag, contentsByHollowType["all"]);
            return shuffleBag;
        }

        private void AddItemsToShuffleBag(ShuffleBag<string> shuffleBag, JsonObject attributesObjectArray)
        {
            //THIS HELPS MAYBE?
            if (shuffleBag == null)
            { return; }
            if (attributesObjectArray == null)
            { return; }

            var attArray = attributesObjectArray.AsArray();
            if (attributesObjectArray == null)
            { return; }


            foreach (var collectible in attArray)
            {
                if (!collectible["code"].Exists)
                { continue; }
                shuffleBag.Add(collectible["code"].AsString(), collectible["amount"].AsInt(1));
            }
            //END JHR
        }
    }
}
