using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIDemonV2.ViewModels;

public partial class RightPanelViewModel : ObservableObject
{
	private readonly IMessageRepository _messageRepository;
	private readonly ICodeRunnerService _codeRunnerService;
	private readonly IMessageExportService _messageExportService;
	private readonly IDialogService _dialogService;

	private Message _selectedMessage;
	public Message? SelectedMessage
	{
		get => _selectedMessage;
		private set => SetProperty(ref _selectedMessage, value);
	}

	public event Action<Message>? MessageUpdated;
	public event Action<string>? ResendMessageRequested;

	[ObservableProperty]
	public string messageContent;

	[ObservableProperty]
	public string consoleOutput;

	public RightPanelViewModel(
		IMessageRepository messageRepository,
		ICodeRunnerService codeRunnerService,
		IMessageExportService messageExportService,
		IDialogService dialogService)
	{
		_messageRepository = messageRepository;
		_codeRunnerService = codeRunnerService;
		_messageExportService = messageExportService;
		_dialogService = dialogService;
	}

	public void SelectMessage(Message? message)
	{
		SelectedMessage = message;
		MessageContent = message?.MessageContent ?? string.Empty;
		ConsoleOutput = string.Empty;
	}

	[RelayCommand]
	private async Task SaveFavourite()
	{
		if (SelectedMessage != null)
		{
			SelectedMessage.MessageContent = MessageContent;
			SelectedMessage.Favourite = true;
			SelectedMessage.ModificationDate = DateTime.UtcNow;
			await _messageRepository.UpdateAsync(SelectedMessage);
			MessageUpdated?.Invoke(SelectedMessage);
		}
	}

	[RelayCommand]
	private async Task RunCode()
	{
		if (!string.IsNullOrEmpty(SelectedMessage?.MessageContent) &&
			!string.IsNullOrEmpty(SelectedMessage?.ProgrammingLanguage))
		{
			ConsoleOutput = string.Empty;

			await _codeRunnerService.RunCodeAsync(
				MessageContent,
				SelectedMessage.ProgrammingLanguage,
				output =>
				{
					Dispatcher.UIThread.Post(() =>
					{
						ConsoleOutput += output;
					});
				});
		}
	}

	[RelayCommand]
	private void ResendMessage()
	{
		ResendMessageRequested?.Invoke(MessageContent);
	}

	[RelayCommand]
	private async Task DeleteMessage()
	{
		if (SelectedMessage != null)
		{
			if (await _dialogService.ShowConfirmationDialog("Delete message", "Are you sure that you want to delete this message? It will remove all your changes made so far."))
			{
				MessageContent = string.Empty;
				SelectedMessage.Favourite = false;
				SelectedMessage.ModificationDate = DateTime.UtcNow;
				SelectedMessage.MessageContent = SelectedMessage.OriginalMessage;
				await _messageRepository.UpdateAsync(SelectedMessage);
				MessageUpdated?.Invoke(SelectedMessage);
				SelectedMessage = null;
			}
		}
	}

	[RelayCommand]
	private async Task ClearMessage()
	{
		if (SelectedMessage != null)
		{
			if (MessageContent != SelectedMessage.MessageContent &&
			!await _dialogService.ShowConfirmationDialog("Clear message", "Are you sure that you want to clear this message? It will remove all your changes made so far."))
				return;

			SelectedMessage = null;
			MessageContent = string.Empty;
			ConsoleOutput = string.Empty;
		}
	}

	[RelayCommand]
	private async Task ExportMessage()
	{
		if (SelectedMessage != null)
		{
			await _messageExportService.ExportMessageAsScriptAsync(SelectedMessage);
			await _dialogService.ShowConfirmationDialog("Eksport", "Script exported successfully.", true);
		}
	}
}