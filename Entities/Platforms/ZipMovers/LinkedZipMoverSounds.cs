// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Celeste.Core.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.AdventureHelper.Entities.Platforms.ZipMovers
{
    public sealed class LinkedZipMoverSounds : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, LinkedZipMoverSounds> owners = new();
        private readonly ISourceEngineContext engine;

        private Func<SourceSolidEntityState, string, bool>[] additionalRiderChecks =
            Array.Empty<Func<SourceSolidEntityState, string, bool>>();

        public event Action<SourceSolidEntityState, string> BeforeStopSound;

        public event Func<SourceSolidEntityState, string, bool> AdditionalRiderCheck
        {
            add
            {
                if (value != null)
                    additionalRiderChecks = additionalRiderChecks
                        .Concat(value.GetInvocationList().Cast<Func<SourceSolidEntityState, string, bool>>()).ToArray();
            }
            remove
            {
                if (value == null) return;
                var current = additionalRiderChecks.ToList();
                var remove = value.GetInvocationList().Cast<Func<SourceSolidEntityState, string, bool>>().ToArray();
                for (int at = current.Count - remove.Length; at >= 0; at--)
                {
                    if (!current.Skip(at).Take(remove.Length).SequenceEqual(remove)) continue;
                    current.RemoveRange(at, remove.Length);
                    break;
                }

                additionalRiderChecks = current.ToArray();
            }
        }

        public static LinkedZipMoverSounds For(ISourceEngineContext engine) =>
            owners.GetValue(engine, static context => new LinkedZipMoverSounds(context));

        public bool CheckAdditionalRider(SourceSolidEntityState mover, string colorCode)
        {
            foreach (var handler in additionalRiderChecks)
                if (handler(mover, colorCode))
                    return true;
            return false;
        }

        public void NotifyBeforeStopSound(SourceSolidEntityState mover, string colorCode) =>
            BeforeStopSound?.Invoke(mover, colorCode);

        private readonly Dictionary<string, SoundSourceComponentState> sounds = new();

        public LinkedZipMoverSounds(ISourceEngineContext engine) => this.engine = engine;

        public void Play(string colorCode, SourceSolidEntityState block) =>
            Play(colorCode, LinkedZipMoverSoundType.NonReturning, block);

        public void Play(string colorCode, LinkedZipMoverSoundType type, SourceSolidEntityState block)
        {
            string key = colorCode + "-" + type;
            if (sounds.TryGetValue(key, out var current))
            {
                if (type == LinkedZipMoverSoundType.Returning && !current.Playing)
                    current.Play("event:/game/01_forsaken_city/zip_mover");
                return;
            }

            var player = ((ISourceScene)block.Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player == null) return;
            var source = new SoundSourceComponentState(engine) { Position = player.Position };
            sounds.Add(key, source);
            source.Play("event:/game/01_forsaken_city/zip_mover");
        }

        public void Stop(string colorCode) => Stop(colorCode, LinkedZipMoverSoundType.NonReturning);

        public void Stop(string colorCode, LinkedZipMoverSoundType type)
        {
            string key = colorCode + "-" + type;
            if (!sounds.TryGetValue(key, out var source)) return;
            sounds.Remove(key);
            source.Stop();
        }

        public void Dispose()
        {
            foreach (var source in sounds.Values) source.Stop();
            sounds.Clear();
            additionalRiderChecks = Array.Empty<Func<SourceSolidEntityState, string, bool>>();
            BeforeStopSound = null;
        }
    }
}
