using AIStudio.Chat;
using AIStudio.Components;
using AIStudio.Settings;

namespace AIStudio.Tests.Components;

/// <summary>
/// Checks what the composer counts as a message the user started.
/// </summary>
/// <remarks>
/// That decides whether a chat template may still fill the composer, and whether leaving the chat
/// warns about unsaved changes. A draft brought back from a chat is always the user's own, while a
/// chat template only suggests a message.
/// </remarks>
[TestFixture]
public sealed class ChatComposerStateTests
{
    [Test]
    public void ADraftBroughtBackIsTheMessageOfTheUser()
    {
        var draft = new ChatDraft();
        draft.SetText("Summarize the meeting");
        draft.AddAttachments([new FileAttachment(FileAttachmentType.DOCUMENT, "agenda.pdf", "/documents/agenda.pdf", 1024)]);
        var composer = new ChatComposerState();

        composer.RestoreFromDraft(draft);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(composer.UserInput, Is.EqualTo("Summarize the meeting"));
            Assert.That(composer.FileAttachments.Select(attachment => attachment.FilePath), Is.EqualTo(new[] { "/documents/agenda.pdf" }));
            Assert.That(composer.HasUserDraft, Is.True, "A chat template must not replace the message the user came back to.");
        }
    }

    [Test]
    public void AnEmptyDraftLeavesTheComposerFree()
    {
        var composer = new ChatComposerState();
        composer.SetUserInput("A message of the chat before");

        composer.RestoreFromDraft(new ChatDraft());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(composer.UserInput, Is.Empty, "The message of the chat before belongs to that chat.");
            Assert.That(composer.HasUserDraft, Is.False, "Nobody started a message in this chat, so a chat template may still fill the composer.");
        }
    }

    [Test]
    public void TheMessageOfAChatTemplateIsNoDraftOfTheUser()
    {
        var composer = new ChatComposerState();

        composer.ApplyTemplate(new ChatTemplate { PredefinedUserPrompt = "Translate the following text:" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(composer.UserInput, Is.EqualTo("Translate the following text:"));
            Assert.That(composer.HasUserDraft, Is.False, "Leaving the chat must not warn about a message the user never wrote.");
        }
    }

    [Test]
    public void AnEditedChatTemplateLeavesTheComposerOfAStartedChatEmpty()
    {
        var chatTemplate = CreateChatTemplate("Translate the following text:", "/documents/glossary.pdf");
        var editedChatTemplate = CreateChatTemplate("Translate the following text into German:", "/documents/glossary-de.pdf");
        var composer = new ChatComposerState();
        composer.ApplyTemplate(chatTemplate);

        // Sending the first message empties the composer:
        composer.Clear();

        var wasFilled = composer.ApplyChangedTemplate(chatTemplate, editedChatTemplate, hasChatStarted: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wasFilled, Is.False);
            Assert.That(composer.UserInput, Is.Empty, "The message of the chat template belongs to the first message only, which was sent already.");
            Assert.That(composer.FileAttachments, Is.Empty, "The attachments of the chat template were sent with the first message already.");
        }
    }

    [Test]
    public void AnEditedChatTemplateFillsTheComposerOfAChatNotStartedYet()
    {
        var chatTemplate = CreateChatTemplate("Translate the following text:", "/documents/glossary.pdf");
        var editedChatTemplate = CreateChatTemplate("Translate the following text into German:", "/documents/glossary-de.pdf");
        var composer = new ChatComposerState();
        composer.ApplyTemplate(chatTemplate);

        var wasFilled = composer.ApplyChangedTemplate(chatTemplate, editedChatTemplate, hasChatStarted: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wasFilled, Is.True);
            Assert.That(composer.UserInput, Is.EqualTo("Translate the following text into German:"));
            Assert.That(composer.FileAttachments.Select(attachment => attachment.FilePath), Is.EqualTo(new[] { "/documents/glossary-de.pdf" }));
            Assert.That(composer.HasUserDraft, Is.False);
        }
    }

    [Test]
    public void AnEditedChatTemplateKeepsTheMessageOfTheUser()
    {
        var chatTemplate = CreateChatTemplate("Translate the following text:", "/documents/glossary.pdf");
        var editedChatTemplate = CreateChatTemplate("Translate the following text into German:", "/documents/glossary-de.pdf");
        var composer = new ChatComposerState();
        composer.ApplyTemplate(chatTemplate);
        composer.SetUserInput("Summarize the meeting");

        var wasFilled = composer.ApplyChangedTemplate(chatTemplate, editedChatTemplate, hasChatStarted: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wasFilled, Is.False);
            Assert.That(composer.UserInput, Is.EqualTo("Summarize the meeting"));
            Assert.That(composer.HasUserDraft, Is.True);
        }
    }

    [Test]
    public void AnUnchangedChatTemplateLeavesTheComposerAsItIs()
    {
        var chatTemplate = CreateChatTemplate("Translate the following text:", "/documents/glossary.pdf");
        var composer = new ChatComposerState();
        composer.Clear();

        var wasFilled = composer.ApplyChangedTemplate(chatTemplate, chatTemplate, hasChatStarted: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(wasFilled, Is.False);
            Assert.That(composer.UserInput, Is.Empty, "A change of some other setting must not fill the composer again.");
        }
    }

    private static ChatTemplate CreateChatTemplate(string predefinedUserPrompt, string attachmentPath) => new()
    {
        Id = "5a6f3a52-0d39-4f39-9c39-3c8f3fbd5b0e",
        PredefinedUserPrompt = predefinedUserPrompt,
        FileAttachments = [new FileAttachment(FileAttachmentType.DOCUMENT, Path.GetFileName(attachmentPath), attachmentPath, 1024)],
    };
}