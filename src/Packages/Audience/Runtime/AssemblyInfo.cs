using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Immutable.Audience.Runtime.Tests")]
[assembly: InternalsVisibleTo("Immutable.Audience.Unity")]

// First-party SampleApp reaches Json.Serialize and
// JsonReader.DeserializeObject; both stay internal so their
// signatures aren't frozen into the public API.
[assembly: InternalsVisibleTo("Immutable.Audience.Samples.SampleApp")]

// SampleApp tests pin against the same internal constants the runtime SDK uses.
[assembly: InternalsVisibleTo("Immutable.Audience.Samples.SampleApp.Tests")]
