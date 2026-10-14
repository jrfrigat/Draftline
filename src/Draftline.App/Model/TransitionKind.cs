using System.Text.Json.Serialization;

namespace Draftline.App.Model;

/// <summary>What a block does when it is pressed in the preview.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TransitionKind>))]
public enum TransitionKind
{
    /// <summary>Nothing: the block is a drawing.</summary>
    None,

    /// <summary>Another screen takes the place of this one.</summary>
    Navigate,

    /// <summary>Another screen opens in a dialog over this one.</summary>
    OpenDialog
}
