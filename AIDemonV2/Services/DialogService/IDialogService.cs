using Avalonia.Controls;

public interface IDialogService
{
	void Initialize(Window mainWindow);
	Task<bool> ShowConfirmationDialog(string title, string message);
}

