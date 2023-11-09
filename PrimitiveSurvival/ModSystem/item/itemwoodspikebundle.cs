namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API.Common;

    public class ItemWoodSpikeBundle : Item
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null || byEntity == null)
            { return; }
            var world = byEntity.World;
            if (world == null)
            { return; }
            var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);

            var face = blockSel.Face.ToString();
            if (face == "down")
            { return; }
            if (face == "up")
            {
                if (block.Fertility <= 0 && !block.Code.Path.Contains("tallgrass-"))
                { return; }
                var blockSelAbove = blockSel.Clone();
                blockSelAbove.Position.Y += 1;
                var blockAbove = world.BlockAccessor.GetBlock(blockSelAbove.Position, BlockLayersAccess.Default);
                if (blockAbove.BlockId == 0 || blockAbove.Code.Path.Contains("tallgrass-") || block.Code.Path.Contains("tallgrass-"))
                {
                    var blockNew = world.GetBlock(new AssetLocation("primitivesurvival:woodspikes"));
                    var blockAccessor = world.BlockAccessor;
                    if (block.Code.Path.Contains("tallgrass-"))
                    { blockAccessor.SetBlock(blockNew.BlockId, blockSel.Position); }
                    else
                    { blockAccessor.SetBlock(blockNew.BlockId, blockSelAbove.Position); }
                    slot.TakeOut(1);
                    slot.MarkDirty();
                }
            }
            else //nsew
            {
                var blockSelBeside = blockSel.Clone();
                if (face == "east")
                { blockSelBeside.Position.X += 1; }
                else if (face == "west")
                { blockSelBeside.Position.X -= 1; }
                else if (face == "north")
                { blockSelBeside.Position.Z -= 1; }
                else
                { blockSelBeside.Position.Z += 1; }
                var blockBeside = world.BlockAccessor.GetBlock(blockSelBeside.Position, BlockLayersAccess.Default);

                if (blockBeside.BlockId == 0 || block.FirstCodePart() == "woodsupportspikes")
                {
                    var placeOk = false;
                    if (blockBeside.BlockId == 0 && block.Fertility > 0)
                    { placeOk = true; }
                    else
                    {
                        var selFace = block.LastCodePart();
                        if ((face == "east" || face == "west") && (selFace == "north" || selFace == "south"))
                        { placeOk = true; }
                        else if ((face == "north" || face == "south") && (selFace == "east" || selFace == "west"))
                        { placeOk = true; }
                    }
                    if (placeOk)
                    {
                        var blockNew = world.GetBlock(new AssetLocation("primitivesurvival:woodsupportspikes-" + face));
                        var blockAccessor = world.BlockAccessor;
                        blockAccessor.SetBlock(blockNew.BlockId, blockSelBeside.Position);
                        slot.TakeOut(1);
                        slot.MarkDirty();
                    }
                }
            }
        }
    }
}
