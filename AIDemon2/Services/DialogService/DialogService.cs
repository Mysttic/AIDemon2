using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;

public class DialogService : IDialogService
{
	private Window? _mainWindow;

	public void Initialize(Window mainWindow)
	{
		_mainWindow = mainWindow;
	}

	/// <summary>
	/// Okno właściciela było przekazywane do dialogów bez sprawdzenia, więc
	/// wywołanie przed <see cref="Initialize"/> kończyło się NullReferenceException
	/// gdzieś w środku biblioteki okien.
	/// </summary>
	private Window RequireWindow() =>
		_mainWindow ?? throw new InvalidOperationException(
			"DialogService was not initialised with a main window.");

	public async Task<bool> ShowConfirmationDialog(string title, string message, bool oneDecision = false)
	{
		var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
		{
			ContentTitle = title,
			ContentMessage = message,
			ButtonDefinitions = oneDecision ? ButtonEnum.Ok : ButtonEnum.YesNo,
			Icon = Icon.None
		});

		var result = await messageBox.ShowAsPopupAsync(RequireWindow());
		return result == ButtonResult.Yes;
	}

	public async Task<string?> SelectExportFormat()
	{
		var messageBox = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
		{
			ContentTitle = "Choose export format",
			ContentMessage = "Pick the format to save the messages in:",
			ButtonDefinitions = new[]
			{
				new ButtonDefinition { Name = "JSON", IsDefault = true },
				new ButtonDefinition { Name = "CSV" },
				new ButtonDefinition { Name = "Cancel", IsCancel = true }
			},
			Icon = Icon.Question
		});

		var result = await messageBox.ShowAsPopupAsync(RequireWindow());
		// Zamknięcie okna krzyżykiem oddaje pusty ciąg, nie "Cancel".
		return string.IsNullOrEmpty(result) || result == "Cancel" ? null : result.ToLower();
	}

	public Task<string?> SelectMessagesExportFilePath(string format) =>
		SelectFilePath("messages", format, new[]
		{
			new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } },
			new FilePickerFileType("CSV files") { Patterns = new[] { "*.csv" } }
		});

	public Task<string?> SelectMessageScriptExportFilePath(string language, string format) =>
		SelectFilePath($"{language} script {DateTime.Now.ToShortDateString()}", format, new[]
		{
			new FilePickerFileType($"{language} file") { Patterns = new[] { $"*.{format}" } }
		});

	/// <summary>
	/// SaveFileDialog i FileDialogFilter są w Avalonii 11 przestarzałe na rzecz
	/// StorageProvider. Stare API oddawało ścieżkę wprost; nowe zwraca
	/// <see cref="IStorageFile"/>, z którego ścieżkę lokalną trzeba wyłuskać —
	/// dla dostawcy plikowego na Windows zawsze się to udaje.
	/// </summary>
	private async Task<string?> SelectFilePath(string initialName, string format,
		IReadOnlyList<FilePickerFileType> fileTypes)
	{
		var file = await RequireWindow().StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Save messages",
			FileTypeChoices = fileTypes,
			DefaultExtension = format,
			SuggestedFileName = $"{initialName}.{format}"
		});

		return file?.TryGetLocalPath();
	}
}
