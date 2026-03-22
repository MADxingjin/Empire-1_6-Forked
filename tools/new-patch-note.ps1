# new-patch-note.ps1
# Interactive script to add a new PatchNoteDef and update Manifest.xml
# Run from the Empire/ directory (the one containing About/, 1.6/, etc.)

$ErrorActionPreference = "Stop"

$defsDir = "1.6\Defs\FCPatchNoteDefs"
$manifestPath = "About\Manifest.xml"

# Verify we're in the right directory
if (-not (Test-Path $defsDir) -or -not (Test-Path $manifestPath)) {
    Write-Host "ERROR: Run this script from the Empire/ directory (the one containing About/ and 1.6/)." -ForegroundColor Red
    exit 1
}

# Read current version from Manifest.xml
$manifestContent = Get-Content $manifestPath -Raw
if ($manifestContent -match '<version>([^<]+)</version>') {
    $currentVersion = $Matches[1]
} else {
    $currentVersion = "unknown"
}
Write-Host "Current version: $currentVersion" -ForegroundColor Cyan
Write-Host ""

# --- Collect inputs ---

# Version
do {
    $version = Read-Host "New version (major.minor.patch, e.g. 0.51.0)"
} while ($version -notmatch '^\d+\.\d+\.\d+$')

$versionParts = $version -split '\.'
$major = [int]$versionParts[0]
$minor = [int]$versionParts[1]
$patch = [int]$versionParts[2]
$defName = "${major}_${minor}_${patch}"

# Label
do {
    $label = Read-Host "Label (short title for this update)"
} while ([string]::IsNullOrWhiteSpace($label))

# Description
do {
    $description = Read-Host "Description (one-line summary)"
} while ([string]::IsNullOrWhiteSpace($description))

# Patch note type
$types = @("Hotfix", "Patch", "Minor", "Major")
Write-Host ""
Write-Host "Patch note type:"
for ($i = 0; $i -lt $types.Count; $i++) {
    Write-Host "  $($i + 1)) $($types[$i])"
}
do {
    $typeChoice = Read-Host "Select type (1-4)"
} while ($typeChoice -notmatch '^[1-4]$')
$patchNoteType = $types[[int]$typeChoice - 1]

# Authors
$authorsInput = Read-Host "Author(s) (comma-separated)"
if ([string]::IsNullOrWhiteSpace($authorsInput)) {
    $authorsInput = ""
}
$authors = ($authorsInput -split ',') | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }

# Change lines
Write-Host ""
Write-Host "Enter change lines (one per line, empty line to finish):"
$changeLines = @()
while ($true) {
    $line = Read-Host ">"
    if ([string]::IsNullOrWhiteSpace($line)) { break }
    $changeLines += $line
}

if ($changeLines.Count -eq 0) {
    Write-Host "ERROR: At least one change line is required." -ForegroundColor Red
    exit 1
}

# --- Auto-fill date ---
$today = Get-Date
$releaseDay = $today.Day
$releaseMonth = $today.Month
$releaseYear = $today.Year

# --- Build XML block ---
$indent = "`t"

$changeLinesXml = ($changeLines | ForEach-Object { "${indent}${indent}<li>$([System.Security.SecurityElement]::Escape($_))</li>" }) -join "`n"
$authorsXml = ($authors | ForEach-Object { "${indent}${indent}<li>$([System.Security.SecurityElement]::Escape($_))</li>" }) -join "`n"

$xmlBlock = @"

${indent}<FactionColonies.PatchNoteDef ParentName="EmpirePatchBase">
${indent}${indent}<defName>$defName</defName>
${indent}${indent}<label>$([System.Security.SecurityElement]::Escape($label))</label>
${indent}${indent}<description>$([System.Security.SecurityElement]::Escape($description))</description>

${indent}${indent}<major>$major</major>
${indent}${indent}<minor>$minor</minor>
${indent}${indent}<patch>$patch</patch>

${indent}${indent}<releaseDay>$releaseDay</releaseDay>
${indent}${indent}<releaseMonth>$releaseMonth</releaseMonth>
${indent}${indent}<releaseYear>$releaseYear</releaseYear>

${indent}${indent}<patchNoteType>$patchNoteType</patchNoteType>

${indent}${indent}<patchNoteLines>
$changeLinesXml
${indent}${indent}</patchNoteLines>

${indent}${indent}<patchNoteImagePaths>
${indent}${indent}${indent}<li>PatchNoteImages/global/empire</li>
${indent}${indent}</patchNoteImagePaths>

${indent}${indent}<patchNoteImageDescriptions>
${indent}${indent}${indent}<li>$([System.Security.SecurityElement]::Escape($label))</li>
${indent}${indent}</patchNoteImageDescriptions>

${indent}${indent}<additionalNotes>
${indent}${indent}${indent}<li>Please raise any issues you find on the Github page.</li>
${indent}${indent}</additionalNotes>

${indent}${indent}<authors>
$authorsXml
${indent}${indent}</authors>
${indent}</FactionColonies.PatchNoteDef>
"@

# --- Insert into the correct version file ---
$targetFile = "$defsDir\PatchNoteDefs_v$major.$minor.xml"

if (Test-Path $targetFile) {
    # Append to existing file: insert new def before </Defs>
    $content = Get-Content $targetFile -Raw
    $content = $content -replace '</Defs>', "$xmlBlock`n</Defs>"
    Set-Content $targetFile -Value $content -NoNewline
    Write-Host "  Appended to existing file: $targetFile" -ForegroundColor Cyan
} else {
    # Create new file for this minor version
    $fileContent = "<?xml version=`"1.0`" encoding=`"utf-8`" ?>`n<Defs>$xmlBlock`n</Defs>`n"
    Set-Content $targetFile -Value $fileContent -NoNewline
    Write-Host "  Created new file: $targetFile" -ForegroundColor Cyan
}

# --- Update Manifest.xml ---
$manifestContent = $manifestContent -replace '<version>[^<]+</version>', "<version>$version</version>"
Set-Content $manifestPath -Value $manifestContent -NoNewline

# --- Summary ---
Write-Host ""
Write-Host "Done!" -ForegroundColor Green
Write-Host "  PatchNoteDef '$defName' added to $targetFile"
Write-Host "  Manifest.xml updated: $currentVersion -> $version"
Write-Host "  Type: $patchNoteType | Date: $releaseYear-$releaseMonth-$releaseDay"
Write-Host "  Changes: $($changeLines.Count) line(s) | Authors: $($authors -join ', ')"
