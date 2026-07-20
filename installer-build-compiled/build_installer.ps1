$ErrorActionPreference = "Stop"

# Define paths relative to this script directory
$installerDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $installerDir
$buildOutputDir = Join-Path $installerDir "build_output"
$tempPayloadDir = Join-Path $installerDir "payload_temp"
$zipPath = Join-Path $installerDir "app.zip"
$icoPath = "$repoRoot\POSales\POSales\Resources\app_logo.ico"
$tempIcoPath = Join-Path $installerDir "app_logo.ico"
$setupExePath = Join-Path $installerDir "IRAS_SPOT_POS_Setup.exe"

# Resolve user's Downloads directory dynamically
$downloadsDir = Join-Path ([System.Environment]::GetFolderPath("UserProfile")) "Downloads"
$targetSetupPath = Join-Path $downloadsDir "IRAS_SPOT_POS_Setup.exe"

# Rebuild C# project first to ensure the installer has the latest code
Write-Host "Building C# project in Debug configuration..."
$projectPath = "$repoRoot\POSales\POSales\POSales.csproj"

# MSBuild Paths check
$msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path $msbuildPath)) {
    $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
}

if (Test-Path $msbuildPath) {
    Write-Host "Using MSBuild at: $msbuildPath"
    & $msbuildPath $projectPath /t:Build /p:Configuration=Debug /p:OutDir="$buildOutputDir" /p:FrameworkPathOverride="C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
} else {
    Write-Host "MSBuild not found at default paths, trying dotnet build..."
    dotnet build $projectPath -c Debug -p:OutDir="$buildOutputDir"
}

# Cleanup from previous runs
if (Test-Path $tempPayloadDir) { Remove-Item -Recurse -Force $tempPayloadDir }
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
if (Test-Path $tempIcoPath) { Remove-Item -Force $tempIcoPath }

Write-Host "Creating temporary payload folder..."
New-Item -ItemType Directory -Path $tempPayloadDir

Write-Host "Copying build output to payload..."
Copy-Item -Path (Join-Path $buildOutputDir "*") -Destination $tempPayloadDir -Recurse -Force

# Remove development-only files
Get-ChildItem -Path $tempPayloadDir -Filter "*.pdb" -Recurse | Remove-Item -Force
if (Test-Path (Join-Path $tempPayloadDir "dbconnection_local.txt")) {
    Remove-Item (Join-Path $tempPayloadDir "dbconnection_local.txt") -Force
}

Write-Host "Zipping payload..."
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($tempPayloadDir, $zipPath)

# Copy icon for compilation
Copy-Item -Path $icoPath -Destination $tempIcoPath -Force

Write-Host "Compiling setup installer with csc.exe..."
$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$arguments = @(
    "/target:winexe",
    "/out:`"$setupExePath`"",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.IO.Compression.dll",
    "/reference:System.IO.Compression.FileSystem.dll",
    "/resource:`"$zipPath`",app.zip",
    "/win32icon:`"$tempIcoPath`"",
    "`"$(Join-Path $installerDir "Setup.cs")`""
)

# Run compiler
$process = Start-Process -FilePath $cscPath -ArgumentList $arguments -Wait -NoNewWindow -PassThru
if ($process.ExitCode -ne 0) {
    throw "csc.exe compilation failed with exit code $($process.ExitCode)"
}

Write-Host "Cleaning up temporary files..."
Remove-Item -Recurse -Force $tempPayloadDir
Remove-Item -Recurse -Force $buildOutputDir
Remove-Item -Force $zipPath
Remove-Item -Force $tempIcoPath

Write-Host "Copying installer to Downloads folder..."
if (!(Test-Path $downloadsDir)) {
    New-Item -ItemType Directory -Path $downloadsDir
}
Copy-Item -Path $setupExePath -Destination $targetSetupPath -Force

Write-Host "Installer successfully built and copied to: $targetSetupPath"
