using AIStudio.Settings.DataModel;
using AIStudio.Tools.Services;

namespace AIStudio.Tests.Tools;

/// <summary>
/// Checks how a started chat follows the data source options of its chat template, also when they change while the AI answers.
/// </summary>
/// <remarks>
/// A chat takes over the new options only while it still uses the ones it followed until then. A change made while
/// the AI answers waits for the end of the answer, and it must still compare with the options from before the
/// change, no matter how often the configuration changes meanwhile.
/// </remarks>
[TestFixture]
public sealed class DeferredChatDataSourceChangesTests
{
    private static readonly Guid CHAT_ID = Guid.Parse("8d0f3c4e-5a0b-4d7e-9f61-2b7c0e9a4d15");

    [Test]
    public void AChatFollowsTheOptionsItStillUses()
    {
        var previousOptions = CreateOptions("handbook");
        var updatedOptions = CreateOptions("handbook", "glossary");

        var optionsToFollow = DeferredChatDataSourceChanges.ChooseOptionsToFollow(CreateOptions("handbook"), previousOptions, updatedOptions);

        Assert.That(optionsToFollow, Is.SameAs(updatedOptions));
    }

    [Test]
    public void AChatKeepsTheOptionsTheUserChose()
    {
        var optionsToFollow = DeferredChatDataSourceChanges.ChooseOptionsToFollow(CreateOptions("archive"), CreateOptions("handbook"), CreateOptions("handbook", "glossary"));

        Assert.That(optionsToFollow, Is.Null, "Options the user chose in the chat must survive an edit of the chat template.");
    }

    [Test]
    public void AChatWithoutAChangeKeepsItsOptions()
    {
        var optionsToFollow = DeferredChatDataSourceChanges.ChooseOptionsToFollow(CreateOptions("handbook"), previousAutomaticOptions: null, CreateOptions("handbook", "glossary"));

        Assert.That(optionsToFollow, Is.Null, "A chat just loaded follows only what changes from now on.");
    }

    [Test]
    public void AChangeOfSomeOtherSettingLeavesTheChatAsItIs()
    {
        var optionsToFollow = DeferredChatDataSourceChanges.ChooseOptionsToFollow(CreateOptions("handbook", "glossary"), CreateOptions("glossary", "handbook"), CreateOptions("handbook", "glossary"));

        Assert.That(optionsToFollow, Is.Null, "Nothing changed for the chat, so it must not be stored again.");
    }

    [Test]
    public void AChangeDuringAnAnswerWaitsUntilItIsTaken()
    {
        var changes = new DeferredChatDataSourceChanges();
        var previousOptions = CreateOptions("handbook");
        changes.Defer(CHAT_ID, previousOptions);

        // The change must not depend on the options the component goes on with:
        previousOptions.PreselectedDataSourceIds.Add("glossary");

        var takenOptions = changes.Take(CHAT_ID);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(takenOptions?.PreselectedDataSourceIds, Is.EqualTo(new[] { "handbook" }));
            Assert.That(changes.Take(CHAT_ID), Is.Null, "A change is taken over once only.");
        }
    }

    [Test]
    public void SeveralChangesDuringAnAnswerCompareWithTheOptionsBeforeTheFirst()
    {
        var changes = new DeferredChatDataSourceChanges();
        changes.Defer(CHAT_ID, CreateOptions("handbook"));
        changes.Defer(CHAT_ID, CreateOptions("handbook", "glossary"));

        var takenOptions = changes.Take(CHAT_ID);

        Assert.That(takenOptions?.PreselectedDataSourceIds, Is.EqualTo(new[] { "handbook" }), "Only the options before the first change tell whether the chat still used the automatic ones.");
    }

    [Test]
    public void AChangeWaitsForItsOwnChatOnly()
    {
        var changes = new DeferredChatDataSourceChanges();
        changes.Defer(CHAT_ID, CreateOptions("handbook"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(changes.Take(Guid.NewGuid()), Is.Null);
            Assert.That(changes.Take(CHAT_ID), Is.Not.Null);
        }
    }

    private static DataSourceOptions CreateOptions(params string[] dataSourceIds) => new()
    {
        DisableDataSources = false,
        PreselectedDataSourceIds = [..dataSourceIds],
    };
}