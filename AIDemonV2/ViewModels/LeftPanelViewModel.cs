using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace AIDemonV2.ViewModels;

public partial class LeftPanelViewModel : ViewModelBase
{
	public ObservableCollection<SavedMessage> SavedMessages { get; set; } = new ObservableCollection<SavedMessage>();

	ISettingsRepository _settingsRepository;

	public LeftPanelViewModel(ISettingsRepository settingsRepository)
	{
		_settingsRepository = settingsRepository;
	}

}

