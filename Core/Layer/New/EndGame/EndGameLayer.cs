﻿using System;
using System.Collections.Generic;
using Helion.Audio;
using Helion.Audio.Sounds;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using Helion.Util.Timing;
using Helion.World;
using NLog;

namespace Helion.Layer.New.EndGame
{
    public partial class EndGameLayer : IGameLayer
    {
        private const int LettersPerSecond = 10;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static readonly IEnumerable<string> EndGameMaps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EndPic", "EndGame1", "EndGame2", "EndGameW", "EndGame4", "EndGameC", "EndGame3",
            "EndDemon", "EndGameS", "EndChess", "EndTitle", "EndSequence", "EndBunny"
        };
        private static readonly IList<string> TheEndImages = new[]
        {
            "END0", "END1", "END2", "END3", "END4", "END5", "END6"
        };

        public event EventHandler? Exited;

        public readonly IWorld World;
        public readonly MapInfoDef? NextMapInfo;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly IMusicPlayer m_musicPlayer;
        private readonly SoundManager m_soundManager;
        private readonly string m_flatImage;
        private readonly IList<string> m_displayText;
        private readonly Ticker m_ticker = new(LettersPerSecond);
        private EndGameDrawState m_drawState = EndGameDrawState.Text;
        private TimeSpan m_timespan;
        private bool m_shouldScroll;
        private bool m_forceState;
        private int m_xOffset;
        private int m_xOffsetStop;
        private int m_theEndImageIndex;
        
        public EndGameLayer(ArchiveCollection archiveCollection, IMusicPlayer musicPlayer, SoundManager soundManager, IWorld world,
            ClusterDef cluster, MapInfoDef? nextMapInfo)
        {
            World = world;
            NextMapInfo = nextMapInfo;
            var language = archiveCollection.Definitions.Language;

            m_archiveCollection = archiveCollection;
            m_musicPlayer = musicPlayer;
            m_soundManager = soundManager;
            m_flatImage = language.GetMessage(cluster.Flat);
            m_displayText = LookUpDisplayText(language, cluster);
            m_timespan = GetPageTime();

            m_ticker.Start();
            string music = cluster.Music;
            if (music == "")
                music = archiveCollection.Definitions.MapInfoDefinition.GameDefinition.FinaleMusic;
            PlayMusic(music);
        }
        
        private static IList<string> LookUpDisplayText(LanguageDefinition language, ClusterDef cluster)
        {
            if (cluster.ExitText.Count != 1)
                return cluster.ExitText;
            
            return language.GetMessages(cluster.ExitText[0]);
        }
        
        private TimeSpan GetPageTime() =>
            TimeSpan.FromSeconds(m_archiveCollection.Definitions.MapInfoDefinition.GameDefinition.PageTime);
        
        private void PlayMusic(string music)
        {
            m_musicPlayer.Stop();
            if (music.Empty())
                return;

            music = m_archiveCollection.Definitions.Language.GetMessage(music);
            
            Entry? entry = m_archiveCollection.Entries.FindByName(music);
            if (entry == null)
            {
                Log.Warn($"Cannot find end game music file: {music}");
                return;
            }

            byte[] data = entry.ReadData();
            // Eventually we'll need to not assume .mus all the time.
            byte[]? midiData = MusToMidi.Convert(data);

            if (midiData != null)
                m_musicPlayer.Play(midiData);
            else
                Log.Warn($"Cannot decode end game music file: {music}");
        }

        public void Dispose()
        {
            // TODO: Anything we need to dispose of? What about `Exited`?
        }
    }
}