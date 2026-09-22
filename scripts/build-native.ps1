# Builds prism twice per runtime: a static library for Native AOT to link into the executable,
# and a shared library for everything else. Both use the static C runtime, which is what
# Native AOT links, so the static objects agree with it and the DLL needs no redistributable.
param(
	[string[]]$Runtimes = @("win-x64"),
	[string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root "native/prism"
$architectures = @{ "win-x64" = "x64"; "win-arm64" = "ARM64"; "win-x86" = "Win32" }
# CMake falls back to Ninja on some setups, which cannot take -A, so the newest Visual Studio with
# the C++ tools is looked up and named explicitly.
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio/Installer/vswhere.exe"
$studio = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -format json | ConvertFrom-Json | Select-Object -First 1
if (-not $studio) { throw "No Visual Studio with the C++ tools was found." }
$years = @{ "16" = "2019"; "17" = "2022"; "18" = "2026" }
$major = $studio.installationVersion.Split(".")[0]
if (-not $years.ContainsKey($major)) { throw "Visual Studio $major is not one this script knows the CMake generator for." }
$generator = "Visual Studio $major $($years[$major])"
# prism builds its screen reader import libraries with whichever lib tool is first on PATH, and
# llvm-lib leaves x86 stdcall names undecorated, so they cannot link. MSVC's own lib is named here.
$toolset = (Get-Content (Join-Path $studio.installationPath "VC/Auxiliary/Build/Microsoft.VCToolsVersion.default.txt")).Trim()
$librarian = Join-Path $studio.installationPath "VC/Tools/MSVC/$toolset/bin/Hostx64/x64/lib.exe"
if (-not (Test-Path $librarian)) { throw "MSVC's lib.exe was not found at $librarian." }

function Write-LinkTargets([string]$libraries) {
	# prism's install step lists every import library its Windows backends need, with the DLL
	# behind each. Those DLLs belong to screen readers most machines do not have, so each one is
	# delay loaded, or the executable would refuse to start without all of them.
	$imports = Get-Content (Join-Path $libraries "prism-static-windows.txt") | Where-Object { $_.Trim() } | ForEach-Object { , ($_ -split "\s+") }
	$items = @('		<NativeLibrary Include="$(MSBuildThisFileDirectory)prism.lib" />')
	foreach ($import in $imports) {
		$items += "		<NativeLibrary Include=`"`$(MSBuildThisFileDirectory)$($import[0]).lib`" />"
		$items += "		<LinkerArg Include=`"/DELAYLOAD:$($import[1])`" />"
	}
	$targets = @("<Project>", "", "	<ItemGroup>") + $items + @("	</ItemGroup>", "", "</Project>")
	Set-Content -Path (Join-Path $libraries "Refractor.Native.targets") -Value $targets -Encoding utf8
}

foreach ($runtime in $Runtimes) {
	$architecture = $architectures[$runtime]
	foreach ($kind in "static", "shared") {
		$build = Join-Path $root "artifacts/native-build/$runtime/$kind"
		$install = Join-Path $root "artifacts/native/$runtime/$kind"
		$shared = if ($kind -eq "shared") { "ON" } else { "OFF" }
		cmake -S $source -B $build -G $generator -A $architecture "-DBUILD_SHARED_LIBS=$shared" "-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded" "-DCMAKE_INSTALL_PREFIX=$install" "-DPRISM_LIB_TOOL=$librarian"
		if ($LASTEXITCODE -ne 0) { throw "Configuring prism ($runtime, $kind) failed." }
		cmake --build $build --config $Configuration --parallel
		if ($LASTEXITCODE -ne 0) { throw "Building prism ($runtime, $kind) failed." }
		cmake --install $build --config $Configuration
		if ($LASTEXITCODE -ne 0) { throw "Installing prism ($runtime, $kind) failed." }
		if ($kind -eq "static") { Write-LinkTargets (Join-Path $install "lib") }
	}
}
