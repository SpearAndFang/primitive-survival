namespace PrimitiveSurvival.ModSystem
{
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Common;
    using Vintagestory.API.Server;
    using Vintagestory.API.Client;
    using Vintagestory.API.Util;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    using PrimitiveSurvival.ModConfig;
    using Vintagestory.Client.NoObf;


    public class PrimitiveSurvivalSystem : ModSystem
    {
        public IShaderProgram EntityGenericShaderProgram { get; private set; }

        private readonly string thisModID = "primitivesurvival118";
        private static Dictionary<IServerChunk, int> fishingChunks;

        public static List<string> chunkList;

        private bool prevChunksLoaded;

        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;

        //readonly IShaderProgram overlayShaderProg;
        private VenomOverlayRenderer vrenderer;

        private readonly Harmony harmony = new Harmony("com.spearandfang.primitivesurvival");

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            var PSPumpkinPatchOriginal = typeof(PumpkinCropBehavior).GetMethod(nameof(PumpkinCropBehavior.CanSupportPumpkin));
            var PSPumpkinPatchPrefix = typeof(PS_CanSupportPumpkin_Patch).GetMethod(nameof(PS_CanSupportPumpkin_Patch.PSCanSupportPumpkinPrefix));
            this.harmony.Patch(PSPumpkinPatchOriginal, prefix: new HarmonyMethod(PSPumpkinPatchPrefix));

            if (ModConfig.Loaded.ShowModNameInHud)
            {
                var PSBlockGetPlacedBlockInfoOriginal = typeof(Block).GetMethod(nameof(Block.GetPlacedBlockInfo));
                var PSBlockGetPlacedBlockInfoPostfix = typeof(PS_BlockGetPlacedBlockInfo_Patch).GetMethod(nameof(PS_BlockGetPlacedBlockInfo_Patch.PSBlockGetPlacedBlockInfoPostfix));
                this.harmony.Patch(PSBlockGetPlacedBlockInfoOriginal, postfix: new HarmonyMethod(PSBlockGetPlacedBlockInfoPostfix));
            }

            if (ModConfig.Loaded.ShowModNameInGuis)
            {
                var PSCollectibleGetHeldItemInfoOriginal = typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetHeldItemInfo));
                var PSCollectibleGetHeldItemInfoPostfix = typeof(PS_CollectibleGetHeldItemInfo_Patch).GetMethod(nameof(PS_CollectibleGetHeldItemInfo_Patch.PSCollectibleGetHeldItemInfoPostfix));
                this.harmony.Patch(PSCollectibleGetHeldItemInfoOriginal, postfix: new HarmonyMethod(PSCollectibleGetHeldItemInfoPostfix));
            }

            this.capi = api;
            this.capi.Event.ReloadShader += this.LoadCustomShaders;
            this.LoadCustomShaders();
            this.capi.RegisterEntityRendererClass("entitygenericshaperenderer", typeof(EntityGenericShapeRenderer));
            this.vrenderer = new VenomOverlayRenderer(api);
            api.Event.RegisterRenderer(this.vrenderer, EnumRenderStage.Ortho);

            //JHR
            (this.capi.World as ClientMain).RegisterDialog(new GuiDialogHollowTransform(this.capi));
            //END JHR
        }


        public override void StartPre(ICoreAPI api)
        {
            // Load/create common config file in ..\VintageStoryData\ModConfig\thisModID
            var cfgFileName = this.thisModID + ".json";
            try
            {
                ModConfig fromDisk;
                if ((fromDisk = api.LoadModConfig<ModConfig>(cfgFileName)) == null)
                { api.StoreModConfig(ModConfig.Loaded, cfgFileName); }
                else
                { ModConfig.Loaded = fromDisk; }
            }
            catch
            {
                api.StoreModConfig(ModConfig.Loaded, cfgFileName);
            }
            //to disable furrowed farm land in case of xskills
            api.World.Config.SetBool("FurrowedLandEnabled", ModConfig.Loaded.FurrowedLandEnabled);
            //this next one is weird when someone already has a raft - but I wanted to disable the block too in order to remove it from the handbook
            api.World.Config.SetBool("RaftEnabled", ModConfig.Loaded.RaftEnabled);
            api.World.Config.SetBool("ParticulatorEnabled", ModConfig.Loaded.ParticulatorEnabled);
            base.StartPre(api);
        }



        public bool LoadCustomShaders()
        {

            this.EntityGenericShaderProgram = this.capi.Shader.NewShaderProgram();
            // 1.17 My custom shader broke, but the built in shader works really well
            (this.EntityGenericShaderProgram as ShaderProgram).AssetDomain = "game"; // this.Mod.Info.ModID;
            //this.capi.Shader.RegisterFileShaderProgram("entitygenericshader", this.EntityGenericShaderProgram);
            this.capi.Shader.RegisterFileShaderProgram("entityanimated", this.EntityGenericShaderProgram);
            this.EntityGenericShaderProgram.Compile();
            return true;
        }

        public void RegisterClasses(ICoreAPI api)
        {
            api.RegisterEntity("entityearthworm", typeof(EntityEarthworm));
            api.RegisterEntity("entityfireflies", typeof(EntityFireflies));
            api.RegisterEntity("entitypsglowingagent", typeof(EntityPSGlowingAgent));
            api.RegisterEntity("entitylivingdead", typeof(EntityLivingDead));
            api.RegisterEntity("entitygenericglowingagent", typeof(EntityGenericGlowingAgent));
            api.RegisterEntity("entityskullofthedead", typeof(EntitySkullOfTheDead));
            api.RegisterEntity("entitywillowisp", typeof(EntityWillowisp));
            api.RegisterEntity("entitybioluminescent", typeof(EntityBioluminescent));

            api.RegisterEntityBehaviorClass("carryable", typeof(EntityBehaviorCarryable));

            AiTaskRegistry.Register("meleeattackvenomous", typeof(AiTaskMeleeAttackVenomous));
            AiTaskRegistry.Register("meleeattackcrab", typeof(AiTaskMeleeAttackCrab));

            //JHR
            api.RegisterCollectibleBehaviorClass("inTreeHollowTransform", typeof(BehaviorInTreeHollowTransform));
            //END JHR

            api.RegisterBlockBehaviorClass("RightClickPickupSpawnWorm", typeof(RightClickPickupSpawnWorm));
            api.RegisterBlockBehaviorClass("RightClickPickupRaft", typeof(RightClickPickupRaft));
            api.RegisterBlockBehaviorClass("RightClickPickupFireflies", typeof(RightClickPickupFireflies));

            api.RegisterBlockEntityClass("bedeadfall", typeof(BEDeadfall));
            api.RegisterBlockEntityClass("besnare", typeof(BESnare));
            api.RegisterBlockEntityClass("belimbtrotlinelure", typeof(BELimbTrotLineLure));
            api.RegisterBlockEntityClass("befishbasket", typeof(BEFishBasket));
            api.RegisterBlockEntityClass("beweirtrap", typeof(BEWeirTrap));
            api.RegisterBlockEntityClass("betemporalbase", typeof(BETemporalBase));
            api.RegisterBlockEntityClass("betemporallectern", typeof(BETemporallectern));
            api.RegisterBlockEntityClass("betemporalcube", typeof(BETemporalCube));
            api.RegisterBlockEntityClass("bewoodsupportspikes", typeof(BEWoodSupportSpikes));
            api.RegisterBlockEntityClass("bealcove", typeof(BEAlcove));
            api.RegisterBlockEntityClass("bemetalbucket", typeof(BEMetalBucket));
            api.RegisterBlockEntityClass("bemetalbucketfilled", typeof(BEMetalBucketFilled));
            api.RegisterBlockEntityClass("befireflies", typeof(BEFireflies));
            api.RegisterBlockEntityClass("befirework", typeof(BEFirework));
            api.RegisterBlockEntityClass("befuse", typeof(BEFuse));
            api.RegisterBlockEntityClass("beparticulator", typeof(BEParticulator));
            api.RegisterBlockEntityClass("betreehollowgrown", typeof(BETreeHollowGrown));
            api.RegisterBlockEntityClass("betreehollowplaced", typeof(BETreeHollowPlaced));
            api.RegisterBlockEntityClass("bebombfuse", typeof(BEBombFuse));
            api.RegisterBlockEntityClass("besupport", typeof(BESupport));
            api.RegisterBlockEntityClass("beirrigationvessel", typeof(BEIrrigationVessel));
            api.RegisterBlockEntityClass("besmoker", typeof(BESmoker));
            api.RegisterBlockEntityClass("befurrowedland", typeof(BEFurrowedLand));

            api.RegisterBlockClass("BlockLiquidIrrigationVesselBase", typeof(BlockLiquidIrrigationVesselBase));
            api.RegisterBlockClass("BlockLiquidIrrigationVesselTopOpened", typeof(BlockLiquidIrrigationVesselTopOpened));

            api.RegisterBlockClass("blockearthwormcastings", typeof(BlockEarthwormCastings));
            api.RegisterBlockClass("blockfuse", typeof(BlockFuse));
            api.RegisterBlockClass("blockstakeinwater", typeof(BlockStakeInWater));
            api.RegisterBlockClass("blockdeadfall", typeof(BlockDeadfall));
            api.RegisterBlockClass("blocksnare", typeof(BlockSnare));
            api.RegisterBlockClass("blocklimbtrotlinelure", typeof(BlockLimbTrotLineLure));
            api.RegisterBlockClass("blockfishbasket", typeof(BlockFishBasket));
            api.RegisterBlockClass("blockweirtrap", typeof(BlockWeirTrap));
            api.RegisterBlockClass("blocktemporal", typeof(BlockTemporal));
            api.RegisterBlockClass("blocktemporalbase", typeof(BlockTemporalBase));
            api.RegisterBlockClass("blocktemporallectern", typeof(BlockTemporallectern));
            api.RegisterBlockClass("blocktemporalcube", typeof(BlockTemporalCube));
            api.RegisterBlockClass("blockspiketrap", typeof(BlockSpikeTrap));
            api.RegisterBlockClass("blockwoodsupportspikes", typeof(BlockWoodSupportSpikes));
            api.RegisterBlockClass("blockpsstairs", typeof(BlockPSStairs));
            api.RegisterBlockClass("blockpspillar", typeof(BlockPSPillar));
            api.RegisterBlockClass("blocknsew", typeof(BlockNSEW));
            api.RegisterBlockClass("blockalcove", typeof(BlockAlcove));
            api.RegisterBlockClass("blockmetalbucket", typeof(BlockMetalBucket));
            api.RegisterBlockClass("blockmetalbucketfilled", typeof(BlockMetalBucketFilled));
            api.RegisterBlockClass("blockmonkeybridge", typeof(BlockMonkeyBridge));
            api.RegisterBlockClass("blockhide", typeof(BlockHide));
            api.RegisterBlockClass("blockraft", typeof(BlockRaft));
            api.RegisterBlockClass("blockblood", typeof(BlockBlood));
            api.RegisterBlockClass("blockfireflies", typeof(BlockFireflies));
            api.RegisterBlockClass("blockfirework", typeof(BlockFirework));
            api.RegisterBlockClass("blockparticulator", typeof(BlockParticulator));
            api.RegisterBlockClass("blockbstairs", typeof(BlockBStairs));
            api.RegisterBlockClass("blocktreehollowgrown", typeof(BlockTreeHollowGrown));
            api.RegisterBlockClass("blocktreehollowplaced", typeof(BlockTreeHollowPlaced));
            api.RegisterBlockClass("blockhandofthedead", typeof(BlockHandOfTheDead));
            api.RegisterBlockClass("blockskullofthedead", typeof(BlockSkullOfTheDead));
            api.RegisterBlockClass("blockbombfuse", typeof(BlockBombFuse));
            api.RegisterBlockClass("blocksupport", typeof(BlockSupport));
            api.RegisterBlockClass("blockirrigationvessel", typeof(BlockIrrigationVessel));
            api.RegisterBlockClass("blocksmoker", typeof(BlockSmoker));
            api.RegisterBlockClass("blockfurrowedland", typeof(BlockFurrowedLand));
            api.RegisterBlockClass("blockpipe", typeof(BlockPipe));

            api.RegisterItemClass("itemcordage", typeof(ItemCordage));
            api.RegisterItemClass("itemfuse", typeof(ItemFuse));
            api.RegisterItemClass("itemwoodspikebundle", typeof(ItemWoodSpikeBundle));
            api.RegisterItemClass("itempsgear", typeof(ItemPSGear));
            api.RegisterItemClass("itemmonkeybridge", typeof(ItemMonkeyBridge));
            api.RegisterItemClass("itempelt", typeof(ItemPelt));
            api.RegisterItemClass("itemearthworm", typeof(ItemEarthworm));
            api.RegisterItemClass("itemsnake", typeof(ItemSnake));
            api.RegisterItemClass("itemcrab", typeof(ItemCrab));
            api.RegisterItemClass("itempsfish", typeof(ItemPSFish));
            api.RegisterItemClass("itemfisheggs", typeof(ItemFishEggs));
            api.RegisterItemClass("itemlinktool", typeof(ItemLinkTool));
            api.RegisterItemClass("itemlivingdead", typeof(ItemLivingDead));
            api.RegisterItemClass("itemwillowisp", typeof(ItemWillowisp));
            api.RegisterItemClass("itembioluminescent", typeof(ItemBioluminescent));
            api.RegisterItemClass("itemstick", typeof(ItemStick));
            api.RegisterItemClass("ItemHoeExtended", typeof(ItemHoeExtended));
        }

        public void UpdateSpawnRate(ICoreAPI api, string entityCode, double multiplier)
        {
            if (multiplier == 1)
            { return; }
            if (multiplier >= 10)
            { multiplier = 10; } //put a limiter on this to prevent a total lagfest

            for (var i = api.World.EntityTypes.Count - 1; i >= 0; i--)
            {
                if (api.World.EntityTypes[i].Code.Path == entityCode)
                {
                    if (api.World.EntityTypes[i].Server?.SpawnConditions?.Runtime != null)
                    {
                        api.World.EntityTypes[i].Server.SpawnConditions.Runtime.Chance *= multiplier;
                    }
                    if (api.World.EntityTypes[i].Server?.SpawnConditions?.Worldgen != null)
                    {
                        api.World.EntityTypes[i].Server.SpawnConditions.Worldgen.TriesPerChunk.avg *= (float)multiplier;
                    }
                    break;
                }
            }
        }


        public void UpdateSpawnRates(ICoreAPI api)
        {
            this.UpdateSpawnRate(api, "bioluminescent-globe", ModConfig.Loaded.SpawnMultiplierBioluminescentGlobe);
            this.UpdateSpawnRate(api, "bioluminescent-jelly", ModConfig.Loaded.SpawnMultiplierBioluminescentJelly);
            this.UpdateSpawnRate(api, "bioluminescent-orangejelly", ModConfig.Loaded.SpawnMultiplierBioluminescentOrangeJelly);
            this.UpdateSpawnRate(api, "bioluminescent-worm", ModConfig.Loaded.SpawnMultiplierBioluminescentWorm);

            this.UpdateSpawnRate(api, "bairdicrab", ModConfig.Loaded.SpawnMultiplierCrabBairdi);
            this.UpdateSpawnRate(api, "landcrab", ModConfig.Loaded.SpawnMultiplierCrabLand);

            this.UpdateSpawnRate(api, "livingdead-normal", ModConfig.Loaded.SpawnMultiplierLivingDead);

            this.UpdateSpawnRate(api, "blackrat", ModConfig.Loaded.SpawnMultiplierSnakeBlackRat);
            this.UpdateSpawnRate(api, "chainviper", ModConfig.Loaded.SpawnMultiplierSnakeChainViper);
            this.UpdateSpawnRate(api, "coachwhip", ModConfig.Loaded.SpawnMultiplierSnakeCoachWhip);
            this.UpdateSpawnRate(api, "pitviper", ModConfig.Loaded.SpawnMultiplierSnakePitViper);

            this.UpdateSpawnRate(api, "willowisp-green", ModConfig.Loaded.SpawnMultiplierWillowispGreen);
            this.UpdateSpawnRate(api, "willowisp-white", ModConfig.Loaded.SpawnMultiplierWillowispWhite);
            this.UpdateSpawnRate(api, "willowisp-yellow", ModConfig.Loaded.SpawnMultiplierWillowispYellow);
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            this.prevChunksLoaded = false;
            base.StartServerSide(api);
            this.sapi = api;
            api.Event.SaveGameLoaded += this.OnSaveGameLoading;
            api.Event.GameWorldSave += this.OnSaveGameSaving;
            var repleteTick = api.Event.RegisterGameTickListener(this.RepleteFishStocks, 60000 * ModConfig.Loaded.FishChunkRepletionMinutes);
        }


        public override void AssetsFinalize(ICoreAPI api)
        {
            this.UpdateSpawnRates(api);
        }


        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.World.Logger.Event("started 'Primitive Survival' mod");
            this.RegisterClasses(api);
        }


        private void OnSaveGameLoading()
        {
            fishingChunks = new Dictionary<IServerChunk, int>();
            // attempt to load the (short) list of all active fishing chunks
            var data = this.sapi.WorldManager.SaveGame.GetData("chunklist");
            chunkList = data == null ? new List<string>() : SerializerUtil.Deserialize<List<string>>(data);
        }


        private void OnSaveGameSaving()
        {
            var chunkcount = 0;
            foreach (var chunk in fishingChunks)
            {
                if (chunk.Value == 0)
                { continue; }

                chunk.Key.SetServerModdata(this.thisModID, SerializerUtil.Serialize(chunk.Value));
                chunkcount++;
            }
            //Debug.WriteLine("----------- Chunk depletion data saved to " + chunkcount + " chunks");
            // now attempt to save the (short) list of all active fishing chunks
            this.sapi.WorldManager.SaveGame.StoreData("chunklist", SerializerUtil.Serialize(chunkList));
            /*
            Debug.WriteLine("----------- Chunk depletion data saved for " + chunkList.Count() + " chunks");
            foreach (var entry in chunkList)
            {
                Debug.WriteLine(entry);
            }
            */
        }

        private static void AddChunkToDictionary(IServerChunk chunk)
        {
            var data = chunk.GetServerModdata("primitivesurvival");
            var fishing = data == null ? 0 : SerializerUtil.Deserialize<int>(data);
            fishingChunks.Add(chunk, fishing);
        }


        public static void UpdateChunkInDictionary(ICoreServerAPI api, BlockPos pos, int rate)
        {
            //deplete
            var chunk = api.WorldManager.GetChunk(pos);
            if (!fishingChunks.ContainsKey(chunk))
            {
                AddChunkToDictionary(chunk);
            }
            var chunkindex = pos.X.ToString() + "," + pos.Y.ToString() + "," + pos.Z.ToString();
            if (!chunkList.Contains(chunkindex))
            {
                chunkList.Add(chunkindex);
            }
            if (0 <= fishingChunks[chunk] && fishingChunks[chunk] < ModConfig.Loaded.FishChunkMaxDepletionPercent)
            { fishingChunks[chunk] += rate; }

            if (fishingChunks[chunk] < 0)
            { fishingChunks[chunk] = 0; }
            if (fishingChunks[chunk] > ModConfig.Loaded.FishChunkMaxDepletionPercent)
            { fishingChunks[chunk] = ModConfig.Loaded.FishChunkMaxDepletionPercent; }

            chunk.MarkModified();

            //Debug
            /*
            var fishing = fishingChunks[chunk];
            var msg = "depleted (caught)";
            if (rate < 0)
            { msg = "repleted (escaped)"; }
            Debug.WriteLine("----------- Chunk " + pos.ToVec3d() + " - " + msg + ":" + fishing);
            */
        }

        private void RepleteFishStocks(float par)
        {
            if (!this.prevChunksLoaded)
            {
                var chunklistcount = 0;
                foreach (var chunk in chunkList)
                {
                    var coords = chunk.Split(',');
                    /*
                    var pos = new BlockPos
                    {
                        X = coords[0].ToInt(),
                        Y = coords[1].ToInt(),
                        Z = coords[2].ToInt()
                    };*/
                    var pos = new BlockPos(coords[0].ToInt(), coords[1].ToInt(), coords[2].ToInt(), 0);

                    var getchunk = this.sapi.WorldManager.GetChunk(pos);
                    if (getchunk != null)
                    {
                        var getdata = getchunk.GetServerModdata("primitivesurvival");
                        var fishing = getdata == null ? 0 : SerializerUtil.Deserialize<int>(getdata);
                        if (!fishingChunks.ContainsKey(getchunk))
                        {
                            fishingChunks.Add(getchunk, fishing);
                            chunklistcount++;
                            this.prevChunksLoaded = true;
                        }
                        getchunk.MarkModified();
                    }
                }
                //Debug.WriteLine("Chunk data restored for " + chunklistcount + " chunks");
            }

            foreach (var key in fishingChunks.Keys.ToList())
            {
                fishingChunks[key] = fishingChunks[key] - ModConfig.Loaded.FishChunkRepletionRate;
                if (fishingChunks[key] < 0)
                { fishingChunks[key] = 0; }
                // Debug.WriteLine("----------- Chunk repletion:" + fishingChunks[key]);
            }
        }

        public static int FishDepletedPercent(ICoreServerAPI api, BlockPos pos)
        {
            var rate = 0;
            var chunk = api.WorldManager.GetChunk(pos);
            if (fishingChunks.ContainsKey(chunk))
            {
                rate = fishingChunks[chunk];
            }
            return rate;
        }

        public override void Dispose()
        {
            var PSPumpkinPatchOriginal = typeof(PumpkinCropBehavior).GetMethod(nameof(PumpkinCropBehavior.CanSupportPumpkin));
            this.harmony.Unpatch(PSPumpkinPatchOriginal, HarmonyPatchType.Prefix, "*");

            if (ModConfig.Loaded.ShowModNameInHud)
            {
                var PSBlockGetPlacedBlockInfoOriginal = typeof(Block).GetMethod(nameof(Block.GetPlacedBlockInfo));
                this.harmony.Unpatch(PSBlockGetPlacedBlockInfoOriginal, HarmonyPatchType.Postfix, "*");
            }

            if (ModConfig.Loaded.ShowModNameInGuis)
            {
                var PSCollectibleGetHeldItemInfoOriginal = typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetHeldItemInfo));
                this.harmony.Unpatch(PSCollectibleGetHeldItemInfoOriginal, HarmonyPatchType.Postfix, "*");
            }

            base.Dispose();
        }

        //allow pumpkin vines to grow on furrowed land and irrigation vessels
        public class PS_CanSupportPumpkin_Patch
        {
            [HarmonyPrefix]
            public static bool PSCanSupportPumpkinPrefix(ref bool __result, ICoreAPI api, BlockPos pos)
            {
                var bclass = api.World.BlockAccessor.GetBlock(pos, 1).Class;
                if (bclass == "blockfurrowedland" || bclass == "blockirrigationvessel")
                {
                    __result = true;
                    return false; //skip original
                }
                return true;
            }
        }

        // display mod name in the hud for blocks
        public class PS_BlockGetPlacedBlockInfo_Patch
        {
            [HarmonyPostfix]
            public static void PSBlockGetPlacedBlockInfoPostfix(ref string __result, IPlayer forPlayer)
            {
                var domain = forPlayer.Entity?.BlockSelection?.Block?.Code?.Domain;
                if (domain != null)
                {
                    if (domain == "primitivesurvival")
                    {
                        __result += "\n\n<font color=\"#D8EAA3\"><i>Primitive Survival</i></font>\n\n";
                    }
                }
            }
        }


        public class PS_CollectibleGetHeldItemInfo_Patch
        {
            [HarmonyPostfix]
            public static void PSCollectibleGetHeldItemInfoPostfix(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
            {
                var domain = inSlot.Itemstack?.Collectible?.Code?.Domain;
                if (domain != null)
                {
                    if (domain == "primitivesurvival")
                    {
                        dsc.AppendLine("\n<font color=\"#D8EAA3\"><i>Primitive Survival</i></font>");
                    }
                }
            }
        }
    }
}






