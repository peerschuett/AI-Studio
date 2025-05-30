using Microsoft.AspNetCore.Components;

using Timer = System.Timers.Timer;

namespace AIStudio.Components;

public partial class DebouncedTextField : MudComponentBase
{
    [Parameter]
    public string Label { get; set; } = string.Empty;
    
    [Parameter]
    public string Text { get; set; } =  string.Empty;
    
    [Parameter]
    public EventCallback<string> TextChanged { get; set; }
    
    [Parameter]
    public Func<string, Task> WhenTextChangedAsync { get; set; } = _ => Task.CompletedTask;
    
    [Parameter]
    public Action<string> WhenTextCanged { get; set; } = _ => { };
    
    [Parameter]
    public int Lines { get; set; } = 1;

    [Parameter]
    public int MaxLines { get; set; } = 1;

    [Parameter]
    public Dictionary<string, object?> Attributes { get; set; } = [];

    [Parameter]
    public Func<string, string?> ValidationFunc { get; set; } = _ => null;
    
    [Parameter]
    public string HelpText { get; set; } = string.Empty;
    
    [Parameter]
    public string Placeholder { get; set; } = string.Empty;
    
    [Parameter]
    public string Icon { get; set; } = string.Empty;
    
    [Parameter]
    public TimeSpan DebounceTime { get; set; } = TimeSpan.FromMilliseconds(800);
    
    [Parameter]
    public bool Disabled { get; set; }
    
    private readonly Timer debounceTimer = new();
    private string text = string.Empty;

    #region Overrides of ComponentBase

    protected override async Task OnInitializedAsync()
    {
        this.text = this.Text;
        this.debounceTimer.AutoReset = false;
        this.debounceTimer.Interval = this.DebounceTime.TotalMilliseconds;
        this.debounceTimer.Elapsed += (_, _) =>
        {
            this.debounceTimer.Stop();
            this.InvokeAsync(async () => await this.TextChanged.InvokeAsync(this.text));
            this.InvokeAsync(async () => await this.WhenTextChangedAsync(this.text));
            this.InvokeAsync(() => this.WhenTextCanged(this.text));
        };
        
        await base.OnInitializedAsync();
    }

    #endregion

    private void OnTextChanged(string value)
    {
        this.text = value;
        this.debounceTimer.Stop();
        this.debounceTimer.Start();
    }
}