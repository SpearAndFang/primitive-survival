namespace PrimitiveSurvival.ModConfig
{
    public class ModConfig
    {
        public static ModConfig Loaded { get; set; } = new ModConfig();
        public int DeadfallBaitStolenPercent { get; set; } = 10;
        public float DeadfallMaxAnimalHeight { get; set; } = 0.7f;
        public int DeadfallMaxDamageSet { get; set; } = 10;
        public int DeadfallMaxDamageBaited { get; set; } = 20;
        public int DeadfallTrippedPercent { get; set; } = 10;
        public int FallDamageMultiplierWoodSpikes { get; set; } = 25;
        public int FallDamageMultiplierMetalSpikes { get; set; } = 80;
        public int FishBasketCatchPercent { get; set; } = 5;
        public int FishBasketBaitedCatchPercent { get; set; } = 10;
        public int FishBasketBaitStolenPercent { get; set; } = 5;
        public int FishBasketEscapePercent { get; set; } = 15;
        public double FishBasketUpdateMinutes { get; set; } = 2.2;
        public int FishBasketRotRemovedPercent { get; set; } = 10;
        public int FishChanceOfEggsPercent { get; set; } = 20;
        public int FishChunkDepletionRate { get; set; } = 10;
        public int FishChunkRepletionRate { get; set; } = 1;
        public int FishChunkRepletionMinutes { get; set; } = 20;
        public int FishEggsChunkRepletionRate { get; set; } = 10;
        public int FishChunkMaxDepletionPercent { get; set; } = 95;
        public int FurrowedLandUpdateFrequency { get; set; } = 120;
        public double FurrowedLandBlockageChancePercent { get; set; } = 0.05;
        public bool FurrowedLandEnabled { get; set; } = true;
        public double FurrowedLandMinMoistureClose { get; set; } = 0.85;
        public double FurrowedLandMinMoistureFar { get; set; } = 0.6;

        public int LimbTrotlineCatchPercent { get; set; } = 1;
        public int LimbTrotlineBaitedCatchPercent { get; set; } = 3;
        public int LimbTrotlineLuredCatchPercent { get; set; } = 3;
        public int LimbTrotlineBaitedLuredCatchPercent { get; set; } = 6;
        public int LimbTrotlineBaitStolenPercent { get; set; } = 5;
        public double LimbTrotlineUpdateMinutes { get; set; } = 2.4;
        public int LimbTrotlineRotRemovedPercent { get; set; } = 10;
        public int MonkeyBridgeMaxLength { get; set; } = 120;
        public bool RaftEnabled { get; set; } = true;
        public float RaftWaterSpeedModifier { get; set; } = 0.1f;
        public float RaftFlotationModifier { get; set; } = 0.025f;
        public bool ShowModNameInHud { get; set; } = true;
        public bool ShowModNameInGuis { get; set; } = false;
        public int SnareBaitStolenPercent { get; set; } = 10;
        public float SnareMaxAnimalHeight { get; set; } = 0.8f;
        public int SnareMaxDamageSet { get; set; } = 12;
        public int SnareMaxDamageBaited { get; set; } = 24;
        public int SnareTrippedPercent { get; set; } = 10;
        public double SpawnMultiplierBioluminescentGlobe { get; set; } = 1.0;
        public double SpawnMultiplierBioluminescentJelly { get; set; } = 1.0;
        public double SpawnMultiplierBioluminescentOrangeJelly { get; set; } = 1.0;
        public double SpawnMultiplierBioluminescentWorm { get; set; } = 1.0;
        public double SpawnMultiplierCrabBairdi { get; set; } = 1.0;
        public double SpawnMultiplierCrabLand { get; set; } = 1.0;
        public double SpawnMultiplierLivingDead { get; set; } = 1.0;
        public double SpawnMultiplierSnakeBlackRat { get; set; } = 1.0;
        public double SpawnMultiplierSnakeChainViper { get; set; } = 1.0;
        public double SpawnMultiplierSnakeCoachWhip { get; set; } = 1.0;
        public double SpawnMultiplierSnakePitViper { get; set; } = 1.0;
        public double SpawnMultiplierWillowispGreen { get; set; } = 1.0;
        public double SpawnMultiplierWillowispWhite { get; set; } = 1.0;
        public double SpawnMultiplierWillowispYellow { get; set; } = 1.0;
        public int TreeHollowsMaxItems { get; set; } = 8;
        public bool TreeHollowsEnableDeveloperTools { get; set; } = false;
        public int TreeHollowsMaxPerChunk { get; set; } = 1;
        public float TreeHollowsSpawnProbability { get; set; } = 0.15f;
        public double TreeHollowsUpdateMinutes { get; set; } = 360.0;
        public int WeirTrapCatchPercent { get; set; } = 2;
        public int WeirTrapEscapePercent { get; set; } = 15;
        public double WeirTrapUpdateMinutes { get; set; } = 5.0;
        public int WeirTrapRotRemovedPercent { get; set; } = 10;
        public int WormFoundPercentRock { get; set; } = 1;
        public int WormFoundPercentStickFlint { get; set; } = 10;
    }
}
