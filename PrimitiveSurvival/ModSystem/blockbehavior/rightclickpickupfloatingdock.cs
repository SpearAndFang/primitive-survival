namespace PrimitiveSurvival.ModSystem
{
    using Vintagestory.API;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.API.Util;
    
    #nullable disable
    
    public class RightClickPickupFloatingDock : BlockBehavior
    {
        public float delayTime;

        [DocumentAsJson("Optional", "None")]
        SoundAttributes pickupSound;
        public RightClickPickupFloatingDock(Block block) : base(block)
        {
        }


        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            delayTime = 1f;
            //var strloc = this.block.Attributes?["placeSound"].AsString();
            //this.pickupSound = strloc == null ? null : AssetLocation.Create(strloc, this.block.Code.Domain);
            JsonObject jsonSound = properties["sound"];
            if (!jsonSound.Exists) jsonSound = block.Attributes?["placeSound"];
            pickupSound = jsonSound?.AsObject<SoundAttributes?>(null, block.Code.Domain, true) ?? new SoundAttributes();

        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            var stacks = new ItemStack[] { this.block.OnPickBlock(world, blockSel.Position) };
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            { return false; }

            if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack != null)
            {
                return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
            }
            
            handling = EnumHandling.PreventDefault;
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            return secondsUsed < delayTime;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (secondsUsed < delayTime - 0.7f)
            { return; }

            if (!byPlayer.Entity.Controls.Sneak)// && byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
            {
                if (world.Side == EnumAppSide.Server)
                {
                    var curType = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Default).FirstCodePart(1);
                    var tempBlock = world.GetBlock(new AssetLocation("primitivesurvival:floatingdock-" + curType + "-ns"));
                    if (tempBlock == null)
                    { return; }
                    var newStack = new ItemStack(tempBlock);

                    if (byPlayer.InventoryManager.TryGiveItemstack(newStack, true))
                    {
                        world.BlockAccessor.SetBlock(0, blockSel.Position);
                        world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);

                        world.PlaySoundAt(pickupSound.Location != null ? pickupSound : block.GetSounds(world.BlockAccessor, blockSel).Place, byPlayer, null);
                        //world.PlaySoundAt(this.pickupSound ?? this.block.GetSounds(world.BlockAccessor, blockSel.Position).Place, byPlayer, null);

                        handling = EnumHandling.PreventDefault;
                        return;
                    }
                }
                handling = EnumHandling.PreventDefault;
                return;
            }
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handling);
        }

        

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            return base.OnPickBlock(world, pos, ref handling);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handled)
        {
            return new WorldInteraction[]
            {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-behavior-rightclickpickup",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handled));
        }
    }
}

