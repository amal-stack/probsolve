# Defines `prob` and its tab completion. All settings are optional:
#   PROB_CONFIG      path to the prob.yaml to use. Unset: the tool walks up from the current directory.
#   PROB_SCRIPT      path to prob.cs. Default: next to this file.
#   PROB_NOTES_DIRS  semicolon-separated notes directories, relative to the config's folder, used only
#                    for tab completion. Default: problems

$global:ProbScriptDefault = Join-Path $PSScriptRoot 'prob.cs'

# Default config: this repo's prob.yaml. Delete these two lines to rely on the walk-up search only.
$defaultConfig = Join-Path $PSScriptRoot '..\..\prob.yaml'
if (-not $env:PROB_CONFIG -and (Test-Path $defaultConfig)) {
    $env:PROB_CONFIG = (Resolve-Path $defaultConfig).Path
}

function global:prob {
    $script = if ($env:PROB_SCRIPT) { $env:PROB_SCRIPT } else { $global:ProbScriptDefault }
    dotnet run $script -- @args
}

function global:Get-ProbRoot {
    if ($env:PROB_CONFIG) {
        $config = Resolve-Path $env:PROB_CONFIG -ErrorAction SilentlyContinue
        if ($config) { return Split-Path -Parent $config.Path }
        return
    }
    $dir = (Get-Location).Path
    while ($dir) {
        if (Test-Path (Join-Path $dir 'prob.yaml')) { return $dir }
        $dir = Split-Path -Parent $dir
    }
}

Register-ArgumentCompleter -Native -CommandName prob -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    if ($wordToComplete -like '-*') { return }
    $words  = @($commandAst.CommandElements | ForEach-Object { $_.ToString() })
    $before = @($words | Select-Object -Skip 1)                                # everything after 'prob'
    if ($wordToComplete) { $before = @($before | Select-Object -SkipLast 1) }  # drop the word being completed

    $candidates = @()
    if ($before.Count -eq 0) {
        $candidates = 'new', 'list', 'link', 'status', 'sync', 'init'
    }
    elseif ($before[0] -in 'link', 'status', 'sync') {
        $given = @($before | Select-Object -Skip 1 | Where-Object { $_ -notlike '-*' })
        if ($given.Count -eq 0) {
            $root = Get-ProbRoot
            if (-not $root) { return }
            $dirs = if ($env:PROB_NOTES_DIRS) { $env:PROB_NOTES_DIRS -split ';' } else { @('problems') }
            $candidates = foreach ($dir in $dirs) {
                $path = Join-Path $root $dir
                if (Test-Path $path) {
                    Get-ChildItem $path -Filter *.md -File -Recurse |
                        Where-Object Name -ne 'README.md' |
                        ForEach-Object BaseName
                }
            }
        }
        elseif ($before[0] -eq 'status' -and $given.Count -eq 1) {
            $candidates = 'solved', 'pending', 'revisit'
        }
    }

    $candidates | Sort-Object -Unique |
        Where-Object { $_ -like "$wordToComplete*" } |
        ForEach-Object { [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_) }
}