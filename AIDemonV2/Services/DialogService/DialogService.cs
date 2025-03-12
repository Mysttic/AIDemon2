using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

public class DialogService : IDialogService
{
	private Window? _mainWindow;

	public void Initialize(Window mainWindow)
	{
		_mainWindow = mainWindow;
	}

	public async Task<bool> ShowConfirmationDialog(string title, string message)
	{
		if (_mainWindow == null)
			throw new InvalidOperationException("DialogService is not initialized with a main window.");

		var messageBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
		{
			ContentTitle = title,
			ContentMessage = message,
			ButtonDefinitions = ButtonEnum.YesNo,
			Icon = Icon.Question
		});

		var result = await messageBox.ShowAsPopupAsync(_mainWindow);
		return result == ButtonResult.Yes;
	}
}