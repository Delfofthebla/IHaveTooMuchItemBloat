namespace System.Runtime.CompilerServices;

// netstandard2.1 lacks this type, which the compiler needs for init accessors and records.
// God I hate using netstandard2.1
internal static class IsExternalInit;
