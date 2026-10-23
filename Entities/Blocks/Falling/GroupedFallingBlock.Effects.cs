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
        private static readonly string sourcePlayerRegistryKey =
            typeof(global::Celeste.Simulation.RoomPlayerSession).FullName;

        private bool PlayerFallCheck()
        {
            foreach (GroupedFallingBlock block in Group)
            {
                if (block._climbFall)
                {
                    if (block.HasPlayerRider())
                    {
                        return true;
                    }
                }
                else
                {
                    if (block.HasPlayerOnTop())
                    {
                        return true;
                    }
                }
            }

            foreach (SourceJumpThruEntityState jp in Jumpthrus)
            {
                if (jp.HasPlayerRider())
                {
                    return true;
                }
            }

            return false;
        }

        private bool PlayerWaitCheck()
        {
            if (Triggered)
            {
                return true;
            }

            if (PlayerFallCheck())
            {
                return true;
            }

            if (_climbFall)
            {
                foreach (GroupedFallingBlock block in Group)
                {
                    if (!block.CollideCheck(sourcePlayerRegistryKey, Position - Vector2.UnitX))
                    {
                        if (CollideCheck(sourcePlayerRegistryKey, Position + Vector2.UnitX))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void LandParticles()
        {
            foreach (GroupedFallingBlock block in Group)
            {
                for (int i = 2; i <= block.Width; i += 4)
                {
                    foreach (SourceSolidEntityState solid in Registry.GetTrackedEntities("Celeste.Solid"))
                    {
                        if (solid is GroupedFallingBlock)
                        {
                            continue;
                        }

                        if (SourceEntityCollision.CheckAt(block, solid, block.Position + new Vector2(i, 3f)))
                        {
                            Level.ParticlesFG.Emit(Process.ParticleTypes.FallingBlock.P_FallDustA, 1,
                                new Vector2(block.X + i, block.Bottom), Vector2.One * 4f, (float)-Math.PI / 2f);
                            float direction = (!(i < block.Width / 2f)) ? 0f : ((float)Math.PI);
                            Level.ParticlesFG.Emit(Process.ParticleTypes.FallingBlock.P_LandDust, 1,
                                new Vector2(block.X + i, block.Bottom), Vector2.One * 4f, direction);
                        }
                    }
                }
            }
        }

        private void ShakeSfx()
        {
            Vector2 center = GetGroupCenter();
            if (_tileType == '3')
            {
                Process.AudioSession.Play("event:/game/01_forsaken_city/fallblock_ice_shake", center,
                    Process.SaveContext);
            }
            else if (_tileType == '9')
            {
                Process.AudioSession.Play("event:/game/03_resort/fallblock_wood_shake", center, Process.SaveContext);
            }
            else if (_tileType == 'g')
            {
                Process.AudioSession.Play("event:/game/06_reflection/fallblock_boss_shake", center,
                    Process.SaveContext);
            }
            else
            {
                Process.AudioSession.Play("event:/game/general/fallblock_shake", center, Process.SaveContext);
            }
        }

        private void ImpactSfx()
        {
            Vector2 bottomCenter = GetGroupBottomCenter();
            if (_tileType == '3')
            {
                Process.AudioSession.Play("event:/game/01_forsaken_city/fallblock_ice_impact", bottomCenter,
                    Process.SaveContext);
            }
            else if (_tileType == '9')
            {
                Process.AudioSession.Play("event:/game/03_resort/fallblock_wood_impact", bottomCenter,
                    Process.SaveContext);
            }
            else if (_tileType == 'g')
            {
                Process.AudioSession.Play("event:/game/06_reflection/fallblock_boss_impact", bottomCenter,
                    Process.SaveContext);
            }
            else
            {
                Process.AudioSession.Play("event:/game/general/fallblock_impact", bottomCenter, Process.SaveContext);
            }
        }

        private Vector2 GetGroupCenter()
        {
            float area = 0;
            float sumX = 0;
            float sumY = 0;
            foreach (GroupedFallingBlock block in Group)
            {
                float blockArea = block.Width * block.Height;
                float distX = block.CenterX - CenterX;
                float distY = block.CenterY - CenterY;
                area += blockArea;
                sumX += distX * blockArea;
                sumY += distY * blockArea;
            }

            return Center + new Vector2(sumX / area, sumY / area);
        }

        private Vector2 GetGroupBottomCenter()
        {
            float area = 0;
            float sumX = 0;
            float maxY = float.MinValue;
            foreach (GroupedFallingBlock block in Group)
            {
                float blockArea = block.Width * block.Height;
                float distX = block.CenterX - CenterX;
                area += blockArea;
                sumX += distX * blockArea;
                if (block.Bottom > maxY)
                {
                    maxY = block.Bottom;
                }
            }

            return new Vector2(CenterX + (sumX / area), maxY);
        }
    }
}
