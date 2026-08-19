[assembly: Parallelizable(ParallelScope.Children)]

// I don't know why this parallelism limit was originally put here.
// I *do* know that I tried removing it, and ran into the following .NET runtime problem:
// https://github.com/dotnet/runtime/issues/107197
// So we can't really parallelize integration tests harder either until the runtime fixes that,
// *or* we fix serv3 to not spam expression trees.

// TODO: Если упрётесь в таймаут по памяти (16 GB) или зависание - откатить обратно на 2.
[assembly: LevelOfParallelism(4)] // ADT-Tweak
