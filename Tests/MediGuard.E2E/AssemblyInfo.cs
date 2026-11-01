using Xunit;

// E2E browser tests share one Chrome-driven session against a single running app and a
// real (development) database. Running classes in parallel would race on the shared cart
// and demo data and multiply browser processes, so the whole assembly executes serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
