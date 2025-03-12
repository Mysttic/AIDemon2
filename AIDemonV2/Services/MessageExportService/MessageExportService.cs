using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AIDemonV2.Extensions;
using CsvHelper;
using CsvHelper.Configuration;

public class MessageExportService : IMessageExportService
{
	private readonly IMessageRepository _messageRepository;
	private readonly IDialogService _dialogService;

	public MessageExportService(IMessageRepository messageRepository, IDialogService dialogService)
	{
		_messageRepository = messageRepository;
		_dialogService = dialogService;
	}

	public async Task ExportMessagesAsync()
	{
		// Wybór formatu
		var format = await _dialogService.SelectExportFormat();
		if (format == null) return; // Anulowanie operacji

		// Wybór pliku
		var filePath = await _dialogService.SelectMessagesExportFilePath(format);
		if (string.IsNullOrEmpty(filePath)) return;

		// Pobranie wiadomości
		var messages = await _messageRepository.GetAllAsync();

		// Eksport
		switch (format)
		{
			case "json":
				await ExportToJsonAsync(messages, filePath);
				break;
			case "csv":
				await ExportToCsvAsync(messages, filePath);
				break;
		}
	}

	private async Task ExportToJsonAsync(IEnumerable<Message> messages, string filePath)
	{
		var dtos = messages.Select(MessageDto.FromMessage);
		var options = new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve };
		var json = JsonSerializer.Serialize(dtos, options);
		await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
	}

	private async Task ExportToCsvAsync(IEnumerable<Message> messages, string filePath)
	{
		var dtos = messages.Select(MessageDto.FromMessage);
		using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
		using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));
		await csv.WriteRecordsAsync(dtos);
	}

	public async Task ExportMessageAsScriptAsync(Message message)
	{
		if (string.IsNullOrWhiteSpace(message.ProgrammingLanguage) || string.IsNullOrWhiteSpace(message.MessageContent))
		{
			throw new InvalidOperationException("Wiadomość nie zawiera języka programowania ani treści.");
		}

		// Pobierz rozszerzenie dla języka
		string extension = GetScriptExtension(message.ProgrammingLanguage);
		if (extension == null)
		{
			throw new InvalidOperationException($"Nieobsługiwany język: {message.ProgrammingLanguage}");
		}

		// Wybierz miejsce zapisu
		string? filePath = await _dialogService.SelectMessageScriptExportFilePath(message.ProgrammingLanguage, extension);
		if (string.IsNullOrEmpty(filePath)) return;

		// Tworzenie zawartości skryptu
		var scriptContent = GenerateScriptContent(message);

		// Zapisz plik
		await File.WriteAllTextAsync(filePath, scriptContent, Encoding.UTF8);
	}

	private static string GetScriptExtension(string? language)
	{
		return language?.ToLower() switch
		{
			"python" => "py",
			"powershell" => "ps1",
			_ => null // Jeśli język nie jest obsługiwany, zwróć null
		} ?? string.Empty;
	}

	private static string GenerateScriptContent(Message message)
	{
		var sb = new StringBuilder();

		sb.AppendLine($"# {message.ProgrammingLanguage.ToUpper()} Script");
		sb.AppendLine($"# Exported at {DateTime.Now}");
		sb.AppendLine("# ---------------------------");

		// Treść wiadomości jako kod skryptu
		sb.AppendLine(message.MessageContent.RemoveMarkdownCodeBlockMarkers());
		sb.AppendLine();

		return sb.ToString();
	}
}
