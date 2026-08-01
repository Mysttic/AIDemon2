using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AIDemon2.ViewModels;

public partial class RightPanelViewModel : ObservableObject
{
	private readonly IMessageRepository _messageRepository;
	private readonly ICodeRunnerService _codeRunnerService;
	private readonly IMessageExportService _messageExportService;
	private readonly IDialogService _dialogService;

	/// <summary>
	/// Przekazanie roboty na wątek UI. Wstrzykiwalne, bo Dispatcher.UIThread.Post
	/// bez uruchomionej aplikacji Avalonii NIE rzuca — po cichu kolejkuje w próżnię,
	/// więc test widziałby pustą konsolę zamiast błędu.
	/// </summary>
	private readonly Action<Action> _uiPost;

	private Message? _selectedMessage;
	public Message? SelectedMessage
	{
		get => _selectedMessage;
		private set => SetProperty(ref _selectedMessage, value);
	}

	public event Action<Message>? MessageUpdated;
	public event Action<string>? ResendMessageRequested;

	[ObservableProperty]
	private string messageContent = string.Empty;

	[ObservableProperty]
	private string consoleOutput = string.Empty;

	public RightPanelViewModel(
		IMessageRepository messageRepository,
		ICodeRunnerService codeRunnerService,
		IMessageExportService messageExportService,
		IDialogService dialogService,
		Action<Action>? uiPost = null)
	{
		_messageRepository = messageRepository;
		_codeRunnerService = codeRunnerService;
		_messageExportService = messageExportService;
		_dialogService = dialogService;
		_uiPost = uiPost ?? (action => Dispatcher.UIThread.Post(action));
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
			// Kod pochodzi od modelu AI i wykonuje się z pełnymi uprawnieniami
			// użytkownika. Skoro usunięcie wiadomości wymaga potwierdzenia,
			// to uruchomienie dowolnego skryptu tym bardziej.
			if (!await _dialogService.ShowConfirmationDialog(
					"Run code",
					$"This will run the {SelectedMessage.ProgrammingLanguage} code below on your computer " +
					$"with your own permissions. It can read, change or delete your files." +
					$"{Environment.NewLine}{Environment.NewLine}" +
					$"Run it only if you understand what it does. Continue?"))
				return;

			ConsoleOutput = string.Empty;

			try
			{
				await _codeRunnerService.RunCodeAsync(
					MessageContent,
					SelectedMessage.ProgrammingLanguage,
					output => _uiPost(() => ConsoleOutput += output));
			}
			catch (NotSupportedException ex)
			{
				// Brakujący interpreter albo język niedostępny na tym systemie.
				// Bez tego bloku wyjątek wychodzi z AsyncRelayCommand na pętlę
				// komunikatów, a użytkownik — który przed chwilą potwierdził groźny
				// dialog — dostaje pustą konsolę albo zamknięcie aplikacji.
				_uiPost(() => ConsoleOutput = ex.Message);
			}
		}
	}

	[RelayCommand]
	private void ResendMessage()
	{
		ResendMessageRequested?.Invoke(MessageContent);
	}

	/// <summary>
	/// Zdejmuje wiadomość z ulubionych i przywraca jej pierwotną treść.
	///
	/// Komenda nazywała się DeleteMessage, a przycisk miał ikonę kosza na czerwonym
	/// tle — mimo że wiadomość zostaje w rozmowie i w bazie. Jedyne prawdziwe
	/// usuwanie (miękkie, przez flagę Deleted) robi "Cleanup" w lewym panelu.
	/// Nazwa i wygląd obiecywały nieodwracalną operację, której tu nie ma.
	/// </summary>
	[RelayCommand]
	private async Task UnpinMessage()
	{
		if (SelectedMessage == null)
			return;

		if (!await _dialogService.ShowConfirmationDialog(
				"Usuń z ulubionych",
				"Wiadomość zniknie z listy ulubionych, a jej treść wróci do pierwotnej wersji. " +
				"Sama wiadomość zostanie w rozmowie. Kontynuować?"))
			return;

		MessageContent = string.Empty;
		SelectedMessage.Favourite = false;
		SelectedMessage.ModificationDate = DateTime.UtcNow;
		SelectedMessage.MessageContent = SelectedMessage.OriginalMessage;
		await _messageRepository.UpdateAsync(SelectedMessage);
		MessageUpdated?.Invoke(SelectedMessage);
		SelectedMessage = null;
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