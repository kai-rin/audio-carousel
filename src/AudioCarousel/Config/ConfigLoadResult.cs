namespace AudioCarousel.Config;

// WasUnreadable: the file exists but could not be read (e.g. locked by an
// antivirus). Config holds in-memory defaults and saving is blocked so the
// user's real file is never overwritten.
public sealed record ConfigLoadResult(ConfigSchema Config, bool FreshlyCreated, bool WasCorrupted, bool WasUnreadable);
