using AccountingTool;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataBaseAccess;
using Financial_Manager.Client.Model;
using Financial_Manager.Client.Services;
using Financial_Manager.Client.Services.Interfaces;
using Financial_Manager.Client.View.Pages;
using Financial_Manager.Client.View.Pages.DialogPages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;

namespace Financial_Manager.Client.ViewModel
{
    public class MainViewModel : ObservableObject
    {
        private readonly INavigationService _navigationService;
        private readonly IConfigurationService _configurationService;
        private readonly ISignalRService _signalRService;

        private ConfigurationSetting _configurationSettings;
        private DbAccess _dbAccess;
        private ApplicationContext _context;
        
        private readonly DispatcherQueue dispatcherQueue;

        private string? _receivedConfirmationCode;
        private User? _newLoggedUser;

        public MainViewModel(INavigationService navigationService, 
            IConfigurationService configurationService, ISignalRService signalRService, DispatcherQueueProvider dispatcherQueueProvider)
        {

            _navigationService = navigationService;
            _configurationService = configurationService;
            _signalRService = signalRService;
            dispatcherQueue = dispatcherQueueProvider.DispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));

            _signalRService.OnConnected += OnConnected;
            _signalRService.OnAccessErrorReceived += OnAccessErrorReceived;
            _signalRService.OnSyncWarningReceived += OnSyncWarningReceived;
            _signalRService.OnDataCollectionsReceived += OnDataCollectionsReceived;
            _signalRService.OnConfirmCodeReceived += OnConfirmCodeReceived;
            _signalRService.OnConfirmErrorReceived += OnConfirmErrorReceived;
            _signalRService.OnDisconnected += OnDisconnected;

            _configurationSettings = InitializeConfiguration();

            _context = new ApplicationContext();
            _dbAccess = new DbAccess(_context);

            CurrentLoggedUser = (_dbAccess.GetAll<User>().Count() > 0) ? _dbAccess.GetAll<User>().First() : null;

            /* For testing logging */
            //_configurationSettings.IsLogged = false;
            //_configurationService.SaveConfigurationSettings(_configurationSettings);

            CategorySelectionMode = SelectionModes[0];
            FinanceSelectionMode = SelectionModes[0];

            // Command's initializing
            #region ...

            SendLoginRequestCommand = new RelayCommand(SendLoginRequest);

            BackToLoginEnterPageCommand = new RelayCommand(BackToLoginEnterPage);
            SendConfirmationCodeAgainCommand = new RelayCommand(() => 
            { 
                SendConfirmationCode(long.Parse(EnteredUserId!)); 
            });
            MainWindowClosedEventCommand = new RelayCommand(CloseConnectionWithServer);
            CheckEnteredConfirmationCodeCommand = new RelayCommand(async () => { await CheckReceivedConfirmationCode(); });
            CopyErrorMessageToClipboardCommand = new RelayCommand(CopyErrorMessageToClipboard);
            CopyUserIdToClipboardCommand = new RelayCommand(CopyUserIdToClipboard);
            BackToUsualPageCommand = new RelayCommand(BackToUsualPage);
            RefreshConnectionCommand = new RelayCommand(async () => {await RefreshConnection();  });
            ShowAddingNewCategoryPanelCommand = new RelayCommand<XamlRoot>(async (root) => { await ShowNewAddingCategoryPanel(root!); });
            RemoveSelectedCategoriesCommand = new RelayCommand<XamlRoot>(async (root) => { await RemoveSelectedCategories(root!); }); ;
            ShowUpdateDialogForSelectedCategoryCommand = new RelayCommand<XamlRoot>(async (root) => { await ShowUpdateDialogForSelectedCategory(root!); });
            
            SortCategoriesByTitleCommand = new RelayCommand<string>(async (sortType) => { await SortCategoriesByTitle(sortType!); });

            SortFinancialOperationsByExcludingCommand = new RelayCommand(async () => { await GetAndApllyFilteredSettings(); });

            SortFinancialOperationsByTitleCommand = new RelayCommand<string>
                ((sortType) => {SortFinancialOperationsByChangingPosition(sortType!, SortingTool.PositionalSortingType.ByTitle); });
            SortFinancialOperationsByAmountCommand = new RelayCommand<string>
                ((sortType) => {SortFinancialOperationsByChangingPosition(sortType!, SortingTool.PositionalSortingType.ByAmount); });
            SortFinancialOperationsByDateCommand = new RelayCommand<string>
                ((sortType) => {SortFinancialOperationsByChangingPosition(sortType!, SortingTool.PositionalSortingType.ByDate); });

            ShowAddingNewFinancialPanelCommand = new RelayCommand<XamlRoot>(async (root) => { await ShowAddingNewFinancialPanel(root!); });
            RemoveSelectedFinancialsCommand = new RelayCommand<XamlRoot>(async (root) => { await RemoveSelectedFinancialOperations(root!); });
            ShowUpdateDialogForSelectedFinancialCommand = new RelayCommand<XamlRoot>(async (root) => {await ShowUpdateDialogForSelectedFinancial(root!); });

            LogOutCurrentUserCommand = new RelayCommand<XamlRoot>(async (root) => 
            {
                var result = await ShowContentDialog<string>(
                    root!, 
                    "Logging out", 
                    "Yes, log out",
                    ContentDialogButton.Primary,
                    "Are you sure you want to log out?",
                    closeBtnText: "Cancel");

                if(result == ContentDialogResult.Primary)
                {
                    _navigationService.ChangePage<LoadingPage>();
                    LogOutCurrentUser();
                    _navigationService.ChangePage<LoginEnterPage>();
                }
            });

            TestClickCommand = new RelayCommand(() => { throw new ArgumentException("Test exception"); });
            #endregion

            TopMessageType = "Disconnected";
        }

        // Collections
        #region ...
        public ObservableCollection<SelectionCurrencyItem> CurrencyItems { get; private set; } = new ObservableCollection<SelectionCurrencyItem>()
        {
            new SelectionCurrencyItem {Title = "USD", Currency = Currencies.USD, IconPath = "../../Assets/usd.png"},
            new SelectionCurrencyItem {Title = "GPB", Currency = Currencies.GPB, IconPath = "../../Assets/gpb.png"},
            new SelectionCurrencyItem {Title = "UAH", Currency = Currencies.UAH, IconPath = "../../Assets/uah.png"},
            new SelectionCurrencyItem {Title = "EUR", Currency = Currencies.EUR, IconPath = "../../Assets/eur.png"},
            new SelectionCurrencyItem {Title = "JPY", Currency = Currencies.JPY, IconPath = "../../Assets/jpy.png"},
            new SelectionCurrencyItem {Title = "CNY", Currency = Currencies.CNY, IconPath = "../../Assets/cny.png"},
        };
        public ObservableCollection<SelectionCurrencyItem>? SortingComboboxCurrencies { get; set; }
        public ObservableCollection<SelectionModeItem> SelectionModes { get; private set; } = new ObservableCollection<SelectionModeItem>()
        {
            new SelectionModeItem {Title = "Single", SelectionMode = ListViewSelectionMode.Single},
            new SelectionModeItem {Title = "Multiple", SelectionMode = ListViewSelectionMode.Multiple},
            new SelectionModeItem {Title = "Extended", SelectionMode = ListViewSelectionMode.Extended}
        };
        public ObservableCollection<FinancialOperation>? FinancialOperations { get; set; }
        public ObservableCollection<FinancialOperation>? ExcludingFinancialOperations { get; set; } //sorted fin. op's by: category, type (income/expense), currency
        public ObservableCollection<OperationCategory>? OperationCategories { get; set; }
        public ObservableCollection<OperationCategory>? ComboboxOperationCategories { get; set; }
        public ObservableCollection<OperationCategory>? SortingComboboxOperationCategories { get; set; }
        #endregion

        // Event handlers
        #region ...
        private void OnSyncWarningReceived()
        {
            this.dispatcherQueue.TryEnqueue(() =>
            {
                TopMessageType = "Warning";
            });
        }

        private void OnAccessErrorReceived()
        {
            LogOutCurrentUser();

            throw new Exception("You don't have an access anymore.");
        }

        private async void OnDataCollectionsReceived(List<FinancialOperation> financialOperations, List<OperationCategory> operationCategories)
        {
            await _dbAccess.UpdateEntitiesAsync(operationCategories);
            await _dbAccess.UpdateEntitiesAsync(financialOperations);

            LoadDataFromLocalDB();

            this.dispatcherQueue.TryEnqueue(() =>
            {
                SetStartupDataProperties();

                // to don't set the same page twice, "CheckReceivedConfirmationCode" method
                if (_configurationSettings.IsLogged)
                    _navigationService.ChangePage<MainPage>();
            });
        }

        private void OnDisconnected()
        {
            this.dispatcherQueue.TryEnqueue(() =>
            {
                TopMessageType = "Disconnected";
            });
        }

        private void OnConfirmErrorReceived(string obj)
        {
            this.dispatcherQueue.TryEnqueue(() =>
            {
                LoginEnterPageErrorMessage = obj;
            });
        }

        private void OnConfirmCodeReceived(string code, User user)
        {
            _receivedConfirmationCode = code;
            _newLoggedUser = user;

            var users = _dbAccess.GetAll<User>();

            if (users.Count() == 0)
            {
                _dbAccess.Add(user);
            }
            else
            {
                _dbAccess.RemoveRange(users);
                _dbAccess.Add(user);
            }

            this.dispatcherQueue.TryEnqueue(() =>
            {
                _navigationService.ChangePage<LoginConfirmPage>();
            });
        }

        private async void OnConnected()
        {
            TopMessageType = "Connected";

            if (_configurationSettings.IsLogged)
            {
                await _signalRService.RequestDataCollections(CurrentLoggedUser!.UserId);
            }
            else
            {
                _navigationService.ChangePage<LoginEnterPage>();
            }
        }
        #endregion

        // Commands
        #region ...
        public ICommand TestClickCommand { get; }
        public ICommand SendLoginRequestCommand { get; }
        public ICommand BackToLoginEnterPageCommand { get; }
        public ICommand SendConfirmationCodeAgainCommand { get; }
        public ICommand MainWindowClosedEventCommand { get; }
        public ICommand CheckEnteredConfirmationCodeCommand { get; }
        public ICommand CopyErrorMessageToClipboardCommand { get; }
        public ICommand CopyUserIdToClipboardCommand { get; }
        public ICommand BackToUsualPageCommand { get; }
        public ICommand RefreshConnectionCommand { get; }
        public ICommand ShowAddingNewCategoryPanelCommand { get; }
        public ICommand ShowAddingNewFinancialPanelCommand { get; }
        public ICommand RemoveSelectedCategoriesCommand { get; }
        public ICommand RemoveSelectedFinancialsCommand { get; }
        public ICommand ShowUpdateDialogForSelectedCategoryCommand { get; }
        public ICommand ShowUpdateDialogForSelectedFinancialCommand { get; }
        public ICommand SortCategoriesByTitleCommand { get; }
        public ICommand SortFinancialOperationsByExcludingCommand { get; }
        public ICommand SortFinancialOperationsByTitleCommand { get; }
        public ICommand SortFinancialOperationsByAmountCommand { get; }
        public ICommand SortFinancialOperationsByDateCommand { get; }
        public ICommand LogOutCurrentUserCommand { get; }
        #endregion

        // Properties
        #region ...

        private Page? _currentSelectedPage;
        public Page? CurrentSelectedPage
        {
            get { return _currentSelectedPage; }
            set { SetProperty(ref _currentSelectedPage, value); }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set { SetProperty(ref _isConnected, value); }
        }

        private string? _topMessageType;
        public string? TopMessageType
        {
            get { return _topMessageType; }
            set { SetProperty(ref _topMessageType, value); }
        }

        private string? _enteredUserId;
        public string? EnteredUserId
        {
            get { return _enteredUserId; }
            set { SetProperty(ref _enteredUserId, value); }
        }

        private string? _enteredConfirmCode;
        public string? EnteredConfirmCode
        {
            get { return _enteredConfirmCode; }
            set { SetProperty(ref _enteredConfirmCode, value); }
        }

        private string? _loginEnterPageErrorMessage;
        public string? LoginEnterPageErrorMessage
        {
            get { return _loginEnterPageErrorMessage; }
            set { SetProperty(ref _loginEnterPageErrorMessage, value); }
        }

        private string? _loginConfirmPageErrorMessage;
        public string? LoginConfirmPageErrorMessage
        {
            get { return _loginConfirmPageErrorMessage; }
            set { SetProperty(ref _loginConfirmPageErrorMessage, value); }
        }

        private string? _unhandledErrorMessage;
        public string? UnhandledErrorMessage
        {
            get { return _unhandledErrorMessage; }
            set { SetProperty(ref _unhandledErrorMessage, value); }
        }

        private User? _currentLoggedUser;
        public User? CurrentLoggedUser
        {
            get { return _currentLoggedUser; }
            set { SetProperty(ref _currentLoggedUser, value); }
        }

        private SelectionModeItem? _categorySelectionMode;
        public SelectionModeItem? CategorySelectionMode
        {
            get { return _categorySelectionMode; }
            set { SetProperty(ref _categorySelectionMode, value); }
        }

        private SelectionModeItem? _financeSelectionMode;
        public SelectionModeItem? FinanceSelectionMode
        {
            get { return _financeSelectionMode; }
            set { SetProperty(ref _financeSelectionMode, value); }
        }

        private string? _newCategoryTitle;
        public string? NewCategoryTitle
        {
            get { return _newCategoryTitle; }
            set { SetProperty(ref _newCategoryTitle, value); }
        }

        private string? _newCategoryDescription;
        public string? NewCategoryDescription
        {
            get { return _newCategoryDescription; }
            set { SetProperty(ref _newCategoryDescription, value); }
        }

        private string? _newFinancialTitle;
        public string? NewFinancialTitle
        {
            get { return _newFinancialTitle; }
            set { SetProperty(ref _newFinancialTitle, value); }
        }

        private string? _newFinancialDescription;
        public string? NewFinancialDescription
        {
            get { return _newFinancialDescription; }
            set { SetProperty(ref _newFinancialDescription, value); }
        }

        private double _newFinancialAmount;
        public double NewFinancialAmount
        {
            get { return _newFinancialAmount; }
            set { SetProperty(ref _newFinancialAmount, value); }
        }

        private int _newFinancialSelectedTypeIndex;
        public int NewFinancialSelectedTypeIndex
        {
            get { return _newFinancialSelectedTypeIndex; }
            set { SetProperty(ref _newFinancialSelectedTypeIndex, value); }
        }

        private SelectionCurrencyItem? _newFinancialSelectedCurrency;
        public SelectionCurrencyItem? NewFinancialSelectedCurrency
        {
            get { return _newFinancialSelectedCurrency; }
            set { SetProperty(ref _newFinancialSelectedCurrency, value); }
        }

        private OperationCategory? _newFinancialSelectedCategory;
        public OperationCategory? NewFinancialSelectedCategory
        {
            get { return _newFinancialSelectedCategory; }
            set { SetProperty(ref _newFinancialSelectedCategory, value); }
        }

        private SelectionCurrencyItem? _sortingSelectedCurrencyItem;
        public SelectionCurrencyItem? SortingSelectedCurrencyItem
        {
            get { return _sortingSelectedCurrencyItem; }
            set { SetProperty(ref _sortingSelectedCurrencyItem, value); }
        }

        private int _sortingSelectedTypeIndex;
        public int SortingSelectedTypeIndex
        {
            get { return _sortingSelectedTypeIndex; }
            set { SetProperty(ref _sortingSelectedTypeIndex, value); }
        }

        private OperationCategory? _sortingSelectedOperationCategoryItem;
        public OperationCategory? SortingSelectedOperationCategoryItem
        {
            get { return _sortingSelectedOperationCategoryItem; }
            set { SetProperty(ref _sortingSelectedOperationCategoryItem, value); }
        }

        private SelectedPositionalSortingType? _currentFinancialPositionalSortingType;
        public SelectedPositionalSortingType? CurrentFinancialPositionalSortingType
        {
            get { return _currentFinancialPositionalSortingType; }
            set { SetProperty(ref _currentFinancialPositionalSortingType, value); }
        }

        public List<OperationCategory> SelectedOperationCategories = new List<OperationCategory>();
        public List<FinancialOperation> SelectedFinancialOperations = new List<FinancialOperation>();
        #endregion

        // Methods
        #region ...
        private ConfigurationSetting InitializeConfiguration()
        {
            ConfigurationSetting? config = _configurationService.GetConfigurationSettings();
            if (config == null)
            {
                config = new ConfigurationSetting
                {
                    LocalDataBaseConnectionString = "Data Source=LocalDataBase.db",
                    ServerConnectionString = "https://localhost:8443/mainHub",
                    IsLogged = false,
                    CurrentPageName = "Profile"
                };
                _configurationService.SaveConfigurationSettings(config);
                return config;
            }
            return config;
        }
        private void SetStartupDataProperties()
        {
            // collection initialization
            SortingComboboxCurrencies =
            [
                new SelectionCurrencyItem {Title = "All"}, ..CurrencyItems
            ];

            CurrentFinancialPositionalSortingType = new SelectedPositionalSortingType();

            SortFinancialOperationsByChangingPosition(SortingTool.SortTypes.Descending.ToString(), SortingTool.PositionalSortingType.ByDate);

            SortingSelectedOperationCategoryItem = SortingComboboxOperationCategories![0];
        }

        private void LoadDataFromLocalDB()
        {
            var operationCategories = _context.OperationCategories
                .Include(c => c.FinancialOperations).ToList();

            OperationCategories = new ObservableCollection<OperationCategory>(operationCategories);

            // collection initialization
            ComboboxOperationCategories =
            [
                new OperationCategory {Id = 0, Title = "Without category"}, .. OperationCategories
            ];

            SortingComboboxOperationCategories =
            [
                new OperationCategory {Id = -1, Title = "All"}, ..ComboboxOperationCategories
            ];

            var financialOperations = _context.FinancialOperations
                .Include(op => op.Category).ToList();

            FinancialOperations = new ObservableCollection<FinancialOperation>(financialOperations);

            ExcludingFinancialOperations = new ObservableCollection<FinancialOperation>(financialOperations);
        }

        private void SendLoginRequest()
        {
            if (long.TryParse(EnteredUserId, out long result))
            {
                SendConfirmationCode(result);
            }
            else
            {
                LoginEnterPageErrorMessage = "Incorrect format of the provided ID";
            }
        }

        private void BackToLoginEnterPage()
        {
            _navigationService.ChangePage<LoginEnterPage>();
        }

        // button on "Error page"
        private void BackToUsualPage()
        {
            if(_signalRService.GetConnectionState() == HubConnectionState.Disconnected)
            {
                if (_configurationSettings.IsLogged)
                {
                    LoadDataFromLocalDB();
                    SetStartupDataProperties();

                    _navigationService.ChangePage<MainPage>();
                }
                else
                {
                    _navigationService.ChangePage<LoginEnterPage>();
                }
            }
            else
            {
                SetCurrentUsualPage();
            }
        }

        private async void SendConfirmationCode(long userId)
        {
            await _signalRService.SendUserConfirmationCode(userId);
        }

        private async Task CheckReceivedConfirmationCode()
        {
            if(!string.IsNullOrWhiteSpace(EnteredConfirmCode) && EnteredConfirmCode == _receivedConfirmationCode)
            {
                CurrentLoggedUser = _newLoggedUser;

                await _signalRService.RequestDataCollections(CurrentLoggedUser!.UserId);
                _navigationService.ChangePage<MainPage>();

                _configurationSettings.IsLogged = true;
                _configurationService.SaveConfigurationSettings(_configurationSettings);

                EnteredUserId = string.Empty;
                EnteredConfirmCode = string.Empty;
            }
            else
            {
                LoginConfirmPageErrorMessage = "Invalid confirmation code.";
            }
        }

        private void CopyErrorMessageToClipboard()
        {
            DataPackage dataPackage = new();
            dataPackage.RequestedOperation = DataPackageOperation.Copy;
            dataPackage.SetText(UnhandledErrorMessage);
            Clipboard.SetContent(dataPackage);
        }

        private void CopyUserIdToClipboard()
        {
            if (CurrentLoggedUser != null)
            {
                DataPackage dataPackage = new();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(CurrentLoggedUser.UserId.ToString());
                Clipboard.SetContent(dataPackage);
            }
        }

        private void SetCurrentUsualPage()
        {
            if (_configurationSettings!.IsLogged)
                _navigationService.ChangePage<MainPage>();
            else
                _navigationService.ChangePage<LoginEnterPage>();
        }

        private async Task RefreshConnection()
        {
            if (_signalRService.GetConnectionState() == HubConnectionState.Disconnected)
            {
                _navigationService.ChangePage<LoadingPage>();
                await _signalRService.StartAsync();
            }
            else if(_signalRService.GetConnectionState() == HubConnectionState.Connected)
            {
                TopMessageType = "Connected";

                _navigationService.ChangePage<LoadingPage>();

                if (CurrentLoggedUser != null && _configurationSettings.IsLogged)
                    await _signalRService.RequestDataCollections(CurrentLoggedUser.UserId);
                else if (!_configurationSettings.IsLogged)
                    _navigationService.ChangePage<LoginEnterPage>();
            }
        }

        private async Task ShowNewAddingCategoryPanel(XamlRoot root)
        {
            NewCategoryTitle = $"New category ({OperationCategories!.Count + 1})";

            var result = await ShowContentDialog<AddNewCategoryPage>(root, "New category", 
                "Add", ContentDialogButton.Primary, new AddNewCategoryPage(), closeBtnText: "Cancel");

            if (result == ContentDialogResult.Primary)
                await AddNewCategory(root);
            else
            {
                NewCategoryTitle = string.Empty;
                NewCategoryDescription = string.Empty;
            }
        }

        private async Task AddNewCategory(XamlRoot root)
        {
            if (!string.IsNullOrWhiteSpace(NewCategoryTitle))
            {
                var category = new OperationCategory(NewCategoryTitle, NewCategoryDescription);

                OperationCategories!.Add(category);
                ComboboxOperationCategories!.Add(category);
                SortingComboboxOperationCategories!.Add(category);
                _dbAccess.Add(category);

                await _signalRService.AddNewItemToDataCollections(category, CurrentLoggedUser!.UserId);

                NewCategoryTitle = string.Empty;
                NewCategoryDescription = string.Empty;
            }
            else
            {
                await ShowNewAddingCategoryPanel(root);
            }
        }

        private async Task ShowUpdateDialogForSelectedCategory(XamlRoot root)
        {
            if(SelectedOperationCategories.Count == 1)
            {
                var selectedCategory = SelectedOperationCategories[0];

                NewCategoryTitle = selectedCategory.Title;
                NewCategoryDescription = selectedCategory.Description;

                var result = await ShowContentDialog<UpdateCategoryPage>(root, "Update category",
                    "Update", ContentDialogButton.Primary, new UpdateCategoryPage(), closeBtnText: "Cancel");

                if (result == ContentDialogResult.Primary)
                    await UpdateCategory(root, selectedCategory.Id);
                else
                {
                    NewCategoryTitle = string.Empty;
                    NewCategoryDescription = string.Empty;
                }
            }
        }

        private async Task UpdateCategory(XamlRoot root, int selectedCategoryId)
        {
            OperationCategory? category = OperationCategories!.FirstOrDefault(c => c.Id == selectedCategoryId);
            if (category != null && !string.IsNullOrWhiteSpace(NewCategoryTitle))
            {
                category.Title = NewCategoryTitle;
                category.Description = NewCategoryDescription;
                _dbAccess.Update(category);

                await _signalRService.UpdateItemInDataCollections(category, CurrentLoggedUser!.UserId);

                NewCategoryTitle = string.Empty;
                NewCategoryDescription = string.Empty;
            }
            else
            {
                await ShowUpdateDialogForSelectedCategory(root);
            }
        }

        private async Task RemoveSelectedCategories(XamlRoot root)
        {
            if(SelectedOperationCategories.Any())
            {
                var result = await ShowContentDialog<string>(root, "Remove item/items", "Yes, remove", 
                    ContentDialogButton.Primary, $"Are you sure you want to delete these ({SelectedOperationCategories.Count}) selected item/items?", 
                    closeBtnText: "Cancel");

                if(result == ContentDialogResult.Primary)
                {
                    await Task.Run(async () =>
                    {
                        var categoriesToRemove = OperationCategories!.Intersect(SelectedOperationCategories).ToList();

                        this.dispatcherQueue.TryEnqueue(() =>
                        {
                            foreach (var category in categoriesToRemove)
                            {
                                OperationCategories!.Remove(category);
                                ComboboxOperationCategories!.Remove(category);
                                SortingComboboxOperationCategories!.Remove(category);
                            }
                            _dbAccess.RemoveRange(categoriesToRemove);
                        });

                        await _signalRService.RemoveItemsFromDataCollections(categoriesToRemove, CurrentLoggedUser!.UserId);
                    });
                }
            }
        }

        private async Task SortCategoriesByTitle(string sortType)
        {
            SortingTool.SortTypes selectedType = sortType == "Descending"
                ? SortingTool.SortTypes.Descending
                : SortingTool.SortTypes.Ascending;

            await Task.Run(() =>
            {
                this.dispatcherQueue.TryEnqueue(() =>
                {
                    var sortedCategories = SortingTool.SortCategoriesByTitle(OperationCategories!, selectedType).ToList();

                    OperationCategories!.Clear();

                    foreach (var category in sortedCategories)
                    {
                        OperationCategories.Add(category);
                    }
                });
            });
        }

        private async Task<ContentDialogResult> ShowContentDialog<TContent>(XamlRoot root, string title, 
            string primaryBtnText, ContentDialogButton defaultBtn, TContent content, string? secondarybtnText = null, string? closeBtnText = null)
        {
            ContentDialog dialog = new ContentDialog();

            dialog.XamlRoot = root;
            dialog.Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style;
            dialog.Title = title;
            dialog.PrimaryButtonText = primaryBtnText;
            dialog.CloseButtonText = closeBtnText;
            dialog.SecondaryButtonText = secondarybtnText;
            dialog.DefaultButton = defaultBtn;
            dialog.Content = content;

            return await dialog.ShowAsync();
        }

        private async Task ShowAddingNewFinancialPanel(XamlRoot root)
        {
            NewFinancialTitle = $"New finance ({FinancialOperations!.Count + 1})";
            SetDefaultValuesForNewFinacialProperties();

            var result = await ShowContentDialog<AddNewFinancialPage>(root, "New finance",
                "Add", ContentDialogButton.Primary, new AddNewFinancialPage(), closeBtnText: "Cancel");

            if (result == ContentDialogResult.Primary)
                await AddNewFinance(root);
            else
            {
                NewFinancialTitle = string.Empty;
                NewFinancialDescription = string.Empty;
                SetDefaultValuesForNewFinacialProperties();
            }
        }

        private async Task AddNewFinance(XamlRoot root)
        {
            if (!string.IsNullOrWhiteSpace(NewFinancialTitle) && NewFinancialSelectedCurrency != null && NewFinancialAmount > 0)
            {
                var finance = new FinancialOperation(NewFinancialTitle, NewFinancialAmount, 
                    (NewFinancialSelectedTypeIndex == 0) ? OperationType.Income : OperationType.Expense, 
                    NewFinancialSelectedCurrency.Currency!.Value, DateTime.Now, NewFinancialDescription, 
                    (NewFinancialSelectedCategory!.Id == 0 ? null : NewFinancialSelectedCategory));

                FinancialOperations!.Add(finance);
                ExcludingFinancialOperations!.Insert(0, finance);
                _dbAccess.Add(finance);

                await _signalRService.AddNewItemToDataCollections(finance, CurrentLoggedUser!.UserId);

                NewFinancialTitle = string.Empty;
                NewFinancialDescription = string.Empty;
                SetDefaultValuesForNewFinacialProperties();
            }
            else
            {
                await ShowAddingNewFinancialPanel(root);
            }
        }

        private async Task RemoveSelectedFinancialOperations(XamlRoot root)
        {
            if (SelectedFinancialOperations.Any())
            {
                var result = await ShowContentDialog<string>(root, "Remove item/items", "Yes, remove",
                    ContentDialogButton.Primary, 
                    $"Are you sure you want to delete these ({SelectedFinancialOperations.Count}) selected item/items?", 
                    closeBtnText: "Cancel");

                if (result == ContentDialogResult.Primary)
                {
                    await Task.Run(async () =>
                    {
                        var financialsToRemove = FinancialOperations!.Intersect(SelectedFinancialOperations).ToList();
                        this.dispatcherQueue.TryEnqueue(() =>
                        {
                            foreach (var finance in financialsToRemove)
                            {
                                FinancialOperations!.Remove(finance);
                                ExcludingFinancialOperations!.Remove(finance);
                            }
                            _dbAccess.RemoveRange(financialsToRemove);
                        });

                        await _signalRService.RemoveItemsFromDataCollections(financialsToRemove, CurrentLoggedUser!.UserId);
                    });
                }
            }
        }

        private async Task ShowUpdateDialogForSelectedFinancial(XamlRoot root)
        {
            if (SelectedFinancialOperations.Count == 1)
            {
                var selectedFinancialOperation = SelectedFinancialOperations[0];

                NewFinancialTitle = selectedFinancialOperation.Title;
                NewFinancialDescription = selectedFinancialOperation.Description;
                NewFinancialAmount = selectedFinancialOperation.Amount;
                NewFinancialSelectedTypeIndex = selectedFinancialOperation.Type == OperationType.Income ? 0 : 1;
                NewFinancialSelectedCurrency = CurrencyItems.FirstOrDefault(cur => cur.Currency == selectedFinancialOperation.Currency);
                NewFinancialSelectedCategory = selectedFinancialOperation.Category;

                var result = await ShowContentDialog<UpdateFinancialPage>(root, "Update financial operation",
                    "Update", ContentDialogButton.Primary, new UpdateFinancialPage(), closeBtnText: "Cancel");

                if (result == ContentDialogResult.Primary)
                    await UpdateFinancialOperation(root, selectedFinancialOperation.Id);
                else
                {
                    NewFinancialTitle = string.Empty;
                    NewFinancialDescription = string.Empty;
                    SetDefaultValuesForNewFinacialProperties();
                }
            }
        }

        private async Task UpdateFinancialOperation(XamlRoot root, int selectedFinancialOperationId)
        {
            FinancialOperation? mainFinancialOperation = 
                FinancialOperations!.FirstOrDefault(op => op.Id == selectedFinancialOperationId);
            FinancialOperation? excludedFinancialOperation = 
                ExcludingFinancialOperations!.FirstOrDefault(op => op.Id == selectedFinancialOperationId);

            if (mainFinancialOperation != null && excludedFinancialOperation != null 
                && !string.IsNullOrWhiteSpace(NewFinancialTitle) && NewFinancialSelectedCurrency != null && NewFinancialAmount > 0)
            {
                mainFinancialOperation.Title = excludedFinancialOperation.Title = NewFinancialTitle;
                mainFinancialOperation.Description = excludedFinancialOperation.Description = NewFinancialDescription;
                mainFinancialOperation.Amount = excludedFinancialOperation.Amount = NewFinancialAmount;
                mainFinancialOperation.Type = excludedFinancialOperation.Type = NewFinancialSelectedTypeIndex == 0 
                    ? OperationType.Income : OperationType.Expense;
                mainFinancialOperation.Currency = excludedFinancialOperation.Currency = NewFinancialSelectedCurrency.Currency!.Value;
                mainFinancialOperation.Category = excludedFinancialOperation.Category = NewFinancialSelectedCategory;
                _dbAccess.Update(mainFinancialOperation);

                await _signalRService.UpdateItemInDataCollections(mainFinancialOperation, CurrentLoggedUser!.UserId);

                NewFinancialTitle = string.Empty;
                NewFinancialDescription = string.Empty;
                SetDefaultValuesForNewFinacialProperties();
            }
            else
            {
                await ShowUpdateDialogForSelectedFinancial(root);
            }
        }

        private void SetDefaultValuesForNewFinacialProperties()
        {
            NewFinancialAmount = 0;
            NewFinancialSelectedCurrency = CurrencyItems[0];
            NewFinancialSelectedTypeIndex = 0;
            NewFinancialSelectedCategory = ComboboxOperationCategories![0];
        }

        private async Task GetAndApllyFilteredSettings()
        {
            Currencies? currency = null;
            OperationType? operationType = null;
            OperationCategory? operationCategory = null;

            if (SortingSelectedCurrencyItem != null && SortingSelectedCurrencyItem.Currency != null)
            {
                currency = SortingSelectedCurrencyItem.Currency;
            }
            if(SortingSelectedTypeIndex != 0)
            {
                operationType = SortingSelectedTypeIndex == 1 ? OperationType.Income : OperationType.Expense;
            }
            if(SortingSelectedOperationCategoryItem != null)
            {
                if (SortingSelectedOperationCategoryItem.Id != 0)
                    operationCategory = SortingSelectedOperationCategoryItem;
            }
            if(FinancialOperations != null)
            {
                await SortFinancialOperationsByExcluding(currency, operationType, operationCategory);
            }
        }

        private async Task SortFinancialOperationsByExcluding(Currencies? currency = null, OperationType? type = null, 
            OperationCategory? operationCategory = null)
        {
            await Task.Run(() =>
            {
                var sortedFinancialOperations = FinancialOperations!.Where(op =>
                    (currency == null || op.Currency == currency) &&
                    (type == null || op.Type == type) &&
                    (operationCategory == null ? op.Category == null : 
                    (operationCategory.Id == -1 ? true : op.Category == operationCategory))).ToList();

                switch (CurrentFinancialPositionalSortingType!.PositionalSortingType)
                {
                    case SortingTool.PositionalSortingType.ByTitle:
                        sortedFinancialOperations = SortingTool.SortFinancialsByTitle
                        (sortedFinancialOperations!, CurrentFinancialPositionalSortingType!.SortingType).ToList();
                        break;

                    case SortingTool.PositionalSortingType.ByAmount:
                        sortedFinancialOperations = SortingTool.SortByAmount
                        (sortedFinancialOperations!, CurrentFinancialPositionalSortingType!.SortingType).ToList();
                        break;

                    case SortingTool.PositionalSortingType.ByDate:
                        sortedFinancialOperations = SortingTool.SortByCreatedDate
                        (sortedFinancialOperations!, CurrentFinancialPositionalSortingType!.SortingType).ToList();
                        break;
                }

                this.dispatcherQueue.TryEnqueue(() =>
                {
                    ExcludingFinancialOperations!.Clear();

                    foreach (var item in sortedFinancialOperations)
                    {
                        ExcludingFinancialOperations!.Add(item);
                    }
                });
            });
        }

        private async void SortFinancialOperationsByChangingPosition(string sortType, SortingTool.PositionalSortingType positionalSortType)
        {
            SortingTool.SortTypes selectedType = sortType == "Descending"
                ? SortingTool.SortTypes.Descending
                : SortingTool.SortTypes.Ascending;

            await Task.Run(() =>
            {
                this.dispatcherQueue.TryEnqueue(() =>
                {
                    CurrentFinancialPositionalSortingType!.SortingType = selectedType;
                    CurrentFinancialPositionalSortingType!.PositionalSortingType = positionalSortType;

                    List<FinancialOperation> sortedFinancialOperations = new List<FinancialOperation>();

                    switch (positionalSortType)
                    {
                        case SortingTool.PositionalSortingType.ByTitle:
                            sortedFinancialOperations = SortingTool.SortFinancialsByTitle(ExcludingFinancialOperations!, selectedType).ToList();
                            break;

                        case SortingTool.PositionalSortingType.ByAmount:
                            sortedFinancialOperations = SortingTool.SortByAmount(ExcludingFinancialOperations!, selectedType).ToList();
                            break;

                        case SortingTool.PositionalSortingType.ByDate:
                            sortedFinancialOperations = SortingTool.SortByCreatedDate(ExcludingFinancialOperations!, selectedType).ToList();
                            break;
                    }

                    if(sortedFinancialOperations.Any())
                    {
                        ExcludingFinancialOperations!.Clear();

                        foreach (var financialOperation in sortedFinancialOperations)
                        {
                            ExcludingFinancialOperations.Add(financialOperation);
                        }
                    }
                });
            });
        }

        private void LogOutCurrentUser()
        {
            _configurationSettings.IsLogged = false;
            _configurationService.SaveConfigurationSettings(_configurationSettings);

            if (CurrentLoggedUser != null)
                _dbAccess.Remove(CurrentLoggedUser);
        }

        private async void CloseConnectionWithServer()
        {
            await _signalRService.StopAsync();
        }
        #endregion
    }
}
