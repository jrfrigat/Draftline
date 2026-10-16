using Flare.Abstractions;
using Flare.Abstractions.Tokens;
using Flare.Theme.FluentUI2;
using Flare.Theme.FluentUI2.Tokens;
using Flare.Theming;

namespace Draftline.App.Theming;
/// <summary>
/// The look Draftline runs: Fluent UI 2 with one value changed.
///
/// Fluent UI 2 puts 1.5rem 2rem of padding around a page (1rem on a narrow screen). That is right for a
/// document and wrong for an editor: the sheet is the product here, and chrome that eats the window is
/// sheet the person cannot see. The value is replaced where it is declared - in the theme's own layout
/// tokens - and not by overwriting the CSS variable from our stylesheet, because a token belongs to the
/// theme that defines it and a rule that patches the variable would break silently the day Flare renames
/// it.
///
/// The theme is built on Fluent UI 2 rather than beside it: the root then carries both theme classes
/// (<c>flare-theme-draftline-fluent2</c> and <c>flare-theme-fluent2</c>), so every Fluent UI 2 stylesheet
/// still applies and only the tokens we replace differ. The id is Draftline's own, so it does not claim
/// to be the stock theme - which is why the bootstrap in index.html names it too.
/// </summary>
public static class DraftlineTheme
{
    /// <summary>What the layout puts around a page: nothing, because the sheet reaches the window's edges.</summary>
    private const string NoContentPadding = "0";
    /// <summary>Builds the theme the application registers.</summary>
    public static ITheme Build()
    {
        DesignTokens design = FluentUI2Tokens.Design with
        {
            Layout = FluentUI2Tokens.Design.Layout with
            {
                ContentPadding = NoContentPadding,
                ContentPaddingMobile = NoContentPadding,
            },
        };
        return new FlareThemeBuilder("draftline-fluent2", "Draftline - Fluent UI 2", design).WithBase(new FluentUI2Theme()).Build();
    }

}
