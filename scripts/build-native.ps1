# Builds prism twice per runtime: a static library for Native AOT to link into the executable,
# and a shared library for everything else. iOS only gets the static library, since an app there
# cannot load a library of its own at run time. Each static build also gets a Refractor.Native.targets
# holding everything the final link needs, so the package's own targets stay platform neutral.
param(
	[string[]]$Runtimes = @("win-x64"),
	[string]$Configuration = "Release"
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root "native/prism"
$windowsArchitectures = @{ "win-x64" = "x64"; "win-arm64" = "ARM64"; "win-x86" = "Win32" }
$appleArchitectures = @{ "osx-arm64" = "arm64"; "osx-x64" = "x86_64" }
# The oldest macOS that .NET 8, the package's lowest target, runs on.
$appleDeploymentTarget = "12.0"
$iosTargets = @{
	"ios-arm64" = @{ Sysroot = "iphoneos"; Architecture = "arm64" }
	"iossimulator-arm64" = @{ Sysroot = "iphonesimulator"; Architecture = "arm64" }
	"iossimulator-x64" = @{ Sysroot = "iphonesimulator"; Architecture = "x86_64" }
}
# prism's own iOS builds target this release.
$iosDeploymentTarget = "14.0"

function Get-WindowsToolchain {
	# CMake falls back to Ninja on some setups, which cannot take -A, so the newest Visual Studio
	# with the C++ tools is looked up and named explicitly.
	$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio/Installer/vswhere.exe"
	$studio = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -format json | ConvertFrom-Json | Select-Object -First 1
	if (-not $studio) { throw "No Visual Studio with the C++ tools was found." }
	$years = @{ "16" = "2019"; "17" = "2022"; "18" = "2026" }
	$major = $studio.installationVersion.Split(".")[0]
	if (-not $years.ContainsKey($major)) { throw "Visual Studio $major is not one this script knows the CMake generator for." }
	# prism builds its screen reader import libraries with whichever lib tool is first on PATH,
	# and llvm-lib leaves x86 stdcall names undecorated, so they cannot link.
	$toolset = (Get-Content (Join-Path $studio.installationPath "VC/Auxiliary/Build/Microsoft.VCToolsVersion.default.txt")).Trim()
	$librarian = Join-Path $studio.installationPath "VC/Tools/MSVC/$toolset/bin/Hostx64/x64/lib.exe"
	if (-not (Test-Path $librarian)) { throw "MSVC's lib.exe was not found at $librarian." }
	return @{ Generator = "Visual Studio $major $($years[$major])"; Librarian = $librarian }
}

function Write-Targets([string]$directory, [string[]]$items) {
	$lines = @("<Project>", "", "	<ItemGroup>") + ($items | ForEach-Object { "		$_" }) + @("	</ItemGroup>", "", "</Project>")
	Set-Content -Path (Join-Path $directory "Refractor.Native.targets") -Value $lines -Encoding utf8
}

function Write-WindowsLinkTargets([string]$libraries) {
	# prism's install step lists every import library its Windows backends need, with the DLL
	# behind each. Those DLLs belong to screen readers most machines do not have, so each one is
	# delay loaded, or the executable would refuse to start without all of them.
	$imports = Get-Content (Join-Path $libraries "prism-static-windows.txt") | Where-Object { $_.Trim() } | ForEach-Object { , ($_ -split "\s+") }
	$items = @('<NativeLibrary Include="$(MSBuildThisFileDirectory)prism.lib" />')
	foreach ($import in $imports) {
		$items += "<NativeLibrary Include=`"`$(MSBuildThisFileDirectory)$($import[0]).lib`" />"
		$items += "<LinkerArg Include=`"/DELAYLOAD:$($import[1])`" />"
	}
	$items += '<NativeLibrary Include="ole32.lib;onecore.lib;runtimeobject.lib;uiautomationcore.lib;rpcrt4.lib;powrprof.lib;delayimp.lib" />'
	# The Orca and Speech Dispatcher bridges are Linux only, so their delay loads find nothing to defer.
	$items += '<LinkerArg Include="/IGNORE:4199" />'
	Write-Targets $libraries $items
}

function Write-AppleLinkTargets([string]$libraries) {
	# Each backend registers itself from a static nothing else refers to, so the archive has to be
	# loaded whole or the linker drops every backend.
	$items = @('<LinkerArg Include="-Wl,-force_load,&quot;$(MSBuildThisFileDirectory)libprism.a&quot;" />')
	foreach ($framework in "Foundation", "AVFoundation", "AppKit", "IOKit", "CoreFoundation") {
		$items += "<LinkerArg Include=`"-framework $framework`" />"
	}
	$items += '<LinkerArg Include="-lc++" />'
	Write-Targets $libraries $items
}

function Write-IosLinkTargets([string]$libraries) {
	# The .NET iOS build links native references itself, for Mono and Native AOT alike.
	Write-Targets $libraries @('<NativeReference Include="$(MSBuildThisFileDirectory)libprism.a" Kind="Static" ForceLoad="true" IsCxx="true" SmartLink="false" Frameworks="Foundation AVFoundation UIKit" />')
}

function Invoke-Checked([string]$description) {
	if ($LASTEXITCODE -ne 0) { throw "$description failed." }
}

$windows = $null
foreach ($runtime in $Runtimes) {
	$kinds = if ($iosTargets.ContainsKey($runtime)) { @("static") } else { @("static", "shared") }
	foreach ($kind in $kinds) {
		$build = Join-Path $root "artifacts/native-build/$runtime/$kind"
		$install = Join-Path $root "artifacts/native/$runtime/$kind"
		$shared = if ($kind -eq "shared") { "ON" } else { "OFF" }
		$arguments = @("-S", $source, "-B", $build, "-DBUILD_SHARED_LIBS=$shared", "-DCMAKE_INSTALL_PREFIX=$install")
		if ($windowsArchitectures.ContainsKey($runtime)) {
			$windows ??= Get-WindowsToolchain
			# The static C runtime is what Native AOT links, so the static objects agree with it and
			# the DLL needs no redistributable.
			$arguments += @("-G", $windows.Generator, "-A", $windowsArchitectures[$runtime], "-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded", "-DPRISM_LIB_TOOL=$($windows.Librarian)")
		} elseif ($iosTargets.ContainsKey($runtime)) {
			$target = $iosTargets[$runtime]
			$arguments += @("-G", "Unix Makefiles", "-DCMAKE_BUILD_TYPE=$Configuration", "-DCMAKE_SYSTEM_NAME=iOS", "-DCMAKE_OSX_SYSROOT=$($target.Sysroot)", "-DCMAKE_OSX_ARCHITECTURES=$($target.Architecture)", "-DCMAKE_OSX_DEPLOYMENT_TARGET=$iosDeploymentTarget", "-DPRISM_ENABLE_TESTS=OFF", "-DPRISM_ENABLE_DEMOS=OFF", "-DPRISM_ENABLE_GDEXTENSION=OFF")
		} elseif ($appleArchitectures.ContainsKey($runtime)) {
			$arguments += @("-G", "Unix Makefiles", "-DCMAKE_BUILD_TYPE=$Configuration", "-DCMAKE_OSX_ARCHITECTURES=$($appleArchitectures[$runtime])", "-DCMAKE_OSX_DEPLOYMENT_TARGET=$appleDeploymentTarget")
		} else {
			throw "$runtime is not a runtime this script can build."
		}
		cmake @arguments
		Invoke-Checked "Configuring prism ($runtime, $kind)"
		cmake --build $build --config $Configuration --parallel
		Invoke-Checked "Building prism ($runtime, $kind)"
		cmake --install $build --config $Configuration
		Invoke-Checked "Installing prism ($runtime, $kind)"
		if ($kind -ne "static") { continue }
		$libraries = Join-Path $install "lib"
		if ($windowsArchitectures.ContainsKey($runtime)) { Write-WindowsLinkTargets $libraries }
		elseif ($iosTargets.ContainsKey($runtime)) { Write-IosLinkTargets $libraries }
		else { Write-AppleLinkTargets $libraries }
	}
}
