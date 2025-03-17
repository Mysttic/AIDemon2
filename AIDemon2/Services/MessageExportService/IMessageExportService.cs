using System.Collections.Generic;
using System.Threading.Tasks;

public interface IMessageExportService
{
	Task ExportMessagesAsync();
	Task ExportMessageAsScriptAsync(Message message);
}
