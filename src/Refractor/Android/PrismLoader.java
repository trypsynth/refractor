package com.trypsynth.refractor;

// Java looks a library's class loader up from the class that loads it, and prism's JNI_OnLoad
// finds prism's Java classes through that loader, so prism has to be loaded from a class of the
// app's own. A call straight from .NET has no such class.
public final class PrismLoader {
	private PrismLoader() {}

	public static void load(String path) {
		System.load(path);
	}
}
