using AIStudio.Provider;
using AIStudio.Settings;

using Microsoft.AspNetCore.Components;

namespace AIStudio.Components;

public partial class ConfidenceInfo : ComponentBase, IMessageBusReceiver, IDisposable
{
    [Parameter]
    public PopoverTriggerMode Mode { get; set; } = PopoverTriggerMode.BUTTON;
    
    [Parameter]
    public LLMProviders LLMProvider { get; set; }
    
    [Inject]
    private SettingsManager SettingsManager { get; init; } = null!;
    
    [Inject]
    private MessageBus MessageBus { get; init; } = null!;

    private Confidence currentConfidence;
    private bool showConfidence;

    public ConfidenceInfo()
    {
        this.currentConfidence = LLMProviders.NONE.GetConfidence(this.SettingsManager);
    }

    #region Overrides of ComponentBase

    protected override async Task OnParametersSetAsync()
    {
        this.MessageBus.RegisterComponent(this);
        this.MessageBus.ApplyFilters(this, [], [ Event.COLOR_THEME_CHANGED ]);
        
        this.currentConfidence = this.LLMProvider.GetConfidence(this.SettingsManager);
        await base.OnParametersSetAsync();
    }

    #endregion
    
    private void ToggleConfidence()
    {
        this.showConfidence = !this.showConfidence;
    }
    
    private void HideConfidence()
    {
        this.showConfidence = false;
    }
    
    private IEnumerable<(string Index, string Source)> GetConfidenceSources()
    {
        var index = 0;
        foreach (var source in this.currentConfidence.Sources)
            yield return ($"Source {++index}", source);
    }

    private string GetCurrentConfidenceColor() => $"color: {this.currentConfidence.Level.GetColor(this.SettingsManager)};";
    
    private string GetPopoverStyle() => $"border-color: {this.currentConfidence.Level.GetColor(this.SettingsManager)};";
    
    #region Implementation of IMessageBusReceiver

    public string ComponentName => nameof(ConfidenceInfo);
    
    public Task ProcessMessage<T>(ComponentBase? sendingComponent, Event triggeredEvent, T? data)
    {
        switch (triggeredEvent)
        {
            case Event.COLOR_THEME_CHANGED:
                this.showConfidence = false;
                this.StateHasChanged();
                break;
        }
        
        return Task.CompletedTask;
    }

    public Task<TResult?> ProcessMessageWithResult<TPayload, TResult>(ComponentBase? sendingComponent, Event triggeredEvent, TPayload? data)
    {
        return Task.FromResult<TResult?>(default);
    }

    #endregion
    
    #region Implementation of IDisposable

    public void Dispose()
    {
        this.MessageBus.Unregister(this);
    }

    #endregion
}