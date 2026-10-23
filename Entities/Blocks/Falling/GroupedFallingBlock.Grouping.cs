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
    public partial class GroupedFallingBlock : SourceSolidEntityState
    {
        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (!HasGroup)
            {
                MasterOfGroup = true;
                Group = new List<GroupedFallingBlock>();
                Jumpthrus = new List<SourceJumpThruEntityState>();
                GroupBoundsMin = new IntPoint((int)X, (int)Y);
                GroupBoundsMax = new IntPoint((int)Right, (int)Bottom);
                AddToGroupAndFindChildren(this);
                IntRect rectangle = new(GroupBoundsMin.X / 8, GroupBoundsMin.Y / 8,
                    ((GroupBoundsMax.X - GroupBoundsMin.X) / 8) + 1, ((GroupBoundsMax.Y - GroupBoundsMin.Y) / 8) + 1);
                VirtualMap<char> virtualMap = new(rectangle.Width, rectangle.Height, '0');
                foreach (GroupedFallingBlock item in Group)
                {
                    int num = (int)(item.X / 8f) - rectangle.X;
                    int num2 = (int)(item.Y / 8f) - rectangle.Y;
                    int num3 = (int)(item.Width / 8f);
                    int num4 = (int)(item.Height / 8f);
                    for (int i = num; i < num + num3; i++)
                    {
                        for (int j = num2; j < num2 + num4; j++)
                        {
                            virtualMap[i, j] = _tileType;
                        }
                    }
                }

                _tiles = ((ISourceAutotilingResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                    .ForegroundAutotiler.GenerateMap(virtualMap, new AutotileBehavior
                    {
                        EdgesExtend = false,
                        EdgesIgnoreOutOfLevel = false,
                        PaddingIgnoreOutOfLevel = false
                    }).TileGrid;
                _tiles.Position = new Vector2(GroupBoundsMin.X - X, GroupBoundsMin.Y - Y);
                Add(_tiles);
            }

            if (MasterOfGroup)
            {
                Add(new SimulationCoroutine(engine, Sequence()));
                Group.Sort((block, otherBlock) => { return otherBlock.Bottom.CompareTo(block.Bottom); });
            }
        }

        public void Trigger()
        {
            if (MasterOfGroup)
            {
                Triggered = true;
            }
            else
            {
                _master.Triggered = true;
            }
        }

        private void AddToGroupAndFindChildren(GroupedFallingBlock from)
        {
            if (from.X < GroupBoundsMin.X)
            {
                GroupBoundsMin.X = (int)from.X;
            }

            if (from.Y < GroupBoundsMin.Y)
            {
                GroupBoundsMin.Y = (int)from.Y;
            }

            if (from.Right > GroupBoundsMax.X)
            {
                GroupBoundsMax.X = (int)from.Right;
            }

            if (from.Bottom > GroupBoundsMax.Y)
            {
                GroupBoundsMax.Y = (int)from.Bottom;
            }

            from.HasGroup = true;
            Group.Add(from);
            if (from != this)
            {
                from._master = this;
            }

            foreach (SourceJumpThruEntityState item in SourceSceneCollision.AllRect<SourceJumpThruEntityState>(Registry,
                         "Celeste.JumpThru",
                         new IntRect((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height)))
            {
                if (!Jumpthrus.Contains(item))
                {
                    AddJumpThru(item);
                }
            }

            foreach (SourceJumpThruEntityState item2 in SourceSceneCollision.AllRect<SourceJumpThruEntityState>(
                         Registry, "Celeste.JumpThru",
                         new IntRect((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2)))
            {
                if (!Jumpthrus.Contains(item2))
                {
                    AddJumpThru(item2);
                }
            }

            foreach (GroupedFallingBlock entity in Registry.GetTrackedEntities(typeof(GroupedFallingBlock)))
            {
                if (!entity.HasGroup && entity._tileType == _tileType &&
                    (SourceSceneCollision.CheckRect(
                         new IntRect((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height), entity) ||
                     SourceSceneCollision.CheckRect(
                         new IntRect((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2), entity)))
                {
                    AddToGroupAndFindChildren(entity);
                }
            }
        }

        private void AddJumpThru(SourceJumpThruEntityState jp)
        {
            Jumpthrus.Add(jp);
            foreach (GroupedFallingBlock entity in Registry.GetTrackedEntities(typeof(GroupedFallingBlock)))
            {
                if (!entity.HasGroup && entity._tileType == _tileType &&
                    SourceSceneCollision.CheckRect(
                        new IntRect((int)jp.X - 1, (int)jp.Y, (int)jp.Width + 2, (int)jp.Height), entity))
                {
                    AddToGroupAndFindChildren(entity);
                }
            }
        }

        public override void OnStaticMoverTrigger(SourceStaticMoverState sm)
        {
            if (MasterOfGroup)
            {
                Triggered = true;
            }
            else
            {
                _master.Triggered = true;
            }
        }

        public override void OnShake(Vector2 amount)
        {
            if (MasterOfGroup)
            {
                base.OnShake(amount);
                _tiles.Position += amount;
                foreach (SourceJumpThruEntityState jumpthru in Jumpthrus)
                {
                    foreach (var image in jumpthru.Components.OfType<SpriteImage>())
                    {
                        image.Position += amount;
                    }
                }
            }
        }
    }
}
