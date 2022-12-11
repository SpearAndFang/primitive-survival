namespace PrimitiveSurvival.ModSystem
{
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Common.Entities;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.Util;

    public class EntityBehaviorCarryable : EntityBehavior
    {
        public EntityBehaviorCarryable(Entity entity) : base(entity)
        { }

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            base.Initialize(properties, typeAttributes);
        }

        private WorldInteraction[] interactions = null;

        public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
        {
            this.interactions = ObjectCacheUtil.GetOrCreate(world.Api, "carryableEntityInteractions", () => new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-behavior-rightclickpickup",
                        MouseButton = EnumMouseButton.Right,
                    }
                });

            return this.interactions;
        }

        public override void GetInfoText(StringBuilder infotext)
        {
            infotext.AppendLine(Lang.Get("primitivesurvival:creature-carryable"));
            base.GetInfoText(infotext);
        }

        public override string PropertyName()
        {
            return "carryable";
        }
    }
}
