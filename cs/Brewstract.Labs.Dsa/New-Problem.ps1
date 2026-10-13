<#
.SYNOPSIS
Creates a C# problem scaffold and matching Markdown notes file.

.EXAMPLE
.\New-Problem.ps1 'Two Sum' -Platform 'LeetCode:1'

.EXAMPLE
.\New-Problem.ps1 'Two Sum' -LeetCode 1

.EXAMPLE
.\New-Problem.ps1 'Graph Traversal' -Platform 'LC:1337' -NoSampleMethod
#>
[CmdletBinding(PositionalBinding=$false)]
param(
    [Parameter(Mandatory, Position = 0)]
    [Alias('ProblemName')]
    [ValidateNotNullOrEmpty()]
    [string] $Name,

    [string] $Platform,

    [Alias('LeetCode')]
    [int] $LeetCodeId,

    [string] $OutputDirectory = (Join-Path $PSScriptRoot 'src\Brewstract.Labs.Dsa.Scratchpad\Problems'),

    [switch] $NoSampleMethod,

    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-ProblemClassName {
    param([Parameter(Mandatory)][string] $Name)

    $normalizedName = $Name.Trim()
    # Separate PascalCase words before tokenizing, so names like "TwoSum" work as well as "Two Sum".
    $normalizedName = [regex]::Replace($normalizedName, '(?<=[\p{Ll}\p{Nd}])(?=\p{Lu})', ' ')
    # Keep only Unicode letters and digits; punctuation and spaces are not valid in a C# class name.
    $words = [regex]::Matches($normalizedName, '[\p{L}\p{Nd}]+')

    if ($words.Count -eq 0) {
        throw "Problem name '$Name' must contain at least one letter or digit."
    }

    $className = foreach ($wordMatch in $words) {
        $word = $wordMatch.Value
        $tail = $word.Substring(1)
        # Normalize all-uppercase tokens such as "URL" to "Url" in the generated class name.
        if ($word -ceq $word.ToUpperInvariant()) {
            $tail = $tail.ToLowerInvariant()
        }

        $word.Substring(0, 1).ToUpperInvariant() + $tail
    }
    $className = $className -join ''

    if ($className -match '^\p{Nd}') {
        $className = "Problem$className"
    }

    return $className
}

function ConvertTo-CSharpStringLiteral {
    param([Parameter(Mandatory)][string] $Value)

    # Escape characters that have special meaning in regular C# string literals.
    $escaped = $Value.Replace('\', '\\').Replace('"', '\"')
    $escaped = $escaped.Replace("`r", '\r').Replace("`n", '\n').Replace("`t", '\t')
    return '"' + $escaped + '"'
}

$Name = $Name.Trim()
if ([string]::IsNullOrWhiteSpace($Name)) {
    throw 'Name cannot be blank.'
}

# Provide a typed shortcut for the common LeetCode case while keeping the generic platform parameter.
if ($PSBoundParameters.ContainsKey('LeetCodeId')) {
    if (-not [string]::IsNullOrWhiteSpace($Platform)) {
        throw 'Use either -LeetCode or -Platform, not both.'
    }

    $Platform = "LeetCode:$LeetCodeId"
}

$platformAttribute = ''
if (-not [string]::IsNullOrWhiteSpace($Platform)) {
    # Split the single argument into a platform name and an ID; only the name may not contain the separator.
    if ($Platform -notmatch '^(?<name>[^:]+):(?<id>.+)$') {
        throw "Platform must use the format 'Name:Id', for example 'LeetCode:121' or 'OtherSite:problem-abc'."
    }

    $platformName = $Matches.name.Trim()
    $platformIdText = $Matches.id.Trim()
    if ([string]::IsNullOrWhiteSpace($platformName) -or [string]::IsNullOrWhiteSpace($platformIdText)) {
        throw "Platform '$Platform' must contain a non-empty name and ID."
    }

    # LeetCode's attribute uses an integer ID and its platform constant; other platforms use On's string overload.
    if ($platformName -in @('LC', 'LeetCode', 'Leet Code')) {
        $platformId = 0
        if (-not [int]::TryParse(
            $platformIdText,
            [System.Globalization.NumberStyles]::AllowLeadingSign,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [ref] $platformId
        )) {
            throw "LeetCode platform ID '$platformIdText' must be a 32-bit integer."
        }

        $platformAttribute = "[On(Platforms.LeetCode, $platformId)]"
    }
    else {
        $platformNameLiteral = ConvertTo-CSharpStringLiteral -Value $platformName
        $platformIdLiteral = ConvertTo-CSharpStringLiteral -Value $platformIdText
        $platformAttribute = "[On($platformNameLiteral, $platformIdLiteral)]"
    }
}

$className = ConvertTo-ProblemClassName -Name $Name
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$csPath = Join-Path $OutputDirectory "$className.cs"
$mdPath = Join-Path $OutputDirectory "$className.md"

if (-not $Force -and ((Test-Path -LiteralPath $csPath) -or (Test-Path -LiteralPath $mdPath))) {
    throw "A scaffold file already exists for '$className'. Use -Force to overwrite it."
}

$classAttributes = @()
if ($platformAttribute) {
    $classAttributes += $platformAttribute
}

$method = ''
if (-not $NoSampleMethod) {
    $method = @'
    [Solution]
    [Complexity(Time = BigO.Linear, Space = BigO.Constant)]
    public static void Solve()
    {
        throw new NotImplementedException();
    }
'@
}

$attributeBlock = if ($classAttributes.Count -gt 0) {
    ($classAttributes -join [Environment]::NewLine) + [Environment]::NewLine
}
else {
    ''
}

$classBody = if ($method) {
    $method + [Environment]::NewLine
}
else {
    ''
}

$csContent = @"
namespace Brewstract.Labs.Dsa.Scratchpad.Problems;

$attributeBlock`public class $className
{
$classBody}
"@

$mdContent = @"
# $Name

Solution: [$className.cs]($className.cs)


## Keywords
"@

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($csPath, $csContent + [Environment]::NewLine, $utf8WithoutBom)
[System.IO.File]::WriteAllText($mdPath, $mdContent + [Environment]::NewLine, $utf8WithoutBom)

Write-Output "Created:"
Write-Output "  $csPath"
Write-Output "  $mdPath"
