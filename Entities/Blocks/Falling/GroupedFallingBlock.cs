// Copyright (c) Communal Helper Team. Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.AdventureHelper.Entities.Blocks.Falling
{
    [SceneTracked(false)]
    [CustomEntity("AdventureHelper/GroupedFallingBlock")]
    [RuntimeTypeAlias("Celeste.Mod.AdventureHelper.Entities.GroupedFallingBlock")]
    public partial class GroupedFallingBlock : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;
        private OriginalLevelSceneRoot Level => (OriginalLevelSceneRoot)Scene;
        private IOriginalGameplayProcess Process => (IOriginalGameplayProcess)engine;
        private SceneEntityRegistry Registry => ((ISourceScene)Scene).SourceScene.Entities;

        public static GroupedFallingBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(level.EngineContext, data, offset, level.SolidWorldFactory);

        public List<GroupedFallingBlock> Group;
        public List<SourceJumpThruEntityState> Jumpthrus;
        public IntPoint GroupBoundsMin;
        public IntPoint GroupBoundsMax;
        public bool Triggered;
        public float FallDelay;

        private readonly char _tileType;
        private readonly bool _climbFall;
        private TileGrid _tiles;
        private GroupedFallingBlock _master;

        public GroupedFallingBlock(ISourceEngineContext engine, Vector2 position, float width, float height,
            char tileType, bool climbFall, ISourceSolidWorld world)
            : base(position, width, height, true, world)
        {
            this.engine = engine;
            _climbFall = climbFall;
            _tileType = tileType;
            Depth = -9000;
            Add(new LightOccludeState());
            SurfaceSoundIndex = ((IOriginalTileSurfaceEnvironment)engine).SurfaceIndices.TileToIndex[tileType];
            Add(new TileInterceptorState(_tiles, highPriority: false));
        }

        public GroupedFallingBlock(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorldFactory worlds)
            : this(engine, data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3'),
                data.Bool("climbFall", true), worlds.Create(data.Position + offset, true))
        {
        }

        public bool HasStartedFalling { get; private set; }
        public bool HasGroup { get; private set; }
        public bool MasterOfGroup { get; private set; }
        public Vector2 GroupPosition => new(GroupBoundsMin.X, GroupBoundsMin.Y);
    }
}
