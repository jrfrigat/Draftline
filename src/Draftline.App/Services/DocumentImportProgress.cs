namespace Draftline.App.Services;

/// <summary>Progress within one measurable import stage, or an indeterminate operation.</summary>
public sealed record DocumentImportProgress(string Stage, long? Completed = null, long? Total = null)
{
    public int? Percent => Total > 0 && Completed.HasValue
        ? (int)Math.Clamp(Completed.Value * 100d / Total.Value, 0, 100) : null;
}
