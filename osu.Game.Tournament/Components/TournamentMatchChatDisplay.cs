// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Game.Online.API;
using osu.Game.Online.Chat;
using osu.Game.Overlays.Chat;
using osu.Game.Tournament.IPC;
using osu.Game.Tournament.Models;

namespace osu.Game.Tournament.Components
{
    public partial class TournamentMatchChatDisplay : StandAloneChatDisplay
    {
        private readonly Bindable<string> chatChannel = new Bindable<string>();

        private ChannelManager? manager;

        [Resolved]
        private LadderInfo ladderInfo { get; set; } = null!;

        public TournamentMatchChatDisplay()
        {
            RelativeSizeAxes = Axes.X;
            Height = 144;
            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;

            CornerRadius = 0;
        }

        [BackgroundDependencyLoader]
        private void load(MatchIPCInfo? ipc, IAPIProvider api)
        {
            if (ipc != null)
            {
                chatChannel.BindTo(ipc.ChatChannel);
                chatChannel.BindValueChanged(c =>
                {
                    if (string.IsNullOrWhiteSpace(c.NewValue))
                        return;

                    int id = int.Parse(c.NewValue);

                    if (id <= 0) return;

                    if (manager == null)
                    {
                        AddInternal(manager = new ChannelManager(api));
                        Channel.BindTo(manager.CurrentChannel);
                    }

                    foreach (var ch in manager.JoinedChannels.ToList())
                        manager.LeaveChannel(ch);

                    var channel = new Channel
                    {
                        Id = id,
                        Type = ChannelType.Public
                    };

                    manager.JoinChannel(channel);
                    manager.CurrentChannel.Value = channel;
                }, true);
            }
        }

        public void Expand() => this.FadeIn(300);

        public void Contract() => this.FadeOut(200);

        protected override ChatLine? CreateMessage(Message message)
        {
            if (message.Content.StartsWith("!mp", StringComparison.Ordinal))
                return null;

            return new MatchMessage(message, ladderInfo);
        }

        protected override StandAloneDrawableChannel CreateDrawableChannel(Channel channel) => new MatchChannel(channel);

        public partial class MatchChannel : StandAloneDrawableChannel
        {
            public MatchChannel(Channel channel)
                : base(channel)
            {
                ScrollbarVisible = false;
            }

            protected override void Update()
            {
                base.Update();

                long? lastSeconds = null;

                for (int i = 0; i < ChatLineFlow.Count; i++)
                {
                    if (ChatLineFlow[i] is ChatLine chatline)
                    {
                        long seconds = chatline.Message.Timestamp.ToUnixTimeSeconds();

                        chatline.AlternatingBackground = i % 2 == 0;
                        chatline.RequiresTimestamp = seconds != lastSeconds;
                        lastSeconds = seconds;
                    }
                }
            }
        }

        protected partial class MatchMessage : StandAloneMessage
        {

            public MatchMessage(Message message, LadderInfo info)
                : base(message)
            {
                TimestampWidth = 58;
                if (info.CurrentMatch.Value is TournamentMatch match)
                {
                    if (match.Team1.Value?.Players.Any(u => u.OnlineID == Message.Sender.OnlineID) == true)
                        UsernameColour = TournamentGame.COLOUR_RED;
                    else if (match.Team2.Value?.Players.Any(u => u.OnlineID == Message.Sender.OnlineID) == true)
                        UsernameColour = TournamentGame.COLOUR_BLUE;
                }
            }
            protected override void updateTimestamp()
            {
                drawableTimestamp.Text = Message.Timestamp.LocalDateTime
                    .ToUniversalTime()
                    .ToLocalisableString(prefer24HourTime.Value ? @"HH:mm:ss" : @"hh:mm:ss tt");
            }
        }
    }
}
