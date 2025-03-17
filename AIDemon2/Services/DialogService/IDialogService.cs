using Avalonia.Controls;

public interface IDialogService
{
	void Initialize(Window mainWindow);

	Task<bool> ShowConfirmationDialog(string title, string message, bool oneDecision = false);

	Task<string?> SelectExportFormat();
	Task<string?> SelectMessagesExportFilePath(string format);
	Task<string?> SelectMessageScriptExportFilePath(string language, string format);
}