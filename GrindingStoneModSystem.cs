using GrindingStone.BlockEntity;
using GrindingStone.Blocks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace GrindingStone
{
    public class GrindingStoneModSystem : ModSystem
    {

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass(Mod.Info.ModID + ".grindingStone", typeof(BlockGrindingStone));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".grindingStoneEntity", typeof(BlockEntityGrindingStone));
        }

    }
}
