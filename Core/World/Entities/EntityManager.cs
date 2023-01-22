using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Entities.Spawn;
using Helion.World.Geometry.Sectors;
using Helion.World.Sound;
using NLog;
using Helion.World.Stats;
using Helion.Resources.Archives.Entries;

namespace Helion.World.Entities;

public class EntityManager : IDisposable
{
    public class EntityModelPair
    {
        public EntityModelPair(EntityModel model, Entity entity)
        {
            Model = model;
            Entity = entity;
        }

        public EntityModel Model { get; set; }
        public Entity Entity { get; set; }
    }

    public class WorldModelPopulateResult
    {
        public WorldModelPopulateResult(IList<Player> players, Dictionary<int, EntityModelPair> entities)
        {
            Players = players;
            Entities = entities;
        }

        public readonly IList<Player> Players;
        public readonly Dictionary<int, EntityModelPair> Entities;
    }

    public const int NoTid = 0;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly LinkableList<Entity> Entities = new();
    public readonly LinkedList<Entity> TeleportSpots = new();
    public readonly SpawnLocations SpawnLocations;
    public readonly IWorld World;

    private readonly WorldSoundManager m_soundManager;
    private readonly Dictionary<int, ISet<Entity>> TidToEntity = new();

    public readonly EntityDefinitionComposer DefinitionComposer;
    public readonly List<Player> Players = new();
    public readonly List<Player> VoodooDolls = new();
    private readonly LookupArray<Player?> RealPlayersByNumber = new();

    private int m_id;

    public int MaxId => m_id;

    public EntityManager(IWorld world)
    {
        World = world;
        m_soundManager = world.SoundManager;
        SpawnLocations = new SpawnLocations(world);
        DefinitionComposer = world.ArchiveCollection.EntityDefinitionComposer;
    }

    public static bool ZHeightSet(double z)
    {
        return z != Fixed.Lowest().ToDouble() && z != 0.0;
    }

    public IEnumerable<Entity> FindByTid(int tid)
    {
        return TidToEntity.TryGetValue(tid, out ISet<Entity>? entities) ? entities : Enumerable.Empty<Entity>();
    }

    public Entity? FindById(int id)
    {
        LinkableNode<Entity>? node = Entities.Head;
        while (node != null)
        {
            if (node.Value.Id == id)
                return node.Value;
            node = node.Next;
        }
        return null;
    }

    public Entity? Create(string className, in Vec3D pos, bool init = false)
    {
        var def = DefinitionComposer.GetByName(className);
        if (def != null)
            return Create(def, pos, 0.0, 0.0, 0, init: init);
        return null;
    }

    public Entity Create(EntityDefinition definition, Vec3D position, double zHeight, double angle, int tid, bool init = false,
        bool executeStateFunctions = true)
    {
        int id = m_id++;
        Sector sector = World.BspTree.ToSector(position);
        position.Z = GetPositionZ(sector, in position, zHeight);
        Entity entity = World.DataCache.GetEntity(id, tid, definition, position, angle, sector, World);

        if (entity.Definition.Properties.FastSpeed > 0 && World.IsFastMonsters)
            entity.Properties.MonsterMovementSpeed = entity.Definition.Properties.FastSpeed;

        // This only needs to happen on map population
        if (init && !ZHeightSet(zHeight))
        {
            entity.Position.Z = entity.Sector.ToFloorZ(position);
            entity.PrevPosition = entity.Position;
        }

        FinishCreatingEntity(entity, zHeight, executeStateFunctions);
        return entity;
    }

    public void Destroy(Entity entity)
    {
        if (entity.IsDisposed)
            return;

        // TODO: Remove from spawns if it is a spawn.

        // To avoid more object allocation and deallocation, I'm going to
        // leave empty sets in the map in case they get populated again.
        // Most maps wouldn't even approach a number that high for us to
        // worry about. If it ever becomes an issue, then we can add a line
        // of code that removes empty sets here as well.
        if (TidToEntity.TryGetValue(entity.ThingId, out ISet<Entity>? entities))
            entities.Remove(entity);

        if (entity.Flags.IsTeleportSpot)
            TeleportSpots.Remove(entity);

        entity.Dispose();
    }

    public Player CreatePlayer(int playerIndex, Entity spawnSpot, bool isVoodooDoll)
    {
        Player player;
        EntityDefinition? playerDefinition = DefinitionComposer.GetByName(Constants.PlayerClass);
        if (playerDefinition == null)
        {
            Log.Error("Missing player definition class {0}, cannot create player {1}", Constants.PlayerClass, playerIndex);
            throw new HelionException("Missing the default player class, should never happen");
        }

        player = CreatePlayerEntity(playerIndex, playerDefinition, spawnSpot.Position, 0.0, spawnSpot.AngleRadians);
        player.IsVooDooDoll = isVoodooDoll;

        if (isVoodooDoll)
        {
            VoodooDolls.Add(player);
            return player;
        }

        AddRealPlayer(player);
        return player;
    }

    private void AddRealPlayer(Player player)
    {
        RealPlayersByNumber.Set(player.PlayerNumber, player);
        Players.Add(player);
    }

    public void PopulateFrom(IMap map, LevelStats levelStats)
    {
        List<Entity> relinkEntities = new();

        foreach (IThing mapThing in map.GetThings())
        {
            if (!ShouldSpawn(mapThing))
                continue;

            EntityDefinition? definition = DefinitionComposer.GetByID(mapThing.EditorNumber);
            if (definition == null)
            {
                Log.Warn("Cannot find entity by editor number {0} at {1}", mapThing.EditorNumber, mapThing.Position.XY);
                continue;
            }

            if (!definition.States.Labels.ContainsKey(Constants.FrameStates.Spawn))
                continue;

            if (World.Config.Game.NoMonsters && definition.Flags.CountKill)
                continue;

            if (definition.Flags.CountKill && !definition.Flags.Friendly)
                levelStats.TotalMonsters++;
            if (definition.Flags.CountItem)
                levelStats.TotalItems++;

            double angleRadians = MathHelper.ToRadians(mapThing.Angle);
            Vec3D position = mapThing.Position.Double;
            // position.Z is the potential zHeight variable, not the actual z position. We need to pass it to Create to ensure the zHeight is set
            Entity entity = Create(definition, position, position.Z, angleRadians, mapThing.ThingId, init: true, executeStateFunctions: false);
            if (mapThing.Flags.Ambush)
                entity.Flags.Ambush = mapThing.Flags.Ambush;

            if (entity.FrameState.Frame.Ticks > 0)
                entity.FrameState.SetTics((World.Random.NextByte() % entity.FrameState.Frame.Ticks) + 1);

            if (!entity.Flags.ActLikeBridge && ZHeightSet(position.Z))
                relinkEntities.Add(entity);
            PostProcessEntity(entity);
        }

        //Relink entities with a z-height only, this way they can properly stack with other things in the map now that everything exists
        for (int i = 0; i < relinkEntities.Count; i++)
        {
            relinkEntities[i].UnlinkFromWorld();
            World.Link(relinkEntities[i]);
            relinkEntities[i].PrevPosition = relinkEntities[i].Position;
        }
    }

    public WorldModelPopulateResult PopulateFrom(WorldModel worldModel)
    {
        List<Player> players = new();
        Dictionary<int, EntityModelPair> entities = new();
        for (int i = 0; i < worldModel.Entities.Count; i++)
        {
            var entityModel = worldModel.Entities[i];
            var definition = DefinitionComposer.GetByName(entityModel.Name);
            if (definition == null)
            {
                Log.Error($"Failed to find entity definition for:{entityModel.Name}");
                continue;
            }

            var entity = World.DataCache.GetEntity(entityModel, definition, World);
            Entities.Add(entity.EntityListNode);

            entities.Add(entityModel.Id, new(entityModel, entity));
        }

        for (int i = 0; i < worldModel.Players.Count; i++)
        {
            bool isVoodooDoll = players.Any(x => x.PlayerNumber == worldModel.Players[i].Number);
            Player? player = CreatePlayerFromModel(worldModel.Players[i], entities, isVoodooDoll);
            if (player == null)
            {
                Log.Error($"Failed to create player {worldModel.Players[i].Name}.");
                continue;
            }

            players.Add(player);
        }

        m_id = entities.Keys.Max() + 1;

        for (int i = 0; i < worldModel.Entities.Count; i++)
        {
            var entityModel = worldModel.Entities[i];
            if (!entities.TryGetValue(entityModel.Id, out EntityModelPair? entity))
                continue;

            if (entityModel.Owner.HasValue)
            {
                entities.TryGetValue(entityModel.Owner.Value, out var entityOwner);
                if (entityOwner != null)
                    entity.Entity.SetOwner(entityOwner.Entity);
            }

            if (entityModel.Target.HasValue)
            {
                entities.TryGetValue(entityModel.Target.Value, out var entityTarget);
                if (entityTarget != null)
                    entity.Entity.SetTarget(entityTarget.Entity);
            }

            if (entityModel.Tracer.HasValue)
            {
                entities.TryGetValue(entityModel.Tracer.Value, out var tracerTarget);
                if (tracerTarget != null)
                    entity.Entity.SetTracer(tracerTarget.Entity);
            }
        }

        return new WorldModelPopulateResult(players, entities);
    }

    public void FinalizeFromWorldLoad(WorldModelPopulateResult result, Entity entity)
    {
        World.Link(entity);

        if (result.Entities.TryGetValue(entity.Id, out var pair))
        {
            entity.HighestFloorSector = GetValidSector(World, entity.Sector, pair.Model.HighSec);
            entity.LowestCeilingSector = GetValidSector(World, entity.Sector, pair.Model.LowSec);
            entity.HighestFloorZ = entity.HighestFloorSector.ToFloorZ(entity.Position);
            entity.LowestCeilingZ = entity.LowestCeilingSector.ToCeilingZ(entity.Position);

            entity.HighestFloorObject = GetBoundingObject(result, entity.HighestFloorSector, pair.Model.HighEntity);
            entity.LowestCeilingObject = GetBoundingObject(result, entity.LowestCeilingSector, pair.Model.LowEntity);
        }

        PostProcessEntity(entity);
        FinalizeEntity(entity);
    }

    public Player? GetRealPlayer(int playerNumber)
    {
        RealPlayersByNumber.TryGetValue(playerNumber, out var player);
        return player;
    }

    private static object GetBoundingObject(WorldModelPopulateResult result, Sector sector, int? entityId)
    {
        if (!entityId.HasValue)
            return sector;

        if (!result.Entities.TryGetValue(entityId.Value, out var pair))
            return false;

        return pair.Entity;
    }

    private static Sector GetValidSector(IWorld world, Sector sector, int? id)
    {
        if (!id.HasValue || !world.IsSectorIdValid(id.Value))
            return sector;

        return world.Sectors[id.Value];
    }

    private Player? CreatePlayerFromModel(PlayerModel playerModel, Dictionary<int, EntityModelPair> entities, bool isVoodooDoll)
    {
        var playerDefinition = DefinitionComposer.GetByName(playerModel.Name);
        if (playerDefinition != null)
        {
            Player player = new(playerModel, entities, playerDefinition, World);
            player.IsVooDooDoll = isVoodooDoll;

            Entities.Add(player.EntityListNode);
            entities.Add(player.Id, new(playerModel, player));

            if (isVoodooDoll)
            {
                VoodooDolls.Add(player);
                return player;
            }

            AddRealPlayer(player);
            return player;
        }

        return null;
    }

    private bool ShouldSpawn(IThing mapThing)
    {
        // Ignore difficulty on spawns...
        if ((mapThing.EditorNumber > 0 && mapThing.EditorNumber < 5) || mapThing.EditorNumber == 1)
            return true;

        // TODO: These should be offloaded into SinglePlayerWorld...
        if (mapThing.Flags.MultiPlayer)
            return false;

        return (SkillLevel)World.SkillDefinition.SpawnFilter switch
        {
            SkillLevel.VeryEasy or SkillLevel.Easy => mapThing.Flags.Easy,
            SkillLevel.Medium => mapThing.Flags.Medium,
            SkillLevel.Hard or SkillLevel.Nightmare => mapThing.Flags.Hard,
            _ => false,
        };
    }

    private static double GetPositionZ(Sector sector, in Vec3D position, double zHeight)
    {
        if (ZHeightSet(zHeight))
            return zHeight + sector.ToFloorZ(position);

        return position.Z;
    }

    private static void FinalizeEntity(Entity entity, double zHeight = 0)
    {
        if (entity.Flags.SpawnCeiling)
        {
            double offset = ZHeightSet(zHeight) ? -zHeight : 0;
            entity.Position.Z = entity.Sector.ToCeilingZ(entity.Position) - entity.Height + offset;
        }

        entity.CheckOnGround();
        entity.ResetInterpolation();
    }

    private void FinishCreatingEntity(Entity entity, double zHeight, bool executeStateFunctions)
    {
        Entities.Add(entity.EntityListNode);

        World.Link(entity);
        FinalizeEntity(entity, zHeight);

        entity.SpawnPoint = entity.Position;
        // Vanilla did not execute action functions on creation, it just set the state
        // Action functions will not execute until Tick() is called
        if (entity.Definition.SpawnState != null)
            entity.FrameState.SetFrameIndexNoAction(entity.Definition.SpawnState.Value);
    }

    private void PostProcessEntity(Entity entity)
    {
        SpawnLocations.AddPossibleSpawnLocation(entity);

        if (entity.ThingId != NoTid)
        {
            if (TidToEntity.TryGetValue(entity.ThingId, out ISet<Entity>? entities))
                entities.Add(entity);
            else
                TidToEntity.Add(entity.ThingId, new HashSet<Entity> { entity });
        }

        if (entity.Flags.IsTeleportSpot)
            TeleportSpots.AddLast(entity);
    }

    private Player CreatePlayerEntity(int playerNumber, EntityDefinition definition, Vec3D position, double zHeight, double angle)
    {
        int id = m_id++;
        Sector sector = World.BspTree.ToSector(position);
        position.Z = GetPositionZ(sector, position, zHeight);
        Player player = new(id, 0, definition, position, angle, sector, World, playerNumber);

        var armor = DefinitionComposer.GetByName(Inventory.ArmorClassName);
        if (armor != null)
            player.Inventory.Add(armor, 0);

        FinishCreatingEntity(player, zHeight, false);

        return player;
    }

    public void Dispose()
    {
        LinkableNode<Entity>? node = Entities.Head;
        LinkableNode<Entity>? nextNode;
        while (node != null)
        {
            nextNode = node.Next;
            node.Value.Dispose();
            node = nextNode;
        }

        GC.SuppressFinalize(this);
    }
}
