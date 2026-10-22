param([Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$dest = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $dest) { throw 'Choose a new empty output directory; existing files will not be overwritten.' }
New-Item -ItemType Directory -Path $dest | Out-Null
$fixed = @{
    'LICENSE.txt' = 'licenses/PowerSDR-GPL-2.0.txt'
    'lib/SharpDX/LICENSE.txt' = 'licenses/SharpDX-MIT.txt'
    'lib/NR_Algorithms_x86/src/rnnoise/COPYING' = 'licenses/RNNoise-COPYING.txt'
    'lib/NR_Algorithms_x86/src/libspecbleach/LICENSE' = 'licenses/SpecBleach-LGPL-2.1.txt'
    'THIRD-PARTY-NOTICES.md' = 'THIRD-PARTY-NOTICES.md'
    'THIRD-PARTY-NEURAL-CW.txt' = 'THIRD-PARTY-NEURAL-CW.txt'
}
foreach ($item in $fixed.GetEnumerator()) {
    $target = Join-Path $dest $item.Value
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repo $item.Key) -Destination $target
}
[xml]$config = Get-Content -LiteralPath (Join-Path $repo 'Console/packages.config')
$report = foreach ($package in $config.packages.package) {
    $id = [string]$package.id
    $version = [string]$package.version
    $folder = Join-Path $repo ('packages/' + $id + '.' + $version)
    $notices = @()
    if (Test-Path -LiteralPath $folder) {
        $notices = @(Get-ChildItem -LiteralPath $folder -File -Recurse | Where-Object {
            $_.Name -match '^(LICENSE|COPYING|NOTICE|ThirdPartyNotices)(\..*)?$'
        })
    }
    foreach ($file in $notices) {
        $relative = $file.FullName.Substring($folder.Length).TrimStart('\','/')
        $target = Join-Path $dest ('licenses/nuget/' + $id + '.' + $version + '/' + $relative)
        New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target
    }
    [pscustomobject]@{ Package=$id; Version=$version; NoticeCount=$notices.Count;
        Review= $(if ($notices.Count) { 'Collected; compatibility and completeness still require review' } else { 'MISSING local license/notice evidence' }) }
}
$report | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dest 'notice-collection-report.json') -Encoding UTF8
$missing = @($report | Where-Object NoticeCount -eq 0)
Write-Output "Collected notices for $(@($report).Count) package records; $($missing.Count) need license evidence. No release approval implied."
if ($missing.Count) { Write-Warning ($missing.Package -join ', ') }
