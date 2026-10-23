// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Components.Timing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Visibility;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Mods.AdventureHelper.Modules;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;

namespace Celeste.Mods.AdventureHelper.Entities.Platforms.ZipMovers
{
    public abstract class LinkedZipMoverState : SourceSolidEntityState
    {
        protected readonly string defaultPath;
        protected readonly bool returning;
        protected readonly ISourceEngineContext engine;
        protected readonly LinkedZipMoverSounds sounds;
        protected readonly Vector2 start;
        protected readonly Vector2 target;
        protected readonly float speedMultiplier;
        protected readonly string syncFlagCode;
        protected readonly string spritePath;
        protected readonly AtlasEntry[,] edges = new AtlasEntry[3, 3];
        protected readonly List<AtlasEntry> innerCogs;
        protected readonly AtlasEntry temp = new();
        protected readonly Sprite streetlight;
        protected readonly BloomPointState bloom;
        protected readonly PackedColor ropeColor;
        protected readonly PackedColor ropeLightColor;
        protected LinkedZipMoverPathRenderer pathRenderer;
        protected float percent;

        public string ColorCode { get; }

        protected LinkedZipMoverState(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorld world, LinkedZipMoverSounds sounds, string defaultPath, bool returning)
            : base(data.Position + offset, data.Width, data.Height, false, world)
        {
            this.defaultPath = defaultPath;
            this.returning = returning;
            this.engine = engine;
            this.sounds = sounds;
            start = Position;
            target = data.Nodes[0] + offset;
            speedMultiplier = data.Float("speedMultiplier", 1f);
            spritePath = data.Attr("spritePath", defaultPath);
            // Original Trim return values are discarded: preserve exact path lookup behavior.
            string resourcePath = spritePath == string.Empty ? defaultPath : spritePath;
            ColorCode = data.Attr("colorCode", "000000");
            syncFlagCode = $"ZipMoverSync:{ColorCode}";
            ropeColor = PackedColor.FromHex(ColorCode);
            ropeLightColor = ropeColor * 1.1f;
            Depth = -9999;
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            innerCogs = atlas.GetAtlasSubtextures(resourcePath + "/innercog");
            if (innerCogs.Count == 0) innerCogs = atlas.GetAtlasSubtextures(defaultPath + "/innercog");
            Add(new SimulationCoroutine(engine, Sequence()));
            Add(new LightOccludeState());
            var light = atlas.GetAtlasSubtexturesAt(resourcePath + "/light", 0);
            Add(streetlight = new Sprite(engine, atlas,
                light?.AtlasPath is not ("__fallback" or null) ? resourcePath + "/light" : "objects/zipmover/light"));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames");
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position =
                new Vector2(Width / 2f - streetlight.Width / 2f,
                    returning ? 0f : Height / 2f - streetlight.Height / 2f);
            Add(bloom = new BloomPointState(1f, 6f));
            bloom.Position = new Vector2(Width / 2f, returning ? 4f : Height / 2f - streetlight.Height / 2f + 3f);
            string path = atlas.Has(resourcePath + "/block") ? resourcePath : defaultPath;
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                edges[i, j] = atlas[path + "/block"].GetSubtexture(i * 8, j * 8, 8, 8);
            SurfaceSoundIndex = 7;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(pathRenderer = new LinkedZipMoverPathRenderer(engine,
                this, start, target, spritePath, () => percent, ropeColor, ropeLightColor, defaultPath));
        }

        public override void Removed(object scene)
        {
            ((ISourceScene)scene).SourceScene.RemoveEntity(pathRenderer);
            pathRenderer = null;
            SyncFlag = false;
            sounds.Stop(ColorCode,
                returning ? LinkedZipMoverSoundType.Returning : LinkedZipMoverSoundType.NonReturning);
            base.Removed(scene);
        }

        public override void Update()
        {
            base.Update();
            bloom.Y = (returning ? 0f : Height / 2f - streetlight.Height / 2f) + streetlight.CurrentAnimationFrame * 3;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (!EverestCullHelper.IsRectangleVisible(engine, X, Y, Width, Height)) return;
            var position = Position;
            Position = SourceVector2Math.Add(Position, Shake);
            ZipMoverDrawing.Body(context, this, percent, innerCogs, edges, temp, 0f);
            base.Render(context);
            Position = position;
        }

        protected abstract IEnumerator Sequence();

        protected bool HasActivationRider() => HasPlayerRider() || sounds.CheckAdditionalRider(this, ColorCode);

        protected void StopSequenceSound()
        {
            sounds.NotifyBeforeStopSound(this, ColorCode);
            sounds.Stop(ColorCode,
                returning ? LinkedZipMoverSoundType.Returning : LinkedZipMoverSoundType.NonReturning);
        }

        protected bool SyncFlag
        {
            get => ((OriginalLevelSceneRoot)Scene).Session.GetFlag(syncFlagCode);
            set => ((OriginalLevelSceneRoot)Scene).Session.SetFlag(syncFlagCode, value);
        }
    }
}
