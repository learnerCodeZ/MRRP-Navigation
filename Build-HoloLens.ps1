param(
    [string]$UnityPath = "E:\unity project\2022.3.62f3c1\Editor\Unity.exe",
    [string]$ProjectPath = "E:\MRRP-Navigation",
    [string]$BuildConfig = "Release",
    [string]$BuildPlatform = "ARM64",
    [switch]$SkipUnityBuild,
    [switch]$SkipMSBuild,
    [switch]$CreateCertificate
)

$ErrorActionPreference = "Stop"

$ProjectName = "MRReP-Navigation"
$UnityBuildOutput = Join-Path $ProjectPath "Build\HoloLens"
$AppxOutputDir = Join-Path $ProjectPath "Build\AppxOutput"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " HoloLens 2 APPX Build Pipeline" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipUnityBuild)
{
    Write-Host "[Step 1/3] Building Unity UWP Solution..." -ForegroundColor Yellow

    if (-not (Test-Path $UnityPath))
    {
        Write-Error "Unity not found at: $UnityPath"
        exit 1
    }

    $unityArgs = @(
        "-quit",
        "-batchmode",
        "-nographics",
        "-projectPath", $ProjectPath,
        "-executeMethod", "MRReP.Editor.BuildAppx.Build",
        "-logFile", (Join-Path $ProjectPath "Build\unity_build.log")
    )

    Write-Host "  Unity: $UnityPath"
    Write-Host "  Project: $ProjectPath"
    Write-Host "  Log: $(Join-Path $ProjectPath 'Build\unity_build.log')"
    Write-Host ""

    $buildDir = Join-Path $ProjectPath "Build"
    if (-not (Test-Path $buildDir))
    {
        New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
    }

    $proc = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow

    $logFile = Join-Path $ProjectPath "Build\unity_build.log"
    if (Test-Path $logFile)
    {
        Write-Host "--- Unity Build Log (last 30 lines) ---" -ForegroundColor Gray
        Get-Content $logFile -Tail 30
        Write-Host "--- End Log ---" -ForegroundColor Gray
    }

    if ($proc.ExitCode -ne 0)
    {
        Write-Error "Unity build failed with exit code: $($proc.ExitCode)"
        exit 1
    }

    Write-Host "[Step 1/3] Unity build completed." -ForegroundColor Green
    Write-Host ""
}
else
{
    Write-Host "[Step 1/3] Skipping Unity build (SkipUnityBuild)." -ForegroundColor DarkGray
    Write-Host ""
}

$slnPath = Join-Path $UnityBuildOutput "$ProjectName.sln"
if (-not (Test-Path $slnPath))
{
    Write-Error "Solution file not found: $slnPath"
    Write-Error "Run without -SkipUnityBuild first to generate the VS solution."
    exit 1
}

if ($CreateCertificate)
{
    Write-Host "[Step 1.5] Creating self-signed certificate..." -ForegroundColor Yellow

    $certDir = Join-Path $ProjectPath "Build"
    $certPath = Join-Path $certDir "MRReP_Cert.pfx"
    $pubPath = Join-Path $certDir "MRReP_Cert.cer"

    if (-not (Test-Path $certPath))
    {
        $ps = @{
            Type = "Custom"
            Subject = "CN=MRReP"
            KeySpec = "Signature"
            KeyExportPolicy = "Exportable"
            KeyLength = 2048
            HashAlgorithm = "SHA256"
            NotAfter = (Get-Date).AddYears(5)
            CertStoreLocation = "Cert:\CurrentUser\My"
            TextExtension = @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
        }
        $cert = New-SelfSignedCertificate @ps
        $pwd = ConvertTo-SecureString -String "mrrep123" -Force -AsPlainText
        Export-PfxCertificate -Cert $cert -FilePath $certPath -Password $pwd | Out-Null
        Export-Certificate -Cert $cert -FilePath $pubPath | Out-Null

        Write-Host "  Certificate created: $certPath" -ForegroundColor Green
        Write-Host "  Password: mrrep123" -ForegroundColor DarkGray
        Write-Host "  Public key: $pubPath" -ForegroundColor DarkGray
    }
    else
    {
        Write-Host "  Certificate already exists: $certPath" -ForegroundColor DarkGray
    }
    Write-Host ""
}

if (-not $SkipMSBuild)
{
    Write-Host "[Step 2/3] Building APPX with MSBuild..." -ForegroundColor Yellow

    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere))
    {
        Write-Error "vswhere not found. Is Visual Studio installed?"
        exit 1
    }

    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
    if (-not $msbuild -or -not (Test-Path $msbuild))
    {
        Write-Error "MSBuild not found"
        exit 1
    }

    Write-Host "  MSBuild: $msbuild"
    Write-Host "  Solution: $slnPath"
    Write-Host "  Config: $BuildConfig | Platform: $BuildPlatform"
    Write-Host ""

    if (-not (Test-Path $AppxOutputDir))
    {
        New-Item -ItemType Directory -Path $AppxOutputDir -Force | Out-Null
    }

    $msbuildArgs = @(
        $slnPath,
        "/p:Configuration=$BuildConfig",
        "/p:Platform=$BuildPlatform",
        "/p:AppxBundle=Never",
        "/p:UapAppxPackageBuildMode=SideloadOnly",
        "/p:AppxPackageOutputDir=$AppxOutputDir",
        "/p:AppxPackageSigningEnabled=False",
        "/m",
        "/v:minimal"
    )

    $proc = Start-Process -FilePath $msbuild -ArgumentList $msbuildArgs -Wait -PassThru -NoNewWindow

    if ($proc.ExitCode -ne 0)
    {
        Write-Error "MSBuild failed with exit code: $($proc.ExitCode)"
        Write-Host "Try running MSBuild manually or check the solution in Visual Studio." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "[Step 2/3] MSBuild completed." -ForegroundColor Green
    Write-Host ""
}
else
{
    Write-Host "[Step 2/3] Skipping MSBuild (SkipMSBuild)." -ForegroundColor DarkGray
    Write-Host ""
}

Write-Host "[Step 3/3] Locating APPX files..." -ForegroundColor Yellow

$appxFiles = Get-ChildItem -Path (Join-Path $ProjectPath "Build") -Filter "*.appx" -Recurse -ErrorAction SilentlyContinue
$msixFiles = Get-ChildItem -Path (Join-Path $ProjectPath "Build") -Filter "*.msix" -Recurse -ErrorAction SilentlyContinue

$allPackages = @($appxFiles) + @($msixFiles)

if ($allPackages.Count -gt 0)
{
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " BUILD SUCCESSFUL" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    foreach ($pkg in $allPackages)
    {
        Write-Host "  Package: $($pkg.FullName)" -ForegroundColor White
        Write-Host "  Size: $([math]::Round($pkg.Length / 1MB, 2)) MB" -ForegroundColor DarkGray
        Write-Host ""
    }
    Write-Host "To install on HoloLens 2:" -ForegroundColor Cyan
    Write-Host "  Option A: Device Portal (https://<HL_IP>) -> Apps -> Add" -ForegroundColor White
    Write-Host "  Option B: Visual Studio -> Deploy (Debug/Release)" -ForegroundColor White
    Write-Host "  Option C: windeploytool /post" -ForegroundColor White
}
else
{
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host " BUILD COMPLETED" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  No .appx/.msix found yet." -ForegroundColor Yellow
    Write-Host "  If you skipped MSBuild, open the solution in VS:" -ForegroundColor White
    Write-Host "  $slnPath" -ForegroundColor White
    Write-Host ""
    Write-Host "  In Visual Studio:" -ForegroundColor White
    Write-Host "  1. Set platform = ARM64, config = Release" -ForegroundColor White
    Write-Host "  2. Right-click project -> Store -> Create App Packages" -ForegroundColor White
    Write-Host "  3. Choose 'Sideload' -> Create" -ForegroundColor White
}

Write-Host ""
Write-Host "Done." -ForegroundColor Cyan
