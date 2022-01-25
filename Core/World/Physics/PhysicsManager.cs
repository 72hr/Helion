using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.RandomGenerators;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using Helion.World.Special.SectorMovement;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Physics;

/// <summary>
/// Responsible for handling all the physics and collision detection in a
/// world.
/// </summary>
public class PhysicsManager
{
    private const int MaxSlides = 3;
    private const double SlideStepBackTime = 1.0 / 32.0;
    private const double MinMovementThreshold = 0.01;
    private const double SetEntityToFloorSpeedMax = 9;
    private const double MinMoveFactor = 32 / 65536.0;
    private const double DefaultMoveFactor = 1.0;
    private const double MudMoveFactorLow = 15000 / 65536.0;
    private const double MudMoveFactorMed = MudMoveFactorLow * 2;
    private const double MudMoveFactorHigh = MudMoveFactorLow * 4;


    public static readonly double LowestPossibleZ = Fixed.Lowest().ToDouble();

    public BlockmapTraverser BlockmapTraverser { get; private set; }

    private readonly IWorld m_world;
    private readonly BspTree m_bspTree;
    private readonly BlockMap m_blockmap;
    private readonly EntityManager m_entityManager;
    private readonly WorldSoundManager m_soundManager;
    private readonly IRandom m_random;
    private readonly LineOpening m_lineOpening = new();
    private readonly TryMoveData m_tryMoveData = new();
    private readonly List<Entity> m_crushEntities = new();
    private readonly List<Entity> m_sectorMoveEntities = new();
    private readonly SectorMoveOrderComparer m_sectorMoveOrderComparer = new();
    private readonly List<Entity> m_stackCrush = new();

    /// <summary>
    /// Creates a new physics manager which utilizes the arguments for any
    /// collision detection or linking to the world.
    /// </summary>
    /// <param name="world">The world to operate on.</param>
    /// <param name="bspTree">The BSP tree for the world.</param>
    /// <param name="blockmap">The blockmap for the world.</param>
    /// <param name="soundManager">The sound manager to play sounds from.</param>
    /// <param name="entityManager">entity manager.</param>
    /// <param name="random">Random number generator to use.</param>
    public PhysicsManager(IWorld world, BspTree bspTree, BlockMap blockmap, WorldSoundManager soundManager, EntityManager entityManager, IRandom random)
    {
        m_world = world;
        m_bspTree = bspTree;
        m_blockmap = blockmap;
        m_soundManager = soundManager;
        m_entityManager = entityManager;
        m_random = random;
        BlockmapTraverser = new BlockmapTraverser(m_blockmap);
    }

    /// <summary>
    /// Links an entity to the world.
    /// </summary>
    /// <param name="entity">The entity to link.</param>
    /// <param name="tryMove">Optional data used for when linking during movement.</param>
    /// <param name="clampToLinkedSectors">If the entity should be clamped between linked sectors. If false then on the current Sector ceiling/floor will be used. (Doom compatibility).</param>
    public void LinkToWorld(Entity entity, TryMoveData? tryMove = null, bool clampToLinkedSectors = true)
    {
        if (!entity.Flags.NoBlockmap)
            m_blockmap.Link(entity);

        if (!entity.Flags.NoSector)
            LinkToSectors(entity, tryMove);

        ClampBetweenFloorAndCeiling(entity, clampToLinkedSectors);
    }

    /// <summary>
    /// Performs all the movement logic on the entity.
    /// </summary>
    /// <param name="entity">The entity to move.</param>
    public void Move(Entity entity)
    {
        MoveXY(entity);
        MoveZ(entity);
    }

    public SectorMoveStatus MoveSectorZ(Sector sector, SectorPlane sectorPlane, SectorPlaneFace moveType,
        double speed, double destZ, CrushData? crush, bool compatibilityBlockMovement)
    {
        double startZ = sectorPlane.Z;
        if (!m_world.Config.Compatibility.VanillaSectorPhysics && IsSectorMovementBlocked(sector, moveType, startZ, destZ))
            return SectorMoveStatus.BlockedAndStop;

        // Save the Z value because we are only checking if the dest is valid
        // If the move is invalid because of a blocking entity then it will not be set to destZ
        Entity? highestBlockEntity = null;
        double? highestBlockHeight = 0.0;
        SectorMoveStatus status = SectorMoveStatus.Success;
        sectorPlane.PrevZ = startZ;
        sectorPlane.Z = destZ;
        sectorPlane.Plane.MoveZ(destZ - startZ);

        if (!m_world.Config.Compatibility.VanillaSectorPhysics && IsSectorMovementBlocked(sector, moveType, startZ, destZ))
        {
            FixPlaneClip(sector, sectorPlane, moveType);
            status = SectorMoveStatus.BlockedAndStop;
        }

        // Move lower entities first to handle stacked entities
        // Ordering by Id is only required for EntityRenderer nudging to prevent z-fighting
        GetSectorMoveOrderedEntities(m_sectorMoveEntities, sector);
        for(int i = 0; i < m_sectorMoveEntities.Count; i++)
        {
            Entity entity = m_sectorMoveEntities[i];
            entity.SaveZ = entity.Position.Z;
            entity.PrevSaveZ = entity.PrevPosition.Z;
            entity.WasCrushing = entity.IsCrushing();

            // At slower speeds we need to set entities to the floor
            // Otherwise the player will fall and hit the floor repeatedly creating a weird bouncing effect
            if (moveType == SectorPlaneFace.Floor && startZ > destZ && SpeedShouldStickToFloor(speed) &&
                entity.OnGround && entity.HighestFloorSector == sector)
            {
                entity.SetZ(entity.OnEntity?.Box.Top ?? destZ, false);
                // Setting this so SetEntityBoundsZ does not mess with forcing this entity to to the floor
                // Otherwise this is a problem with the instant lift hack
                entity.PrevPosition.Z = entity.Position.Z;
            }

            ClampBetweenFloorAndCeiling(entity);

            double thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;
            if (thingZ + entity.Height > entity.LowestCeilingZ)
            {
                if (moveType == SectorPlaneFace.Ceiling)
                    PushDownBlockingEntities(entity);
                // Clipped something that wasn't directly on this entity before the move and now it will be
                // Push the entity up, and the next loop will verify it is legal
                else
                    PushUpBlockingEntity(entity);
            }
        }

        for (int i = 0; i < m_sectorMoveEntities.Count; i++)
        {
            Entity entity = m_sectorMoveEntities[i];
            ClampBetweenFloorAndCeiling(entity);
            entity.PrevPosition.Z = entity.PrevSaveZ;
            // This allows the player to pickup items like the original
            if (entity.IsPlayer && !entity.Flags.NoClip)
                IsPositionValid(entity, entity.Position.XY, m_tryMoveData);

            if ((moveType == SectorPlaneFace.Ceiling && startZ < destZ) ||
                (moveType == SectorPlaneFace.Floor && startZ > destZ))
                continue;

            double thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;
            if (thingZ + entity.Height > entity.LowestCeilingZ)
            {
                if (entity.Flags.Dropped)
                {
                    m_entityManager.Destroy(entity);
                    continue;
                }

                // Need to gib things even when not crushing and do not count as blocking
                if (entity.Flags.Corpse && !entity.Flags.DontGib)
                {
                    SetToGiblets(entity);
                    continue;
                }

                // Doom checked against shootable instead of solid...
                if (!entity.Flags.Shootable)
                    continue;

                if (crush != null)
                {
                    if (crush.CrushMode == ZDoomCrushMode.Hexen || crush.Damage == 0)
                    {
                        highestBlockEntity = entity;
                        highestBlockHeight = entity.Height;
                    }

                    status = SectorMoveStatus.Crush;
                    m_crushEntities.Add(entity);
                }
                else if (CheckSectorMoveBlock(entity, moveType))
                {
                    highestBlockEntity = entity;
                    highestBlockHeight = entity.Height;
                    status = SectorMoveStatus.Blocked;
                }
            }
        }

        if (highestBlockEntity != null && highestBlockHeight.HasValue && !highestBlockEntity.IsDead)
        {
            double diff = 0;
            double thingZ = highestBlockEntity.OnGround ? highestBlockEntity.HighestFloorZ : highestBlockEntity.Position.Z;
            // Set the sector Z to the difference of the blocked height (only works if not being crushed)
            // Could probably do something fancy to figure this out if the entity is being crushed, but this is quite rare
            if (compatibilityBlockMovement || highestBlockEntity.WasCrushing)
            {
                sectorPlane.Z = startZ;
                sectorPlane.Plane.MoveZ(startZ - destZ);
            }
            else
            {
                diff = Math.Abs(startZ - destZ) - (thingZ + highestBlockHeight.Value - highestBlockEntity.LowestCeilingZ);
                if (speed < 0)
                    diff = -diff;

                sectorPlane.Z = startZ + diff;
                sectorPlane.Plane.MoveZ(startZ - destZ + diff);
            }

            // Entity blocked movement, reset all entities in moving sector after resetting sector Z
            for (int i = 0; i < m_sectorMoveEntities.Count; i++)
            {
                Entity relinkEntity = m_sectorMoveEntities[i];
                // Check for entities that may be dead from being crushed
                if (relinkEntity.IsDisposed)
                    continue;
                relinkEntity.UnlinkFromWorld();
                relinkEntity.SetZ(relinkEntity.SaveZ + diff, false);
                LinkToWorld(relinkEntity);
            }
        }

        if (crush != null && m_crushEntities.Count > 0)
            CrushEntities(m_crushEntities, sector, crush);

        m_crushEntities.Clear();
        m_sectorMoveEntities.Clear();
        return status;
    }

    private void GetSectorMoveOrderedEntities(List<Entity> entites, Sector sector)
    {
        LinkableNode<Entity>? node = sector.Entities.Head;
        while (node != null)
        {
            m_sectorMoveEntities.Add(node.Value);
            node = node.Next;
        }
        entites.Sort(m_sectorMoveOrderComparer);
    }

    // Constants and logic from WinMBF.
    // Credit to Lee Killough et al.
    public static double GetMoveFactor(Entity entity)
    {
        double sectorFriction = GetFrictionFromSectors(entity);
        double moveFactor = DefaultMoveFactor;

        if (sectorFriction != Constants.DefaultFriction)
        {
            if (sectorFriction >= Constants.DefaultFriction)
                moveFactor = (0x10092 - sectorFriction * 65536.0) * 0x70 / 0x158 / 65536.0;
            else
                moveFactor = (sectorFriction * 65536.0 - 0xDB34) * 0xA / 0x80 / 65536.0;

            moveFactor = Math.Clamp(moveFactor, MinMoveFactor, double.MaxValue);
            // The move factor was based on 2048 being default in Boom.
            moveFactor /= 2048.0 / 65536.0;
        }

        if (sectorFriction < Constants.DefaultFriction)
        {
            double momentum = entity.Velocity.XY.Length();
            if (momentum > MudMoveFactorHigh)
                moveFactor *= 8;
            else if (momentum > MudMoveFactorMed)
                moveFactor *= 4;
            else if (momentum > MudMoveFactorLow)
                moveFactor *= 2;
        }

        return moveFactor;
    }

    private static bool IsSectorMovementBlocked(Sector sector, SectorPlaneFace moveType, double startZ, double destZ)
    {
        if (moveType == SectorPlaneFace.Floor && destZ < startZ)
            return false;

        if (moveType == SectorPlaneFace.Ceiling && destZ > startZ)
            return false;

        return sector.Ceiling.Z < sector.Floor.Z;
    }

    private static void FixPlaneClip(Sector sector, SectorPlane sectorPlane, SectorPlaneFace moveType)
    {
        if (moveType == SectorPlaneFace.Floor)
        {
            sectorPlane.Plane.MoveZ(sectorPlane.Z - sector.Ceiling.Z);
            sectorPlane.Z = sector.Ceiling.Z;
        }
        else
        {
            sectorPlane.Plane.MoveZ(sector.Floor.Z - sectorPlane.Z);
            sectorPlane.Z = sector.Floor.Z;
        }
    }

    private static bool SpeedShouldStickToFloor(double speed) =>
        -speed < SetEntityToFloorSpeedMax || -speed == SectorMoveData.InstantToggleSpeed;

    private static bool CheckSectorMoveBlock(Entity entity, SectorPlaneFace moveType)
    {
        // If the entity was pushed up by a floor and changed it's z pos then this floor is blocked
        if (moveType == SectorPlaneFace.Ceiling || entity.SaveZ != entity.Position.Z)
            return true;

        return false;
    }

    private void CrushEntities(List<Entity> crushEntities, Sector sector, CrushData crush)
    {
        if (crush.Damage == 0 || (m_world.Gametick & 3) != 0)
            return;

        // Check for stacked entities, so we can crush the stack
        LinkableNode<Entity>? node = sector.Entities.Head;
        while (node != null)
        {
            Entity checkEntity = node.Value;
            if (checkEntity.OverEntity != null && crushEntities.Contains(checkEntity.OverEntity))
                m_stackCrush.Add(checkEntity);
            node = node.Next;
        }

        for (int i = 0; i < crushEntities.Count; i++)
        {
            if (m_stackCrush.Contains(crushEntities[i]))
                continue;
            m_stackCrush.Add(crushEntities[i]);
        }

        for (int i = 0; i < m_stackCrush.Count; i++)
        {
            Entity crushEntity = m_stackCrush[i];
            m_world.HandleEntityHit(crushEntity, crushEntity.Velocity, null);

            if (!crushEntity.IsDead && m_world.DamageEntity(crushEntity, null, crush.Damage, false) &&
                !crushEntity.Flags.NoBlood)
            {
                Vec3D pos = crushEntity.Position;
                pos.Z += crushEntity.Height / 2;
                Entity? blood = m_entityManager.Create(crushEntity.GetBloodType(), pos);
                if (blood != null)
                {
                    blood.Velocity.X += m_random.NextDiff() / 16.0;
                    blood.Velocity.Y += m_random.NextDiff() / 16.0;
                }
            }
        }

        m_stackCrush.Clear();
    }

    private void SetToGiblets(Entity entity)
    {
        if (!entity.SetCrushState())
        {
            m_entityManager.Destroy(entity);
            m_entityManager.Create("REALGIBS", entity.Position);
        }
    }

    private static void PushUpBlockingEntity(Entity pusher)
    {
        if (!(pusher.LowestCeilingObject is Entity))
            return;

        Entity entity = (Entity)pusher.LowestCeilingObject;
        entity.SetZ(pusher.Box.Top, false);
    }

    private static void PushDownBlockingEntities(Entity pusher)
    {
        // Because of how ClampBetweenFloorAndCeiling works, try to push down the entire stack and stop when something clips a floor
        if (pusher.HighestFloorObject is Sector && pusher.HighestFloorZ > pusher.LowestCeilingZ - pusher.Height)
            return;

        pusher.SetZ(pusher.LowestCeilingZ - pusher.Height, false);

        if (pusher.OnEntity != null)
        {
            Entity? current = pusher.OnEntity;
            while (current != null)
            {
                if (current.HighestFloorObject is Sector && current.HighestFloorZ > pusher.Box.Bottom - current.Height)
                    return;

                current.SetZ(pusher.Box.Bottom - current.Height, false);
                pusher = current;
                current = pusher.OnEntity;
            }
        }
    }

    public void HandleEntityDeath(Entity deathEntity)
    {
        if (deathEntity.OnEntity != null || deathEntity.OverEntity != null)
            HandleStackedEntityPhysics(deathEntity);
    }

    private static int CalculateSteps(Vec2D velocity, double radius)
    {
        InvariantWarning(radius > 0.5, "Actor radius too small for safe XY physics movement");

        // We want to pick some atomic distance to keep moving our bounding
        // box. It can't be bigger than the radius because we could end up
        // skipping over a line.
        double moveDistance = radius - 0.5;
        double biggerAxis = Math.Max(Math.Abs(velocity.X), Math.Abs(velocity.Y));
        return (int)(biggerAxis / moveDistance) + 1;
    }

    private static void ApplyFriction(Entity entity)
    {
        double sectorFriction = GetFrictionFromSectors(entity);
        entity.Velocity.X *= sectorFriction;
        entity.Velocity.Y *= sectorFriction;
    }

    private static void StopXYMovementIfSmall(Entity entity)
    {
        if (Math.Abs(entity.Velocity.X) < MinMovementThreshold)
            entity.Velocity.X = 0;
        if (Math.Abs(entity.Velocity.Y) < MinMovementThreshold)
            entity.Velocity.Y = 0;
    }

    private enum LineBlock
    {
        NoBlock,
        BlockStopChecking,
        BlockContinue,
    }

    private LineBlock LineBlocksEntity(Entity entity, in Vec2D position, Line line, TryMoveData? tryMove)
    {
        if (line.BlocksEntity(entity))
            return LineBlock.BlockStopChecking;
        if (line.Back == null)
            return LineBlock.NoBlock;

        LineOpening opening = GetLineOpening(position, line);
        tryMove?.SetIntersectionData(opening);

        if (opening.CanPassOrStepThrough(entity))
            return LineBlock.NoBlock;

        return LineBlock.BlockContinue;
    }

    public LineOpening GetLineOpening(in Vec2D position, Line line)
    {
        m_lineOpening.Set(position, line);
        return m_lineOpening;
    }

    private void SetEntityOnFloorOrEntity(Entity entity, double floorZ, bool smoothZ)
    {
        // Additionally check to smooth camera when stepping up to an entity
        entity.SetZ(floorZ, smoothZ);

        // For now we remove any negative velocity. If upward velocity is
        // reset to zero then the jump we apply to players is lost and they
        // can never jump. Maybe we want to fix this in the future by doing
        // application of jumping after the XY movement instead of before?
        entity.Velocity.Z = Math.Max(0, entity.Velocity.Z);
    }

    private void ClampBetweenFloorAndCeiling(Entity entity, bool clampToLinkedSectors = true)
    {
        // TODO fixme
        if (entity.Definition.Name.Equals("BulletPuff", StringComparison.OrdinalIgnoreCase))
            return;
        if (entity.Flags.NoClip && entity.Flags.NoGravity)
            return;

        object lastHighestFloorObject = entity.HighestFloorObject;
        SetEntityBoundsZ(entity, clampToLinkedSectors);

        double lowestCeil = entity.LowestCeilingZ;
        double highestFloor = entity.HighestFloorZ;

        if (entity.Box.Top > lowestCeil)
        {
            entity.Velocity.Z = 0;
            entity.SetZ(lowestCeil - entity.Height, false);

            if (entity.LowestCeilingObject is Entity blockEntity)
                entity.BlockingEntity = blockEntity;
            else
                entity.BlockingSectorPlane = entity.LowestCeilingSector.Ceiling;
        }

        bool clippedFloor = entity.Box.Bottom < highestFloor;
        if (entity.Box.Bottom <= highestFloor)
        {
            if (entity.HighestFloorObject is Entity highestEntity &&
                highestEntity.Box.Top <= entity.Box.Bottom + entity.GetMaxStepHeight())
            {
                entity.OnEntity = highestEntity;
            }

            if (entity.OnEntity != null)
                entity.OnEntity.OverEntity = entity;

            SetEntityOnFloorOrEntity(entity, highestFloor, lastHighestFloorObject != entity.HighestFloorObject);

            if (clippedFloor)
            {
                if (entity.HighestFloorObject is Entity blockEntity)
                    entity.BlockingEntity = blockEntity;
                else
                    entity.BlockingSectorPlane = entity.HighestFloorSector.Floor;
            }
        }
    }

    private void SetEntityBoundsZ(Entity entity, bool clampToLinkedSectors)
    {
        Sector highestFloor = entity.Sector;
        Sector lowestCeiling = entity.Sector;
        Entity? highestFloorEntity = null;
        Entity? lowestCeilingEntity = null;
        double highestFloorZ = highestFloor.ToFloorZ(entity.Position);
        double lowestCeilZ = lowestCeiling.ToCeilingZ(entity.Position);

        entity.OnEntity = null;
        entity.ClippedWithEntity = false;

        if (clampToLinkedSectors)
        {
            foreach (Sector sector in entity.IntersectSectors)
            {
                double floorZ = sector.ToFloorZ(entity.Position);
                if (floorZ > highestFloorZ)
                {
                    highestFloor = sector;
                    highestFloorZ = floorZ;
                }

                double ceilZ = sector.ToCeilingZ(entity.Position);
                if (ceilZ < lowestCeilZ)
                {
                    lowestCeiling = sector;
                    lowestCeilZ = ceilZ;
                }
            }
        }

        // Only check against other entities if CanPass is set (height sensitive clip detection)
        if (entity.Flags.CanPass && !entity.Flags.NoClip)
        {
            // Get intersecting entities here - They are not stored in the entity because other entities can move around after this entity has linked
            List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(entity.Box.To2D(),
                BlockmapTraverseFlags.Entities, BlockmapTraverseEntityFlags.Solid | BlockmapTraverseEntityFlags.NotCorpse);

            for (int i = 0; i < intersections.Count; i++)
            {
                Entity? intersectEntity = intersections[i].Entity;
                if (intersectEntity == null || ReferenceEquals(entity, intersectEntity) || intersectEntity.Flags.NoClip)
                    continue;

                bool above = entity.PrevPosition.Z >= intersectEntity.Box.Top;
                bool below = entity.PrevPosition.Z + entity.Height <= intersectEntity.Box.Bottom;
                bool clipped = false;
                if (above && entity.Box.Bottom < intersectEntity.Box.Top)
                    clipped = true;
                else if (below && entity.Box.Top > intersectEntity.Box.Bottom)
                    clipped = true;

                if (!above && !below && !clampToLinkedSectors && !intersectEntity.Flags.ActLikeBridge)
                {
                    entity.ClippedWithEntity = true;
                    continue;
                }

                if (above)
                {
                    // Need to check clipping coming from above, if we're above
                    // or clipped through then this is our floor.
                    if ((clipped || entity.Box.Bottom >= intersectEntity.Box.Top) && intersectEntity.Box.Top > highestFloorZ)
                    {
                        highestFloorEntity = intersectEntity;
                        highestFloorZ = intersectEntity.Box.Top;
                    }
                }
                else if (below)
                {
                    // Same check as above but checking clipping the ceiling.
                    if ((clipped || entity.Box.Top <= intersectEntity.Box.Bottom) && intersectEntity.Box.Bottom < lowestCeilZ)
                    {
                        lowestCeilingEntity = intersectEntity;
                        lowestCeilZ = intersectEntity.Box.Bottom;
                    }
                }

                // Need to check if we can step up to this floor.
                if (entity.Box.Bottom + entity.GetMaxStepHeight() >= intersectEntity.Box.Top && intersectEntity.Box.Top > highestFloorZ)
                {
                    highestFloorEntity = intersectEntity;
                    highestFloorZ = intersectEntity.Box.Top;
                }
            }

            DataCache.Instance.FreeBlockmapIntersectList(intersections);
        }

        entity.HighestFloorZ = highestFloorZ;
        entity.LowestCeilingZ = lowestCeilZ;
        entity.HighestFloorSector = highestFloor;
        entity.LowestCeilingSector = lowestCeiling;

        if (highestFloorEntity != null && highestFloorEntity.Box.Top > highestFloor.ToFloorZ(entity.Position))
            entity.HighestFloorObject = highestFloorEntity;
        else
            entity.HighestFloorObject = highestFloor;

        if (lowestCeilingEntity != null && lowestCeilingEntity.Box.Top < lowestCeiling.ToCeilingZ(entity.Position))
            entity.LowestCeilingObject = lowestCeilingEntity;
        else
            entity.LowestCeilingObject = lowestCeiling;

        entity.CheckOnGround();
    }

    private void LinkToSectors(Entity entity, TryMoveData? tryMove)
    {
        Precondition(entity.SectorNodes.Empty(), "Forgot to unlink entity from blockmap");

        Subsector centerSubsector = m_bspTree.ToSubsector(entity.Position);
        Sector centerSector = centerSubsector.Sector;
        HashSet<Sector> sectors = DataCache.Instance.GetSectorSet();
        sectors.Add(centerSector);

        // TODO: Can we replace this by iterating over the blocks were already in?
        Box2D box = entity.Box.To2D();
        m_blockmap.Iterate(box, SectorOverlapFinder);

        entity.Sector = centerSector;
        foreach (Sector sector in sectors)
        {
            entity.IntersectSectors.Add(sector);
            entity.SectorNodes.Add(sector.Link(entity));
        }

        entity.SubsectorNode = centerSubsector.Link(entity);

        DataCache.Instance.FreeSectorSet(sectors);

        GridIterationStatus SectorOverlapFinder(Block block)
        {
            // Doing iteration over enumeration for performance reasons.
            for (int i = 0; i < block.Lines.Count; i++)
            {
                Line line = block.Lines[i];
                if (line.Segment.Intersects(box))
                {
                    sectors.Add(line.Front.Sector);

                    if (line.Back != null)
                        sectors.Add(line.Back.Sector);
                }
            }

            return GridIterationStatus.Continue;
        }
    }

    private static void ClearVelocityXY(Entity entity)
    {
        entity.Velocity.X = 0;
        entity.Velocity.Y = 0;
    }

    public TryMoveData TryMoveXY(Entity entity, Vec2D position)
    {
        m_tryMoveData.SetPosition(position);
        if (entity.Flags.NoClip)
        {
            HandleNoClip(entity, position);
            m_tryMoveData.Success = true;
            return m_tryMoveData;
        }

        if (entity.ClippedWithEntity && !entity.OnGround && entity.IsClippedWithEntity())
        {
            m_tryMoveData.Success = false;
            entity.Velocity = Vec3D.Zero;
            return m_tryMoveData;
        }

        Vec2D velocity = position - entity.Position.XY;
        if (velocity == Vec2D.Zero || entity.IsCrushing())
        {
            m_tryMoveData.Success = false;
            return m_tryMoveData;
        }

        // We advance in small steps that are smaller than the radius of
        // the actor so we don't skip over any lines or things due to fast
        // entity speed.
        int slidesLeft = MaxSlides;
        int numMoves = CalculateSteps(velocity, entity.Radius);
        Vec2D stepDelta = velocity / numMoves;
        bool success = true;
        Vec3D saveVelocity = entity.Velocity;

        for (int movesLeft = numMoves; movesLeft > 0; movesLeft--)
        {
            if (stepDelta == Vec2D.Zero || m_world.WorldState == WorldState.Exit)
                break;

            Vec2D nextPosition = entity.Position.XY + stepDelta;

            if (IsPositionValid(entity, nextPosition, m_tryMoveData))
            {
                entity.MoveLinked = true;
                MoveTo(entity, nextPosition, m_tryMoveData);
                m_world.HandleEntityIntersections(entity, saveVelocity, m_tryMoveData);
                if (entity.Flags.Teleport)
                    break;
                continue;
            }

            if (entity.Flags.SlidesOnWalls && slidesLeft > 0)
            {
                // BlockingLine and BlockingEntity will get cleared on HandleSlide(IsPositionValid) calls.
                // Carry them over so other functions after TryMoveXY can use them for verification.
                var blockingLine = entity.BlockingLine;
                var blockingEntity = entity.BlockingEntity;
                HandleSlide(entity, ref stepDelta, ref movesLeft, m_tryMoveData);
                entity.BlockingLine = blockingLine;
                entity.BlockingEntity = blockingEntity;
                slidesLeft--;
                success = false;
                continue;
            }

            success = false;
            ClearVelocityXY(entity);
            break;
        }

        if (success && entity.OverEntity != null)
            HandleStackedEntityPhysics(entity);

        if (!success)
            m_world.HandleEntityHit(entity, saveVelocity, m_tryMoveData);

        m_tryMoveData.Success = success;
        return m_tryMoveData;
    }

    private void HandleStackedEntityPhysics(Entity entity)
    {
        Entity? currentOverEntity = entity.OverEntity;

        if (entity.OnEntity != null)
            ClampBetweenFloorAndCeiling(entity.OnEntity);

        while (currentOverEntity != null)
        {
            LinkableNode<Entity>? node = entity.Sector.Entities.Head;
            while (node != null)
            {
                Entity relinkEntity = node.Value;
                if (relinkEntity.OnEntity == entity)
                    ClampBetweenFloorAndCeiling(relinkEntity, false);
                node = node.Next;
            }

            entity = currentOverEntity;
            Entity? next = currentOverEntity.OverEntity;
            if (currentOverEntity.OverEntity != null && currentOverEntity.OverEntity.OnEntity != entity)
                currentOverEntity.OverEntity = null;
            currentOverEntity = next;
        }
    }

    private void HandleNoClip(Entity entity, Vec2D position)
    {
        entity.UnlinkFromWorld();
        entity.SetXY(position);
        LinkToWorld(entity);
    }

    public bool IsPositionValid(Entity entity, Vec2D position) =>
        IsPositionValid(entity, position, m_tryMoveData);

    public bool IsPositionValid(Entity entity, Vec2D position, TryMoveData tryMove)
    {
        if (!entity.Flags.Float && !entity.IsPlayer && entity.OnEntity != null && !entity.OnEntity.Flags.ActLikeBridge)
            return false;

        tryMove.Success = true;
        tryMove.LowestCeilingZ = entity.LowestCeilingZ;
        if (entity.HighestFloorObject is Entity highFloorEntity)
        {
            tryMove.HighestFloorZ = highFloorEntity.Box.Top;
            tryMove.DropOffZ = entity.Sector.ToFloorZ(position);
        }
        else
        {
            Sector sector = m_bspTree.ToSector(position.To3D(0));
            tryMove.HighestFloorZ = tryMove.DropOffZ = sector.ToFloorZ(position);
        }

        Box2D nextBox = new(position, entity.Radius);
        entity.BlockingLine = null;
        entity.BlockingEntity = null;
        m_blockmap.Iterate(nextBox, CheckForBlockers);

        if (entity.BlockingLine != null && entity.BlockingLine.BlocksEntity(entity))
        {
            tryMove.Success = false;
            return false;
        }

        if (tryMove.LowestCeilingZ - tryMove.HighestFloorZ < entity.Height || entity.BlockingEntity != null)
        {
            tryMove.Success = false;
            return false;
        }

        tryMove.CanFloat = true;

        if (!entity.CheckDropOff(tryMove))
            tryMove.Success = false;

        return tryMove.Success;

        GridIterationStatus CheckForBlockers(Block block)
        {
            for (int i = 0; i < block.Lines.Count; i++)
            {
                if (entity.Flags.Solid || entity.Flags.Missile)
                {
                    for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null;)
                    {
                        Entity nextEntity = entityNode.Value;
                        if (ReferenceEquals(entity, nextEntity))
                        {
                            entityNode = entityNode.Next;
                            continue;
                        }

                        if (nextEntity.Box.Overlaps2D(nextBox))
                        {
                            tryMove.IntersectEntities2D.Add(nextEntity);
                            bool overlapsZ = entity.Box.OverlapsZ(nextEntity.Box);

                            // Note: Flags.Special is set when the definition is applied using Definition.IsType(EntityDefinitionType.Inventory)
                            // This flag can be modified by dehacked
                            if (overlapsZ && entity.Flags.Pickup && nextEntity.Flags.Special)
                            {
                                // Set the next node - this pickup can be removed from the list
                                entityNode = entityNode.Next;
                                m_world.PerformItemPickup(entity, nextEntity);
                                continue;
                            }
                            else if (entity.CanBlockEntity(nextEntity) && BlocksEntityZ(entity, nextEntity, tryMove, overlapsZ))
                            {
                                tryMove.Success = false;
                                entity.BlockingEntity = nextEntity;
                                return GridIterationStatus.Stop;
                            }
                        }

                        entityNode = entityNode.Next;
                    }
                }

                Line line = block.Lines[i];
                if (line.Segment.Intersects(nextBox))
                {
                    LineBlock blockType = LineBlocksEntity(entity, position, line, tryMove);
                    if (blockType != LineBlock.NoBlock)
                    {
                        entity.BlockingLine = line;
                        tryMove.Success = false;
                        if (!entity.Flags.NoClip && line.HasSpecial)
                            tryMove.AddImpactSpecialLine(line);
                        if (blockType == LineBlock.BlockStopChecking)
                            return GridIterationStatus.Stop;
                    }

                    if (!entity.Flags.NoClip && line.HasSpecial)
                    {
                        if (blockType == LineBlock.NoBlock)
                            tryMove.AddIntersectSpecialLine(line);
                        else
                            tryMove.AddImpactSpecialLine(line);
                    }
                }
            }

            return GridIterationStatus.Continue;
        }
    }

    private bool BlocksEntityZ(Entity entity, Entity other, TryMoveData tryMove, bool overlapsZ)
    {
        if (ReferenceEquals(this, other))
            return false;

        if (entity.Position.Z + entity.Height > other.Position.Z)
        {
            // This entity is higher than the other entity and requires step up checking
            m_lineOpening.SetTop(tryMove, other);
        }
        else
        {
            // This entity is within the other entity's Z or below
            m_lineOpening.SetBottom(tryMove, other);
        }

        tryMove.SetIntersectionData(m_lineOpening);

        // If blocking and not a player, do not check step passing below. Non-players can't step onto other things.
        if (overlapsZ && !entity.IsPlayer)
            return true;

        if (!overlapsZ)
            return false;

        return !m_lineOpening.CanPassOrStepThrough(entity);
    }

    public void MoveTo(Entity entity, Vec2D nextPosition, TryMoveData tryMove)
    {
        entity.UnlinkFromWorld();

        Vec2D previousPosition = entity.Position.XY;
        entity.SetXY(nextPosition);

        LinkToWorld(entity, tryMove);

        for (int i = tryMove.IntersectSpecialLines.Count - 1; i >= 0; i--)
        {
            CheckLineSpecialActivation(entity, tryMove.IntersectSpecialLines[i], previousPosition);
            if (entity.Flags.Teleport)
                break;
        }
    }

    private void CheckLineSpecialActivation(Entity entity, Line line, Vec2D previousPosition)
    {
        if (!m_world.CanActivate(entity, line, ActivationContext.CrossLine))
            return;

        bool fromFront = line.Segment.OnRight(previousPosition);
        if (fromFront != line.Segment.OnRight(entity.Position.XY))
        {
            if (line.Special.IsTeleport() && !fromFront)
                return;

            m_world.ActivateSpecialLine(entity, line, ActivationContext.CrossLine);
        }
    }

    private void HandleSlide(Entity entity, ref Vec2D stepDelta, ref int movesLeft, TryMoveData tryMove)
    {
        if (FindClosestBlockingLine(entity, stepDelta, out MoveInfo moveInfo) &&
            MoveCloseToBlockingLine(entity, stepDelta, moveInfo, out Vec2D residualStep, tryMove))
        {
            ReorientToSlideAlong(entity, moveInfo.BlockingLine!, residualStep, ref stepDelta, ref movesLeft);
            return;
        }

        if (AttemptAxisMove(entity, stepDelta, Axis2D.Y, tryMove))
            return;
        if (AttemptAxisMove(entity, stepDelta, Axis2D.X, tryMove))
            return;

        // If we cannot find the line or thing that is blocking us, then we
        // are fully done moving horizontally.
        ClearVelocityXY(entity);
        stepDelta.X = 0;
        stepDelta.Y = 0;
        movesLeft = 0;
    }

    private static BoxCornerTracers CalculateCornerTracers(Box2D currentBox, Vec2D stepDelta)
    {
        Span<Vec2D> corners = stackalloc Vec2D[3];
        if (stepDelta.X >= 0)
        {
            if (stepDelta.Y >= 0)
            {
                corners[0] = currentBox.TopLeft;
                corners[1] = currentBox.TopRight;
                corners[2] = currentBox.BottomRight;
            }
            else
            {
                corners[0] = currentBox.TopRight;
                corners[1] = currentBox.BottomRight;
                corners[2] = currentBox.BottomLeft;
            }
        }
        else
        {
            if (stepDelta.Y >= 0)
            {
                corners[0] = currentBox.TopRight;
                corners[1] = currentBox.TopLeft;
                corners[2] = currentBox.BottomLeft;
            }
            else
            {
                corners[0] = currentBox.TopLeft;
                corners[1] = currentBox.BottomLeft;
                corners[2] = currentBox.BottomRight;
            }
        }

        Seg2D first = new Seg2D(corners[0], corners[0] + stepDelta);
        Seg2D second = new Seg2D(corners[1], corners[1] + stepDelta);
        Seg2D third = new Seg2D(corners[2], corners[2] + stepDelta);
        return new BoxCornerTracers(first, second, third);
    }

    private void CheckCornerTracerIntersection(Seg2D cornerTracer, Entity entity, ref MoveInfo moveInfo)
    {
        bool hit = false;
        double hitTime = double.MaxValue;
        Line? blockingLine = null;
        Vec2D position = entity.Position.XY;
        m_blockmap.Iterate(cornerTracer, CheckForTracerHit);

        if (hit && hitTime < moveInfo.LineIntersectionTime)
            moveInfo = MoveInfo.From(blockingLine!, hitTime);

        GridIterationStatus CheckForTracerHit(Block block)
        {
            for (int i = 0; i < block.Lines.Count; i++)
            {
                Line line = block.Lines[i];

                if (cornerTracer.Intersection(line.Segment, out double time) &&
                    LineBlocksEntity(entity, position, line, null) != LineBlock.NoBlock &&
                    time < hitTime)
                {
                    hit = true;
                    hitTime = time;
                    blockingLine = line;
                }
            }

            return GridIterationStatus.Continue;
        }
    }

    private bool FindClosestBlockingLine(Entity entity, Vec2D stepDelta, out MoveInfo moveInfo)
    {
        moveInfo = MoveInfo.Empty();

        // We shoot out 3 tracers from the corners in the direction we're
        // travelling to see if there's a blocking line as follows:
        //    _  _
        //    /| /|   If we're travelling northeast, then from the
        //   /  /_    top right corners of the bounding box we will
        //  o--o /|   shoot out tracers in the direction we are going
        //  |  |/     to step to see if we hit anything
        //  o--o
        //
        // This obviously can miss things, but this is how vanilla does it
        // and we want to have compatibility with the mods that use.
        Box2D currentBox = entity.Box.To2D();
        BoxCornerTracers tracers = CalculateCornerTracers(currentBox, stepDelta);
        CheckCornerTracerIntersection(tracers.First, entity, ref moveInfo);
        CheckCornerTracerIntersection(tracers.Second, entity, ref moveInfo);
        CheckCornerTracerIntersection(tracers.Third, entity, ref moveInfo);

        return moveInfo.IntersectionFound;
    }

    private bool MoveCloseToBlockingLine(Entity entity, Vec2D stepDelta, MoveInfo moveInfo, out Vec2D residualStep, TryMoveData tryMove)
    {
        Precondition(moveInfo.LineIntersectionTime >= 0, "Blocking line intersection time should never be negative");
        Precondition(moveInfo.IntersectionFound, "Should not be moving close to a line if we didn't hit one");

        // If it's close enough that stepping back would move us further
        // back than we currently are (or move us nowhere), we don't need
        // to do anything. This also means the residual step is equal to
        // the entire step since we're not stepping anywhere.
        if (moveInfo.LineIntersectionTime <= SlideStepBackTime)
        {
            residualStep = stepDelta;
            return true;
        }

        double t = moveInfo.LineIntersectionTime - SlideStepBackTime;
        Vec2D usedStepDelta = stepDelta * t;
        residualStep = stepDelta - usedStepDelta;

        Vec2D closeToLinePosition = entity.Position.XY + usedStepDelta;
        if (IsPositionValid(entity, closeToLinePosition, tryMove))
        {
            MoveTo(entity, closeToLinePosition, tryMove);
            return true;
        }

        return false;
    }

    private void ReorientToSlideAlong(Entity entity, Line blockingLine, Vec2D residualStep, ref Vec2D stepDelta,
        ref int movesLeft)
    {
        // Our slide direction depends on if we're going along with the
        // line or against the line. If the dot product is negative, it
        // means we are facing away from the line and should slide in
        // the opposite direction from the way the line is pointing.
        Vec2D unitDirection = blockingLine.Segment.Delta.Unit();
        if (stepDelta.Dot(unitDirection) < 0)
            unitDirection = -unitDirection;

        // Because we moved up to the wall, it's almost always the case
        // that we didn't make 100% of a step. For example if we have some
        // movement of 5 map units towards a wall and run into the wall at
        // 3 (leaving 2 map units unhandled), we want to work that residual
        // map unit movement into the existing step length. The following
        // does that by finding the total movement scalar and applying it
        // to the direction we need to slide.
        //
        // We also must take into account that we're adding some scalar to
        // another scalar, which means we'll end up with usually a larger
        // one. This means our step delta could grow beyond the size of the
        // radius of the entity and cause it to skip lines in pathological
        // situations. I haven't encountered such a case yet but it is at
        // least theoretically possible this can happen. Because of this,
        // the movesLeft is incremented by 1 to make sure the stepDelta
        // at the end of this function stays smaller than the radius.
        // TODO: If we have the unit vector, is projection overkill? Can we
        //       just multiply by the component instead?
        Vec2D stepProjection = stepDelta.Projection(unitDirection);
        Vec2D residualProjection = residualStep.Projection(unitDirection);

        // TODO: This is almost surely not how it's done, but it feels okay
        //       enough right now to leave as is.
        entity.Velocity.X = stepProjection.X * Constants.DefaultFriction;
        entity.Velocity.Y = stepProjection.Y * Constants.DefaultFriction;

        double totalRemainingDistance = ((stepProjection * movesLeft) + residualProjection).Length();
        movesLeft += 1;
        stepDelta = unitDirection * totalRemainingDistance / movesLeft;
    }

    private bool AttemptAxisMove(Entity entity, Vec2D stepDelta, Axis2D axis, TryMoveData tryMove)
    {
        if (axis == Axis2D.X)
        {
            Vec2D nextPosition = entity.Position.XY + new Vec2D(stepDelta.X, 0);
            if (IsPositionValid(entity, nextPosition, tryMove))
            {
                MoveTo(entity, nextPosition, tryMove);
                entity.Velocity.Y = 0;
                stepDelta.Y = 0;
                return true;
            }
        }
        else
        {
            Vec2D nextPosition = entity.Position.XY + new Vec2D(0, stepDelta.Y);
            if (IsPositionValid(entity, nextPosition, tryMove))
            {
                MoveTo(entity, nextPosition, tryMove);
                entity.Velocity.X = 0;
                stepDelta.X = 0;
                return true;
            }
        }

        return false;
    }

    private void MoveXY(Entity entity)
    {
        if (entity.Velocity.XY == Vec2D.Zero)
            return;

        TryMoveXY(entity, (entity.Position + entity.Velocity).XY);
        if (entity.ShouldApplyFriction())
            ApplyFriction(entity);
        StopXYMovementIfSmall(entity);
    }

    private static double GetFrictionFromSectors(Entity entity)
    {
        if (entity.Flags.NoClip)
            return Constants.DefaultFriction;

        double lowestFriction = double.MaxValue;
        foreach (var sector in entity.IntersectSectors)
        {
            if (entity.Position.Z != sector.ToFloorZ(entity.Position))
                continue;

            if (sector.Friction < lowestFriction)
                lowestFriction = sector.Friction;
        }

        if (lowestFriction == double.MaxValue)
            return Constants.DefaultFriction;

        return lowestFriction;
    }

    private void MoveZ(Entity entity)
    {
        if (m_world.WorldState == WorldState.Exit)
            return;

        entity.BlockingEntity = null;
        entity.BlockingLine = null;
        entity.BlockingSectorPlane = null;

        if (entity.Flags.NoGravity && entity.ShouldApplyFriction())
            entity.Velocity.Z *= Constants.DefaultFriction;
        if (entity.ShouldApplyGravity())
            entity.Velocity.Z -= m_world.Gravity * entity.Properties.Gravity;

        double floatZ = entity.GetEnemyFloatMove();
        if (entity.Velocity.Z == 0 && floatZ == 0)
            return;

        Vec3D previousVelocity = entity.Velocity;
        double newZ = entity.Position.Z + entity.Velocity.Z + floatZ;
        entity.SetZ(newZ, false);

        // Passing MoveLinked emulates some vanilla functionality where things are not checked against linked sectors when they haven't moved
        ClampBetweenFloorAndCeiling(entity, entity.MoveLinked);

        if (entity.IsBlocked())
            m_world.HandleEntityHit(entity, previousVelocity, null);

        if (entity.OverEntity != null)
            HandleStackedEntityPhysics(entity);
    }
}
