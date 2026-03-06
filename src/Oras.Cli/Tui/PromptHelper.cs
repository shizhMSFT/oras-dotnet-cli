using Spectre.Console;

namespace Oras.Tui;

/// <summary>
/// Reusable prompt helpers for consistent TUI interactions.
/// </summary>
internal static class PromptHelper
{
    public static string PromptText(string prompt, string? defaultValue = null, bool allowEmpty = false)
    {
        var textPrompt = new TextPrompt<string>(prompt)
            .PromptStyle("green");

        if (!string.IsNullOrEmpty(defaultValue))
        {
            textPrompt.DefaultValue(defaultValue);
        }

        if (allowEmpty)
        {
            textPrompt.AllowEmpty();
        }

        return AnsiConsole.Prompt(textPrompt);
    }

    public static string PromptSecret(string prompt)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<string>(prompt)
                .PromptStyle("green")
                .Secret());
    }

    public static T PromptSelection<T>(string title, IEnumerable<T> choices, Func<T, string>? converter = null) where T : notnull
    {
        var prompt = new SelectionPrompt<T>()
            .Title(title)
            .PageSize(10)
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
            .AddChoices(choices);

        if (converter != null)
        {
            prompt.UseConverter(converter);
        }

        return AnsiConsole.Prompt(prompt);
    }

    public static T PromptSelectionWithSearch<T>(string title, IEnumerable<T> choices, Func<T, string>? converter = null, bool enableSearch = true) where T : notnull
    {
        var prompt = new SelectionPrompt<T>()
            .Title(title)
            .PageSize(15)
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
            .AddChoices(choices);

        if (converter != null)
        {
            prompt.UseConverter(converter);
        }

        if (enableSearch)
        {
            prompt.EnableSearch();
        }

        return AnsiConsole.Prompt(prompt);
    }

    public static List<T> PromptMultiSelection<T>(string title, IEnumerable<T> choices, Func<T, string>? converter = null) where T : notnull
    {
        var prompt = new MultiSelectionPrompt<T>()
            .Title(title)
            .PageSize(15)
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
            .InstructionsText("[grey](Press [blue]<space>[/] to select, [green]<enter>[/] to accept)[/]")
            .AddChoices(choices);

        if (converter != null)
        {
            prompt.UseConverter(converter);
        }

        return AnsiConsole.Prompt(prompt);
    }

    public static bool PromptConfirmation(string message, bool defaultValue = false)
    {
        return AnsiConsole.Confirm(message, defaultValue);
    }

    public static void ShowError(string message, string? recommendation = null)
    {
        AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(message)}[/]");
        if (!string.IsNullOrEmpty(recommendation))
        {
            AnsiConsole.MarkupLine($"[yellow]Recommendation: {Markup.Escape(recommendation)}[/]");
        }
    }

    public static void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓ {Markup.Escape(message)}[/]");
    }

    public static void ShowInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]ℹ {Markup.Escape(message)}[/]");
    }

    public static void ShowWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]⚠ {Markup.Escape(message)}[/]");
    }
}
