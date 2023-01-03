using System;

public struct RateLimit
{
    public RateLimit(int count, TimeSpan timeSpan)
    {
        this.count = count;
        this.timeSpan = timeSpan;
    }

    public int count;
    public TimeSpan timeSpan;

    /// <summary>
    /// The chat command/message rate limit for users in a channel who do not have moderator permissions.
    /// </summary>
    public readonly static RateLimit ChatRegular = new RateLimit(20, new TimeSpan(0, 0, 30));

    /// <summary>
    /// The chat command/message rate limit for users in a channel who are the broadcaster or have moderator permissions.
    /// </summary>
    public readonly static RateLimit ChatModerator = new RateLimit(100, new TimeSpan(0, 0, 30));

    /// <summary>
    /// The site-wide rate limit for verified bots to send chat commands/messages. <i><b>Note:</b> Channel level rate limits still apply based on the bot's user permissions.</i>
    /// </summary>
    public readonly static RateLimit SiteLimitVerified = new RateLimit(7500, new TimeSpan(0, 0, 30));

    /// <summary>
    /// The authentication attempt rate limit for a regular Twitch account.
    /// </summary>
    public readonly static RateLimit AuthAttemptsRegular = new RateLimit(20, new TimeSpan(0, 0, 10));

    /// <summary>
    /// The join attempt rate limit for a regular Twitch account.
    /// </summary>
    public readonly static RateLimit JoinAttemptsRegular = new RateLimit(20, new TimeSpan(0, 0, 10));

    /// <summary>
    /// The authentication attempt rate limit for a Twitch-verified bot.
    /// </summary>
    public readonly static RateLimit AuthAttemptsVerified = new RateLimit(200, new TimeSpan(0, 0, 10));

    /// <summary>
    /// The join attempt rate limit for a Twitch-verified bot.
    /// </summary>
    public readonly static RateLimit JoinAttemptsVerified = new RateLimit(2000, new TimeSpan(0, 0, 10));

    /// <summary>
    /// The number of whispers allowed per second. <i><b>Note:</b> Whispers use a composite rate limit. Make sure to also check the rate against <see cref="WhispersB" /> and <see cref="WhisperChannels" />.</i>
    /// </summary>
    public readonly static RateLimit WhispersA = new RateLimit(3, new TimeSpan(0, 0, 1));

    /// <summary>
    /// The number of whispers allowed per minute. <i><b>Note:</b> Whispers use a composite rate limit. Make sure to also check the rate against <see cref="WhispersA" /> and <see cref="WhisperChannels" />.</i>
    /// </summary>
    public readonly static RateLimit WhispersB = new RateLimit(100, new TimeSpan(0, 1, 0));

    /// <summary>
    /// The number of channels per day in which an account is allowed to send whispers. <i><b>Note:</b> Whispers use a composite rate limit. Make sure to also check the rate against <see cref="WhispersA" /> and <see cref="WhispersB" />.</i>
    /// </summary>
    public readonly static RateLimit WhisperChannels = new RateLimit(40, new TimeSpan(1, 0, 0, 0));
}
