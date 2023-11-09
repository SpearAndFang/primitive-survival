namespace PrimitiveSurvival.ModSystem
{
    using System.Linq;
    using Vintagestory.API.Common;
    using Vintagestory.API.Client;
    using Vintagestory.API.Datastructures;

    public class BEFireflies : BlockEntity
    {
        private readonly int flowersRequired = 10;
        private readonly int tempRequired = 25; //degrees C
        private readonly int hourRequired = 3; // midnight to 3am 
        private readonly int listenerModifier = 30000; // 30000
        private readonly int firefliesCatchPercent = 10;
        private readonly string[] fireflyTypes = { "treetop", "mysticlantern", "blueghost", "rover", "fairyring", "candle", "marshimp" };

        // Stored values
        private int scanIteration;
        private int quantityNearbyFlowers;
        private int scanQuantityNearbyFlowers;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsServer())
            {
                this.RegisterGameTickListener(this.OnScanSurroundingArea, api.World.Rand.Next(5000) + this.listenerModifier);
            }
        }


        private void OnScanSurroundingArea(float dt)
        {
            if (this.Api.Side == EnumAppSide.Client)
            { return; }
            if (this.scanIteration == 0)
            { this.scanQuantityNearbyFlowers = 0; }

            // Let's count/collect amount of flowers in a 20x20x20 cube
            var minX = -8 + (8 * (this.scanIteration / 2));
            var minZ = -8 + (8 * (this.scanIteration % 2));
            var size = 8;


            //Changed for 1.17
            this.Api.World.BlockAccessor.WalkBlocks(this.Pos.AddCopy(minX, -5, minZ), this.Pos.AddCopy(minX + size - 1, 5, minZ + size - 1), (block, x, y, z) =>
            //this.Api.World.BlockAccessor.WalkBlocks(this.Pos.AddCopy(minX, -5, minZ), this.Pos.AddCopy(minX + size - 1, 5, minZ + size - 1), (block, pos) =>
            {
                if (block.Id == 0)
                { return; }
                if (block.Attributes?.IsTrue("beeFeed") == true)
                { this.scanQuantityNearbyFlowers++; }
            });

            this.scanIteration++;
            if (this.scanIteration == 4)
            {
                this.scanIteration = 0;
                this.OnScanComplete();
            }
        }

        private void OnScanComplete()
        {
            this.quantityNearbyFlowers = this.scanQuantityNearbyFlowers;
            this.MarkDirty();

            if (this.quantityNearbyFlowers >= this.flowersRequired)
            {
                //check for nighttime and temperature
                var conds = this.Api.World.BlockAccessor.GetClimateAt(this.Pos, EnumGetClimateMode.NowValues); //small aside - get the temperature and kill the worm if necessary
                var hourOfDay = this.Api.World.Calendar.HourOfDay;

                //Debug.WriteLine("flowers:" + this.quantityNearbyFlowers);
                //Debug.WriteLine("temp:" + conds.Temperature);
                //Debug.WriteLine("hour:" + hourOfDay);

                if ((conds.Temperature >= this.tempRequired) && (hourOfDay <= this.hourRequired) && (this.Api.World.Rand.Next(100) < this.firefliesCatchPercent))
                {
                    var fftype = this.Api.World.Rand.Next(this.fireflyTypes.Count() - 1);
                    var thisBlockPath = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default).Code.Path;
                    var location = "primitivesurvival:fireflies-" + this.fireflyTypes[fftype];
                    //Debug.WriteLine(location);
                    var type = this.Api.World.GetEntityType(new AssetLocation(location));
                    if (type == null)
                    {
                        this.Api.World.Logger.Error("ItemCreature: No such entity - {0}", location);
                        if (this.Api.World.Side == EnumAppSide.Client)
                        { (this.Api as ICoreClientAPI).TriggerIngameError(this, "nosuchentity", "No such entity '{0}' loaded."); }
                        return;
                    }

                    var entity = this.Api.World.ClassRegistry.CreateEntity(type);
                    if (entity != null)
                    {
                        entity.ServerPos.X = this.Pos.X + 0.5f;
                        entity.ServerPos.Y = this.Pos.Y;
                        entity.ServerPos.Z = this.Pos.Z + 0.5f;
                        if (thisBlockPath.Contains("angled"))
                        { entity.ServerPos.Yaw = 0.78f; }
                        entity.Pos.SetFrom(entity.ServerPos);
                        entity.PositionBeforeFalling.Set(entity.ServerPos.X, entity.ServerPos.Y, entity.ServerPos.Z);
                        entity.Attributes.SetString("origin", "playerplaced");
                        this.Api.World.BlockAccessor.SetBlock(0, this.Pos);
                        this.Api.World.SpawnEntity(entity);
                    }
                    this.MarkDirty(false);
                }
            }
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("scanIteration", this.scanIteration);
            tree.SetInt("quantityNearbyFlowers", this.quantityNearbyFlowers);
            tree.SetInt("scanQuantityNearbyFlowers", this.scanQuantityNearbyFlowers);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.scanIteration = tree.GetInt("scanIteration");
            this.quantityNearbyFlowers = tree.GetInt("quantityNearbyFlowers");
            this.scanQuantityNearbyFlowers = tree.GetInt("scanQuantityNearbyFlowers");
            this.MarkDirty(true);
        }
    }
}
