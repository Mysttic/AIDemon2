using Avalonia;
using Microsoft.Extensions.DependencyInjection;

namespace AIDemon2.Views;

/// <summary>
/// Dostęp widoków do kontenera zależności.
///
/// Widoki pobierały usługi, powtarzając w każdym pliku rzutowanie
/// <c>(IServiceProvider)Application.Current!.Resources["Services"]</c>, co dawało
/// po dwa ostrzeżenia o możliwym null na plik i cichy <c>NullReferenceException</c>,
/// gdyby zasób był nieustawiony.
///
/// To nadal jest service locator i docelowo powinien zniknąć na rzecz wstrzykiwania
/// ViewModeli przez binding <c>DataContext</c>. Do tego czasu przynajmniej jest
/// w jednym miejscu i zawodzi z czytelnym komunikatem.
/// </summary>
internal static class ViewServices
{
	public const string ResourceKey = "Services";

	public static T Get<T>() where T : notnull
	{
		if (Application.Current?.Resources[ResourceKey] is not IServiceProvider provider)
			throw new InvalidOperationException(
				$"Zasób \"{ResourceKey}\" nie zawiera kontenera zależności. " +
				"Widok został utworzony przed konfiguracją aplikacji.");

		return provider.GetRequiredService<T>();
	}
}
