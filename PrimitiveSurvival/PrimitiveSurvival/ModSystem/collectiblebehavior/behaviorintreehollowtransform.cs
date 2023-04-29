using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace PrimitiveSurvival.ModSystem
{
    public class BehaviorInTreeHollowTransform : CollectibleBehavior
    {
        public ModelTransform Transform { get; set; } = ModelTransform.NoTransform;

        public BehaviorInTreeHollowTransform(CollectibleObject collectibleObject) : base(collectibleObject) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            if (properties.AsObject<ModelTransform>() is ModelTransform transform)
            {
                this.Transform = transform;
            }
        }
    }
}
