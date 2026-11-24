using System.Collections.Concurrent;

using AIStudio.Settings.DataModel;

namespace AIStudio.Tools.Services;

/// <summary>
/// Keeps the data source changes which wait for the AI to finish its answer, per chat.
/// </summary>
/// <remarks>
/// A started chat follows the data source options of its chat template, or of the chat defaults,
/// only while it still uses the options it started with. When these change while the AI answers,
/// the chat must not take them over right away: the answer works with the options of its message,
/// and storing the chat would store the answer unfinished. The change then waits here until the
/// answer is over.<br/><br/>
/// It waits outside the chat component on purpose: the component appears anew, e.g., whenever the
/// workspace sidebar is shown or hidden, and the user may open another chat while the answer goes
/// on. Whoever shows the chat once the answer is over takes the change over. Nothing is stored on
/// disk, so a change still waiting when AI Studio closes is gone.
/// </remarks>
public sealed class DeferredChatDataSourceChanges
{
    private readonly ConcurrentDictionary<Guid, DataSourceOptions> previousAutomaticOptionsByChatId = new();

    /// <summary>
    /// Lets a change of the automatic data source options wait until the answer in the chat is over.
    /// </summary>
    /// <remarks>
    /// When a change waits already, the chat keeps the options it followed before that first change:
    /// only they tell whether the chat still uses the options it started with.
    /// </remarks>
    /// <param name="chatId">The chat in which the AI answers.</param>
    /// <param name="previousAutomaticOptions">The automatic options the chat followed until the change.</param>
    public void Defer(Guid chatId, DataSourceOptions previousAutomaticOptions)
    {
        if (chatId == Guid.Empty)
            return;

        this.previousAutomaticOptionsByChatId.TryAdd(chatId, previousAutomaticOptions.CreateCopy());
    }

    /// <summary>
    /// Takes the change which waits for the chat, if any.
    /// </summary>
    /// <param name="chatId">The chat whose answer is over.</param>
    /// <returns>The automatic options the chat followed before the change, or null when no change waits.</returns>
    public DataSourceOptions? Take(Guid chatId) => this.previousAutomaticOptionsByChatId.TryRemove(chatId, out var previousAutomaticOptions) ? previousAutomaticOptions : null;

    /// <summary>
    /// Decides which data source options a started chat takes over after the automatic options changed.
    /// </summary>
    /// <param name="chatOptions">The data source options of the chat.</param>
    /// <param name="previousAutomaticOptions">The automatic options the chat followed until now, or null when nothing changed for it.</param>
    /// <param name="updatedAutomaticOptions">The automatic options a chat would start with right now.</param>
    /// <returns>The options to take over, or null when the chat keeps its own.</returns>
    public static DataSourceOptions? ChooseOptionsToFollow(DataSourceOptions chatOptions, DataSourceOptions? previousAutomaticOptions, DataSourceOptions updatedAutomaticOptions)
    {
        // A chat whose options the user changed keeps them:
        if (previousAutomaticOptions is null || !AreEqual(chatOptions, previousAutomaticOptions))
            return null;

        // Nothing to take over, e.g., after a change of some other setting:
        if (AreEqual(chatOptions, updatedAutomaticOptions))
            return null;

        return updatedAutomaticOptions;
    }

    /// <summary>
    /// Whether two data source options ask for the same, no matter in which order they list the data sources.
    /// </summary>
    public static bool AreEqual(DataSourceOptions left, DataSourceOptions right)
    {
        return left.DisableDataSources == right.DisableDataSources
               && left.AutomaticDataSourceSelection == right.AutomaticDataSourceSelection
               && left.AutomaticValidation == right.AutomaticValidation
               && left.RetrievalMode == right.RetrievalMode
               && left.PreselectedDataSourceIds.ToHashSet(StringComparer.Ordinal).SetEquals(right.PreselectedDataSourceIds);
    }
}