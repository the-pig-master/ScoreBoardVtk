param(
    [string]$Version = (Get-Date -Format 'yyyy.MM.dd'),
    [switch]$SkipTests,
    [switch]$SkipInstaller
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$solutionPath = Join-Path $repoRoot 'ScoreBoardVtk.sln'
$testsProject = Join-Path $repoRoot 'ScoreBoardVtk.Tests\ScoreBoardVtk.Tests.csproj'
$wpfProject = Join-Path $repoRoot 'ScoreBoardVtk.Wpf\ScoreBoardVtk.Wpf.csproj'
$cmdProject = Join-Path $repoRoot 'ScoreBoardVtk.Cmd\ScoreBoardVtk.Cmd.csproj'
$installerScript = Join-Path $repoRoot 'installer\ScoreBoardVtk.iss'
$iconPath = Join-Path $repoRoot 'ScoreBoardVtk.Wpf\Assets\ScoreBoardVtk.ico'
$readmePath = Join-Path $repoRoot 'README.md'
$readmeRuPath = Join-Path $repoRoot 'README.ru.md'

$artifactsRoot = Join-Path $repoRoot 'artifacts'
$publishRoot = Join-Path $artifactsRoot 'publish'
$stagingRoot = Join-Path $artifactsRoot 'staging'
$packagesRoot = Join-Path $artifactsRoot 'packages'

$targets = @(
    @{ Rid = 'win-x86'; Arch = 'x86' },
    @{ Rid = 'win-x64'; Arch = 'x64' }
)

function Write-Section([string]$Message)
{
    Write-Host ''
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Get-InnoSetupCompiler
{
    $candidates = @(
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    )

    foreach ($candidate in $candidates)
    {
        if (Test-Path $candidate)
        {
            return $candidate
        }
    }

    return $null
}

function Invoke-DotNetPublish([string]$ProjectPath, [string]$RuntimeIdentifier, [string]$OutputPath)
{
    dotnet publish $ProjectPath `
        -c Release `
        -r $RuntimeIdentifier `
        --self-contained true `
        -p:PublishSingleFile=false `
        -p:PublishTrimmed=false `
        -o $OutputPath

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet publish failed for $ProjectPath ($RuntimeIdentifier)."
    }
}

function Assert-HardwareProfile([string]$HostSettingsPath)
{
    if (-not (Test-Path $HostSettingsPath))
    {
        throw "hostsettings.json was not found: $HostSettingsPath"
    }

    $content = Get-Content $HostSettingsPath -Raw
    if ($content -notmatch '"profile"\s*:\s*"Hardware"')
    {
        throw "Release hostsettings profile is not Hardware: $HostSettingsPath"
    }
}

function New-ZipPackage([string]$SourceFolder, [string]$DestinationZip)
{
    if (Test-Path $DestinationZip)
    {
        Remove-Item $DestinationZip -Force
    }

    $parent = Split-Path -Parent $SourceFolder
    $folderName = Split-Path -Leaf $SourceFolder

    Push-Location $parent
    try
    {
        Compress-Archive -Path $folderName -DestinationPath $DestinationZip -CompressionLevel Optimal
    }
    finally
    {
        Pop-Location
    }
}

function New-Installer([string]$CompilerPath, [string]$Arch, [string]$SourceFolder)
{
    & $CompilerPath `
        "/DAppVersion=$Version" `
        "/DBuildArch=$Arch" `
        "/DSourceDir=$SourceFolder" `
        "/DOutputDir=$packagesRoot" `
        "/DIconFile=$iconPath" `
        $installerScript

    if ($LASTEXITCODE -ne 0)
    {
        throw "Inno Setup compilation failed for $Arch."
    }
}

Write-Section 'Cleaning artifacts'
if (Test-Path $artifactsRoot)
{
    Remove-Item $artifactsRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null
New-Item -ItemType Directory -Force -Path $packagesRoot | Out-Null

if (-not $SkipTests)
{
    Write-Section 'Running release tests'
    dotnet test $testsProject -c Release

    if ($LASTEXITCODE -ne 0)
    {
        throw 'dotnet test failed.'
    }
}

$innoCompiler = if ($SkipInstaller) { $null } else { Get-InnoSetupCompiler }
if (-not $SkipInstaller -and -not $innoCompiler)
{
    Write-Warning 'Inno Setup 6 (ISCC.exe) was not found. Zip packages will be created, installer generation will be skipped.'
}

$packageOutputs = New-Object System.Collections.Generic.List[string]

foreach ($target in $targets)
{
    $rid = [string]$target.Rid
    $arch = [string]$target.Arch
    $packageBaseName = "ScoreBoardVtk-$Version-$rid"
    $wpfPublishDir = Join-Path $publishRoot "$rid\\wpf"
    $cmdPublishDir = Join-Path $publishRoot "$rid\\cmd"
    $stageDir = Join-Path $stagingRoot $packageBaseName
    $cmdStageDir = Join-Path $stageDir 'tools\\cmd'

    Write-Section "Publishing $rid"
    Invoke-DotNetPublish -ProjectPath $wpfProject -RuntimeIdentifier $rid -OutputPath $wpfPublishDir
    Invoke-DotNetPublish -ProjectPath $cmdProject -RuntimeIdentifier $rid -OutputPath $cmdPublishDir

    Assert-HardwareProfile (Join-Path $wpfPublishDir 'hostsettings.json')
    Assert-HardwareProfile (Join-Path $cmdPublishDir 'hostsettings.json')

    Write-Section "Staging $rid"
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null
    New-Item -ItemType Directory -Force -Path $cmdStageDir | Out-Null

    Copy-Item -Path (Join-Path $wpfPublishDir '*') -Destination $stageDir -Recurse -Force
    Copy-Item -Path (Join-Path $cmdPublishDir '*') -Destination $cmdStageDir -Recurse -Force
    Copy-Item -Path $readmePath -Destination (Join-Path $stageDir 'README.md') -Force
    Copy-Item -Path $readmeRuPath -Destination (Join-Path $stageDir 'README.ru.md') -Force

    $zipPath = Join-Path $packagesRoot "$packageBaseName.zip"
    Write-Section "Creating archive $rid"
    New-ZipPackage -SourceFolder $stageDir -DestinationZip $zipPath
    $packageOutputs.Add($zipPath)

    if ($innoCompiler)
    {
        Write-Section "Creating installer $rid"
        New-Installer -CompilerPath $innoCompiler -Arch $arch -SourceFolder $stageDir
    }
}

Write-Section 'Package outputs'
Get-ChildItem $packagesRoot | Select-Object FullName, Length
