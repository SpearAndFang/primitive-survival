namespace PrimitiveSurvival.ModSystem
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent;
    //using System.Diagnostics;


    public class BlockMetalBucket : BlockLiquidContainerTopOpened
    {

        public override float CapacityLitres => 10;
        protected WorldInteraction[] newinteractions;
        public ItemStack[] lcdstacks;
        protected override string meshRefsCacheKey => Code.ToShortString() + "meshRefs";
        public override bool AllowHeldLiquidTransfer => true; 


        public override void OnLoaded(ICoreAPI api)
        { base.OnLoaded(api); }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity.Controls.Sneak)
            { return; }
            var bucketPath = slot.Itemstack.Block.Code.Path;
            var pos = blockSel.Position;
            var block = byEntity.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default);

            var contentStack = this.GetContent(slot.Itemstack);

            if (block.Code.Path.Contains("lava-") && bucketPath.Contains("-empty") && contentStack == null) //lava block and empty bucket?
            {
                if (block.Code.Path.Contains("-7")) //lots of lava?
                {
                    if (this.api.World.Side == EnumAppSide.Server)
                    {

                        IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
                        IBlockAccessor blockAcc = byEntity.World.BlockAccessor;

                        if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMetalBucket bect)
                        { }
                        else
                        {
                            //swap empty bucket with "filled" one
                            var newblock = byEntity.World.GetBlock(new AssetLocation("primitivesurvival:" + bucketPath.Replace("-empty", "-filled")));
                            var newStack = new ItemStack(newblock);
                            slot.TakeOut(1);
                            slot.MarkDirty();
                            
                            //HAHA WTF don't comment out
                            //if (!byEntity.TryGiveItemStack(newStack));
                            byEntity.TryGiveItemStack(newStack);

                            //remove lava from in world
                            newblock = byEntity.World.GetBlock(new AssetLocation("lava-still-3"));
                            this.api.World.BlockAccessor.SetBlock(newblock.BlockId, pos); //replace lava with less lava
                            newblock.OnNeighbourBlockChange(byEntity.World, pos, pos.NorthCopy());
                            this.api.World.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
                            this.api.World.BlockAccessor.MarkBlockDirty(pos); //let the server know the lava's gone
                        }
                        
                        
                    }
                    handHandling = EnumHandHandling.PreventDefault;
                    if ((byEntity as EntityPlayer) != null)  //bucket scooping lava animation
                    {
                        if (((byEntity as EntityPlayer).Player as IClientPlayer) != null)
                        { ((byEntity as EntityPlayer).Player as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract); }
                    }
                    return;
                }
            }
            else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
            }
        }


        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            var val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (val)
            {
                if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEMetalBucket bect)
                {
                    var targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    var dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    var dz = byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    var angleHor = (float)Math.Atan2(dx, dz);
                    var deg22dot5rad = GameMath.PIHALF / 4;
                    var roundRad = ((int)Math.Round(angleHor / deg22dot5rad)) * deg22dot5rad;
                    bect.MeshAngle = roundRad;
                }
            }
            return val;
        }
    }
}
