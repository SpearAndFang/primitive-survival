namespace PrimitiveSurvival.ModSystem
{
    using HarmonyLib;
    using PrimitiveSurvival.ModConfig;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.API.Util;
    using Vintagestory.API.Config;
    using Vintagestory.Client.NoObf;
    using Vintagestory.GameContent;
 
    //using System.Diagnostics;

    public class PrimitiveSurvivalSystem : ModSystem
    {
        public IShaderProgram EntityGenericShaderProgram { get; private set; }

        private readonly string thisModID = "primitivesurvival119";
        private static Dictionary<IServerChunk, int> fishingChunks;

        public static List<string> chunkList;

        private bool prevChunksLoaded;

        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;

        private VenomOverlayRenderer vrenderer;

        private readonly Harmony harmony = new Harmony("com.spearandfang.primitivesurvival");

        private IServerNetworkChannel serverChannel;
        private ICoreAPI api;

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
                //this.Mod.Logger.Event($"ShowModNameInHud set to {ModConfig.Loaded.ShowModNameInHud} on client");
            }

            if (ModConfig.Loaded.ShowModNameInGuis)
            {
                var PSCollectibleGetHeldItemInfoOriginal = typeof(CollectibleObject).GetMethod(nameof(CollectibleObject.GetHeldItemInfo));
                var PSCollectibleGetHeldItemInfoPostfix = typeof(PS_CollectibleGetHeldItemInfo_Patch).GetMethod(nameof(PS_CollectibleGetHeldItemInfo_Patch.PSCollectibleGetHeldItemInfoPostfix));
                this.harmony.Patch(PSCollectibleGetHeldItemInfoOriginal, postfix: new HarmonyMethod(PSCollectibleGetHeldItemInfoPostfix));
                //this.Mod.Logger.Event($"ShowModNameInGuis set to {ModConfig.Loaded.ShowModNameInGuis} on client");

            }

            this.capi = api;
            this.capi.Event.ReloadShader += this.LoadCustomShaders;
            this.LoadCustomShaders();
            
            //1.20 this broke again
            //this.capi.RegisterEntityRendererClass("entitygenericshaperenderer", typeof(EntityGenericShapeRenderer));
            

            this.vrenderer = new VenomOverlayRenderer(api);
            api.Event.RegisterRenderer(this.vrenderer, EnumRenderStage.Ortho);

            //JHR
            (this.capi.World as ClientMain).RegisterDialog(new GuiDialogHollowTransform(this.capi));
            //END JHR

            capi.Network.RegisterChannel("primitivesurvival")
               .RegisterMessageType<SyncClientPacket>()
               .SetMessageHandler<SyncClientPacket>(packet =>
               {
                   ModConfig.Loaded.AltarDropsFish = packet.AltarDropsFish;
                   this.Mod.Logger.Event($"Received AltarDropsFish of {packet.AltarDropsFish} from server");
                   ModConfig.Loaded.AltarDropsGold = packet.AltarDropsGold;
                   this.Mod.Logger.Event($"Received AltarDropsGold of {packet.AltarDropsGold} from server");
                   ModConfig.Loaded.AltarDropsVegetables = packet.AltarDropsVegetables;
                   this.Mod.Logger.Event($"Received AltarDropsVegetables of {packet.AltarDropsVegetables} from server");
                   ModConfig.Loaded.DeadfallBaitStolenPercent = packet.DeadfallBaitStolenPercent;
                   this.Mod.Logger.Event($"Received DeadfallBaitStolenPercent of {packet.DeadfallBaitStolenPercent} from server");
                   ModConfig.Loaded.DeadfallMaxAnimalHeight = packet.DeadfallMaxAnimalHeight;
                   this.Mod.Logger.Event($"Received DeadfallMaxAnimalHeight of {packet.DeadfallMaxAnimalHeight} from server");
                   ModConfig.Loaded.DeadfallMaxDamageSet = packet.DeadfallMaxDamageSet;
                   this.Mod.Logger.Event($"Received DeadfallMaxDamageSet of {packet.DeadfallMaxDamageSet} from server");
                   ModConfig.Loaded.DeadfallMaxDamageBaited = packet.DeadfallMaxDamageBaited;
                   this.Mod.Logger.Event($"Received DeadfallMaxDamageBaited of {packet.DeadfallMaxDamageBaited} from server");
                   ModConfig.Loaded.DeadfallTrippedPercent = packet.DeadfallTrippedPercent;
                   this.Mod.Logger.Event($"Received DeadfallTrippedPercent of {packet.DeadfallTrippedPercent} from server");
                   ModConfig.Loaded.FallDamageMultiplierWoodSpikes = packet.FallDamageMultiplierWoodSpikes;
                   this.Mod.Logger.Event($"Received FallDamageMultiplierWoodSpikes of {packet.FallDamageMultiplierWoodSpikes} from server");
                   ModConfig.Loaded.FallDamageMultiplierMetalSpikes = packet.FallDamageMultiplierMetalSpikes;
                   this.Mod.Logger.Event($"Received FallDamageMultiplierMetalSpikes of {packet.FallDamageMultiplierMetalSpikes} from server");
                   
                   ModConfig.Loaded.FishBasketCatchPercent = packet.FishBasketCatchPercent;
                   this.Mod.Logger.Event($"Received FishBasketCatchPercent of {packet.FishBasketCatchPercent} from server");
                   ModConfig.Loaded.FishBasketBaitedCatchPercent = packet.FishBasketBaitedCatchPercent;
                   this.Mod.Logger.Event($"Received FishBasketBaitedCatchPercent of {packet.FishBasketBaitedCatchPercent} from server");
                   ModConfig.Loaded.FishBasketBaitStolenPercent = packet.FishBasketBaitStolenPercent;
                   this.Mod.Logger.Event($"Received FishBasketBaitStolenPercent of {packet.FishBasketBaitStolenPercent} from server");
                   ModConfig.Loaded.FishBasketEscapePercent = packet.FishBasketEscapePercent;
                   this.Mod.Logger.Event($"Received FishBasketEscapePercent of {packet.FishBasketEscapePercent} from server");
                   ModConfig.Loaded.FishBasketUpdateMinutes = packet.FishBasketUpdateMinutes;
                   this.Mod.Logger.Event($"Received FishBasketUpdateMinutes of {packet.FishBasketUpdateMinutes} from server");
                   ModConfig.Loaded.FishBasketRotRemovedPercent = packet.FishBasketRotRemovedPercent;
                   this.Mod.Logger.Event($"Received FishBasketRotRemovedPercent of {packet.FishBasketRotRemovedPercent} from server");
                   ModConfig.Loaded.FishChanceOfEggsPercent = packet.FishChanceOfEggsPercent;
                   this.Mod.Logger.Event($"Received FishChanceOfEggsPercent of {packet.FishChanceOfEggsPercent} from server");
                   ModConfig.Loaded.FishChunkDepletionRate = packet.FishChunkDepletionRate;
                   this.Mod.Logger.Event($"Received FishChunkDepletionRate of {packet.FishChunkDepletionRate} from server");
                   ModConfig.Loaded.FishChunkRepletionRate = packet.FishChunkRepletionRate;
                   this.Mod.Logger.Event($"Received FishChunkRepletionRate of {packet.FishChunkRepletionRate} from server");
                   ModConfig.Loaded.FishChunkRepletionMinutes = packet.FishChunkRepletionMinutes;
                   this.Mod.Logger.Event($"Received FishChunkRepletionMinutes of {packet.FishChunkRepletionMinutes} from server");
                   
                   ModConfig.Loaded.FishEggsChunkRepletionRate = packet.FishEggsChunkRepletionRate;
                   this.Mod.Logger.Event($"Received FishEggsChunkRepletionRate of {packet.FishEggsChunkRepletionRate} from server");
                   ModConfig.Loaded.FishChunkMaxDepletionPercent = packet.FishChunkMaxDepletionPercent;
                   this.Mod.Logger.Event($"Received FishChunkMaxDepletionPercent of {packet.FishChunkMaxDepletionPercent} from server");
                   ModConfig.Loaded.FurrowedLandUpdateFrequency = packet.FurrowedLandUpdateFrequency;
                   this.Mod.Logger.Event($"Received FurrowedLandUpdateFrequency of {packet.FurrowedLandUpdateFrequency} from server");
                   ModConfig.Loaded.FurrowedLandBlockageChancePercent = packet.FurrowedLandBlockageChancePercent;
                   this.Mod.Logger.Event($"Received FurrowedLandBlockageChancePercent of {packet.FurrowedLandBlockageChancePercent} from server");
                   ModConfig.Loaded.FurrowedLandMinMoistureClose = packet.FurrowedLandMinMoistureClose;
                   this.Mod.Logger.Event($"Received FurrowedLandMinMoistureClose of {packet.FurrowedLandMinMoistureClose} from server");
                   ModConfig.Loaded.FurrowedLandMinMoistureFar = packet.FurrowedLandMinMoistureFar;
                   this.Mod.Logger.Event($"Received FurrowedLandMinMoistureFar of {packet.FurrowedLandMinMoistureFar} from server");
                   ModConfig.Loaded.LimbTrotlineCatchPercent = packet.LimbTrotlineCatchPercent;
                   this.Mod.Logger.Event($"Received LimbTrotlineCatchPercent of {packet.LimbTrotlineCatchPercent} from server");
                   ModConfig.Loaded.LimbTrotlineBaitedCatchPercent = packet.LimbTrotlineBaitedCatchPercent;
                   this.Mod.Logger.Event($"Received LimbTrotlineBaitedCatchPercent of {packet.LimbTrotlineBaitedCatchPercent} from server");
                   ModConfig.Loaded.LimbTrotlineLuredCatchPercent = packet.LimbTrotlineLuredCatchPercent;
                   this.Mod.Logger.Event($"Received LimbTrotlineLuredCatchPercent of {packet.LimbTrotlineLuredCatchPercent} from server");
                   ModConfig.Loaded.LimbTrotlineBaitedLuredCatchPercent = packet.LimbTrotlineBaitedLuredCatchPercent;
                   this.Mod.Logger.Event($"Received LimbTrotlineBaitedLuredCatchPercent of {packet.LimbTrotlineBaitedLuredCatchPercent} from server");

                   ModConfig.Loaded.LimbTrotlineBaitStolenPercent = packet.LimbTrotlineBaitStolenPercent;
                   this.Mod.Logger.Event($"Received LimbTrotlineBaitStolenPercent of {packet.LimbTrotlineBaitStolenPercent} from server");
                   ModConfig.Loaded.LimbTrotlineUpdateMinutes = packet.LimbTrotlineUpdateMinutes;
                   this.Mod.Logger.Event($"Received LimbTrotlineUpdateMinutes of {packet.LimbTrotlineUpdateMinutes} from server");
                   ModConfig.Loaded.LimbTrotlineRotRemovedPercent = packet.LimbTrotlineRotRemovedPercent;
                   this.Mod.Logger.Event($"Received LimbTrotlineRotRemovedPercent of {packet.LimbTrotlineRotRemovedPercent} from server");
                   ModConfig.Loaded.MonkeyBridgeMaxLength = packet.MonkeyBridgeMaxLength;
                   this.Mod.Logger.Event($"Received MonkeyBridgeMaxLength of {packet.MonkeyBridgeMaxLength} from server");
                   ModConfig.Loaded.ParticulatorMaxParticlesQuantity = packet.ParticulatorMaxParticlesQuantity;
                   this.Mod.Logger.Event($"Received ParticulatorMaxParticlesQuantity of {packet.ParticulatorMaxParticlesQuantity} from server");
                   ModConfig.Loaded.ParticulatorMaxParticlesSize = packet.ParticulatorMaxParticlesSize;
                   this.Mod.Logger.Event($"Received ParticulatorMaxParticlesSize of {packet.ParticulatorMaxParticlesSize} from server");
                   ModConfig.Loaded.ParticulatorHideCodeTabs = packet.ParticulatorHideCodeTabs;
                   this.Mod.Logger.Event($"Received ParticulatorHideCodeTabs of {packet.ParticulatorHideCodeTabs} from server");
                   ModConfig.Loaded.PipeUpdateFrequency = packet.PipeUpdateFrequency;
                   this.Mod.Logger.Event($"Received PipeUpdateFrequency of {packet.PipeUpdateFrequency} from server");
                   ModConfig.Loaded.PipeBlockageChancePercent = packet.PipeBlockageChancePercent;
                   this.Mod.Logger.Event($"Received PipeBlockageChancePercent of {packet.PipeBlockageChancePercent} from server");
                   ModConfig.Loaded.PipeMinMoisture = packet.PipeMinMoisture;
                   this.Mod.Logger.Event($"Received PipeMinMoisture of {packet.PipeMinMoisture} from server");
                   
                   ModConfig.Loaded.RaftWaterSpeedModifier = packet.RaftWaterSpeedModifier;
                   this.Mod.Logger.Event($"Received RaftWaterSpeedModifier of {packet.RaftWaterSpeedModifier} from server");
                   ModConfig.Loaded.RaftFlotationModifier = packet.RaftFlotationModifier;
                   this.Mod.Logger.Event($"Received RaftFlotationModifier of {packet.RaftFlotationModifier} from server");
                   ModConfig.Loaded.SnareBaitStolenPercent = packet.SnareBaitStolenPercent;
                   this.Mod.Logger.Event($"Received SnareBaitStolenPercent of {packet.SnareBaitStolenPercent} from server");
                   ModConfig.Loaded.SnareMaxAnimalHeight = packet.SnareMaxAnimalHeight;
                   this.Mod.Logger.Event($"Received SnareMaxAnimalHeight of {packet.SnareMaxAnimalHeight} from server");
                   ModConfig.Loaded.SnareMaxDamageSet = packet.SnareMaxDamageSet;
                   this.Mod.Logger.Event($"Received SnareMaxDamageSet of {packet.SnareMaxDamageSet} from server");
                   ModConfig.Loaded.SnareMaxDamageBaited = packet.SnareMaxDamageBaited;
                   this.Mod.Logger.Event($"Received SnareMaxDamageBaited of {packet.SnareMaxDamageBaited} from server");
                   ModConfig.Loaded.SnareTrippedPercent = packet.SnareTrippedPercent;
                   this.Mod.Logger.Event($"Received SnareTrippedPercent of {packet.SnareTrippedPercent} from server");
                   ModConfig.Loaded.SpawnMultiplierBioluminescentGlobe = packet.SpawnMultiplierBioluminescentGlobe;
                   this.Mod.Logger.Event($"Received SpawnMultiplierBioluminescentGlobe of {packet.SpawnMultiplierBioluminescentGlobe} from server");

                   ModConfig.Loaded.SpawnMultiplierBioluminescentJelly = packet.SpawnMultiplierBioluminescentJelly;
                   this.Mod.Logger.Event($"Received SpawnMultiplierBioluminescentJelly of {packet.SpawnMultiplierBioluminescentJelly} from server");
                   ModConfig.Loaded.SpawnMultiplierBioluminescentOrangeJelly = packet.SpawnMultiplierBioluminescentOrangeJelly;
                   this.Mod.Logger.Event($"Received SpawnMultiplierBioluminescentOrangeJelly of {packet.SpawnMultiplierBioluminescentOrangeJelly} from server");
                   ModConfig.Loaded.SpawnMultiplierBioluminescentWorm = packet.SpawnMultiplierBioluminescentWorm;
                   this.Mod.Logger.Event($"Received SpawnMultiplierBioluminescentWorm of {packet.SpawnMultiplierBioluminescentWorm} from server");
                   ModConfig.Loaded.SpawnMultiplierCrabBairdi = packet.SpawnMultiplierCrabBairdi;
                   this.Mod.Logger.Event($"Received SpawnMultiplierCrabBairdi of {packet.SpawnMultiplierCrabBairdi} from server");
                   ModConfig.Loaded.SpawnMultiplierCrabLand = packet.SpawnMultiplierCrabLand;
                   this.Mod.Logger.Event($"Received SpawnMultiplierCrabLand of {packet.SpawnMultiplierCrabLand} from server");
                   ModConfig.Loaded.SpawnMultiplierLivingDead = packet.SpawnMultiplierLivingDead;
                   this.Mod.Logger.Event($"Received SpawnMultiplierLivingDead of {packet.SpawnMultiplierLivingDead} from server");
                   ModConfig.Loaded.SpawnMultiplierSnakeBlackRat = packet.SpawnMultiplierSnakeBlackRat;
                   this.Mod.Logger.Event($"Received SpawnMultiplierSnakeBlackRat of {packet.SpawnMultiplierSnakeBlackRat} from server");
                   ModConfig.Loaded.SpawnMultiplierSnakeChainViper = packet.SpawnMultiplierSnakeChainViper;
                   this.Mod.Logger.Event($"Received SpawnMultiplierSnakeChainViper of {packet.SpawnMultiplierSnakeChainViper} from server");
                   ModConfig.Loaded.SpawnMultiplierSnakeCoachWhip = packet.SpawnMultiplierSnakeCoachWhip;
                   this.Mod.Logger.Event($"Received SpawnMultiplierSnakeCoachWhip of {packet.SpawnMultiplierSnakeCoachWhip} from server");
                   ModConfig.Loaded.SpawnMultiplierSnakePitViper = packet.SpawnMultiplierSnakePitViper;
                   this.Mod.Logger.Event($"Received SpawnMultiplierSnakePitViper of {packet.SpawnMultiplierSnakePitViper} from server");

                   ModConfig.Loaded.SpawnMultiplierWillowispGreen = packet.SpawnMultiplierWillowispGreen;
                   this.Mod.Logger.Event($"Received SpawnMultiplierWillowispGreen of {packet.SpawnMultiplierWillowispGreen} from server");
                   ModConfig.Loaded.SpawnMultiplierWillowispWhite = packet.SpawnMultiplierWillowispWhite;
                   this.Mod.Logger.Event($"Received SpawnMultiplierWillowispWhite of {packet.SpawnMultiplierWillowispWhite} from server");
                   ModConfig.Loaded.SpawnMultiplierWillowispYellow = packet.SpawnMultiplierWillowispYellow;
                   this.Mod.Logger.Event($"Received SpawnMultiplierWillowispYellow of {packet.SpawnMultiplierWillowispYellow} from server");
                   ModConfig.Loaded.TreeHollowsMaxItems = packet.TreeHollowsMaxItems;
                   this.Mod.Logger.Event($"Received TreeHollowsMaxItems of {packet.TreeHollowsMaxItems} from server");
                   ModConfig.Loaded.TreeHollowsEnableDeveloperTools = packet.TreeHollowsEnableDeveloperTools;
                   this.Mod.Logger.Event($"Received TreeHollowsEnableDeveloperTools of {packet.TreeHollowsEnableDeveloperTools} from server");
                   ModConfig.Loaded.TreeHollowsMaxPerChunk = packet.TreeHollowsMaxPerChunk;
                   this.Mod.Logger.Event($"Received TreeHollowsMaxPerChunk of {packet.TreeHollowsMaxPerChunk} from server");
                   ModConfig.Loaded.TreeHollowsSpawnProbability = packet.TreeHollowsSpawnProbability;
                   this.Mod.Logger.Event($"Received TreeHollowsSpawnProbability of {packet.TreeHollowsSpawnProbability} from server");
                   ModConfig.Loaded.TreeHollowsUpdateMinutes = packet.TreeHollowsUpdateMinutes;
                   this.Mod.Logger.Event($"Received TreeHollowsUpdateMinutes of {packet.TreeHollowsUpdateMinutes} from server");
                   ModConfig.Loaded.WeirTrapCatchPercent = packet.WeirTrapCatchPercent;
                   this.Mod.Logger.Event($"Received WeirTrapCatchPercent of {packet.WeirTrapCatchPercent} from server");
                   ModConfig.Loaded.WeirTrapEscapePercent = packet.WeirTrapEscapePercent;
                   this.Mod.Logger.Event($"Received WeirTrapEscapePercent of {packet.WeirTrapEscapePercent} from server");

                   ModConfig.Loaded.WeirTrapUpdateMinutes = packet.WeirTrapUpdateMinutes;
                   this.Mod.Logger.Event($"Received WeirTrapUpdateMinutes of {packet.WeirTrapUpdateMinutes} from server");
                   ModConfig.Loaded.WeirTrapRotRemovedPercent = packet.WeirTrapRotRemovedPercent;
                   this.Mod.Logger.Event($"Received WeirTrapRotRemovedPercent of {packet.WeirTrapRotRemovedPercent} from server");
                   ModConfig.Loaded.WormFoundPercentRock = packet.WormFoundPercentRock;
                   this.Mod.Logger.Event($"Received WormFoundPercentRock of {packet.WormFoundPercentRock} from server");
                   ModConfig.Loaded.WormFoundPercentStickFlint = packet.WormFoundPercentStickFlint;
                   this.Mod.Logger.Event($"Received WormFoundPercentStickFlint of {packet.WormFoundPercentStickFlint} from server");

               });
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
            api.World.Config.SetBool("MetalBucketDisabled", ModConfig.Loaded.MetalBucketDisabled);
            api.World.Config.SetBool("RelicsDisabled", ModConfig.Loaded.RelicsDisabled);
            api.World.Config.SetBool("ParticulatorEnabled", ModConfig.Loaded.ParticulatorEnabled);

            /* debug
            if (api.Side == EnumAppSide.Server)
            {
                this.Mod.Logger.Event($"FurrowedLandEnabled set to {ModConfig.Loaded.FurrowedLandEnabled} on server");
                this.Mod.Logger.Event($"RaftEnabled set to {ModConfig.Loaded.RaftEnabled} on server");
                this.Mod.Logger.Event($"MetalBucketDisabled set to {ModConfig.Loaded.MetalBucketDisabled} on server");
                this.Mod.Logger.Event($"RelicsDisabled set to {ModConfig.Loaded.RelicsDisabled} on server");
                this.Mod.Logger.Event($"ParticulatorEnabled set to {ModConfig.Loaded.ParticulatorEnabled} on server");
            }
            */
            
            base.StartPre(api);

            // temp stupid bandaid
            // doesn't take effect until after a restart but better than nothing
            // if this doesn't minimize the bug reports then move to harmony patch
            if (api.Side != EnumAppSide.Client)
            {
                var taheight = ClientSettings.Inst.Int["maxTextureAtlasHeight"];
                if (taheight < 4096)
                {
                    try
                    {
                        ClientSettings.Inst.Int["maxTextureAtlasHeight"] = 4096;
                    }
                    catch { }
                }
            }
            // end temp stupid bandaid
        }


        public bool LoadCustomShaders()
        {

            this.EntityGenericShaderProgram = this.capi.Shader.NewShaderProgram();
            (this.EntityGenericShaderProgram as ShaderProgram).AssetDomain = "game";
            this.capi.Shader.RegisterFileShaderProgram("entityanimated", this.EntityGenericShaderProgram);
            this.EntityGenericShaderProgram.Compile();
            return true;
        }

        public void RegisterClasses(ICoreAPI api)
        {
            api.RegisterEntity("entityearthworm", typeof(EntityEarthworm));
            api.RegisterEntity("entityfireflies", typeof(EntityFireflies));
            api.RegisterEntity("entitypsglowingagent", typeof(EntityPSGlowingAgent));
            api.RegisterEntity("entitypsagent", typeof(EntityPSAgent));
            api.RegisterEntity("entitylivingdead", typeof(EntityLivingDead));
            api.RegisterEntity("entitygenericglowingagent", typeof(EntityGenericGlowingAgent));
            api.RegisterEntity("entityskullofthedead", typeof(EntitySkullOfTheDead));
            api.RegisterEntity("entitywillowisp", typeof(EntityWillowisp));
            api.RegisterEntity("entitybioluminescent", typeof(EntityBioluminescent));

            api.RegisterEntityBehaviorClass("carryable", typeof(EntityBehaviorCarryable));

            //3.9
            //AiTaskRegistry.Register("meleeattackvenomous", typeof(AiTaskMeleeAttackVenomous));
            //AiTaskRegistry.Register("meleeattackcrab", typeof(AiTaskMeleeAttackCrab));
            AiTaskRegistry.Register<AiTaskMeleeAttackVenomous>("meleeattackvenomous");
            AiTaskRegistry.Register<AiTaskMeleeAttackCrab>("meleeattackcrab");


            api.RegisterCollectibleBehaviorClass("inTreeHollowTransform", typeof(BehaviorInTreeHollowTransform));
            // ItemHoe class is preventing this from working
            // api.RegisterCollectibleBehaviorClass("BehaviorFurrow", typeof(BehaviorFurrow));

            api.RegisterBlockBehaviorClass("RightClickPickupSpawnWorm", typeof(RightClickPickupSpawnWorm));
            api.RegisterBlockBehaviorClass("RightClickPickupRaft", typeof(RightClickPickupRaft));
            api.RegisterBlockBehaviorClass("RightClickPickupFloatingDock", typeof(RightClickPickupFloatingDock));
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
            api.RegisterBlockEntityClass("befloatingdock", typeof(BEFloatingDock));

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
            api.RegisterBlockClass("blockfloatingdock", typeof(BlockFloatingDock));

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
            api.RegisterItemClass("itemfishingspear", typeof(ItemFishingSpear));

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
            base.StartServerSide(sapi);
            this.sapi = api;

            api.Event.SaveGameLoaded += this.OnSaveGameLoading;
            api.Event.GameWorldSave += this.OnSaveGameSaving;
            var repleteTick = api.Event.RegisterGameTickListener(this.RepleteFishStocks, 60000 * ModConfig.Loaded.FishChunkRepletionMinutes);

            sapi.Event.PlayerJoin += this.OnPlayerJoin; // add method so we can remove it in dispose to prevent memory leaks
            // register network channel to send data to clients
            this.serverChannel = sapi.Network.RegisterChannel("primitivesurvival")
                .RegisterMessageType<SyncClientPacket>()
                .SetMessageHandler<SyncClientPacket>((player, packet) => { /* do nothing. idk why this handler is even needed, but it is */ });

        }


        private void OnPlayerJoin(IServerPlayer player)
        {
            // send the connecting player the settings it needs to be synced
            this.serverChannel.SendPacket(new SyncClientPacket
            {
                AltarDropsFish = ModConfig.Loaded.AltarDropsFish,
                AltarDropsGold = ModConfig.Loaded.AltarDropsGold,
                AltarDropsVegetables = ModConfig.Loaded.AltarDropsVegetables,
                DeadfallBaitStolenPercent = ModConfig.Loaded.DeadfallBaitStolenPercent,
                DeadfallMaxAnimalHeight = ModConfig.Loaded.DeadfallMaxAnimalHeight,
                DeadfallMaxDamageSet = ModConfig.Loaded.DeadfallMaxDamageSet,
                DeadfallMaxDamageBaited = ModConfig.Loaded.DeadfallMaxDamageBaited,
                DeadfallTrippedPercent = ModConfig.Loaded.DeadfallTrippedPercent,
                FallDamageMultiplierWoodSpikes = ModConfig.Loaded.FallDamageMultiplierWoodSpikes,
                FallDamageMultiplierMetalSpikes = ModConfig.Loaded.FallDamageMultiplierMetalSpikes,
                FishBasketCatchPercent = ModConfig.Loaded.FishBasketCatchPercent,
                FishBasketBaitedCatchPercent = ModConfig.Loaded.FishBasketBaitedCatchPercent,
                FishBasketBaitStolenPercent = ModConfig.Loaded.FishBasketBaitStolenPercent,
                FishBasketEscapePercent = ModConfig.Loaded.FishBasketEscapePercent,
                FishBasketUpdateMinutes = ModConfig.Loaded.FishBasketUpdateMinutes,

                FishBasketRotRemovedPercent = ModConfig.Loaded.FishBasketRotRemovedPercent,
                FishChanceOfEggsPercent = ModConfig.Loaded.FishChanceOfEggsPercent,
                FishChunkDepletionRate = ModConfig.Loaded.FishChunkDepletionRate,
                FishChunkRepletionRate = ModConfig.Loaded.FishChunkRepletionRate,
                FishChunkRepletionMinutes = ModConfig.Loaded.FishChunkRepletionMinutes,
                FishEggsChunkRepletionRate = ModConfig.Loaded.FishEggsChunkRepletionRate,
                FishChunkMaxDepletionPercent = ModConfig.Loaded.FishChunkMaxDepletionPercent,
                FurrowedLandUpdateFrequency = ModConfig.Loaded.FurrowedLandUpdateFrequency,
                FurrowedLandBlockageChancePercent = ModConfig.Loaded.FurrowedLandBlockageChancePercent,
                FurrowedLandMinMoistureClose = ModConfig.Loaded.FurrowedLandMinMoistureClose,
                FurrowedLandMinMoistureFar = ModConfig.Loaded.FurrowedLandMinMoistureFar,
                LimbTrotlineCatchPercent = ModConfig.Loaded.LimbTrotlineCatchPercent,
                LimbTrotlineBaitedCatchPercent = ModConfig.Loaded.LimbTrotlineBaitedCatchPercent,
                LimbTrotlineLuredCatchPercent = ModConfig.Loaded.LimbTrotlineLuredCatchPercent,
                LimbTrotlineBaitedLuredCatchPercent = ModConfig.Loaded.LimbTrotlineBaitedLuredCatchPercent,
                LimbTrotlineBaitStolenPercent = ModConfig.Loaded.LimbTrotlineBaitStolenPercent,
                LimbTrotlineUpdateMinutes = ModConfig.Loaded.LimbTrotlineUpdateMinutes,
                LimbTrotlineRotRemovedPercent = ModConfig.Loaded.LimbTrotlineRotRemovedPercent,
                MonkeyBridgeMaxLength = ModConfig.Loaded.MonkeyBridgeMaxLength,

                ParticulatorMaxParticlesQuantity = ModConfig.Loaded.ParticulatorMaxParticlesQuantity,
                ParticulatorMaxParticlesSize = ModConfig.Loaded.ParticulatorMaxParticlesSize,
                ParticulatorHideCodeTabs = ModConfig.Loaded.ParticulatorHideCodeTabs,
                PipeUpdateFrequency = ModConfig.Loaded.PipeUpdateFrequency,
                PipeBlockageChancePercent = ModConfig.Loaded.PipeBlockageChancePercent,
                PipeMinMoisture = ModConfig.Loaded.PipeMinMoisture,
                RaftWaterSpeedModifier = ModConfig.Loaded.RaftWaterSpeedModifier,
                RaftFlotationModifier = ModConfig.Loaded.RaftFlotationModifier,
                SnareBaitStolenPercent = ModConfig.Loaded.SnareBaitStolenPercent,
                SnareMaxAnimalHeight = ModConfig.Loaded.SnareMaxAnimalHeight,
                SnareMaxDamageSet = ModConfig.Loaded.SnareMaxDamageSet,
                SnareMaxDamageBaited = ModConfig.Loaded.SnareMaxDamageBaited,
                SnareTrippedPercent = ModConfig.Loaded.SnareTrippedPercent,

                SpawnMultiplierBioluminescentGlobe = ModConfig.Loaded.SpawnMultiplierBioluminescentGlobe,
                SpawnMultiplierBioluminescentJelly = ModConfig.Loaded.SpawnMultiplierBioluminescentJelly,
                SpawnMultiplierBioluminescentOrangeJelly = ModConfig.Loaded.SpawnMultiplierBioluminescentOrangeJelly,
                SpawnMultiplierBioluminescentWorm = ModConfig.Loaded.SpawnMultiplierBioluminescentWorm,
                SpawnMultiplierCrabBairdi = ModConfig.Loaded.SpawnMultiplierCrabBairdi,
                SpawnMultiplierCrabLand = ModConfig.Loaded.SpawnMultiplierCrabLand,
                SpawnMultiplierLivingDead = ModConfig.Loaded.SpawnMultiplierLivingDead,
                SpawnMultiplierSnakeBlackRat = ModConfig.Loaded.SpawnMultiplierSnakeBlackRat,
                SpawnMultiplierSnakeChainViper = ModConfig.Loaded.SpawnMultiplierSnakeChainViper,
                SpawnMultiplierSnakeCoachWhip = ModConfig.Loaded.SpawnMultiplierSnakeCoachWhip,
                SpawnMultiplierSnakePitViper = ModConfig.Loaded.SpawnMultiplierSnakePitViper,
                SpawnMultiplierWillowispGreen = ModConfig.Loaded.SpawnMultiplierWillowispGreen,
                SpawnMultiplierWillowispWhite = ModConfig.Loaded.SpawnMultiplierWillowispWhite,
                SpawnMultiplierWillowispYellow = ModConfig.Loaded.SpawnMultiplierWillowispYellow,

                TreeHollowsMaxItems = ModConfig.Loaded.TreeHollowsMaxItems,
                TreeHollowsEnableDeveloperTools = ModConfig.Loaded.TreeHollowsEnableDeveloperTools,
                TreeHollowsMaxPerChunk = ModConfig.Loaded.TreeHollowsMaxPerChunk,
                TreeHollowsSpawnProbability = ModConfig.Loaded.TreeHollowsSpawnProbability,
                TreeHollowsUpdateMinutes = ModConfig.Loaded.TreeHollowsUpdateMinutes,
                WeirTrapCatchPercent = ModConfig.Loaded.WeirTrapCatchPercent,
                WeirTrapEscapePercent = ModConfig.Loaded.WeirTrapEscapePercent,
                WeirTrapUpdateMinutes = ModConfig.Loaded.WeirTrapUpdateMinutes,
                WeirTrapRotRemovedPercent = ModConfig.Loaded.WeirTrapRotRemovedPercent,
                WormFoundPercentRock = ModConfig.Loaded.WormFoundPercentRock,
                WormFoundPercentStickFlint = ModConfig.Loaded.WormFoundPercentStickFlint
            }, player);
        }

        
        public override void AssetsFinalize(ICoreAPI api)
        {
            // 3.9 
            base.AssetsFinalize(api);

            this.UpdateSpawnRates(api);
            /*
            if (api.Side != EnumAppSide.Client)
            { return; }
            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj.Code.Path.StartsWith("hoe-"))
                {
                    obj.CollectibleBehaviors = obj.CollectibleBehaviors.Append(new BehaviorFurrow(obj));
                }
            }
            */
        }


        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.World.Logger.Event("started 'Primitive Survival' mod");
            this.RegisterClasses(api);

            //Achievements.AchievementsManager.RegisterAchievement("primitivesurvival", "psthegreathunter", "game:item-stick");
        }


        private void OnSaveGameLoading()
        {
            fishingChunks = new Dictionary<IServerChunk, int>();
            // attempt to load the (short) list of all active fishing chunks

            var data = sapi.WorldManager.SaveGame.GetData("chunklist");
            
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
                        dsc.AppendLine("\n<font color=\"#D8EAA3\"><i>" + Lang.GetMatching("game:tabname-primitive") + "</i></font>");
                    }
                }
            }
        }
    }
}






