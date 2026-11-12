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
# Linux builds are native only, so each runtime has to be built on a machine of its own architecture.
$linuxArchitectures = @{ "linux-x64" = "X64"; "linux-arm64" = "Arm64" }
# prism quietly leaves out a Linux backend whose headers are missing, so they are all required
# here, and a build machine without them fails instead of making a package without them. Spiel is
# left out: no Ubuntu release packages its headers, and it would make prism link GIO.
$linuxBackends = @{ "orca" = "ON"; "speech_dispatcher" = "ON"; "clipboard" = "ON"; "spiel" = "OFF" }

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
	# prism loads the screen reader DLLs most machines do not have itself, so only system libraries are linked.
	$items = @(
		'<NativeLibrary Include="$(MSBuildThisFileDirectory)prism.lib" />',
		'<NativeLibrary Include="ole32.lib;onecore.lib;runtimeobject.lib;uiautomationcore.lib;rpcrt4.lib;powrprof.lib;xaudio2.lib" />'
	)
	Write-Targets $libraries $items
}

function Write-AppleLinkTargets([string]$libraries) {
	# Each backend registers itself from a static nothing else refers to, so the archive has to be
	# loaded whole or the linker drops every backend.
	$items = @('<LinkerArg Include="-Wl,-force_load,&quot;$(MSBuildThisFileDirectory)libprism.a&quot;" />')
	foreach ($framework in "Foundation", "AVFoundation", "AppKit", "IOKit", "CoreFoundation", "CoreServices", "Security") {
		$items += "<LinkerArg Include=`"-framework $framework`" />"
	}
	$items += '<LinkerArg Include="-lc++" />'
	Write-Targets $libraries $items
}

function Get-LinuxLinkName([string]$flag) {
	# -ldbus-1 needs libdbus-1.so, which only the development package installs, so an application's
	# build machine could not link without it. Naming the library by its soname, -l:libdbus-1.so.3,
	# needs only the library itself, which every system using it has.
	if ($flag -notmatch '^-l([^:].*)$') { return $flag }
	$compiler = $env:CC ? $env:CC : "cc"
	$library = & $compiler "-print-file-name=lib$($Matches[1]).so"
	if ($LASTEXITCODE -ne 0 -or -not (Test-Path $library)) { return $flag }
	$soname = objdump -p $library | Select-String 'SONAME\s+(\S+)' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Select-Object -First 1
	return $soname ? "-l:$soname" : $flag
}

function Write-LinuxLinkTargets([string]$libraries, [string]$install) {
	# As on macOS, the backends register themselves from statics nothing refers to, so the whole
	# archive is linked. The system libraries prism uses depend on what the build machine had, so
	# they are read from the CMake package it installs rather than listed here.
	$items = @('<LinkerArg Include="-Wl,--whole-archive;&quot;$(MSBuildThisFileDirectory)libprism.a&quot;;-Wl,--no-whole-archive" />')
	$config = Get-Content (Join-Path $install "share/prism/prism-config.cmake") -Raw
	$modules = [regex]::Matches($config, 'pkg_check_modules\([^"]*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
	foreach ($module in $modules) {
		$flags = pkg-config --libs $module
		Invoke-Checked "Finding the libraries for $module"
		foreach ($flag in -split $flags) { $items += "<LinkerArg Include=`"$(Get-LinuxLinkName $flag)`" />" }
	}
	foreach ($flag in "-lstdc++", "-ldl", "-lpthread", "-lm") { $items += "<LinkerArg Include=`"$flag`" />" }
	Write-Targets $libraries $items
}

function Write-IosLinkTargets([string]$libraries) {
	# The .NET iOS build links native references itself, for Mono and Native AOT alike. It only
	# keeps a symbol reachable at run time when told to, so every function in prism's header is
	# named, since Refractor looks them up in the app itself.
	$items = @('<NativeReference Include="$(MSBuildThisFileDirectory)libprism.a" Kind="Static" ForceLoad="true" IsCxx="true" SmartLink="false" Frameworks="Foundation AVFoundation UIKit" />')
	$functions = Select-String -CaseSensitive -Path (Join-Path $source "include/prism.h") -Pattern "\b(prism_[a-z_]+)\(" -AllMatches | ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
	foreach ($function in $functions) { $items += "<ReferenceNativeSymbol Include=`"$function`" SymbolType=`"Function`" />" }
	Write-Targets $libraries $items
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
			# prism builds its GDK speech backend whenever it finds a GDK install, which would make the
			# result depend on the machine and need xgameruntime.thunks.dll shipped beside it.
			$arguments += @("-G", $windows.Generator, "-A", $windowsArchitectures[$runtime], "-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded", "-DPRISM_LIB_TOOL=$($windows.Librarian)", "-DPRISM_ENABLE_GDK_SPEECH_BACKEND=OFF")
		} elseif ($iosTargets.ContainsKey($runtime)) {
			$target = $iosTargets[$runtime]
			$arguments += @("-G", "Unix Makefiles", "-DCMAKE_BUILD_TYPE=$Configuration", "-DCMAKE_SYSTEM_NAME=iOS", "-DCMAKE_OSX_SYSROOT=$($target.Sysroot)", "-DCMAKE_OSX_ARCHITECTURES=$($target.Architecture)", "-DCMAKE_OSX_DEPLOYMENT_TARGET=$iosDeploymentTarget", "-DPRISM_ENABLE_TESTS=OFF", "-DPRISM_ENABLE_DEMOS=OFF", "-DPRISM_ENABLE_GDEXTENSION=OFF")
		} elseif ($appleArchitectures.ContainsKey($runtime)) {
			# Homebrew on Intel installs into /usr/local/include, and an older simdutf there is found
			# ahead of the one prism bundles, so the bundled headers are named first.
			$simdutf = Join-Path $source "third_party/simdutf/include"
			$arguments += @("-G", "Unix Makefiles", "-DCMAKE_BUILD_TYPE=$Configuration", "-DCMAKE_OSX_ARCHITECTURES=$($appleArchitectures[$runtime])", "-DCMAKE_OSX_DEPLOYMENT_TARGET=$appleDeploymentTarget", "-DCMAKE_CXX_FLAGS=-I$simdutf")
		} elseif ($linuxArchitectures.ContainsKey($runtime)) {
			$machine = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
			if (-not $IsLinux -or $machine -ne $linuxArchitectures[$runtime]) { throw "$runtime has to be built on $($linuxArchitectures[$runtime]) Linux." }
			$arguments += @("-G", "Unix Makefiles", "-DCMAKE_BUILD_TYPE=$Configuration") + ($linuxBackends.GetEnumerator() | ForEach-Object { "-DPRISM_ENABLE_$($_.Key.ToUpperInvariant())_BACKEND=$($_.Value)" })
			# The C++ runtime goes into the shared prism, so it runs on distributions whose libstdc++
			# is older than the build machine's, and is kept out of its exports so it cannot clash
			# with another copy in the same process.
			if ($kind -eq "shared") { $arguments += "-DCMAKE_SHARED_LINKER_FLAGS=-static-libstdc++ -static-libgcc -Wl,--exclude-libs,ALL" }
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
		elseif ($linuxArchitectures.ContainsKey($runtime)) { Write-LinuxLinkTargets $libraries $install }
		else { Write-AppleLinkTargets $libraries }
	}
}
