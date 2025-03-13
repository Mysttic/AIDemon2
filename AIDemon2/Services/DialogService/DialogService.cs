using Avalonia.Controls;
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

	public async Task<bool> ShowConfirmationDialog(string title, string message, bool oneDecision = false)
	{
		if (_mainWindow == null)
			throw new InvalidOperationException("DialogService is not initialized with a main window.");

		var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
		{
			ContentTitle = title,
			ContentMessage = message,
			ButtonDefinitions = oneDecision ? ButtonEnum.Ok : ButtonEnum.YesNo,
			Icon = Icon.None
		});

		var result = await messageBox.ShowAsPopupAsync(_mainWindow);
		return result == ButtonResult.Yes;
	}

	public async Task<string?> SelectExportFormat()
	{
		var messageBox = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
		{
			ContentTitle = "Wybierz format eksportu",
			ContentMessage = "Wybierz format, w którym chcesz zapisać wiadomości:",
			ButtonDefinitions = new[]
			{
				new ButtonDefinition { Name = "JSON", IsDefault = true },
				new ButtonDefinition { Name = "CSV" },
				new ButtonDefinition { Name = "Anuluj", IsCancel = true }
			},
			Icon = Icon.Question
		});

		var result = await messageBox.ShowAsPopupAsync(_mainWindow);
		return result == "Anuluj" ? null : result.ToLower();
	}

	public async Task<string?> SelectMessagesExportFilePath(string format)
	{
		return await SelectFilePath("messages", format, new List<FileDialogFilter>
		{
			new FileDialogFilter { Name = "JSON Files", Extensions = { "json" } },
			new FileDialogFilter { Name = "CSV Files", Extensions = { "csv" } }
		});
	}

	public async Task<string?> SelectMessageScriptExportFilePath(string language, string format)
	{
		return await SelectFilePath($"{language} script {DateTime.Now.ToShortDateString()}", format, new List<FileDialogFilter>
		{
			new FileDialogFilter { Name = $"Plik {language}", Extensions = { format } }
		});
	}

	private async Task<string?> SelectFilePath(string initialName, string format, List<FileDialogFilter> filters)
	{
		var saveFileDialog = new SaveFileDialog
		{
			Title = "Zapisz wiadomości",
			Filters = filters,
			DefaultExtension = format,
			InitialFileName = $"{initialName}.{format}"
		};

		return await saveFileDialog.ShowAsync(_mainWindow);
	}
}