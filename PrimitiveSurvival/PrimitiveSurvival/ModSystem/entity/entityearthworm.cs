namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    using Vintagestory.API.Datastructures;

    public class EntityEarthworm : EntityAgent
    {

        private int cnt = 0;
        private static readonly Random Rnd = new Random();


        public EntityEarthworm()
        {
        }



        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
        }



        public override void OnInteract(EntityAgent byEntity, ItemSlot slot, Vec3d hitPosition, EnumInteractMode mode)
        {
            if (!this.Alive || this.World.Side == EnumAppSide.Client || mode == 0)
            {
                base.OnInteract(byEntity, slot, hitPosition, mode);
                return;
            }

            var stack = new ItemStack(byEntity.World.GetItem(new AssetLocation("primitivesurvival:earthworm")));
            if (!byEntity.TryGiveItemStack(stack))
            { byEntity.World.SpawnItemEntity(stack, this.ServerPos.XYZ); }
            this.Die(); //remove from the ground
            return;
        }


        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (this.cnt++ > 200)
            {
                this.cnt = 0;
                var belowPos = this.Pos.XYZ.AsBlockPos;
                var blockBelow = this.World.BlockAccessor.GetBlock(belowPos, BlockLayersAccess.Default);

                var conds = this.World.BlockAccessor.GetClimateAt(belowPos, EnumGetClimateMode.NowValues); //small aside - get the temperature and kill the worm if necessary
                var escaped = Rnd.Next(200); //one in two hundred chance the worm leaves
                if (conds.Temperature <= 0 || conds.Temperature >= 35 || escaped < 1)
                { this.Die(); } //too cold or hot or the worm just left
                else
                {
                    if (blockBelow.FirstCodePart() == "farmland")
                    {
                        //Debug.WriteLine("firstblock:" + blockBelow.FirstCodePart());
                        if (this.World.BlockAccessor.GetBlockEntity(belowPos) is BlockEntityFarmland befarmland)
                        {
                            befarmland.WaterFarmland(0.3f); //aerate 
                            var tree = new TreeAttribute();
                            befarmland.ToTreeAttributes(tree);

                            var slowN = tree.GetFloat("slowN");
                            var slowK = tree.GetFloat("slowK");
                            var slowP = tree.GetFloat("slowP");
                            if (slowN <= 150)
                            { slowN += 1; }//props.N;
                            if (slowK <= 150)
                            { slowK += 1; } //props.K;
                            if (slowP <= 150)
                            { slowP += 1; } //props.P;

                            if (slowN < 150 && slowK < 150 && slowP < 150)
                            {
                                tree.SetFloat("slowN", slowN);
                                tree.SetFloat("slowK", slowK);
                                tree.SetFloat("slowP", slowP);
                                befarmland.FromTreeAttributes(tree, this.World);
                                befarmland.MarkDirty();
                                this.World.BlockAccessor.MarkBlockEntityDirty(belowPos);
                            }
                            else
                            {
                                this.World.BlockAccessor.BreakBlock(belowPos, null); //For better or worse, you've created a block of Worm Castings
                                var block = this.World.BlockAccessor.GetBlock(new AssetLocation("primitivesurvival:earthwormcastings"));
                                this.World.BlockAccessor.SetBlock(block.BlockId, belowPos);
                            }
                        }
                    }
                }
            }
        }
    }
}
