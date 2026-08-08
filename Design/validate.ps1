$ErrorActionPreference = 'Stop'

$designRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

function Assert-UniqueRequiredCsv {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$IdColumn,
        [Parameter(Mandatory = $true)][string[]]$RequiredColumns
    )

    $rows = @(Import-Csv -LiteralPath $Path)
    if ($rows.Count -eq 0) {
        throw "$Path contains no data rows."
    }

    $columns = @($rows[0].PSObject.Properties.Name)
    foreach ($column in $RequiredColumns) {
        if ($column -notin $columns) {
            throw "$Path is missing required column '$column'."
        }
    }

    foreach ($row in $rows) {
        foreach ($column in $RequiredColumns) {
            if ([string]::IsNullOrWhiteSpace([string]$row.$column)) {
                throw "$Path has a blank '$column' value."
            }
        }
    }

    $duplicates = @($rows | Group-Object -Property $IdColumn | Where-Object Count -gt 1)
    if ($duplicates.Count -gt 0) {
        throw "$Path has duplicate $IdColumn values: $($duplicates.Name -join ', ')."
    }

    Write-Output "OK $([IO.Path]::GetFileName($Path)): $($rows.Count) rows"
}

$assetPath = Join-Path $designRoot 'assets.csv'
Assert-UniqueRequiredCsv -Path $assetPath -IdColumn 'id' -RequiredColumns @(
    'id', 'role', 'type', 'description', 'size/ratio', 'style line ref', 'source', 'relative_scale', 'status', 'provenance_id'
)

$assetRows = @(Import-Csv -LiteralPath $assetPath)
$invalidSources = @($assetRows | Where-Object {
    $_.source -notin @('generate') -and $_.source -notmatch '^reference_media\[[0-9]+\]$'
})
if ($invalidSources.Count -gt 0) {
    throw "assets.csv contains an invalid source value."
}
$invalidStatuses = @($assetRows | Where-Object status -notin @('planned_placeholder', 'generated', 'in_review', 'approved', 'rejected', 'retired'))
if ($invalidStatuses.Count -gt 0) {
    throw "assets.csv contains an invalid status value."
}
$duplicateProvenance = @($assetRows | Group-Object provenance_id | Where-Object Count -gt 1)
if ($duplicateProvenance.Count -gt 0) {
    throw "assets.csv contains duplicate provenance_id values."
}

Assert-UniqueRequiredCsv -Path (Join-Path $designRoot 'balance/characters.csv') -IdColumn 'character_id' -RequiredColumns @('ruleset_version', 'character_id', 'display_name', 'max_hp', 'move_speed_mps', 'active_id', 'passive_id', 'shovel_trait_id', 'tradeoff')
Assert-UniqueRequiredCsv -Path (Join-Path $designRoot 'balance/weapons.csv') -IdColumn 'weapon_id' -RequiredColumns @('ruleset_version', 'weapon_id', 'display_name', 'class', 'damage_per_hit', 'shots_per_s', 'mechanical_identity')
Assert-UniqueRequiredCsv -Path (Join-Path $designRoot 'balance/utilities.csv') -IdColumn 'utility_id' -RequiredColumns @('ruleset_version', 'utility_id', 'display_name', 'charges', 'effect_summary', 'counterplay')
Assert-UniqueRequiredCsv -Path (Join-Path $designRoot 'balance/enemies.csv') -IdColumn 'enemy_id' -RequiredColumns @('ruleset_version', 'enemy_id', 'display_name', 'tier', 'base_hp', 'behaviour_exam')

$expectedCounts = @{
    'characters.csv' = 6
    'weapons.csv' = 10
    'utilities.csv' = 4
    'enemies.csv' = 6
}
foreach ($entry in $expectedCounts.GetEnumerator()) {
    $actual = @(Import-Csv -LiteralPath (Join-Path $designRoot "balance/$($entry.Key)")).Count
    if ($actual -ne $entry.Value) {
        throw "$($entry.Key) expected $($entry.Value) rows but found $actual."
    }
}

$schema = Get-Content -LiteralPath (Join-Path $designRoot 'provenance/provenance.schema.json') -Raw | ConvertFrom-Json
$template = Get-Content -LiteralPath (Join-Path $designRoot 'provenance/provenance.template.json') -Raw | ConvertFrom-Json
if ($schema.'$schema' -ne 'https://json-schema.org/draft/2020-12/schema') {
    throw 'provenance.schema.json does not declare JSON Schema 2020-12.'
}
if ($template.schema_version -ne '1.0.0' -or $template.status -ne 'planned') {
    throw 'provenance.template.json has an unexpected version or status.'
}
if ($template.review.human_approver -ne $null -or $template.artifacts.Count -ne 0) {
    throw 'The provenance template must remain an unapproved empty placeholder.'
}

Write-Output "OK provenance JSON: schema and placeholder template parse"
Write-Output "OK design validation complete: $($assetRows.Count) planned assets"
