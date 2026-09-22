#!/bin/sh
# The emulator runner hands each line of its script to its own shell, so the steps live here.
set -e
apk=$(find samples/SpeakAndroid/bin -name '*-Signed.apk' | head -1)
adb install -r "$apk"
adb logcat -c
adb shell am start -W -n com.trypsynth.refractor.speak/com.trypsynth.refractor.speak.MainActivity
for _ in $(seq 1 90); do
	adb logcat -d -s REFRACTOR | grep -q "spoke\|failed" && break
	sleep 1
done
adb logcat -d -s REFRACTOR DOTNET AndroidRuntime
adb logcat -d -s REFRACTOR | grep -q "spoke"
