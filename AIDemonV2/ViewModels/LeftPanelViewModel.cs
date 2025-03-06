using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIDemonV2.ViewModels;

public partial class LeftPanelViewModel : ObservableObject
{
	public ObservableCollection<SavedMessage> SavedMessages { get; set; } = new ObservableCollection<SavedMessage>();

	public LeftPanelViewModel()
	{
		// Inicjalizacja listy zapisanych wiadomości
		SavedMessages = new ObservableCollection<SavedMessage>
		{
			new SavedMessage
			{
				Message = new Message
				{
					MessageContent = "Testowa wiadomość 1",
					RunDate = DateTime.Now
				}
			},
			new SavedMessage
			{
				Message = new Message
				{
					MessageContent = "Testowa wiadomość 2",
					RunDate = DateTime.Now.AddMinutes(-10)
				}
			}
		};
	}
}

