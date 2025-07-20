using ProtoBuf;

namespace PrimitiveSurvival.ModSystem
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SyncClientPacket
    {
        public bool AltarDropsFish;
        public bool AltarDropsGold;
        public bool AltarDropsVegetables;
        public int DeadfallBaitStolenPercent;
        public float DeadfallMaxAnimalHeight;
        public int DeadfallMaxDamageSet;
        public int DeadfallMaxDamageBaited;
        public int DeadfallTrippedPercent;
        public int FallDamageMultiplierWoodSpikes;
        public int FallDamageMultiplierMetalSpikes;
        public int FishBasketCatchPercent;
        public int FishBasketBaitedCatchPercent;
        public int FishBasketBaitStolenPercent;
        public int FishBasketEscapePercent;
        public double FishBasketUpdateMinutes;
        public int FishBasketRotRemovedPercent;
        public int FishChanceOfEggsPercent;
        public int FishChunkDepletionRate;
        public int FishChunkRepletionRate;
        public int FishChunkRepletionMinutes;
        public int FishEggsChunkRepletionRate;
        public int FishChunkMaxDepletionPercent;
        public int FurrowedLandUpdateFrequency;
        public double FurrowedLandBlockageChancePercent;
        public double FurrowedLandMinMoistureClose;
        public double FurrowedLandMinMoistureFar;
        public int LimbTrotlineCatchPercent;
        public int LimbTrotlineBaitedCatchPercent;
        public int LimbTrotlineLuredCatchPercent;
        public int LimbTrotlineBaitedLuredCatchPercent;
        public int LimbTrotlineBaitStolenPercent;
        public double LimbTrotlineUpdateMinutes;
        public int LimbTrotlineRotRemovedPercent;
        public int MonkeyBridgeMaxLength;
        public int ParticulatorMaxParticlesQuantity;
        public int ParticulatorMaxParticlesSize;
        public bool ParticulatorHideCodeTabs;
        public int PipeUpdateFrequency;
        public double PipeBlockageChancePercent;
        public double PipeMinMoisture;
        public float RaftWaterSpeedModifier;
        public float RaftFlotationModifier;
        public int SnareBaitStolenPercent;
        public float SnareMaxAnimalHeight;
        public int SnareMaxDamageSet;
        public int SnareMaxDamageBaited;
        public int SnareTrippedPercent;
        public double SpawnMultiplierBioluminescentGlobe;
        public double SpawnMultiplierBioluminescentJelly;
        public double SpawnMultiplierBioluminescentOrangeJelly;
        public double SpawnMultiplierBioluminescentWorm;
        public double SpawnMultiplierCrabBairdi;
        public double SpawnMultiplierCrabLand;
        public double SpawnMultiplierLivingDead;
        public double SpawnMultiplierSnakeBlackRat;
        public double SpawnMultiplierSnakeChainViper;
        public double SpawnMultiplierSnakeCoachWhip;
        public double SpawnMultiplierSnakePitViper;
        public double SpawnMultiplierWillowispGreen;
        public double SpawnMultiplierWillowispWhite;
        public double SpawnMultiplierWillowispYellow;
        public int TreeHollowsMaxItems;
        public bool TreeHollowsEnableDeveloperTools;
        public int TreeHollowsMaxPerChunk;
        public float TreeHollowsSpawnProbability;
        public double TreeHollowsUpdateMinutes;
        public int WeirTrapCatchPercent;
        public int WeirTrapEscapePercent;
        public double WeirTrapUpdateMinutes;
        public int WeirTrapRotRemovedPercent;
        public int WormFoundPercentRock;
        public int WormFoundPercentStickFlint;
    }
}

