// Polyfill para `init` e `record` em .NET Standard 2.1 (o tipo só existe a partir do .NET 5).
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit
{
}
