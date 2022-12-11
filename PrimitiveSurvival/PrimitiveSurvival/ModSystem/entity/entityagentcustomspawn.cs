namespace PrimitiveSurvival.ModSystem
{
    //using System.Diagnostics;
    using Vintagestory.API.Common;
    //using Vintagestory.API.Common.Entities;

    public class EntityAgentCustomSpawn : EntityAgent
    {
        public EntityAgentCustomSpawn()
        { }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            var canspawnon = this.Properties.Attributes?["canSpawnOn"]?.AsArray<string>(null);
            if (canspawnon != null)
            {
                var path = this.World.BlockAccessor.GetBlock(this.SidedPos.AsBlockPos.Add(0, -1, 0), BlockLayersAccess.Default).Code.Path;
                //Debug.WriteLine("entity spawned on: " + path);
                var canSpawn = false;
                foreach (var entry in canspawnon)
                {
                    if (path.StartsWith(entry))
                    { canSpawn = true; }
                }
                if (!canSpawn)
                {
                    //Debug.WriteLine("Remove");
                    this.Die(EnumDespawnReason.OutOfRange, null);
                }
            }
        }
    }
}

