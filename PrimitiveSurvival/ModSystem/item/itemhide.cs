namespace PrimitiveSurvival.ModSystem
{
    using System;
    using System.Linq;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.GameContent;

    //using System.Diagnostics;

    public class ItemPelt : Item
    {

        protected static readonly Random Rnd = new Random();

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Controls.Sneak) //sneak place gives you vanilla pelt
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }
            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        { 
            if (blockSel == null || byEntity == null)
            { return; }
            if (byEntity.Controls.Sneak) //sneak place gives you vanilla pelt
            {
                base.OnHeldInteractStop(secondsUsed,slot,byEntity, blockSel, entitySel);    
                return;
            }
            var world = byEntity.World;
            if (world.Side == EnumAppSide.Client)
            { return; }
            if (world == null)
            { return; }

            var tempblock = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);

            if (tempblock?.Class == "blockhide")
            { return; }

            if (secondsUsed > 0)
            {

                var itemType = slot.Itemstack.Item.FirstCodePart(1);
                if (itemType != "pelt")
                { return; } //pelts only!

                var itemSize = slot.Itemstack.Item.FirstCodePart(2);
                var outPath = "primitivesurvival:";
                string outHide;

                if (itemSize == "huge")
                {
                    string[] hideTypes = { "bearhide-", "bearhide-", "bearhide-", "moosehide-all-" };
                    outHide = hideTypes[Rnd.Next(hideTypes.Count())];
                    if (outHide == "bearhide-")
                    {
                        string[] mammalTypes = { "black1", "brown1", "brown2", "brown3", "panda1", "polar1", "sun1" };
                        outHide += mammalTypes[Rnd.Next(mammalTypes.Count())] + "-";
                    }
                }
                else if (itemSize == "large")
                { outHide = "sheephide-bighorn-"; }
                else if (itemSize == "medium")
                {
                    string[] hideTypes = { "wolfhide-", "hyenahide-hyena-", "pighide-wild-" };
                    outHide = hideTypes[Rnd.Next(hideTypes.Count())];
                    if (outHide == "wolfhide-")
                    {
                        string[] mammalTypes = { "grey-", "steppe-", "tundra-" };
                        outHide += mammalTypes[Rnd.Next(mammalTypes.Count())];
                    }
                }
                else //small
                {
                    string[] hideTypes = { "foxhide-", "harehide-", "raccoonhide-raccoon-" };
                    outHide = hideTypes[Rnd.Next(hideTypes.Count())];
                    if (outHide == "foxhide-")
                    {
                        string[] mammalTypes = { "red-", "arctic-" };
                        outHide += mammalTypes[Rnd.Next(mammalTypes.Count())];
                    }
                    else if (outHide == "harehide-")
                    {
                        string[] mammalTypes = { "arctic-", "ashgrey-", "darkbrown-", "darkgrey-", "desert-", "gold-", "lightbrown-", "lightgrey-", "silver-", "smokegrey-" };
                        outHide += mammalTypes[Rnd.Next(mammalTypes.Count())];
                    }
                }

                string[] genderTypes = { "male-", "female-" };
                var outGender = genderTypes[Rnd.Next(genderTypes.Count())];
                outPath += outHide + outGender + "north";

                string facing;
                var targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                var dx = byEntity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                var dz = byEntity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                var angle = Math.Atan2(dx, dz);
                angle += Math.PI;
                angle /= Math.PI / 4;
                var halfQuarter = Convert.ToInt32(angle);
                halfQuarter %= 8;

                if (halfQuarter == 4)
                { facing = "south"; }
                else if (halfQuarter == 6)
                { facing = "east"; }
                else if (halfQuarter == 2)
                { facing = "west"; }
                else if (halfQuarter == 7)
                { facing = "northeast"; }
                else if (halfQuarter == 1)
                { facing = "northwest"; }
                else if (halfQuarter == 5)
                { facing = "southeast"; }
                else if (halfQuarter == 3)
                { facing = "southwest"; }
                else
                { facing = "north"; }

                outPath = outPath.Replace("north", facing);
                var block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default);
                var face = blockSel.Face.ToString();
                if (face == "down")
                { return; }

                if (face == "up" || block.Code.Path.Contains("tallgrass-"))
                {
                    var blockSelAbove = blockSel.Clone();
                    blockSelAbove.Position.Y += 1;
                    var blockAbove = world.BlockAccessor.GetBlock(blockSelAbove.Position, BlockLayersAccess.Default);
                    //Debug.WriteLine(secondsUsed);
                    //Debug.WriteLine(blockAbove.Code.Path);
                    if (blockAbove.BlockId == 0 || blockAbove.Code.Path.Contains("tallgrass-") || block.Code.Path.Contains("tallgrass-"))
                    {
                        var blockNew = world.GetBlock(new AssetLocation(outPath));
                        var blockAccessor = world.BlockAccessor;
                        if (block.Code.Path.Contains("tallgrass-"))
                        {
                            blockAccessor.SetBlock(blockNew.BlockId, blockSel.Position);
                        }
                        else
                        {
                            blockAccessor.SetBlock(blockNew.BlockId, blockSelAbove.Position);
                        }
                        world.PlaySoundAt(blockNew.Sounds.Place, blockSel.Position.X + 0.5f, blockSel.Position.Y+0.5f, blockSel.Position.Z+0.5f, byEntity as IPlayer);
                        slot.TakeOut(1);
                        slot.MarkDirty();
                    }
                }
                else //wall
                {
                    outPath = outPath.Replace("hide", "head");
                    outPath = outPath.Replace(facing, face);
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
                    if (blockBeside.BlockId == 0 || blockBeside.Code.Path.Contains("tallgrass-") || block.Code.Path.Contains("tallgrass-"))
                    {
                        var blockNew = world.GetBlock(new AssetLocation(outPath));
                        if (blockNew != null)
                        {
                            var blockAccessor = world.BlockAccessor;
                            if (block.Code.Path.Contains("tallgrass-"))
                            { blockAccessor.SetBlock(blockNew.BlockId, blockSel.Position); }
                            else
                            { blockAccessor.SetBlock(blockNew.BlockId, blockSelBeside.Position); }
                            world.PlaySoundAt(blockNew.Sounds.Place, blockSel.Position.X + 0.5f, blockSel.Position.Y + 0.5f, blockSel.Position.Z + 0.5f, byEntity as IPlayer);
                            slot.TakeOut(1);
                            slot.MarkDirty();
                        }
                    }
                }
            }
        }
    }
}
