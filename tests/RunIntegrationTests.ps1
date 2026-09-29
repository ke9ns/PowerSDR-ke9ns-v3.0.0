param(
    [ValidateSet('All', 'Audio', 'Rx2', 'Graphics', 'State', 'Speech', 'Squelch')]
    [string]$Suite = 'All',
    [string]$RuntimePath = '',
    [string]$OutputPath = ''
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
if (!$RuntimePath) { $RuntimePath = Join-Path $repo 'bin\Release' }
if (!$OutputPath) { $OutputPath = Join-Path $repo ('artifacts\tests\' + $Suite) }
$RuntimePath = (Resolve-Path -LiteralPath $RuntimePath).Path
New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
$OutputPath = (Resolve-Path -LiteralPath $OutputPath).Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
$dspSources = @('tests\AudioBridgeTests.cs', 'Console\DSP\WdspNative.cs',
    'Console\DSP\WdspReceiver.cs', 'Console\DSP\WdspImpulseBlankers.cs',
    'Console\DSP\DspBackend.cs') | ForEach-Object { Join-Path $repo $_ }
$runtimeFiles = @('wdsp.dll','rnnoise.dll','specbleach.dll','libfftw3-3.dll',
    'libfftw3f-3.dll','SharpDX.dll','SharpDX.Direct2D1.dll','SharpDX.DXGI.dll','SharpDX.Mathematics.dll')
foreach ($file in $runtimeFiles) { Copy-Item -LiteralPath (Join-Path $RuntimePath $file) -Destination $OutputPath -Force }
function Run-Test([string]$Name, [string[]]$Sources, [string[]]$References = @(), [string[]]$Arguments = @()) {
    $exe = Join-Path $OutputPath ($Name + '.exe')
    $compileArgs = @('/nologo','/unsafe','/platform:x86','/define:DEBUG',"/out:$exe","/main:PowerSDR.$Name")
    foreach ($reference in $References) { $compileArgs += '/r:' + $reference }
    & $compiler @compileArgs @Sources
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $Name" }
    & $exe @Arguments | Tee-Object -FilePath (Join-Path $OutputPath ($Name + '.log'))
    if ($LASTEXITCODE -ne 0) { throw "Test failed: $Name" }
}
if ($Suite -in @('All','State')) {
    Run-Test 'NrModeStateTests' @((Join-Path $repo 'tests\NrModeStateTests.cs'),(Join-Path $repo 'Console\DSP\NrModeState.cs'))
    Run-Test 'DisplayStartupGateTests' @((Join-Path $repo 'tests\DisplayStartupGateTests.cs'),(Join-Path $repo 'Console\DisplayStartupGate.cs'))
}
if ($Suite -in @('All','Audio')) { Run-Test 'AudioBridgeTests' $dspSources }
if ($Suite -in @('All','Rx2')) { Run-Test 'PhysicalRx2Tests' ($dspSources + (Join-Path $repo 'tests\PhysicalRx2Tests.cs')) }
if ($Suite -in @('All','Squelch')) { Run-Test 'SquelchTests' ($dspSources + (Join-Path $repo 'tests\SquelchTests.cs')) }
if ($Suite -in @('All','Graphics')) {
    $references = @('System.Drawing.dll','System.Windows.Forms.dll')
    $references += @('SharpDX.dll','SharpDX.Direct2D1.dll','SharpDX.DXGI.dll','SharpDX.Mathematics.dll') | ForEach-Object { Join-Path $OutputPath $_ }
    foreach ($test in @('Direct2DSmokeTest','Direct2DSnapshotTests','Direct2DStatusTests')) {
        Run-Test $test @((Join-Path $repo ('tests\' + $test + '.cs')),(Join-Path $repo 'Console\Direct2DDisplay.cs')) $references
    }
}
if ($Suite -in @('All','Speech')) {
    $speech = Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL\System.Speech\v4.0_4.0.0.0__31bf3856ad364e35\System.Speech.dll'
    Run-Test 'NrSpeechTests' ($dspSources + (Join-Path $repo 'tests\NrSpeechTests.cs')) @($speech) @('--rx2')
}
Write-Output "PASS: $Suite integration suite. Logs: $OutputPath"
