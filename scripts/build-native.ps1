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
		cmake -S $source -B $build -A $architecture "-DBUILD_SHARED_LIBS=$shared" "-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded" "-DCMAKE_INSTALL_PREFIX=$install"
		if ($LASTEXITCODE -ne 0) { throw "Configuring prism ($runtime, $kind) failed." }
		cmake --build $build --config $Configuration --parallel
		if ($LASTEXITCODE -ne 0) { throw "Building prism ($runtime, $kind) failed." }
		cmake --install $build --config $Configuration
		if ($LASTEXITCODE -ne 0) { throw "Installing prism ($runtime, $kind) failed." }
		if ($kind -eq "static") { Write-LinkTargets (Join-Path $install "lib") }
	}
}
