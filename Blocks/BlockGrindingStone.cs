using GrindingStone.BlockEntity;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

#nullable disable

namespace GrindingStone.Blocks
{
    public class BlockGrindingStone : Block
    {
        WorldInteraction[] interactions;

        protected float rotInterval = GameMath.PIHALF / 4;

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

            if (val)
            {
                var be = GetBlockEntity<BlockEntityGrindingStone>(blockSel.Position);
                if (be != null)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    double dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(dx, dz);

                    float roundRad = ((int)Math.Round(angleHor / rotInterval)) * rotInterval;

                    be.RotationYDeg = roundRad * GameMath.RAD2DEG;
                    be.MarkDirty(true);
                }
            }

            return val;
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "grindingStoneInteractions", () =>
            {
                List<ItemStack> grinderStacklist = [];
                List<ItemStack> grindableStacklist = [];

                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    if (IsGrinder(obj))
                    {
                        grinderStacklist.Add(new ItemStack(obj));
                    }
                    if (IsGrindable(obj))
                    {
                        grindableStacklist.Add(new ItemStack(obj));
                    }
                }

                var grinderStacks = grinderStacklist.ToArray();
                var grindableStacks = grindableStacklist.ToArray();

                return new WorldInteraction[] {
                    new()
                    {
                        ActionLangCode = "gstone:blockhelp-grindingstone-store",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = grinderStacks,
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            var begs = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGrindingStone;
                            if (begs != null && begs.inventoryOccupied == false) return grinderStacks;
                            else return null;
                        }
                    },
                    new()
                    {
                        ActionLangCode = "gstone:blockhelp-grindingstone-place",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = grindableStacks,
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            var begs = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGrindingStone;
                            if (begs != null && begs.inventoryOccupied == false) return grindableStacks;
                            else return null;
                        }
                    },
                    new()
                    {
                        ActionLangCode = "gstone:blockhelp-grindingstone-grind",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = grinderStacks,
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            var begs = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityGrindingStone;
                            if (begs != null && begs.isPlacedGrindable) return grinderStacks;
                            else return null;
                        }
                    }
                };
            });
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
        {
            return false;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGrindingStone begs) return begs.OnInteractStart(byPlayer);

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGrindingStone begs) return begs.OnInteractStep(secondsUsed, byPlayer);
            return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGrindingStone begs) return begs.OnInteractCancel(byPlayer, cancelReason);
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityGrindingStone begs)
            {
                begs.OnInteractStop(byPlayer, secondsUsed);
                return;
            }
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public static bool IsGrindable(CollectibleObject colObj)
        {
            return (colObj.GrindingProps != null && colObj.Attributes?["handGrindingProps"].IsTrue("isHandGrindable") == true);
        }
        public static bool IsGrinder(CollectibleObject colObj)
        {
            return (colObj.Attributes?["handGrindingProps"].IsTrue("isHandGrinder") == true);
        }

    }
}