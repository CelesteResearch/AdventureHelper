using Celeste.Core.Components;
using Celeste.Graphics.Rendering;
// SPDX-License-Identifier: MIT
using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Visibility;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.AdventureHelper.Entities.Platforms.ZipMovers
{
    public sealed class LinkedZipMoverPathRenderer : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly SourceSolidEntityState targetZip;
        private readonly Func<float> percent;
        private readonly Vector2 from;
        private readonly Vector2 to;
        private readonly Vector2 sparkAdd;
        private readonly float sparkDirFromA;
        private readonly float sparkDirFromB;
        private readonly float sparkDirToA;
        private readonly float sparkDirToB;
        private readonly AtlasEntry cog;
        private readonly PackedColor ropeColor;
        private readonly PackedColor ropeLightColor;

        public LinkedZipMoverPathRenderer(ISourceEngineContext engine, SourceSolidEntityState targetZip,
            Vector2 start, Vector2 target, string spritePath, Func<float> percent,
            PackedColor ropeColor, PackedColor ropeLightColor, string defaultPath = LinkedZipMoverNoReturn.DefaultPath)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            this.targetZip = targetZip;
            this.percent = percent;
            this.ropeColor = ropeColor;
            this.ropeLightColor = ropeLightColor;
            Depth = 5000;
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            cog = atlas.Has(spritePath + "/cog")
                ? atlas[spritePath + "/cog"]
                : atlas[defaultPath + "/cog"];
            from = SourceVector2Math.Add(start, new Vector2(targetZip.Width / 2f, targetZip.Height / 2f));
            to = SourceVector2Math.Add(target, new Vector2(targetZip.Width / 2f, targetZip.Height / 2f));
            sparkAdd = Vector2Geometry.Perpendicular(((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(
                SourceVector2Math.Subtract(from, to), 5f));
            float angle = Vector2Geometry.Angle(SourceVector2Math.Subtract(from, to));
            sparkDirFromA = angle + .3926991f;
            sparkDirFromB = angle - .3926991f;
            sparkDirToA = angle + 3.14159274f - .3926991f;
            sparkDirToB = angle + 3.14159274f + .3926991f;
        }

        public void CreateSparks() => ZipMoverDrawing.Sparks(this, engine, from, to, sparkAdd,
            sparkDirFromA, sparkDirFromB, sparkDirToA, sparkDirToB);

        public override void Render(ISourceRenderContext context)
        {
            float left = Math.Min(from.X, to.X), top = Math.Min(from.Y, to.Y);
            if (EverestCullHelper.IsRectangleVisible(engine, left, top, Math.Abs(from.X - to.X),
                    Math.Abs(from.Y - to.Y)))
            {
                ZipMoverDrawing.Ropes(context, this, engine, from, to, cog, percent(), ropeColor, ropeLightColor,
                    Vector2.UnitY, PackedColor.Black);
                ZipMoverDrawing.Ropes(context, this, engine, from, to, cog, percent(), ropeColor, ropeLightColor,
                    Vector2.Zero);
            }

            SourcePrimitiveDrawing.Rect((ISourceDrawScope)context,
                new IntRect((int)(targetZip.X - 1f), (int)(targetZip.Y - 1f), (int)targetZip.Width + 2,
                    (int)targetZip.Height + 2),
                PackedColor.Black);
        }
    }
}
