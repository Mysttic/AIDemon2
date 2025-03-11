public interface IChatService
{
	Task<Message> SendMessageAsync(string newMessageText);
	void ResetClient();
}
