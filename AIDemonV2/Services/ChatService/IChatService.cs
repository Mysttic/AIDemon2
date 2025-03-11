public interface IChatService
{
	Task<Message> SendMessageAsync(Message userMessage);
	void ResetClient();
}
