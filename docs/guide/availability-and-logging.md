# Availability and logging

## Knowing when a screen reader starts or stops

prism can watch for backends becoming available or unavailable, such as a screen reader being started or quit. Turn it on by giving <xref:Refractor.PrismOptions> a handler:

```csharp
using Prism prism = new(new PrismOptions {
	AvailabilityChanged = (id, name, isAvailable) => Console.WriteLine($"{name}: {isAvailable}")
});
```

The handler runs on prism's poll thread, never the caller's. A backend that is already running when polling starts is not reported. An exception the handler throws is written to prism's log, and polling carries on.

The handler belongs to the options rather than being an event, because prism starts polling while the `Prism` is being created, so a handler added afterwards could miss what happens first. For the same reason, <xref:Refractor.PrismOptions.AvailabilityBaselineEstablished> runs once after the first scan, for an app that checked a backend itself beforehand and needs to know when to trust the reports.

The rest of the options tune the polling: <xref:Refractor.PrismOptions.PollInterval> sets how often prism looks, <xref:Refractor.PrismOptions.DebounceSamples> how many scans must agree before a change is reported, and <xref:Refractor.PrismOptions.MaxBackoff> how far the interval may stretch while nothing changes. <xref:Refractor.Prism.PauseAvailabilityPolling> and <xref:Refractor.Prism.ResumeAvailabilityPolling> stop and restart it, and <xref:Refractor.PrismOptions.AutoPowerManagement> pauses it while the system sleeps.

## Logging

prism logs through one logger for the whole process. Handlers on <xref:Refractor.PrismLog.MessageLogged> receive each message at or above the level set with <xref:Refractor.PrismLog.SetLevel*>:

```csharp
PrismLog.SetLevel(LogLevel.Warning);
PrismLog.MessageLogged += (_, e) => Console.Error.WriteLine($"{e.Level} {e.Source}: {e.Message}");
```

Handlers run on prism's logging thread, one message at a time, so keep them quick. They must not add or remove handlers, or flush, from there. A handler that throws does not stop the others.

Setting the `PRISM_LOG` environment variable to `trace`, `debug`, `info`, `warn` or `error` makes prism log to standard error with no code at all, until a handler is added. <xref:Refractor.PrismLog.Write*> sends a message of your own through the same logger, and <xref:Refractor.PrismLog.Flush> waits until everything queued has been delivered.
