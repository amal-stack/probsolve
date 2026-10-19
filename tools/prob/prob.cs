#!/usr/bin/env dotnet
#:property TargetFramework=net10.0
#:property PublishAot=false
#:package YamlDotNet@18.1.0
#:package Spectre.Console.Cli@0.57.2
#:package Scriban@7.5.0
#pragma warning disable CA1050

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Scriban;
using Spectre.Console;
using Spectre.Console.Cli;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var app = new CommandApp();
app.Configure(configuration =>
{
    configuration.SetApplicationName("prob");
    configuration
        .AddCommand<NewCommand>("new")
        .WithDescription("Create a problem document and missing language scaffolds.");
    configuration.AddCommand<ListCommand>("list").WithDescription("Search and list problems.");
    configuration
        .AddCommand<LinkCommand>("link")
        .WithDescription("Print or open a clickable implementation link.");
    configuration
        .AddCommand<StatusCommand>("status")
        .WithDescription("Update status and solved date.");
    configuration
        .AddCommand<SyncCommand>("sync")
        .WithDescription("Rebuild problem metadata and/or the generated index.");
    configuration
        .AddCommand<InitCommand>("init")
        .WithDescription("Write an empty starter prob.yaml without overwriting an existing file.");
});
return app.Run(args);

/// <summary>Configuration-selection and configuration-override options shared by all commands.</summary>
public abstract class ConfigSettings : CommandSettings
{
    [CommandOption("--config <PATH>")]
    [Description(
        "Select prob.yaml and skip walk-up search; example: prob list --config ./prob.yaml."
    )]
    public string? Config { get; set; }

    [CommandOption("--set <KEY=VALUE>")]
    [Description(
        "Override a prob.yaml value; repeatable, e.g. prob list --set notes.dir=problems."
    )]
    public string[] Overrides { get; set; } = [];
}

/// <summary>Options for creating a problem document and language scaffolds.</summary>
public sealed class NewSettings : ConfigSettings
{
    [CommandArgument(0, "<title>")]
    [Description(
        "Problem title, such as 'Two Sum'. Examples: prob new 'Two Sum' --category arrays; prob new 'Two Sum' --no-category."
    )]
    public string Title { get; set; } = "";

    [CommandOption("-l|--languages <LANGUAGES>")]
    [Description(
        "Comma-separated language keys. Defaults to default_languages or all configured languages; with --existing, only explicitly selected languages are scaffolded."
    )]
    public string? Languages { get; set; }

    [CommandOption("-e|--existing")]
    [Description("Update an existing problem only; never create its Markdown file.")]
    public bool Existing { get; set; }

    [CommandOption("--tag <TAG>")]
    [Description("Add one or more comma-separated tags; repeatable.")]
    public string[] Tags { get; set; } = [];

    [CommandOption("--category <KEY>")]
    [Description(
        "Create in or move to a configured category; example: prob new 'Two Sum' --category arrays."
    )]
    public string? Category { get; set; }

    [CommandOption("--no-category")]
    [Description(
        "Create in or move to the top-level notes directory; example: prob new 'Two Sum' --no-category."
    )]
    public bool NoCategory { get; set; }

    [CommandOption("--platform <NAME:ID>")]
    [Description("Scaffold-only platform metadata. Repeat for multiple platforms.")]
    public string[] Platforms { get; set; } = [];

    [CommandOption("--status <STATUS>")]
    [Description("Initial status: solved, pending, or revisit. Defaults to pending.")]
    public string? Status { get; set; }

    [CommandOption("--no-sample-method")]
    [Description("Omit the template's sample method.")]
    public bool NoSampleMethod { get; set; }

    [CommandOption("--force <SCOPE>")]
    [Description("Overwrite scope: metadata, scaffold, or all.")]
    public string? Force { get; set; }

    [CommandOption("--yes")]
    [Description("Skip the confirmation required before forced scaffold overwrites.")]
    public bool Yes { get; set; }

    [CommandOption("--dry-run")]
    [Description("Show files that would be written; does not write files.")]
    public bool DryRun { get; set; }

    [CommandOption("--verbose")]
    [Description("Include generated file contents in dry-run output.")]
    public bool Verbose { get; set; }
}

/// <summary>Options for searching indexed problem documents.</summary>
public sealed class ListSettings : ConfigSettings
{
    [CommandArgument(0, "[query]")]
    [Description("Whitespace-separated AND filters, e.g. 'status:solved lang:java two sum'.")]
    public string? Query { get; set; }

    [CommandOption("-f|--format <FORMAT>")]
    [Description(
        "Output format: table (default), long, slugs, or json. Example: prob list --format json."
    )]
    public string Format { get; set; } = "table";

    [CommandOption("--long")]
    [Description("Show the long table format.")]
    public bool Long { get; set; }

    [CommandOption("--fields <FIELDS>")]
    [Description("Comma-separated table or JSON fields, e.g. --fields title,status,languages.")]
    public string? Fields { get; set; }

    [CommandOption("--sort <FIELD>")]
    [Description(
        "Sort by title, created, or solved_on (default: created). Example: --sort solved_on."
    )]
    public string Sort { get; set; } = "created";

    [CommandOption("--reverse")]
    [Description("Reverse the primary sort order, without reversing slug tie-breaks.")]
    public bool Reverse { get; set; }
}

/// <summary>Options for printing or opening a language implementation path.</summary>
public sealed class LinkSettings : ConfigSettings
{
    [CommandArgument(0, "<problem>")]
    [Description("Problem title or slug.")]
    public string Problem { get; set; } = "";

    [CommandOption("-l|--language <LANGUAGES>")]
    [Description(
        "Comma-separated configured language keys; for example: prob link 'Two Sum' -l java,csharp."
    )]
    public string? Languages { get; set; }

    [CommandOption("-n|--notes")]
    [Description("Select the problem notes link. Example: prob link 'Two Sum' --notes.")]
    public bool Notes { get; set; }

    [CommandOption("--line <LINE>")]
    [Description("Line number in the selected file; requires exactly one selected link.")]
    public int? Line { get; set; }

    [CommandOption("--open")]
    [Description("Open the vscode://file URI with the operating system's registered handler.")]
    public bool Open { get; set; }
}

/// <summary>Options for changing problem status and its solved date.</summary>
public sealed class StatusSettings : ConfigSettings
{
    [CommandArgument(0, "<problem>")]
    [Description("Problem title or slug.")]
    public string Problem { get; set; } = "";

    [CommandArgument(1, "<status>")]
    [Description("New status: solved, pending, or revisit.")]
    public string Status { get; set; } = "";

    [CommandOption("--solved-on <DATE>")]
    [Description("Override solved_on using YYYY-MM-DD; defaults to today when solved.")]
    public string? SolvedOn { get; set; }

    [CommandOption("--dry-run")]
    [Description("Preview the metadata and index updates.")]
    public bool DryRun { get; set; }
}

/// <summary>Options for synchronizing problem metadata and the generated index.</summary>
public sealed class SyncSettings : ConfigSettings
{
    [CommandArgument(0, "[problem]")]
    [Description("Problem title or slug to synchronize; example: prob sync 'Two Sum'.")]
    public string? Problem { get; set; }

    [CommandOption("--all")]
    [Description("Synchronize all problem documents; example: prob sync --all.")]
    public bool All { get; set; }

    [CommandOption("-i|--index")]
    [Description("Regenerate only the index; example: prob sync --index.")]
    public bool Index { get; set; }

    [CommandOption("--dry-run")]
    [Description(
        "Show changes without writing problem files or the index; example: prob sync --all --dry-run."
    )]
    public bool DryRun { get; set; }

    [CommandOption("--verbose")]
    [Description(
        "Include generated file contents in dry-run output; example: prob sync --dry-run --verbose."
    )]
    public bool Verbose { get; set; }
}

/// <summary>Options for creating the empty configuration skeleton.</summary>
public sealed class InitSettings : ConfigSettings
{
    [CommandOption("--directory <PATH>")]
    [Description("Directory for prob.yaml. Defaults to the current directory.")]
    public string Directory { get; set; } = System.IO.Directory.GetCurrentDirectory();
}

/// <summary>Dispatches the new command to the problem store.</summary>
public sealed class NewCommand : Command<NewSettings>
{
    /// <summary>Executes problem generation.</summary>
    public override int Execute(
        CommandContext context,
        NewSettings settings,
        CancellationToken cancellationToken
    ) => ProblemStore.Create(settings);
}

/// <summary>Dispatches the list command to the problem store.</summary>
public sealed class ListCommand : Command<ListSettings>
{
    /// <summary>Executes the problem search.</summary>
    public override int Execute(
        CommandContext context,
        ListSettings settings,
        CancellationToken cancellationToken
    ) => ProblemStore.List(settings);
}

/// <summary>Dispatches the link command to the problem store.</summary>
public sealed class LinkCommand : Command<LinkSettings>
{
    /// <summary>Prints or opens the selected implementation.</summary>
    public override int Execute(
        CommandContext context,
        LinkSettings settings,
        CancellationToken cancellationToken
    ) => ProblemStore.Link(settings);
}

/// <summary>Dispatches the status command to the problem store.</summary>
public sealed class StatusCommand : Command<StatusSettings>
{
    /// <summary>Updates status metadata and the generated index.</summary>
    public override int Execute(
        CommandContext context,
        StatusSettings settings,
        CancellationToken cancellationToken
    ) => ProblemStore.ChangeStatus(settings);
}

/// <summary>Dispatches the sync command to the problem store.</summary>
public sealed class SyncCommand : Command<SyncSettings>
{
    /// <summary>Synchronizes problem metadata and/or the generated index.</summary>
    public override int Execute(
        CommandContext context,
        SyncSettings settings,
        CancellationToken cancellationToken
    ) => ProblemStore.Sync(settings);
}

/// <summary>Dispatches configuration initialization.</summary>
public sealed class InitCommand : Command<InitSettings>
{
    /// <summary>Creates a starter configuration file when absent.</summary>
    public override int Execute(
        CommandContext context,
        InitSettings settings,
        CancellationToken cancellationToken
    ) => ProblemStore.Initialize(settings);
}

/// <summary>Deserialized user configuration for problem directories and languages.</summary>
public sealed class ProbConfig
{
    public NotesConfig? Notes { get; set; }

    [YamlMember(Alias = "index_file")]
    public string? IndexFile { get; set; }

    [YamlMember(Alias = "default_languages")]
    public List<string>? DefaultLanguages { get; set; }

    [YamlMember(Alias = "default_category")]
    public string? DefaultCategory { get; set; }

    public bool? Strict { get; set; }

    public Dictionary<string, LanguageConfig>? Languages { get; set; }

    public Dictionary<string, CategoryConfig>? Categories { get; set; }
}

/// <summary>Configures one category's notes directory and optional language defaults.</summary>
public sealed class CategoryConfig
{
    public NotesConfig? Notes { get; set; }

    [YamlMember(Alias = "default_languages")]
    public List<string>? DefaultLanguages { get; set; }
}

/// <summary>Configures the notes directory and optional new-problem template.</summary>
public sealed class NotesConfig
{
    public string? Dir { get; set; }
    public string? Template { get; set; }
}

/// <summary>Configures one language's scaffold location, extension, template, and variables.</summary>
public sealed class LanguageConfig
{
    [YamlMember(Alias = "dir")]
    public string Directory { get; set; } = "";
    public string Extension { get; set; } = "";
    public string Template { get; set; } = "";
    public string? Naming { get; set; }

    [YamlMember(Alias = "vars")]
    public Dictionary<string, object?>? Vars { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Frontmatter fields maintained for each problem document.</summary>
public sealed class ProblemMetadata
{
    public string Title { get; set; } = "";
    public string Status { get; set; } = "pending";
    public string Created { get; set; } = "";

    [YamlMember(Alias = "solved_on")]
    public string? SolvedOn { get; set; }

    public List<string> Tags { get; set; } = [];

    public Dictionary<string, string> Implementations { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>A parsed Markdown problem including its source path and preserved body.</summary>
public sealed record ProblemDocument(string Path, ProblemMetadata Metadata, string Body)
{
    /// <summary>Gets the filename stem used as the document's stable problem slug.</summary>
    public string Slug => System.IO.Path.GetFileNameWithoutExtension(Path);

    public string? Category { get; init; }
}

/// <summary>A file write planned by a command before applying changes.</summary>
public sealed record PlannedWrite(string Path, string Content, string Kind);

/// <summary>Implements config loading, scaffold generation, frontmatter operations, and indexing.</summary>
public static class ProblemStore
{
    private const string LinkStart = "<!-- solution-links:start -->";
    private const string LinkEnd = "<!-- solution-links:end -->";
    private const string IndexStart = "<!-- prob:index:start -->";
    private const string IndexEnd = "<!-- prob:index:end -->";
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    private static bool _nonFrontMatterWarningShown;
    private static readonly IDeserializer YamlReader = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();
    private static readonly ISerializer YamlWriter = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
        .Build();

    /// <summary>Finds the explicitly supplied configuration or searches the current directory and its parents.</summary>
    private static string FindConfig(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var absolute = Path.GetFullPath(explicitPath);
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    $"Configuration file not found: {absolute}",
                    absolute
                );
            }
            return absolute;
        }

        for (
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            directory is not null;
            directory = directory.Parent
        )
        {
            var candidate = Path.Combine(directory.FullName, "prob.yaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        throw new FileNotFoundException(
            "Could not find prob.yaml walking upward from the current directory. Use --config or run 'prob init'."
        );
    }

    /// <summary>Loads configuration, validates language entries, and resolves configured paths from the config directory.</summary>
    private static RepositoryConfig Load(ConfigSettings settings)
    {
        var configPath = FindConfig(settings.Config);
        var root = Path.GetDirectoryName(configPath)!;
        var config =
            YamlReader.Deserialize<ProbConfig>(File.ReadAllText(configPath))
            ?? throw new InvalidOperationException($"Empty configuration: {configPath}");
        config.Notes ??= new NotesConfig();
        config.Languages ??= new Dictionary<string, LanguageConfig>(
            StringComparer.OrdinalIgnoreCase
        );
        config.Categories ??= new Dictionary<string, CategoryConfig>(
            StringComparer.OrdinalIgnoreCase
        );
        ApplyOverrides(config, settings.Overrides, Directory.GetCurrentDirectory());
        if (string.IsNullOrWhiteSpace(config.Notes.Dir))
        {
            throw new InvalidOperationException(
                $"Configuration '{configPath}' must set notes.dir to the directory containing problem Markdown files."
            );
        }
        var configuredLanguages =
            config.Languages
            ?? new Dictionary<string, LanguageConfig>(StringComparer.OrdinalIgnoreCase);
        if (configuredLanguages.Count == 0)
        {
            throw new InvalidOperationException(
                $"Configuration '{configPath}' must define at least one language."
            );
        }

        foreach (var (key, language) in configuredLanguages)
        {
            var name = key;

            if (
                !Regex.IsMatch(name, @"^[a-z0-9]+(-[a-z0-9]+)*$")
                || string.IsNullOrWhiteSpace(language.Directory)
                || string.IsNullOrWhiteSpace(language.Extension)
                || string.IsNullOrWhiteSpace(language.Template)
            )
            {
                throw new InvalidOperationException(
                    $"Language configuration '{key}' requires dir, extension, and template."
                );
            }
            var extension = language.Extension.Trim().TrimStart('.');
            if (extension.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Language '{name}' extension must contain a file extension."
                );
            }
            language.Extension = "." + extension;
            language.Naming = (language.Naming ?? "pascal").Trim().ToLowerInvariant();
            if (language.Naming is not ("pascal" or "snake" or "kebab" or "camel"))
            {
                throw new InvalidOperationException(
                    $"Language '{name}' naming must be pascal, snake, kebab, or camel."
                );
            }
        }
        configuredLanguages = configuredLanguages.ToDictionary(
            pair => pair.Key.Trim().ToLowerInvariant(),
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase
        );

        var notesDirectory = ResolvePath(root, config.Notes.Dir);
        var globalNotesTemplate = string.IsNullOrWhiteSpace(config.Notes.Template)
            ? null
            : ResolvePath(root, config.Notes.Template);
        ValidateDefaultLanguages(
            config.DefaultLanguages,
            configuredLanguages.Keys,
            "default_languages"
        );

        var categoryConfigs = config.Categories;
        foreach (var (key, category) in categoryConfigs)
        {
            if (!Regex.IsMatch(key, @"^[a-z0-9]+(-[a-z0-9]+)*$"))
            {
                throw new InvalidOperationException(
                    $"Category key '{key}' must use lowercase letters and digits separated by single hyphens."
                );
            }
            if (category.Notes is null || string.IsNullOrWhiteSpace(category.Notes.Dir))
            {
                throw new InvalidOperationException(
                    $"Category '{key}' must set categories.{key}.notes.dir."
                );
            }
            ValidateDefaultLanguages(
                category.DefaultLanguages,
                configuredLanguages.Keys,
                $"categories.{key}.default_languages"
            );
        }
        categoryConfigs = categoryConfigs.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase
        );
        var categories = categoryConfigs.ToDictionary(
            pair => pair.Key,
            pair => new ResolvedCategory(
                pair.Key,
                ResolvePath(root, pair.Value.Notes!.Dir!),
                string.IsNullOrWhiteSpace(pair.Value.Notes.Template)
                    ? globalNotesTemplate
                    : ResolvePath(root, pair.Value.Notes.Template),
                pair.Value.DefaultLanguages ?? config.DefaultLanguages
            ),
            StringComparer.OrdinalIgnoreCase
        );
        var configuredNotesDirectories = new HashSet<string>(PathComparer) { notesDirectory };
        foreach (var category in categories.Values)
        {
            if (!configuredNotesDirectories.Add(Path.GetFullPath(category.NotesDirectory)))
            {
                throw new InvalidOperationException(
                    $"Category '{category.Key}' notes.dir must be different from notes.dir and every other category notes.dir."
                );
            }
        }
        if (config.DefaultCategory is not null && !categories.ContainsKey(config.DefaultCategory))
        {
            throw new InvalidOperationException(
                $"default_category '{config.DefaultCategory}' must name a configured category: {string.Join(", ", categories.Keys)}."
            );
        }
        var defaultCategory = config.DefaultCategory is null
            ? null
            : categories[config.DefaultCategory].Key;

        var configuredIndex = string.IsNullOrWhiteSpace(config.IndexFile)
            ? Path.Combine(config.Notes.Dir, "README.md")
            : config.IndexFile;
        var index = ResolvePath(root, configuredIndex);

        var languages = configuredLanguages.ToDictionary(
            pair => pair.Key,
            pair => new ResolvedLanguage(
                pair.Key,
                ResolvePath(root, pair.Value.Directory),
                pair.Value.Extension,
                ResolvePath(root, pair.Value.Template),
                pair.Value.Vars
                    ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
                pair.Value.Naming ?? "pascal"
            ),
            StringComparer.OrdinalIgnoreCase
        );
        return new RepositoryConfig(
            root,
            configPath,
            notesDirectory,
            index,
            string.IsNullOrWhiteSpace(config.Notes.Template) ? null : globalNotesTemplate,
            config.DefaultLanguages,
            languages,
            categories,
            defaultCategory,
            config.Strict == true
        );
    }

    private static void ValidateDefaultLanguages(
        IEnumerable<string>? defaults,
        IEnumerable<string> configuredLanguages,
        string settingName
    )
    {
        var configured = configuredLanguages.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = defaults?.FirstOrDefault(language => !configured.Contains(language));
        if (unknown is not null)
        {
            throw new InvalidOperationException(
                $"{settingName} names unknown language '{unknown}'. Configured languages: {string.Join(", ", configured)}."
            );
        }
    }

    private static void ApplyOverrides(
        ProbConfig config,
        IEnumerable<string> overrides,
        string currentDirectory
    )
    {
        foreach (var entry in overrides)
        {
            var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]))
            {
                throw new InvalidOperationException(
                    $"Invalid --set '{entry}'. Use --set key=value."
                );
            }
            var key = parts[0];
            var value = parts[1];
            if (key == "notes.dir")
            {
                config.Notes ??= new NotesConfig();
                config.Notes.Dir = ResolvePath(currentDirectory, value);
            }
            else if (key == "notes.template")
            {
                config.Notes ??= new NotesConfig();
                config.Notes.Template =
                    value.Length == 0 ? null : ResolvePath(currentDirectory, value);
            }
            else if (key == "index_file")
            {
                config.IndexFile = value.Length == 0 ? null : ResolvePath(currentDirectory, value);
            }
            else if (key == "strict")
            {
                if (!bool.TryParse(value, out var strict))
                {
                    throw new InvalidOperationException(
                        $"Invalid --set value '{value}' for strict; use true or false."
                    );
                }
                config.Strict = strict;
            }
            else if (key == "default_languages")
            {
                config.DefaultLanguages = ParseOverrideList(value, key);
            }
            else if (key == "default_category")
            {
                config.DefaultCategory = value.Length == 0 ? null : value;
            }
            else if (key.StartsWith("categories.", StringComparison.Ordinal))
            {
                ApplyCategoryOverride(config, key, value, currentDirectory);
            }
            else if (key.StartsWith("languages.", StringComparison.Ordinal))
            {
                ApplyLanguageOverride(config, key, value, currentDirectory);
            }
            else
            {
                throw InvalidOverrideKey(key, config);
            }
        }
    }

    private static void ApplyCategoryOverride(
        ProbConfig config,
        string key,
        string value,
        string currentDirectory
    )
    {
        var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (
            parts.Length < 3
            || config.Categories is null
            || !config.Categories.TryGetValue(parts[1], out var category)
        )
        {
            throw InvalidOverrideKey(key, config);
        }
        category.Notes ??= new NotesConfig();
        switch (parts.Length)
        {
            case 4 when parts[2] == "notes" && parts[3] == "dir":
                category.Notes.Dir =
                    value.Length == 0 ? null : ResolvePath(currentDirectory, value);
                break;
            case 4 when parts[2] == "notes" && parts[3] == "template":
                category.Notes.Template =
                    value.Length == 0 ? null : ResolvePath(currentDirectory, value);
                break;
            case 3 when parts[2] == "default_languages":
                category.DefaultLanguages = ParseOverrideList(value, key);
                break;
            default:
                throw InvalidOverrideKey(key, config);
        }
    }

    private static void ApplyLanguageOverride(
        ProbConfig config,
        string key,
        string value,
        string currentDirectory
    )
    {
        var parts = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3 || !config.Languages!.TryGetValue(parts[1], out var language))
        {
            throw InvalidOverrideKey(key, config);
        }
        var pathValue = parts[2] is "dir" or "template";
        var normalizedValue =
            pathValue && value.Length > 0 ? ResolvePath(currentDirectory, value) : value;
        switch (parts.Length)
        {
            case 3 when parts[2] == "dir":
                language.Directory = normalizedValue;
                break;
            case 3 when parts[2] == "extension":
                language.Extension = value;
                break;
            case 3 when parts[2] == "template":
                language.Template = normalizedValue;
                break;
            case 3 when parts[2] == "naming":
                language.Naming = value;
                break;
            case 4 when parts[2] == "vars":
                language.Vars ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                language.Vars[parts[3]] = value;
                break;
            default:
                throw InvalidOverrideKey(key, config);
        }
    }

    private static List<string> ParseOverrideList(string value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }
        var values = value.Split(',', StringSplitOptions.TrimEntries);
        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                $"Invalid --set value for {key}; use a comma-separated list without empty entries."
            );
        }
        return [.. values];
    }

    private static InvalidOperationException InvalidOverrideKey(string key, ProbConfig config)
    {
        var valid = new List<string>
        {
            "notes.dir",
            "notes.template",
            "index_file",
            "strict",
            "default_languages",
            "default_category",
        };
        foreach (var category in config.Categories ?? [])
        {
            valid.AddRange([
                $"categories.{category.Key}.notes.dir",
                $"categories.{category.Key}.notes.template",
                $"categories.{category.Key}.default_languages",
            ]);
        }
        foreach (var (language, languageConfig) in config.Languages ?? [])
        {
            valid.AddRange([
                $"languages.{language}.dir",
                $"languages.{language}.extension",
                $"languages.{language}.template",
                $"languages.{language}.naming",
                .. (languageConfig.Vars ?? []).Keys.Select(variable =>
                    $"languages.{language}.vars.{variable}"
                ),
            ]);
        }
        return new InvalidOperationException(
            $"Unknown --set key '{key}'. Valid key paths: {string.Join(", ", valid)}."
        );
    }

    /// <summary>Resolves a relative filesystem path against its intended base directory.</summary>
    private static string ResolvePath(string root, string path) => Path.GetFullPath(path, root);

    /// <summary>Converts a title or lookup query into its lowercase Markdown filename slug.</summary>
    private static string Slugify(string title)
    {
        var slug = Regex.Replace(title.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (slug.Length == 0)
        {
            throw new InvalidOperationException($"'{title}' produces an empty problem slug.");
        }
        return slug;
    }

    /// <summary>Converts a problem title into a PascalCase, identifier-safe source type name.</summary>
    private static string ClassName(string title)
    {
        var normalized = Regex.Replace(title.Trim(), @"(?<=[\p{Ll}\p{Nd}])(?=\p{Lu})", " ");
        var words = Regex
            .Matches(normalized, @"[\p{L}\p{Nd}]+")
            .Select(match => match.Value)
            .ToArray();
        if (words.Length == 0)
        {
            throw new InvalidOperationException(
                "Problem title must contain at least one letter or digit."
            );
        }
        var identifier = string.Concat(
            words.Select(word =>
            {
                var tail = word.Length > 1 ? word[1..] : "";
                if (word.All(char.IsUpper))
                {
                    tail = tail.ToLowerInvariant();
                }
                return char.ToUpperInvariant(word[0]) + tail;
            })
        );
        return char.IsDigit(identifier[0]) ? $"Problem{identifier}" : identifier;
    }

    /// <summary>Creates a configured source filename stem while keeping the template type name PascalCase.</summary>
    private static string FileStem(string className, string naming)
    {
        if (naming == "pascal")
        {
            return className;
        }
        var words = Regex
            .Replace(className, @"([a-z0-9])([A-Z])", "$1 $2")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return naming switch
        {
            "snake" => string.Join('_', words).ToLowerInvariant(),
            "kebab" => string.Join('-', words).ToLowerInvariant(),
            "camel" => char.ToLowerInvariant(className[0]) + className[1..],
            _ => throw new InvalidOperationException($"Unsupported naming style '{naming}'."),
        };
    }

    /// <summary>Normalizes tags to lowercase kebab-case identifiers.</summary>
    private static List<string> NormalizeTags(IEnumerable<string> values)
    {
        var tags = values.Select(value => value?.Trim().ToLowerInvariant() ?? "").ToList();
        if (tags.Any(tag => tag.Length == 0))
        {
            throw new InvalidOperationException(
                "Tags cannot be empty; use lowercase letters and digits separated by single hyphens."
            );
        }
        var invalid = tags.FirstOrDefault(tag => !Regex.IsMatch(tag, @"^[a-z0-9]+(-[a-z0-9]+)*$"));
        if (invalid is not null)
        {
            throw new InvalidOperationException(
                $"Invalid tag '{invalid}'. Tags must use lowercase letters and digits separated by single hyphens."
            );
        }
        return [.. tags.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Validates and canonicalizes the supported problem status values.</summary>
    private static string NormalizeStatus(string status) =>
        status.Trim().ToLowerInvariant() switch
        {
            "solved" => "solved",
            "pending" => "pending",
            "revisit" => "revisit",
            _ => throw new InvalidOperationException("Status must be solved, pending, or revisit."),
        };

    /// <summary>Parses a metadata date using the required ISO YYYY-MM-DD representation.</summary>
    private static DateOnly ParseDate(string value, string field) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date
        )
            ? date
            : throw new InvalidOperationException($"{field} must use YYYY-MM-DD format.");

    /// <summary>Parses one frontmatter document and preserves its Markdown body verbatim except for line-ending normalization.</summary>
    private static ProblemDocument ReadDocument(string path)
    {
        var text = File.ReadAllText(path)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        if (!text.StartsWith("---\n", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Missing YAML frontmatter: {path}");
        }
        var end = text.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            throw new InvalidOperationException($"Unterminated YAML frontmatter: {path}");
        }
        var yaml = text[4..end];
        var fields =
            YamlReader.Deserialize<Dictionary<string, object?>>(yaml)
            ?? throw new InvalidOperationException($"Empty YAML frontmatter: {path}");
        var requiredFields = new[] { "title", "status", "created" };
        var missingFields = requiredFields
            .Where(required =>
                !fields.Keys.Any(key => key.Equals(required, StringComparison.OrdinalIgnoreCase))
            )
            .ToArray();
        if (missingFields.Length > 0)
        {
            throw new InvalidOperationException(
                $"Frontmatter in '{path}' is missing required field(s): {string.Join(", ", missingFields)}."
            );
        }
        var metadata =
            YamlReader.Deserialize<ProblemMetadata>(yaml)
            ?? throw new InvalidOperationException($"Empty YAML frontmatter: {path}");
        metadata.Implementations = metadata.Implementations is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(
                metadata.Implementations,
                StringComparer.OrdinalIgnoreCase
            );
        metadata.Tags = NormalizeTags(metadata.Tags ?? []);
        if (string.IsNullOrWhiteSpace(metadata.Title))
        {
            throw new InvalidOperationException($"Frontmatter title is required: {path}");
        }
        if (string.IsNullOrWhiteSpace(metadata.Status))
        {
            throw new InvalidOperationException($"Frontmatter status is required: {path}");
        }
        metadata.Status = NormalizeStatus(metadata.Status);
        if (string.IsNullOrWhiteSpace(metadata.Created))
        {
            throw new InvalidOperationException($"Frontmatter created date is required: {path}");
        }
        _ = ParseDate(metadata.Created, "created");
        if (metadata.SolvedOn is not null)
        {
            _ = ParseDate(metadata.SolvedOn, "solved_on");
        }
        return new ProblemDocument(path, metadata, text[(end + 5)..]);
    }

    /// <summary>Reads problem documents, skipping or rejecting Markdown files without front matter according to strict mode.</summary>
    private static List<ProblemDocument> ReadProblems(RepositoryConfig repository)
    {
        var directories = new[] { repository.NotesDirectory }
            .Concat(repository.Categories.Values.Select(category => category.NotesDirectory))
            .Distinct(PathComparer);
        var files = directories
            .Where(Directory.Exists)
            .SelectMany(directory =>
                Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
            )
            .Where(path =>
                !PathComparer.Equals(Path.GetFullPath(path), Path.GetFullPath(repository.IndexFile))
            )
            .Distinct(PathComparer)
            .ToArray();
        var withoutFrontMatter = files
            .Where(path => !File.ReadAllText(path).StartsWith("---", StringComparison.Ordinal))
            .ToArray();
        if (withoutFrontMatter.Length > 0)
        {
            if (repository.Strict)
            {
                throw new InvalidOperationException(
                    "Markdown files without front matter are not allowed in strict mode:\n"
                        + string.Join(Environment.NewLine, withoutFrontMatter)
                );
            }
            if (!_nonFrontMatterWarningShown)
            {
                _nonFrontMatterWarningShown = true;
                AnsiConsole.MarkupLine(
                    "[yellow]Warning: skipping Markdown files without front matter:[/]"
                );
                foreach (var path in withoutFrontMatter)
                {
                    AnsiConsole.WriteLine(path);
                }
            }
        }
        return
        [
            .. files
                .Except(withoutFrontMatter, PathComparer)
                .Select(path =>
                    ReadDocument(path) with
                    {
                        Category = CategoryForPath(repository, path),
                    }
                ),
        ];
    }

    private static string? CategoryForPath(RepositoryConfig repository, string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path))!;
        return repository
            .Categories.Values.FirstOrDefault(category =>
                PathComparer.Equals(Path.GetFullPath(category.NotesDirectory), parent)
            )
            ?.Key;
    }

    /// <summary>Serializes frontmatter while preserving explicit null for an unknown solved date.</summary>
    private static string RenderMetadata(ProblemMetadata metadata)
    {
        var yaml = YamlWriter.Serialize(metadata);
        if (metadata.SolvedOn is null)
        {
            yaml = Regex.Replace(yaml, @"(?m)^solved_on:[ \t]*\r?$", "solved_on: null");
        }
        if (metadata.Tags.Count == 0)
        {
            yaml = Regex.Replace(yaml, @"(?m)^tags:[ \t]*(?:\[\])?\r?\n?", "");
        }
        return yaml.TrimEnd();
    }

    /// <summary>Combines serialized YAML frontmatter and the Markdown body with LF line endings.</summary>
    private static string RenderDocument(ProblemMetadata metadata, string body) =>
        NormalizeLineEndings($"---\n{RenderMetadata(metadata)}\n---\n{body}");

    /// <summary>Creates a forward-slash implementation path relative to its problem Markdown document.</summary>
    private static string RelativeImplementation(
        RepositoryConfig repository,
        string markdownPath,
        string sourcePath
    )
    {
        EnsureSameRoot(Path.GetDirectoryName(markdownPath)!, sourcePath);
        return Path.GetRelativePath(Path.GetDirectoryName(markdownPath)!, sourcePath)
            .Replace('\\', '/');
    }

    /// <summary>Rejects relative links between filesystem roots that cannot share a relative path.</summary>
    private static void EnsureSameRoot(string source, string target)
    {
        var left = Path.GetPathRoot(Path.GetFullPath(source));
        var right = Path.GetPathRoot(Path.GetFullPath(target));
        if (!PathComparer.Equals(left, right))
        {
            throw new InvalidOperationException(
                $"Cannot create a relative link across filesystem roots: '{source}' and '{target}'."
            );
        }
    }

    /// <summary>Rebuilds only the managed solution-link block and places it immediately below the first heading.</summary>
    private static string UpdateLinks(
        string body,
        string markdownPath,
        ProblemMetadata metadata,
        RepositoryConfig repository
    )
    {
        var entries = metadata
            .Implementations.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair =>
            {
                var language = pair.Key.ToLowerInvariant();
                var absolute = ResolvePath(Path.GetDirectoryName(markdownPath)!, pair.Value);
                EnsureSameRoot(Path.GetDirectoryName(markdownPath)!, absolute);
                var relative = Path.GetRelativePath(Path.GetDirectoryName(markdownPath)!, absolute)
                    .Replace('\\', '/');
                return $"- {language}: [{Path.GetFileName(absolute)}]({Uri.EscapeDataString(relative).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)})";
            })
            .ToArray();
        var block = string.Join('\n', [LinkStart, .. entries, LinkEnd]);
        var start = body.IndexOf(LinkStart, StringComparison.Ordinal);
        var end = body.IndexOf(LinkEnd, StringComparison.Ordinal);
        if ((start >= 0) != (end >= 0) || end >= 0 && end < start)
        {
            throw new InvalidOperationException(
                $"Malformed solution-links markers in '{markdownPath}'."
            );
        }
        if (start >= 0)
        {
            body = body[..start] + body[(end + LinkEnd.Length)..];
        }
        var h1 = Regex.Match(body, @"(?m)^# .+$");
        if (h1.Success)
        {
            var afterHeading = h1.Index + h1.Length;
            return body[..afterHeading].TrimEnd()
                + "\n\n"
                + block
                + "\n\n"
                + body[afterHeading..].TrimStart('\n');
        }
        return block + (body.Length == 0 ? "" : "\n\n" + body);
    }

    /// <summary>Reconciles frontmatter implementation paths with configured scaffold files present on disk.</summary>
    private static void RebuildMetadata(
        RepositoryConfig repository,
        ProblemDocument problem,
        bool reportMissing = false
    )
    {
        var priorImplementations = problem.Metadata.Implementations;
        var implementations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var className = ClassName(problem.Metadata.Title);
        foreach (var language in repository.Languages.Values)
        {
            string? previous = null;
            if (priorImplementations.TryGetValue(language.Name, out var trackedPath))
            {
                previous = ResolvePath(Path.GetDirectoryName(problem.Path)!, trackedPath);
                if (File.Exists(previous))
                {
                    implementations[language.Name] = RelativeImplementation(
                        repository,
                        problem.Path,
                        previous
                    );
                    continue;
                }
                if (reportMissing)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]Missing tracked {Markup.Escape(language.Name)} implementation:[/] {Markup.Escape(previous)}"
                    );
                }
            }
            var canonical = Path.Combine(
                language.Directory,
                FileStem(className, language.Naming) + language.Extension
            );
            if (File.Exists(canonical))
            {
                implementations[language.Name] = RelativeImplementation(
                    repository,
                    problem.Path,
                    canonical
                );
            }
        }
        foreach (var (language, trackedPath) in priorImplementations)
        {
            if (repository.Languages.ContainsKey(language))
            {
                continue;
            }
            var absolute = ResolvePath(Path.GetDirectoryName(problem.Path)!, trackedPath);
            if (File.Exists(absolute))
            {
                implementations[language] = trackedPath;
            }
            else if (reportMissing)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Missing tracked {Markup.Escape(language)} implementation:[/] {Markup.Escape(absolute)}"
                );
            }
        }
        problem.Metadata.Implementations = implementations;
    }

    private static void RebaseImplementations(
        RepositoryConfig repository,
        ProblemMetadata metadata,
        string oldMarkdownPath,
        string newMarkdownPath
    )
    {
        foreach (var language in metadata.Implementations.Keys.ToArray())
        {
            var absolute = ResolvePath(
                Path.GetDirectoryName(oldMarkdownPath)!,
                metadata.Implementations[language]
            );
            metadata.Implementations[language] = RelativeImplementation(
                repository,
                newMarkdownPath,
                absolute
            );
        }
    }

    /// <summary>Renders one configured Scriban template with standard and language-specific variables.</summary>
    private static string RenderScaffold(
        ResolvedLanguage language,
        string title,
        string className,
        IReadOnlyList<PlatformId> platforms,
        bool sample
    )
    {
        if (!File.Exists(language.Template))
        {
            throw new FileNotFoundException(
                $"Template for language '{language.Name}' does not exist: {language.Template}",
                language.Template
            );
        }
        var template = Template.Parse(File.ReadAllText(language.Template), language.Template);
        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"Invalid Scriban template '{language.Template}': {string.Join("; ", template.Messages)}"
            );
        }

        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name,
            StrictVariables = true,
        };
        var script = new Scriban.Runtime.ScriptObject();
        foreach (var (key, value) in language.Vars)
        {
            script[key] = value;
        }
        script["class_name"] = className;
        script["file_stem"] = FileStem(className, language.Naming);
        script["title"] = title;
        script["platforms"] = platforms
            .Select(platform => new Dictionary<string, object?>
            {
                ["name"] = platform.Name,
                ["id"] = platform.Id,
                ["name_literal"] = QuoteSourceString(platform.Name),
                ["id_literal"] = QuoteSourceString(platform.Id),
                ["is_leetcode"] = platform.IsLeetCode,
            })
            .ToArray();
        script["has_platforms"] = platforms.Count > 0;
        script["has_leetcode"] = platforms.Any(platform => platform.IsLeetCode);
        script["sample"] = sample;
        context.PushGlobal(script);
        return NormalizeLf(template.Render(context).TrimEnd());
    }

    private static string RenderNotesTemplate(
        RepositoryConfig repository,
        string title,
        string slug,
        IReadOnlyList<string> tags,
        ResolvedCategory? category
    )
    {
        var path =
            category?.NotesTemplate
            ?? repository.NotesTemplate
            ?? throw new InvalidOperationException("A notes template path is required.");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Notes template does not exist: {path}", path);
        }
        var template = Template.Parse(File.ReadAllText(path), path);
        if (template.HasErrors)
        {
            throw new InvalidOperationException(
                $"Invalid Scriban template '{path}': {string.Join("; ", template.Messages)}"
            );
        }
        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name,
            StrictVariables = true,
        };
        var script = new Scriban.Runtime.ScriptObject
        {
            ["title"] = title,
            ["slug"] = slug,
            ["tags"] = tags.ToArray(),
        };
        if (repository.Categories.Count > 0)
        {
            script["category"] = category?.Key;
        }
        context.PushGlobal(script);
        return template
            .Render(context)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    /// <summary>Quotes a platform value as a safe C# or Java string literal.</summary>
    private static string QuoteSourceString(string value) =>
        "\""
        + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
        + "\"";

    /// <summary>Validates and canonicalizes repeatable platform name and identifier options.</summary>
    private static PlatformId[] ParsePlatforms(IEnumerable<string> values) =>
        [
            .. values.Select(value =>
            {
                var parts = value.Split(':', 2, StringSplitOptions.TrimEntries);
                if (
                    parts.Length != 2
                    || parts.Any(string.IsNullOrWhiteSpace)
                    || parts.Any(part => part.Any(char.IsControl))
                )
                {
                    throw new InvalidOperationException(
                        $"Platform must use Name:Id syntax: '{value}'."
                    );
                }
                var isLeetCode =
                    parts[0].Equals("leetcode", StringComparison.OrdinalIgnoreCase)
                    || parts[0].Equals("lc", StringComparison.OrdinalIgnoreCase)
                    || parts[0].Equals("leet code", StringComparison.OrdinalIgnoreCase);
                if (
                    isLeetCode
                    && !int.TryParse(
                        parts[1],
                        NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture,
                        out _
                    )
                )
                {
                    throw new InvalidOperationException(
                        $"LeetCode platform ID '{parts[1]}' must be a 32-bit integer."
                    );
                }
                return new PlatformId(isLeetCode ? "LeetCode" : parts[0], parts[1], isLeetCode);
            }),
        ];

    /// <summary>Selects configured languages from the CLI or configured defaults.</summary>
    private static string[] SelectLanguages(
        RepositoryConfig repository,
        string? requested,
        bool useDefaults = true,
        IReadOnlyList<string>? categoryDefaults = null
    )
    {
        var defaults = (categoryDefaults ?? repository.DefaultLanguages)
            ?.Where(language => !string.IsNullOrWhiteSpace(language))
            .Select(language => language.Trim())
            .ToArray();
        var selected = string.IsNullOrWhiteSpace(requested)
            ? !useDefaults
                ? []
                : defaults is { Length: > 0 }
                    ? defaults
                    : [.. repository.Languages.Keys]
            : requested.Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
            );
        if (selected.Length == 0 && useDefaults)
        {
            selected = [.. repository.Languages.Keys];
        }
        return
        [
            .. selected
                .Select(value =>
                {
                    var normalized = value.Trim().ToLowerInvariant();
                    if (!repository.Languages.ContainsKey(normalized))
                    {
                        throw new InvalidOperationException(
                            $"Unknown language '{value}'. Configured languages: {string.Join(", ", repository.Languages.Keys.Order(StringComparer.OrdinalIgnoreCase))}."
                        );
                    }
                    return normalized;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase),
        ];
    }

    /// <summary>Builds the Markdown file path for a problem slug.</summary>
    private static string ProblemPath(
        RepositoryConfig repository,
        string slug,
        string? category = null
    ) =>
        Path.Combine(
            category is null
                ? repository.NotesDirectory
                : repository.Categories[category].NotesDirectory,
            slug + ".md"
        );

    /// <summary>Finds exactly one problem by title or normalized slug.</summary>
    private static ProblemDocument FindProblem(RepositoryConfig repository, string query)
    {
        var slug = Slugify(query);
        var matches = ReadProblems(repository)
            .Where(problem =>
                problem.Metadata.Title.Equals(query, StringComparison.OrdinalIgnoreCase)
                || problem.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)
            )
            .ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException(
                $"Problem '{query}' was not found in {repository.NotesDirectory}."
            ),
            _ => throw new InvalidOperationException($"Problem lookup '{query}' is ambiguous."),
        };
    }

    /// <summary>Ensures a new title does not reuse another document's slug.</summary>
    private static void CheckSlugCollision(
        RepositoryConfig repository,
        string slug,
        string title,
        ProblemDocument? existing
    )
    {
        foreach (var problem in ReadProblems(repository))
        {
            if (
                existing is not null
                && PathComparer.Equals(
                    Path.GetFullPath(problem.Path),
                    Path.GetFullPath(existing.Path)
                )
            )
            {
                continue;
            }
            if (problem.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Slug collision: '{title}' conflicts with '{problem.Metadata.Title}' at '{slug}'."
                );
            }
        }
    }

    /// <summary>Prevents a generated source path from replacing a different problem's tracked implementation.</summary>
    private static void CheckSourceOwnership(
        RepositoryConfig repository,
        string sourcePath,
        ProblemDocument? existing
    )
    {
        foreach (var problem in ReadProblems(repository))
        {
            if (
                existing is not null
                && PathComparer.Equals(
                    Path.GetFullPath(problem.Path),
                    Path.GetFullPath(existing.Path)
                )
            )
            {
                continue;
            }
            foreach (var trackedPath in problem.Metadata.Implementations.Values)
            {
                var absolute = ResolvePath(Path.GetDirectoryName(problem.Path)!, trackedPath);
                if (PathComparer.Equals(Path.GetFullPath(absolute), Path.GetFullPath(sourcePath)))
                {
                    throw new InvalidOperationException(
                        $"Scaffold '{sourcePath}' is already tracked by problem '{problem.Metadata.Title}'."
                    );
                }
            }
        }
    }

    /// <summary>Plans, confirms, writes, and indexes the requested problem and its language scaffolds.</summary>
    public static int Create(NewSettings settings)
    {
        // Resolve configuration and normalize all user-facing identifiers/options first.
        var repository = Load(settings);
        var title = settings.Title.Trim();
        if (title.Length == 0 || title.Any(char.IsControl))
        {
            throw new InvalidOperationException(
                "Title cannot be empty or contain control characters."
            );
        }
        var slug = Slugify(title);
        var className = ClassName(title);
        if (settings.Category is not null && settings.NoCategory)
        {
            throw new InvalidOperationException(
                "--category and --no-category cannot be used together."
            );
        }
        if (settings.Category is not null && !repository.Categories.ContainsKey(settings.Category))
        {
            throw new InvalidOperationException(
                $"Unknown category '{settings.Category}'. Configured categories: {string.Join(", ", repository.Categories.Keys)}."
            );
        }
        var platforms = ParsePlatforms(settings.Platforms);
        var tags = NormalizeTags(
            settings.Tags.SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries))
        );
        var status = NormalizeStatus(settings.Status ?? "pending");
        var force = settings.Force?.Trim().ToLowerInvariant();
        if (force is not null and not ("metadata" or "scaffold" or "all"))
        {
            throw new InvalidOperationException("--force must be metadata, scaffold, or all.");
        }

        // Find any existing document and preserve its authored body and status metadata.
        var problems = ReadProblems(repository);
        var matching = problems
            .Where(problem =>
                problem.Metadata.Title.Equals(title, StringComparison.OrdinalIgnoreCase)
                || problem.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase)
            )
            .ToArray();
        if (matching.Length > 1)
        {
            throw new InvalidOperationException(
                $"More than one existing problem matches '{title}'."
            );
        }
        var existing = matching.SingleOrDefault();
        if (settings.Existing && existing is null)
        {
            throw new InvalidOperationException(
                $"Problem '{title}' was not found by title or slug; --existing never creates a problem."
            );
        }
        var targetCategory =
            settings.Category is not null ? repository.Categories[settings.Category].Key
            : settings.NoCategory ? null
            : existing is not null ? existing.Category
            : repository.DefaultCategory;
        var category = targetCategory is null ? null : repository.Categories[targetCategory];
        var languages = SelectLanguages(
            repository,
            settings.Languages,
            !settings.Existing,
            category?.DefaultLanguages
        );
        CheckSlugCollision(repository, slug, title, existing);
        var previousMarkdownPath = existing?.Path;
        var markdownPath =
            existing is null ? ProblemPath(repository, slug, targetCategory)
            : settings.Category is not null
            || settings.NoCategory && repository.Categories.Count > 0
                ? ProblemPath(repository, existing.Slug, targetCategory)
            : existing.Path;
        var moving =
            existing is not null
            && !PathComparer.Equals(
                Path.GetFullPath(existing.Path),
                Path.GetFullPath(markdownPath)
            );
        if (moving && File.Exists(markdownPath))
        {
            throw new InvalidOperationException(
                $"Cannot move problem to '{markdownPath}': the target path already exists."
            );
        }
        var metadata =
            existing?.Metadata
            ?? new ProblemMetadata
            {
                Title = title,
                Status = status,
                Created = DateOnly
                    .FromDateTime(DateTime.Today)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                SolvedOn =
                    status == "solved"
                        ? DateOnly
                            .FromDateTime(DateTime.Today)
                            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : null,
            };
        if (
            existing is not null
            && !metadata.Title.Equals(title, StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                $"Slug '{slug}' already belongs to '{metadata.Title}'."
            );
        }
        if (existing is null)
        {
            metadata.Status = status;
            metadata.Created = DateOnly
                .FromDateTime(DateTime.Today)
                .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        if (tags.Count > 0)
        {
            metadata.Tags = NormalizeTags(metadata.Tags.Concat(tags));
        }
        if (moving)
        {
            RebaseImplementations(repository, metadata, previousMarkdownPath!, markdownPath);
            RebuildMetadata(
                repository,
                new ProblemDocument(markdownPath, metadata, existing!.Body),
                reportMissing: true
            );
        }
        var body =
            existing?.Body
            ?? (
                (category?.NotesTemplate ?? repository.NotesTemplate) is null
                    ? $"# {title}\n\n## Keywords\n"
                    : RenderNotesTemplate(repository, title, slug, metadata.Tags, category)
            );
        var originalBody = body;
        var originalMetadata = RenderMetadata(metadata);

        // Reconcile source paths before scaffold processing when metadata is explicitly rebuilt.
        var forceMetadata = force is "metadata" or "all";
        if (forceMetadata && existing is not null && !moving)
        {
            // Reconcile paths from disk before adding missing language scaffolds in this invocation.
            RebuildMetadata(repository, new ProblemDocument(markdownPath, metadata, body));
        }

        // Render missing scaffolds, skip existing ones, and record each selected implementation.
        var planned = new List<PlannedWrite>();
        var forceScaffold = force is "scaffold" or "all";
        foreach (var languageName in languages)
        {
            var language = repository.Languages[languageName];
            if (
                settings.Existing
                && !forceScaffold
                && metadata.Implementations.TryGetValue(languageName, out var trackedPath)
            )
            {
                var trackedSource = ResolvePath(Path.GetDirectoryName(markdownPath)!, trackedPath);
                if (File.Exists(trackedSource))
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]Skipped existing {Markup.Escape(languageName)} implementation:[/] {Markup.Escape(trackedSource)}"
                    );
                    continue;
                }
                metadata.Implementations.Remove(languageName);
            }
            var source = Path.Combine(
                language.Directory,
                FileStem(className, language.Naming) + language.Extension
            );
            CheckSourceOwnership(repository, source, existing);
            if (!File.Exists(source))
            {
                if (existing is not null && force == "metadata")
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]No canonical {Markup.Escape(languageName)} scaffold; preserving metadata paths for existing files.[/]"
                    );
                    continue;
                }
                planned.Add(
                    new PlannedWrite(
                        source,
                        RenderScaffold(
                            language,
                            title,
                            className,
                            platforms,
                            !settings.NoSampleMethod
                        ),
                        "scaffold"
                    )
                );
                metadata.Implementations[languageName] = RelativeImplementation(
                    repository,
                    markdownPath,
                    source
                );
            }
            else if (forceScaffold)
            {
                planned.Add(
                    new PlannedWrite(
                        source,
                        RenderScaffold(
                            language,
                            title,
                            className,
                            platforms,
                            !settings.NoSampleMethod
                        ),
                        "scaffold"
                    )
                );
                metadata.Implementations[languageName] = RelativeImplementation(
                    repository,
                    markdownPath,
                    source
                );
            }
            else if (existing is not null || File.Exists(markdownPath))
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Skipped existing {Markup.Escape(languageName)} scaffold:[/] {Markup.Escape(source)}"
                );
                if (platforms.Length > 0)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]Ignored --platform for {Markup.Escape(languageName)} because its scaffold already exists.[/]"
                    );
                }
                if (!metadata.Implementations.ContainsKey(languageName))
                {
                    metadata.Implementations[languageName] = RelativeImplementation(
                        repository,
                        markdownPath,
                        source
                    );
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Scaffold collision at '{source}'. Use --force scaffold or --force all to overwrite."
                );
            }
        }

        // Update the managed links and metadata only when this command needs to change the document.
        if (
            !settings.Existing
            || forceMetadata
            || moving
            || languages.Length > 0
                && !string.Equals(
                    originalMetadata,
                    RenderMetadata(metadata),
                    StringComparison.Ordinal
                )
        )
        {
            body = UpdateLinks(body, markdownPath, metadata, repository);
        }
        var willWriteMetadata =
            existing is null
            || moving
            || forceMetadata
            || planned.Any(file => file.Kind == "scaffold")
            || !string.Equals(originalMetadata, RenderMetadata(metadata), StringComparison.Ordinal)
            || !string.Equals(originalBody, body, StringComparison.Ordinal);
        if (willWriteMetadata)
        {
            planned.Add(new PlannedWrite(markdownPath, RenderDocument(metadata, body), "metadata"));
        }

        // Show all intended changes first; destructive confirmation is only for real writes.
        var backupsRequired = planned
            .Where(write =>
                File.Exists(write.Path)
                && (write.Kind == "scaffold" || force == "all" && write.Kind == "metadata")
            )
            .Select(write => write.Path)
            .Distinct(PathComparer)
            .ToArray();
        if (backupsRequired.Length > 0 && !settings.Yes && !settings.DryRun)
        {
            if (Console.IsInputRedirected)
            {
                throw new InvalidOperationException(
                    "Forced scaffold overwrite requires an interactive terminal or --yes."
                );
            }
            AnsiConsole.MarkupLine("[yellow]The following files will be overwritten:[/]");
            foreach (var path in backupsRequired)
            {
                AnsiConsole.WriteLine(path);
            }
            if (!AnsiConsole.Confirm("Continue and create backups before overwriting?"))
            {
                AnsiConsole.MarkupLine("[red]Cancelled.[/]");
                return 1;
            }
        }

        // Derive the index from the post-command view, including a not-yet-written new document.
        var indexContent = BuildIndex(
            problems
                .Select(problem =>
                    existing is not null
                    && PathComparer.Equals(
                        Path.GetFullPath(problem.Path),
                        Path.GetFullPath(previousMarkdownPath!)
                    )
                        ? new ProblemDocument(markdownPath, metadata, body)
                        {
                            Category = targetCategory,
                        }
                        : problem
                )
                .Concat(
                    existing is null && willWriteMetadata
                        ?
                        [
                            new ProblemDocument(markdownPath, metadata, body)
                            {
                                Category = targetCategory,
                            },
                        ]
                        : []
                ),
            repository
        );
        if (moving)
        {
            AnsiConsole.MarkupLine(
                $"Moving {Markup.Escape(previousMarkdownPath!)} -> {Markup.Escape(markdownPath)}"
            );
        }
        if (settings.DryRun)
        {
            // Apply writes only after the dry-run branch and any force confirmation have completed.
            foreach (var write in planned)
            {
                AnsiConsole.MarkupLine($"[green]Would write:[/] {Markup.Escape(write.Path)}");
                if (settings.Verbose)
                {
                    AnsiConsole.WriteLine(write.Content);
                }
            }
            AnsiConsole.MarkupLine(
                $"[green]Would update index:[/] {Markup.Escape(repository.IndexFile)}"
            );
            if (settings.Verbose)
            {
                AnsiConsole.WriteLine(indexContent);
            }
            return 0;
        }

        if (moving)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(markdownPath)!);
            File.Move(previousMarkdownPath!, markdownPath);
        }
        foreach (var write in planned)
        {
            if (write.Kind == "scaffold" || write.Kind == "metadata")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(write.Path)!);
                if (
                    File.Exists(write.Path)
                    && (write.Kind == "scaffold" || force == "all" && write.Kind == "metadata")
                )
                {
                    WriteBackup(write.Path);
                }
                var content =
                    write.Kind == "metadata"
                        ? NormalizeLineEndings(write.Content)
                        : NormalizeLf(write.Content);
                File.WriteAllText(write.Path, content, new UTF8Encoding(false));
            }
        }
        WriteIndex(repository, indexContent);
        foreach (var write in planned)
        {
            AnsiConsole.MarkupLine($"[green]Wrote:[/] {Markup.Escape(write.Path)}");
        }
        AnsiConsole.MarkupLine($"[green]Updated index:[/] {Markup.Escape(repository.IndexFile)}");
        return 0;
    }

    /// <summary>Copies a file to the first available .bak, .bak.1, .bak.2, and so on path.</summary>
    private static void WriteBackup(string path)
    {
        var candidate = path + ".bak";
        var suffix = 1;
        while (File.Exists(candidate))
        {
            candidate = path + $".bak.{suffix++}";
        }
        File.Copy(path, candidate);
        AnsiConsole.MarkupLine($"[grey]Backup:[/] {Markup.Escape(candidate)}");
    }

    /// <summary>Filters and displays problems by title terms, status, and configured language.</summary>
    public static int List(ListSettings settings)
    {
        var repository = Load(settings);
        var format = settings.Long ? "long" : settings.Format.Trim().ToLowerInvariant();
        if (format is not ("table" or "long" or "slugs" or "json"))
        {
            throw new InvalidOperationException("--format must be table, long, slugs, or json.");
        }
        if (settings.Fields is not null && format is "long" or "slugs")
        {
            throw new InvalidOperationException("--fields can only be used with table or json.");
        }
        var sort = settings.Sort.Trim().ToLowerInvariant();
        if (sort is not ("title" or "created" or "solved_on"))
        {
            throw new InvalidOperationException("--sort must be title, created, or solved_on.");
        }

        var titleTerms = new List<string>();
        var filters = new List<Func<ProblemDocument, bool>>();
        foreach (
            var token in (settings.Query ?? "").Split(
                ' ',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
            )
        )
        {
            if (token.StartsWith("-", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Bare query terms cannot start with '-': '{token}'. Use a field clause such as status:-solved."
                );
            }
            var match = Regex.Match(
                token,
                @"^(?<field>[a-zA-Z][a-zA-Z0-9_-]*)(?<operator>!=|=|:)(?<value>.*)$"
            );
            if (match.Success)
            {
                filters.Add(
                    ParseFilter(
                        repository,
                        match.Groups["field"].Value.ToLowerInvariant(),
                        match.Groups["operator"].Value,
                        match.Groups["value"].Value
                    )
                );
            }
            else if (token.Contains(':') || token.Contains('='))
            {
                throw new InvalidOperationException(
                    $"Invalid query clause '{token}'. Write clauses as field<operator>value, separated by spaces."
                );
            }
            else
            {
                titleTerms.Add(token);
            }
        }

        var results = ReadProblems(repository)
            .Where(problem => filters.All(filter => filter(problem)))
            .Where(problem =>
                titleTerms.All(term =>
                    problem.Metadata.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                )
            )
            .OrderBy(
                problem => problem,
                Comparer<ProblemDocument>.Create(
                    (left, right) =>
                    {
                        var comparison = CompareSortValues(left, right, sort, settings.Reverse);
                        return comparison != 0
                            ? comparison
                            : StringComparer.Ordinal.Compare(left.Slug, right.Slug);
                    }
                )
            )
            .ToArray();

        if (format == "slugs")
        {
            foreach (var problem in results)
            {
                Console.WriteLine(problem.Slug);
            }
            return 0;
        }

        var hasCategories = repository.Categories.Count > 0;
        var fields = ParseListFields(settings.Fields, format, hasCategories);
        if (format == "json")
        {
            var json = results.Select(problem =>
                new Dictionary<string, object?>
                {
                    ["title"] = problem.Metadata.Title,
                    ["slug"] = problem.Slug,
                    ["status"] = problem.Metadata.Status,
                    ["created"] = problem.Metadata.Created,
                    ["solved_on"] = problem.Metadata.SolvedOn,
                    ["languages"] = LanguageOrder(repository)
                        .Where(problem.Metadata.Implementations.ContainsKey)
                        .ToArray(),
                    ["tags"] = problem.Metadata.Tags,
                    ["category"] = hasCategories
                        ? problem.Category ?? CategoryForPath(repository, problem.Path)
                        : null,
                    ["path"] = Path.GetRelativePath(repository.Root, problem.Path)
                        .Replace('\\', '/'),
                    ["implementations"] = new Dictionary<string, string>(
                        problem.Metadata.Implementations,
                        StringComparer.OrdinalIgnoreCase
                    ),
                }
                    .Where(pair => fields.Contains(pair.Key, StringComparer.Ordinal))
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
            );
            Console.WriteLine(
                JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true })
            );
            return 0;
        }

        var table = new Table().Border(TableBorder.Rounded).Title("Problems");
        foreach (var field in fields)
        {
            table.AddColumn(ListFieldLabel(field));
        }
        foreach (var problem in results)
        {
            table.AddRow(
                fields
                    .Select(field => Markup.Escape(ListFieldValue(problem, field, repository)))
                    .ToArray()
            );
        }
        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[grey]{results.Length} problem(s)[/]");
        return 0;
    }

    private static Func<ProblemDocument, bool> ParseFilter(
        RepositoryConfig repository,
        string field,
        string op,
        string value
    )
    {
        var categoryEnabled = repository.Categories.Count > 0;
        var supported = categoryEnabled
            ? "status, lang, tag, category, solved, created"
            : "status, lang, tag, solved, created";
        if (
            field is not ("status" or "lang" or "tag" or "solved" or "created")
            && !(categoryEnabled && field == "category")
        )
        {
            throw new InvalidOperationException(
                $"Unknown query field '{field}'. Supported fields: {supported}."
            );
        }
        if (value.Length == 0)
        {
            throw new InvalidOperationException($"Query value for '{field}' cannot be empty.");
        }
        if (field is "lang" or "tag")
        {
            var isExact = op is "=" or "!=";
            if (op == ":" && (value.Contains(',') && value.Contains('|')))
            {
                throw new InvalidOperationException(
                    $"'{field}:{value}' mixes commas and pipes; split the clause into two tokens."
                );
            }
            if (
                isExact
                && (value.Contains('|') || value.Split(',').Any(term => term.StartsWith('-')))
            )
            {
                throw new InvalidOperationException(
                    $"Use comma-separated names with {field}= or {field}!=; pipes and negated terms are not supported."
                );
            }
            if (isExact || op == ":")
            {
                var separator = isExact || value.Contains(',') ? ',' : '|';
                var terms = value.Split(separator, StringSplitOptions.TrimEntries);
                if (terms.Any(string.IsNullOrEmpty))
                {
                    throw new InvalidOperationException(
                        $"Empty term in '{field}{op}{value}'. Supply a name for every comma or pipe."
                    );
                }
                if (
                    terms.Any(term => term.Contains('*'))
                    && !(terms.Length == 1 && terms[0] is "*" or "-*")
                )
                {
                    throw new InvalidOperationException(
                        "Wildcards must be the only value: use '*' for any value or '-*' for none."
                    );
                }
                var normalizedTerms = terms
                    .Select(term =>
                    {
                        if (term is "*" or "-*")
                        {
                            return term;
                        }
                        var name = term.TrimStart('-').ToLowerInvariant();
                        if (field == "lang" && !repository.Languages.ContainsKey(name))
                        {
                            throw new InvalidOperationException(
                                $"Unknown language '{name}'. Configured languages: {string.Join(", ", LanguageOrder(repository))}."
                            );
                        }
                        if (field == "tag" && !Regex.IsMatch(name, @"^[a-z0-9]+(-[a-z0-9]+)*$"))
                        {
                            throw new InvalidOperationException(
                                $"Invalid tag '{term}'. Tags must use letters and digits separated by single hyphens."
                            );
                        }
                        return term;
                    })
                    .ToArray();

                if (op == ":")
                {
                    if (terms.Length == 1 && terms[0] == "*")
                    {
                        return problem => MultiValues(problem, field).Count > 0;
                    }
                    if (terms.Length == 1 && terms[0] == "-*")
                    {
                        return problem => MultiValues(problem, field).Count == 0;
                    }
                    if (terms.Any(term => term is "*" or "-*"))
                    {
                        throw new InvalidOperationException(
                            "Wildcards must be the only value: use '*' for any value or '-*' for none."
                        );
                    }
                    bool MatchesTerm(ProblemDocument problem, string term)
                    {
                        var values = MultiValues(problem, field);
                        var missing = term.StartsWith("-", StringComparison.Ordinal);
                        var name = term.TrimStart('-').ToLowerInvariant();
                        var contains = values.Contains(name, StringComparer.OrdinalIgnoreCase);
                        return missing ? !contains : contains;
                    }
                    return separator == ','
                        ? problem => normalizedTerms.All(term => MatchesTerm(problem, term))
                        : problem => normalizedTerms.Any(term => MatchesTerm(problem, term));
                }
                var expected = normalizedTerms.ToHashSet(StringComparer.OrdinalIgnoreCase);
                return problem =>
                {
                    var actual = MultiValues(problem, field)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var equal = actual.SetEquals(expected);
                    return op == "=" ? equal : !equal;
                };
            }
        }
        if (field is "status" or "category")
        {
            if (op != ":")
            {
                throw new InvalidOperationException(
                    $"'{field}' is single-valued; use '{field}:value' (use a pipe for alternatives), not '{op}'."
                );
            }
            if (value.Contains(','))
            {
                throw new InvalidOperationException(
                    $"'{field}' is single-valued; use a pipe instead of a comma."
                );
            }
            var terms = value.Split('|', StringSplitOptions.TrimEntries);
            if (
                terms.Any(string.IsNullOrEmpty)
                || terms.Any(term => term.StartsWith('-')) && terms.Length > 1
            )
            {
                throw new InvalidOperationException(
                    field == "status"
                        ? "Use one negated status (for example status:-solved), or positive alternatives separated by '|'."
                        : "Use one negated category (for example category:-arrays), or positive alternatives separated by '|'."
                );
            }
            if (field == "category")
            {
                if (
                    terms.Any(term => term.Contains('*'))
                    && !(terms.Length == 1 && terms[0] is "*" or "-*")
                )
                {
                    throw new InvalidOperationException(
                        "Wildcards must be the only value: use category:* for categorized problems or category:-* for uncategorized problems."
                    );
                }
                foreach (var term in terms.Where(term => term is not ("*" or "-*")))
                {
                    var name = term.TrimStart('-');
                    if (name.Length == 0 || !repository.Categories.ContainsKey(name))
                    {
                        throw new InvalidOperationException(
                            $"Unknown category '{name}'. Configured categories: {string.Join(", ", repository.Categories.Keys)}."
                        );
                    }
                }
                if (terms.Length == 1 && terms[0] == "*")
                {
                    return problem =>
                        (problem.Category ?? CategoryForPath(repository, problem.Path)) is not null;
                }
                if (terms.Length == 1 && terms[0] == "-*")
                {
                    return problem =>
                        (problem.Category ?? CategoryForPath(repository, problem.Path)) is null;
                }
                var categories = terms.Select(term => term.TrimStart('-')).ToArray();
                var negatedCategory = terms[0].StartsWith("-", StringComparison.Ordinal);
                return problem =>
                {
                    var actual = problem.Category ?? CategoryForPath(repository, problem.Path);
                    var matched = categories.Contains(actual, StringComparer.OrdinalIgnoreCase);
                    return negatedCategory ? !matched : matched;
                };
            }
            var statuses = terms.Select(term => NormalizeStatus(term.TrimStart('-'))).ToArray();
            var negated = terms[0].StartsWith("-", StringComparison.Ordinal);
            return problem =>
                negated
                    ? !statuses.Contains(problem.Metadata.Status, StringComparer.OrdinalIgnoreCase)
                    : statuses.Contains(problem.Metadata.Status, StringComparer.OrdinalIgnoreCase);
        }
        if (op != ":")
        {
            throw new InvalidOperationException(
                $"'{field}' accepts only ':'. Use '{field}:value'."
            );
        }
        if (value.Contains('-') && (field == "solved" || field == "created"))
        {
            if (value.StartsWith("-", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Negation is not supported for {field}; use status:-solved for unsolved problems."
                );
            }
        }
        if (!Regex.IsMatch(value, @"^\d{4}(?:-\d{2})?(?:-\d{2})?$"))
        {
            throw new InvalidOperationException(
                $"{field}: requires an ISO date prefix: YYYY, YYYY-MM, or YYYY-MM-DD."
            );
        }
        ValidateDatePrefix(value, field);
        return problem =>
        {
            var date = field == "created" ? problem.Metadata.Created : problem.Metadata.SolvedOn;
            return date is not null && date.StartsWith(value, StringComparison.Ordinal);
        };
    }

    private static void ValidateDatePrefix(string value, string field)
    {
        var pieces = value.Split('-');
        if (
            !int.TryParse(pieces[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || year < 1
            || year > 9999
            || pieces.Length > 1
                && (!int.TryParse(pieces[1], out var month) || month is < 1 or > 12)
            || pieces.Length > 2
                && (
                    !int.TryParse(pieces[2], out var day)
                    || !DateOnly.TryParseExact(
                        value,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out _
                    )
                )
        )
        {
            throw new InvalidOperationException(
                $"Invalid {field} date prefix '{value}'. Use a valid YYYY, YYYY-MM, or YYYY-MM-DD prefix."
            );
        }
    }

    private static List<string> MultiValues(ProblemDocument problem, string field) =>
        field == "lang" ? [.. problem.Metadata.Implementations.Keys] : problem.Metadata.Tags;

    private static int CompareSortValues(
        ProblemDocument left,
        ProblemDocument right,
        string sort,
        bool reverse
    )
    {
        string? leftValue = sort switch
        {
            "title" => left.Metadata.Title,
            "created" => left.Metadata.Created,
            _ => left.Metadata.SolvedOn,
        };
        string? rightValue = sort switch
        {
            "title" => right.Metadata.Title,
            "created" => right.Metadata.Created,
            _ => right.Metadata.SolvedOn,
        };
        if (leftValue is null || rightValue is null)
        {
            return leftValue is null
                ? rightValue is null
                    ? 0
                    : 1
                : -1;
        }
        var comparison =
            sort == "title"
                ? StringComparer.OrdinalIgnoreCase.Compare(leftValue, rightValue)
                : StringComparer.Ordinal.Compare(leftValue, rightValue);
        return reverse ? -comparison : comparison;
    }

    private static string[] ParseListFields(
        string? requested,
        string format,
        bool categoriesConfigured
    )
    {
        var defaults =
            format == "long"
                ? categoriesConfigured
                    ? new[]
                    {
                        "title",
                        "status",
                        "languages",
                        "created",
                        "solved_on",
                        "tags",
                        "category",
                        "path",
                    }
                    : new[]
                    {
                        "title",
                        "status",
                        "languages",
                        "created",
                        "solved_on",
                        "tags",
                        "path",
                    }
                : format == "json"
                    ? categoriesConfigured
                        ? new[]
                        {
                            "title",
                            "slug",
                            "status",
                            "created",
                            "solved_on",
                            "languages",
                            "tags",
                            "category",
                            "path",
                            "implementations",
                        }
                        : new[]
                        {
                            "title",
                            "slug",
                            "status",
                            "created",
                            "solved_on",
                            "languages",
                            "tags",
                            "path",
                            "implementations",
                        }
                    : new[] { "title", "status", "languages" };
        if (requested is null)
        {
            return defaults;
        }
        var allowed =
            format == "json"
                ? categoriesConfigured
                    ? new[]
                    {
                        "title",
                        "slug",
                        "status",
                        "created",
                        "solved_on",
                        "languages",
                        "tags",
                        "category",
                        "path",
                        "implementations",
                    }
                    : new[]
                    {
                        "title",
                        "slug",
                        "status",
                        "created",
                        "solved_on",
                        "languages",
                        "tags",
                        "path",
                        "implementations",
                    }
                : categoriesConfigured
                    ? new[]
                    {
                        "title",
                        "slug",
                        "status",
                        "created",
                        "solved_on",
                        "languages",
                        "tags",
                        "category",
                        "path",
                    }
                    : new[]
                    {
                        "title",
                        "slug",
                        "status",
                        "created",
                        "solved_on",
                        "languages",
                        "tags",
                        "path",
                    };
        var fields = requested
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(field =>
                field.ToLowerInvariant() switch
                {
                    "lang" => "languages",
                    "tag" => "tags",
                    var name => name,
                }
            )
            .ToArray();
        if (fields.Length == 0)
        {
            throw new InvalidOperationException("--fields must include at least one field.");
        }
        var invalid = fields.FirstOrDefault(field =>
            !allowed.Contains(field, StringComparer.Ordinal)
        );
        if (invalid is not null)
        {
            throw new InvalidOperationException(
                $"Unknown field '{invalid}' for {format}. Available fields: {string.Join(", ", allowed)}."
            );
        }
        return fields;
    }

    private static string ListFieldLabel(string field) =>
        field switch
        {
            "solved_on" => "Solved on",
            "languages" => "Languages",
            "category" => "Category",
            _ => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(field),
        };

    private static string ListFieldValue(
        ProblemDocument problem,
        string field,
        RepositoryConfig repository
    ) =>
        field switch
        {
            "title" => problem.Metadata.Title,
            "slug" => problem.Slug,
            "status" => problem.Metadata.Status,
            "created" => problem.Metadata.Created,
            "solved_on" => problem.Metadata.SolvedOn ?? "—",
            "languages" => string.Join(
                ", ",
                LanguageOrder(repository).Where(problem.Metadata.Implementations.ContainsKey)
            ),
            "tags" => string.Join(", ", problem.Metadata.Tags),
            "category" => problem.Category ?? CategoryForPath(repository, problem.Path) ?? "—",
            "path" => Path.GetRelativePath(repository.Root, problem.Path).Replace('\\', '/'),
            _ => throw new InvalidOperationException($"Unsupported table field '{field}'."),
        };

    private static string[] LanguageOrder(RepositoryConfig repository) =>
        [.. repository.Languages.Keys];

    /// <summary>Prints a clickable implementation link and optionally opens it in VS Code.</summary>
    public static int Link(LinkSettings settings)
    {
        var repository = Load(settings);
        if (settings.Line is < 1)
        {
            throw new InvalidOperationException("--line must be positive.");
        }
        string[] languageNames = settings.Languages is null
            ? []
            :
            [
                .. settings
                    .Languages.Split(
                        ',',
                        StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                    )
                    .Select(language => language.ToLowerInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase),
            ];
        var unknownLanguage = languageNames.FirstOrDefault(name =>
            !repository.Languages.ContainsKey(name)
        );
        if (unknownLanguage is not null)
        {
            throw new InvalidOperationException(
                $"Unknown language '{unknownLanguage}'. Configured languages: {string.Join(", ", LanguageOrder(repository))}."
            );
        }
        var problem = FindProblem(repository, settings.Problem);
        var printEverything = languageNames.Length == 0 && !settings.Notes;
        var selectedLanguages =
            printEverything || languageNames.Length > 0
                ? LanguageOrder(repository)
                    .Where(language =>
                        printEverything
                        || languageNames.Contains(language, StringComparer.OrdinalIgnoreCase)
                    )
                    .ToArray()
                : [];
        var selectedNotes = printEverything || settings.Notes;
        var links = new List<(string Label, string Path)>();
        if (selectedNotes)
        {
            links.Add(("notes", problem.Path));
        }

        List<string> unimplementedLanguages = [];
        foreach (var languageName in selectedLanguages)
        {
            if (!problem.Metadata.Implementations.TryGetValue(languageName, out var relative))
            {
                unimplementedLanguages.Add(languageName);
                continue;
            }
            var path = ResolvePath(Path.GetDirectoryName(problem.Path)!, relative);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Implementation file does not exist: {path}",
                    path
                );
            }
            links.Add((languageName, path));
        }
        if (settings.Line is not null && (printEverything || links.Count != 1))
        {
            throw new InvalidOperationException(
                "--line requires exactly one selected link; select one language or use --notes."
            );
        }
        var line = settings.Line ?? 1;
        foreach (var (label, path) in links)
        {
            var uri = BuildVsCodeUri(path, line);
            AnsiConsole.Console.WriteLine(
                $"{label}: \u001b]8;;{uri}\u001b\\{path}\u001b]8;;\u001b\\"
            );
        }
        if (settings.Open)
        {
            var pathsToOpen = printEverything ? links.Take(1) : links;
            foreach (var (_, path) in pathsToOpen)
            {
                var uri = BuildVsCodeUri(path, line);
                if (!CanPassPathToCommand(path) || !IsCodeOnPath())
                {
                    OpenVsCodeUri(uri);
                }
                else if (!TryOpenWithCode(path, line))
                {
                    OpenVsCodeUri(uri);
                }
            }
        }

        if (unimplementedLanguages is not [])
        {
            AnsiConsole.MarkupLineInterpolated(
                $"\n[grey]No implementation exists for:[/] [yellow]{string.Join(", ", unimplementedLanguages)}[/]"
            );
        }

        return !printEverything && unimplementedLanguages.Count > 0 ? 1 : 0;
    }

    /// <summary>Launches VS Code at a source path and line, or falls back to the registered URI handler.</summary>
    private static bool TryOpenWithCode(string path, int line)
    {
        var startInfo = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe")
            {
                Arguments = $"/c code -g \"{path}:{line}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
            : new ProcessStartInfo("code")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        if (!OperatingSystem.IsWindows())
        {
            startInfo.ArgumentList.Add("-g");
            startInfo.ArgumentList.Add($"{path}:{line}");
        }

        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return true;
            }
            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, _) => { };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return !process.WaitForExit(5000) || process.ExitCode == 0;
        }
        catch (Exception exception)
            when (exception is Win32Exception or InvalidOperationException or IOException)
        {
            return false;
        }
    }

    /// <summary>Checks whether the path can safely be passed through the Windows command interpreter.</summary>
    private static bool CanPassPathToCommand(string path) =>
        !path.Any(character => "&|<>^%".Contains(character));

    /// <summary>Checks PATH for a VS Code command launcher supported on the current operating system.</summary>
    private static bool IsCodeOnPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }
        var names = OperatingSystem.IsWindows()
            ? new[] { "code", "code.exe", "code.cmd", "code.bat" }
            : ["code"];
        return path.Split(Path.PathSeparator)
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .SelectMany(directory => names.Select(name => Path.Combine(directory, name)))
            .Any(File.Exists);
    }

    /// <summary>Opens a VS Code URI using the operating system's registered protocol handler.</summary>
    private static void OpenVsCodeUri(string uri)
    {
        _ = Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }

    /// <summary>Builds a VS Code file URI while preserving path separators and drive letters.</summary>
    private static string BuildVsCodeUri(string path, int line)
    {
        var fullPath = Path.GetFullPath(path).Replace('\\', '/');
        var encoded = string.Join(
            '/',
            fullPath
                .Split('/')
                .Select(
                    (part, index) =>
                        index == 0 && part.Length == 0 || Regex.IsMatch(part, @"^[A-Za-z]:$")
                            ? part
                            : Uri.EscapeDataString(part)
                )
        );
        if (!OperatingSystem.IsWindows() && !encoded.StartsWith('/'))
        {
            encoded = "/" + encoded;
        }
        var uriPath = OperatingSystem.IsWindows() ? "/" + encoded : encoded;
        return $"vscode://file{uriPath}:{line}";
    }

    /// <summary>Updates a problem's status and solved date, then refreshes the generated index.</summary>
    public static int ChangeStatus(StatusSettings settings)
    {
        var repository = Load(settings);
        var problem = FindProblem(repository, settings.Problem);
        var status = NormalizeStatus(settings.Status);
        problem.Metadata.Status = status;
        problem.Metadata.SolvedOn =
            settings.SolvedOn is not null
                ? ParseDate(settings.SolvedOn, "solved_on")
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : status == "solved"
                ? DateOnly
                    .FromDateTime(DateTime.Today)
                    .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : null;
        var markdown = RenderDocument(problem.Metadata, problem.Body);
        var index = BuildIndex(
            ReadProblems(repository)
                .Select(item =>
                    PathComparer.Equals(Path.GetFullPath(item.Path), Path.GetFullPath(problem.Path))
                        ? problem
                        : item
                ),
            repository
        );
        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine($"[green]Would update:[/] {Markup.Escape(problem.Path)}");
            AnsiConsole.WriteLine(markdown);
            AnsiConsole.MarkupLine(
                $"[green]Would update:[/] {Markup.Escape(repository.IndexFile)}"
            );
            AnsiConsole.WriteLine(index);
            return 0;
        }

        File.WriteAllText(problem.Path, NormalizeLineEndings(markdown), new UTF8Encoding(false));
        WriteIndex(repository, index);
        AnsiConsole.MarkupLine(
            $"[green]Updated status for[/] {Markup.Escape(problem.Metadata.Title)}: {Markup.Escape(status)}"
        );
        return 0;
    }

    /// <summary>Synchronizes implementation metadata and links from disk, then regenerates the index.</summary>
    public static int Sync(SyncSettings settings)
    {
        if (settings.All && settings.Problem is not null)
        {
            throw new InvalidOperationException(
                "Use either a problem argument or --all, not both."
            );
        }
        if (settings.Index && (settings.All || settings.Problem is not null))
        {
            throw new InvalidOperationException(
                "--index cannot be combined with a problem argument or --all."
            );
        }

        var repository = Load(settings);
        var problems = ReadProblems(repository);
        var syncProblems = settings.All || settings.Problem is not null;
        var selected =
            settings.All ? problems
            : settings.Problem is not null ? [FindProblem(repository, settings.Problem)]
            : [];
        var updates = new Dictionary<string, ProblemDocument>(PathComparer);
        var planned = new List<PlannedWrite>();
        foreach (var problem in selected)
        {
            RebuildMetadata(repository, problem, reportMissing: true);
            var body = UpdateLinks(problem.Body, problem.Path, problem.Metadata, repository);
            var updated = problem with { Body = body };
            var content = RenderDocument(updated.Metadata, updated.Body);
            if (!string.Equals(File.ReadAllText(problem.Path), content, StringComparison.Ordinal))
            {
                planned.Add(new PlannedWrite(problem.Path, content, "metadata"));
            }
            updates[Path.GetFullPath(problem.Path)] = updated;
        }

        var indexContent = BuildIndex(
            problems.Select(problem =>
                updates.TryGetValue(Path.GetFullPath(problem.Path), out var updated)
                    ? updated
                    : problem
            ),
            repository
        );
        if (settings.DryRun)
        {
            foreach (var write in planned)
            {
                AnsiConsole.MarkupLine($"[green]Would update:[/] {Markup.Escape(write.Path)}");
                if (settings.Verbose)
                {
                    AnsiConsole.WriteLine(write.Content);
                }
            }
            AnsiConsole.MarkupLine(
                $"[green]Would update index:[/] {Markup.Escape(repository.IndexFile)}"
            );
            if (settings.Verbose)
            {
                AnsiConsole.WriteLine(indexContent);
            }
            return 0;
        }

        foreach (var write in planned)
        {
            File.WriteAllText(
                write.Path,
                NormalizeLineEndings(write.Content),
                new UTF8Encoding(false)
            );
            AnsiConsole.MarkupLine($"[green]Updated:[/] {Markup.Escape(write.Path)}");
        }
        WriteIndex(repository, indexContent);
        AnsiConsole.MarkupLine($"[green]Updated index:[/] {Markup.Escape(repository.IndexFile)}");
        if (!syncProblems && planned.Count == 0)
        {
            AnsiConsole.MarkupLine(
                "[grey]Index-only sync complete; problem files were not changed.[/]"
            );
        }
        return 0;
    }

    /// <summary>Builds or previews an index document while preserving content outside its managed markers.</summary>
    private static string BuildIndex(
        IEnumerable<ProblemDocument> documents,
        RepositoryConfig repository
    ) => MergeIndex(repository, RenderIndex(documents, repository));

    /// <summary>Renders the sorted problem table with one configured-language solutions column.</summary>
    private static string RenderIndex(
        IEnumerable<ProblemDocument> documents,
        RepositoryConfig repository
    )
    {
        var problems = documents
            .OrderBy(problem => ParseDate(problem.Metadata.Created, "created"))
            .ThenBy(problem => problem.Slug, StringComparer.Ordinal)
            .ToArray();
        var output = new StringBuilder();
        output.AppendLine(IndexStart);
        output.Append("Problems: ");
        output.AppendLine(
            string.Join(
                " · ",
                new[] { "solved", "pending", "revisit" }.Select(status =>
                    $"{status} {problems.Count(problem => problem.Metadata.Status == status)}"
                )
            )
        );
        output.AppendLine();
        if (repository.Categories.Count == 0)
        {
            AppendIndexTable(output, problems, repository);
        }
        else
        {
            foreach (var category in repository.Categories.Values)
            {
                output.Append("### ").AppendLine(category.Key);
                output.AppendLine();
                AppendIndexTable(
                    output,
                    problems.Where(problem =>
                        string.Equals(
                            problem.Category ?? CategoryForPath(repository, problem.Path),
                            category.Key,
                            StringComparison.OrdinalIgnoreCase
                        )
                    ),
                    repository
                );
                output.AppendLine();
            }
            output.AppendLine("### Uncategorized");
            output.AppendLine();
            AppendIndexTable(
                output,
                problems.Where(problem =>
                    (problem.Category ?? CategoryForPath(repository, problem.Path)) is null
                ),
                repository
            );
        }
        output.Append(IndexEnd).AppendLine();
        return output.ToString();
    }

    private static void AppendIndexTable(
        StringBuilder output,
        IEnumerable<ProblemDocument> problems,
        RepositoryConfig repository
    )
    {
        output.AppendLine("| Problem | Status | Solved on | Solutions |");
        output.AppendLine("| --- | --- | --- | --- |");
        foreach (var problem in problems)
        {
            var title = EscapeTable(problem.Metadata.Title);
            var docRelative = RelativeIndexPath(repository, problem.Path);
            output
                .Append("| [")
                .Append(title)
                .Append("](")
                .Append(docRelative)
                .Append(") | ")
                .Append(problem.Metadata.Status)
                .Append(" | ")
                .Append(problem.Metadata.SolvedOn ?? "—")
                .Append(" | ");
            var solutions = LanguageOrder(repository)
                .Where(problem.Metadata.Implementations.ContainsKey)
                .Select(language =>
                {
                    var source = ResolvePath(
                        Path.GetDirectoryName(problem.Path)!,
                        problem.Metadata.Implementations[language]
                    );
                    return $"[{EscapeTable(language)}]({RelativeIndexPath(repository, source)})";
                });
            var solutionCell = string.Join(" · ", solutions);
            output.Append(string.IsNullOrEmpty(solutionCell) ? "—" : solutionCell).AppendLine(" |");
        }
    }

    private static string MergeIndex(RepositoryConfig repository, string generatedBlock)
    {
        if (!File.Exists(repository.IndexFile))
        {
            return "# Problems\n\n" + generatedBlock;
        }
        var current = File.ReadAllText(repository.IndexFile)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var start = current.IndexOf(IndexStart, StringComparison.Ordinal);
        var end = current.IndexOf(IndexEnd, StringComparison.Ordinal);
        if (start < 0 && end < 0)
        {
            throw new InvalidOperationException(
                $"Index '{repository.IndexFile}' has no prob markers. Add '{IndexStart}' and '{IndexEnd}' around the generated section."
            );
        }
        if (
            start < 0
            || end < start
            || current.IndexOf(IndexStart, start + IndexStart.Length, StringComparison.Ordinal) >= 0
            || current.IndexOf(IndexEnd, end + IndexEnd.Length, StringComparison.Ordinal) >= 0
        )
        {
            throw new InvalidOperationException(
                $"Index markers in '{repository.IndexFile}' are malformed; add exactly one ordered pair of prob markers."
            );
        }
        return current[..start] + generatedBlock.TrimEnd('\n') + current[(end + IndexEnd.Length)..];
    }

    /// <summary>Creates an escaped relative link from the index file to a problem or implementation.</summary>
    private static string RelativeIndexPath(RepositoryConfig repository, string target)
    {
        EnsureSameRoot(Path.GetDirectoryName(repository.IndexFile)!, target);
        return Uri.EscapeDataString(
                Path.GetRelativePath(Path.GetDirectoryName(repository.IndexFile)!, target)
                    .Replace('\\', '/')
            )
            .Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Escapes pipe characters so titles remain valid Markdown table cells.</summary>
    private static string EscapeTable(string value) => value.Replace("|", "\\|");

    /// <summary>Writes generated index content using UTF-8 without a BOM and LF line endings.</summary>
    private static void WriteIndex(RepositoryConfig repository, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(repository.IndexFile)!);
        File.WriteAllText(
            repository.IndexFile,
            NormalizeLineEndings(content),
            new UTF8Encoding(false)
        );
    }

    /// <summary>Normalizes arbitrary generated text to LF and exactly one final newline.</summary>
    private static string NormalizeLf(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').TrimEnd('\n')
        + "\n";

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    /// <summary>Creates an empty starter configuration without changing an existing file.</summary>
    public static int Initialize(InitSettings settings)
    {
        var root = Path.GetFullPath(settings.Directory);
        Directory.CreateDirectory(root);
        var configPath = string.IsNullOrWhiteSpace(settings.Config)
            ? Path.Combine(root, "prob.yaml")
            : Path.GetFullPath(settings.Config);
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        if (File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Skipped existing:[/] {Markup.Escape(configPath)}");
        }
        else
        {
            File.WriteAllText(configPath, EmptyConfiguration, new UTF8Encoding(false));
            AnsiConsole.MarkupLine($"[green]Created:[/] {Markup.Escape(configPath)}");
        }
        return 0;
    }

    /// <summary>Documents the configuration fields without imposing repository-specific values.</summary>
    [StringSyntax("yaml")]
    private const string EmptyConfiguration = """
        # Required: directory containing per-problem Markdown files.
        notes:
          dir:
          # Optional: Scriban template for new problem notes.
          # template: templates/problem.scriban
        # Optional: index Markdown file; defaults to <notes.dir>/README.md.
        index_file:
        # Optional: fail instead of skipping Markdown files without front matter.
        # strict: false
        # Optional: languages used by prob new when --languages is omitted.
        # default_languages: []
        # Optional: categories; each category requires its own notes.dir.
        # categories:
        #   arrays:
        #     notes:
        #       dir: problems/arrays
        #       # template: templates/arrays.scriban
        #     # default_languages: [csharp]
        # Optional: default category for new problems.
        # default_category:
        # Configure each supported language with a scaffold path, extension, and Scriban template.
        # languages:
        #   <language>:
        #     dir:
        #     extension:
        #     template:
        #     naming: pascal
        #     vars: {}
        """;
}

/// <summary>Resolved per-language source, template, and template-variable settings.</summary>
public sealed record ResolvedLanguage(
    string Name,
    string Directory,
    string Extension,
    string Template,
    Dictionary<string, object?> Vars,
    string Naming
);

/// <summary>Resolved notes and language defaults for one category.</summary>
public sealed record ResolvedCategory(
    string Key,
    string NotesDirectory,
    string? NotesTemplate,
    List<string>? DefaultLanguages
);

/// <summary>All configuration resolved for one CLI invocation.</summary>
public sealed record RepositoryConfig(
    string Root,
    string ConfigPath,
    string NotesDirectory,
    string IndexFile,
    string? NotesTemplate,
    List<string>? DefaultLanguages,
    Dictionary<string, ResolvedLanguage> Languages,
    Dictionary<string, ResolvedCategory> Categories,
    string? DefaultCategory,
    bool Strict
);

/// <summary>Canonical platform metadata used by source templates.</summary>
public sealed record PlatformId(string Name, string Id, bool IsLeetCode);
