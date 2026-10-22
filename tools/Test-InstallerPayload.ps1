param([Parameter(Mandatory=$true)][string]$PayloadPath,
      [Parameter(Mandatory=$true)][string]$ReportPath)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PayloadPath).Path.TrimEnd('\','/')
$report = [IO.Path]::GetFullPath($ReportPath)
if ($report.StartsWith($root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Report must be outside the payload.'
}
if (Test-Path -LiteralPath $report) { throw 'Choose a new report path.' }
$bad = @()
$inventory = @(Get-ChildItem -LiteralPath $root -Recurse -File | ForEach-Object {
    $relative = $_.FullName.Substring($root.Length + 1)
    $development = $_.Name -match '^(dumpbin|link|cl|msbuild)\.exe$|^Microsoft\.Build\.|^Microsoft\.VisualStudio\.|\.(obj|pch|ilk|map|metagen)$|\.vcxproj\.'
    if ($development) { $bad += $relative }
    [pscustomobject]@{Path=$relative; Bytes=$_.Length; SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash; DevelopmentFile=$development}
})
$required = @('THIRD-PARTY-NOTICES.md','THIRD-PARTY-NEURAL-CW.txt',
    'ONNX-Runtime-LICENSE.txt','ONNX-Runtime-ThirdPartyNotices.txt',
    'licenses/PowerSDR-GPL-2.0.txt','licenses/SharpDX-MIT.txt',
    'licenses/RNNoise-COPYING.txt','licenses/SpecBleach-LGPL-2.1.txt')
$missing = @($required | Where-Object { !(Test-Path -LiteralPath (Join-Path $root $_) -PathType Leaf) })
[pscustomobject]@{Scope='Mechanical checks only; not legal clearance or a runtime allowlist';
    MissingNotices=$missing; DevelopmentFiles=$bad; Files=$inventory} |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $report -Encoding UTF8
if ($bad.Count -or $missing.Count) { throw "Payload rejected: $($bad.Count) development files, $($missing.Count) missing notices. See $report" }
Write-Output 'Mechanical checks passed. Maintainer must still resolve LICENSE-AUDIT.md and verify the complete runtime allowlist.'
