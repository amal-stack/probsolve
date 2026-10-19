# Probsolve

My problem-solving journey across data structures and algorithms, with problems from multiple platforms, college coursework, and my own.

Browse the [problem index](problems/README.md).

## Layout
| Path | Contents |
|---|---|
| `problems/` | One markdown file per problem: notes, approach, and links to every solution. `README.md` inside it is the generated index. |
| One folder per language | Solutions, one file per problem. Which languages exist, and where, is set in `prob.yaml`. Where the language supports it, the platform and submission id are recorded on the solution. |
| `tools/prob/` | The `prob` CLI, which scaffolds problems and keeps the notes and index in sync. Also contains the language scaffold Scriban templates |
| `prob.yaml` | Configuration for `prob`: directories, languages and templates. |


## The `prob` tool
`prob` is a small C# CLI + Scriban templates that keeps this repo consistent as it grows. Every problem is a markdown note with front matter (status, dates, tags) plus one solution file per language, and `prob` keeps them connected so I don't have to by hand. 

Problems come from any platform, my coursework, or my own ideas, and not every problem has to be solved in every language.
- **Scaffold:** one command creates the note and the solution files from templates, with the platform and submission id recorded on the class.
- **Stay in sync:** each note links to its solutions, and the index is regenerated, so nothing goes stale.
- **Query:** find problems by language, status, tag or date, for example everything I solved in Java but not in C#.
- **Language-agnostic:** adding a language is a config entry and a template, not a code change.

The tool was designed by me and implemented with AI assistance (GitHub Copilot and Claude). The solutions and notes are my own.

Requires the .NET 10 SDK. Load the shell helper in PowerShell that exposes the `prob` command and registers tab completions:

```powershell
. ./tools/prob/prob.profile.ps1
```

Common commands:
```powershell
prob new "Two Sum" -l csharp,java --platform leetcode:1   # scaffold a problem
prob new "Two Sum" -l java                                # add a language later
prob list "lang=java status:-solved"                      # query problems
prob link two-sum --open                                  # open the notes
prob status two-sum solved
prob sync --all                                           # rebuild links and index from disk
```

Run `prob --help` or `prob <cmd> --help` for more information. 

> [!NOTE]
> The **prob** tool was designed by me and implemented with AI assistance (GitHub Copilot and Claude), so my time goes into the problems rather than the tooling. The solutions and notes are my own.
> Each problem's status, dates, tags and solution paths live in the front matter of its markdown file. The index in `problems/README.md` is generated, so edit only outside its markers.

