using AIStudio.Chat;
using AIStudio.Settings;

namespace AIStudio.Components;

public sealed class ChatComposerState
{
    public string UserInput { get; private set; } = string.Empty;

    public HashSet<FileAttachment> FileAttachments { get; } = [];

    public bool HasUserDraft { get; private set; }

    public bool HasComposerContent => !string.IsNullOrWhiteSpace(this.UserInput) || this.FileAttachments.Count > 0;

    public bool HasVisibleUserDraft => this.HasUserDraft && (!string.IsNullOrWhiteSpace(this.UserInput) || this.FileAttachments.Count > 0);

    public void ApplyTemplate(ChatTemplate chatTemplate)
    {
        this.UserInput = chatTemplate.PredefinedUserPrompt;
        this.FileAttachments.Clear();
        foreach (var attachment in chatTemplate.FileAttachments)
            this.FileAttachments.Add(attachment.Normalize());

        this.HasUserDraft = false;
    }

    /// <summary>
    /// Fills the composer anew after the chat template of the chat changed, e.g., because the user edited it.
    /// </summary>
    /// <remarks>
    /// The message and the attachments of a chat template belong to the first message of a chat only.
    /// Once the chat has started, an edited template must not bring them back into the composer: they
    /// would be sent a second time. A message the user started always stays.
    /// </remarks>
    /// <param name="previousChatTemplate">The chat template the composer was filled with.</param>
    /// <param name="chatTemplate">The chat template of the chat now.</param>
    /// <param name="hasChatStarted">Whether the chat already holds a message the user sees.</param>
    /// <returns>True when the composer was filled anew.</returns>
    public bool ApplyChangedTemplate(ChatTemplate previousChatTemplate, ChatTemplate chatTemplate, bool hasChatStarted)
    {
        if (hasChatStarted || this.HasUserDraft || previousChatTemplate == chatTemplate)
            return false;

        this.ApplyTemplate(chatTemplate);
        return true;
    }

    public void SetUserInput(string? userInput)
    {
        this.UserInput = userInput ?? string.Empty;
        this.HasUserDraft = !string.IsNullOrWhiteSpace(userInput);
    }

    public void SetSystemInput(string? userInput)
    {
        this.UserInput = userInput ?? string.Empty;
        this.HasUserDraft = false;
    }

    public void MarkUserDraft()
    {
        this.HasUserDraft = true;
    }

    public void ReplaceFileAttachments(IEnumerable<FileAttachment> fileAttachments)
    {
        this.FileAttachments.Clear();
        foreach (var attachment in fileAttachments)
            this.FileAttachments.Add(attachment.Normalize());
    }

    public void Clear()
    {
        this.UserInput = string.Empty;
        this.FileAttachments.Clear();
        this.HasUserDraft = false;
    }

    /// <summary>
    /// Brings the message the user had not sent yet back into the composer.
    /// </summary>
    /// <remarks>
    /// The draft holds only what the user wrote or attached, so whatever it holds counts as their
    /// message. An empty draft leaves the composer free for a chat template.
    /// </remarks>
    /// <param name="draft">The draft of the chat.</param>
    public void RestoreFromDraft(ChatDraft draft)
    {
        this.UserInput = draft.Text;
        this.ReplaceFileAttachments(draft.FileAttachments);
        this.HasUserDraft = !draft.IsEmpty;
    }
}