using GrindingStone.Blocks;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

#nullable disable

namespace GrindingStone.BlockEntity
{
    public class BlockEntityGrindingStone : Vintagestory.GameContent.BlockEntityDisplay
    {
        public override string InventoryClassName => "grindingstone";
        protected InventoryGeneric inventory;
        public override InventoryBase Inventory => inventory;

        protected float grindingTime = 1f;
        protected bool isGrindingInProgress = false;

        protected CollectibleObject colObj;
        protected CollectibleObject invObj;


        public bool isPlacedGrinder = false;
        public bool isPlacedGrindable = false;
        public bool inventoryOccupied = false;

        protected float rotationYDeg;
        protected float[] rotMat;


        public float RotationYDeg
        {
            get { return rotationYDeg; }
            set
            {
                rotationYDeg = value;
                rotMat = Matrixf.Create().Translate(0.5f, 0, 0.5f).RotateYDeg(rotationYDeg).Translate(-0.5f, 0, -0.5f).Values;
            }
        }

        public BlockEntityGrindingStone()
        {
            inventory = new InventoryDisplayed(this, 1, "GrindingStone-0", null, null);
        }

        internal bool OnInteractStart(IPlayer byPlayer)
        {

            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            bool handSlotOccupied = false;
            bool isHeldGrinder = false;
            bool isHeldGrindable = false;
            //Api.World.Logger.Debug("OnInteractStart");

            if (!inventory[0].Empty)
            {
                invObj = inventory[0].Itemstack.Collectible;
                isPlacedGrindable = BlockGrindingStone.IsGrindable(invObj);
                inventoryOccupied = true;
            }

            if (!slot.Empty)
            {
                colObj = slot.Itemstack.Collectible;
                isHeldGrinder = BlockGrindingStone.IsGrinder(colObj);

                isHeldGrindable = BlockGrindingStone.IsGrindable(colObj);
                handSlotOccupied = true;
            }

            if (inventoryOccupied == false)                         // Inventory is empty
            {
                if (handSlotOccupied == false || colObj == Block)      // Hand slot is empty or has the same type of block
                {
                    TryBlock(byPlayer);                                   // Take the stone block
                    return true;
                }
                else                                                   // Hand slot is occupied
                {
                    if (isHeldGrindable)                                  // Item in hand slot is a grindable one
                    {
                        grindingTime = Api.World.Rand.Next(4) * 2 + 8;
                        MarkDirty();
                        TryPut(slot, byPlayer, ref isPlacedGrindable);       // Place the grindable on the block
                        return true;
                    }
                    else if (isHeldGrinder)                              // Item in hand slot is a grindable one
                    {
                        TryPut(slot, byPlayer, ref isPlacedGrinder);        // Place the grinder on the block
                        return true;
                    }
                    else                                                  // Item in hand slot is not suitable for storage
                    {
                        (Api as ICoreClientAPI)?.TriggerIngameError(this, "doesnotfit", Lang.Get("This item does not fit onto the grinding stone."));
                        return false;
                    }
                }
            }
            else                                                    // Inventory is occupied
            {
                if (handSlotOccupied == false || invObj == colObj)     // Hand slot is empty or has the same item as one stored
                {
                    TryTake(byPlayer);                                    // Take the inventory item
                    return true;
                }
                else if (isHeldGrinder)                                // Hand slot item is a valid grinder 
                {
                    if (isPlacedGrindable)                                // Inventory item is a valid grindable
                    {
                        isGrindingInProgress = true;                         // Commence grinding
                        if (byPlayer.Entity.World is IClientWorldAccessor) byPlayer.Entity.StartAnimation("grindingstone");
                    }

                    return true;
                }
                return false;
            }
        }

        internal bool OnInteractStep(float secondsUsed, IPlayer byPlayer)
        {
            //Api.World.Logger.Debug("OnInteractStep");
            if (isGrindingInProgress) return (secondsUsed < grindingTime);
            return false;
        }

        internal void OnInteractStop(IPlayer byPlayer, float secondsUsed)
        {
            //Api.World.Logger.Debug("OnInteractStop");
            if (isGrindingInProgress) FinishGrinding(byPlayer, secondsUsed >= grindingTime);
        }

        internal bool OnInteractCancel(IPlayer byPlayer, EnumItemUseCancelReason cancelReason)
        {
            //Api.World.Logger.Debug("OnInteractCancel");
            if (isGrindingInProgress && cancelReason != EnumItemUseCancelReason.ReleasedMouse) FinishGrinding(byPlayer, false);
            return true;
        }

        private bool TryPut(ItemSlot slot, IPlayer byPlayer, ref bool storageFlag)
        {
            int moved = slot.TryPutInto(Api.World, inventory[0]);

            if (moved > 0)
            {
                inventoryOccupied = true;
                storageFlag = true;

                updateMeshes();
                MarkDirty(true);

                Api.World.PlaySoundAt(
                    new AssetLocation("sounds/player/build"),
                    byPlayer.Entity,
                    byPlayer,
                    true,
                    16
                );
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                Api.World.Logger.Audit("{0} Put 1x{1} into GrindingStone slotid 0 at {2}.",
                    byPlayer.PlayerName,
                    inventory[0].Itemstack?.Collectible.Code,
                    Pos
                );

            }

            return moved > 0;
        }

        private bool TryTake(IPlayer byPlayer)
        {
            int stacksize = inventory[0].StackSize;
            ItemStack stack = inventory[0].TakeOut(stacksize);
            if (byPlayer.InventoryManager.TryGiveItemstack(stack, true))
            {
                Api.World.PlaySoundAt(
                    new AssetLocation("sounds/player/build"),
                    byPlayer.Entity,
                    byPlayer,
                    true,
                    16
                );
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                Api.World.Logger.Audit("{0} Took {1}x {2} from GrindingStone slotid 0 at {3}.",
                    byPlayer.PlayerName,
                    stacksize,
                    stack.Collectible.Code,
                    Pos
                );
            }

            if (stack.StackSize > 0)
            {
                Api.World.SpawnItemEntity(stack, Pos);
            }

            inventoryOccupied = false;
            isPlacedGrinder = false;
            isPlacedGrindable = false;

            updateMeshes();
            MarkDirty(true);
            return true;
        }


        private bool TryBlock(IPlayer byPlayer)
        {
            if (byPlayer.InventoryManager.TryGiveItemstack(new(Block, 1)))
            {
                Api.World.PlaySoundAt(
                    new AssetLocation("sounds/player/build"),
                    byPlayer.Entity,
                    byPlayer,
                    true,
                    16
                );
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);

                Api.World.BlockAccessor.SetBlock(0, Pos);
                return true;
            }
            return false;
        }

        private void FinishGrinding(IPlayer byPlayer, bool success)
        {
            Api.World.Logger.Debug("Finishing grinding");
            if (success)
            {
                ItemStack grindedStack = invObj.GrindingProps.GroundStack.ResolvedItemstack.Clone();
                ItemStack originalStack = inventory[0].Itemstack?.Clone();
                inventory[0].Itemstack.SetFrom(grindedStack);
                inventory[0].MarkDirty();

                Api.World.Logger.Audit("Transformed {0}x{1} into {2}x{3} in GrindingStone slotid 0 at {4}.",

                    originalStack.StackSize,
                    originalStack.Collectible.Code,
                    inventory[0].StackSize,
                    inventory[0].Itemstack?.Collectible.Code,
                    Pos
                );
                isPlacedGrindable = false;

                updateMeshes();
                MarkDirty(true);
            }
            isGrindingInProgress = false;
            if (byPlayer.Entity.World is IClientWorldAccessor) byPlayer.Entity.StopAnimation("grindingstone");
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
        {
            base.GetBlockInfo(forPlayer, sb);

            sb.AppendLine();

            if (forPlayer?.CurrentBlockSelection == null) return;

            if (!inventory[0].Empty)
            {
                sb.AppendLine(
                    Lang.Get("{0}x {1}",
                        inventory[0].StackSize,
                        inventory[0].Itemstack.GetName()
                    )
                );
            }
        }

        protected override float[][] genTransformationMatrices()
        {
            tfMatrices = new float[1][];
            if (inventory[0].Empty) return tfMatrices;

            var attr = inventory[0].Itemstack.Collectible.Attributes;
            var itemInGrindingStoneTransform = attr?["handGrindingProps"]?["inGrindingStoneTransform"]?.AsObject<ModelTransform>(null);
            var blockInGrindingStoneTransform = Block.Attributes["inGrindingStoneTransform"].AsObject<ModelTransform>(null);

            var blockTf = new Matrixf()
            .Translate(0.5f, 0, 0.5f)
            .RotateYDeg(RotationYDeg)
            .Translate(-0.5f, 0, -0.5f)
            .Values;

            tfMatrices[0] = blockTf;

            if (blockInGrindingStoneTransform != null)
            {
                Mat4f.Mul(tfMatrices[0], blockTf, blockInGrindingStoneTransform.AsMatrix);
            }
            if (itemInGrindingStoneTransform != null)
            {
                Mat4f.Mul(tfMatrices[0], blockTf, itemInGrindingStoneTransform.AsMatrix);
            }
            return tfMatrices;
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            RotationYDeg = tree.GetFloat("rotationYDeg");
            grindingTime = tree.GetFloat("grindingTime");
            // Do this last
            RedrawAfterReceivingTreeAttributes(worldForResolving);     // Redraw on client after we have completed receiving the update from server
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("rotationYDeg", rotationYDeg);
            tree.SetFloat("grindingTime", grindingTime);

        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {

            bool skip = base.OnTesselation(mesher, tessThreadTesselator);
            if (!skip) mesher.AddMeshData(capi.TesselatorManager.GetDefaultBlockMesh(Block), rotMat);
            return true;
        }


    }
}
