using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace AccountingTool
{
    public class User : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public enum UserRoles
        {
            Director,
            Accounter
        }

        public enum UserStates
        {
            Logged,
            NoAccess
        }

        public enum SortingTypes
        {
            ByTitleAsc,
            ByTitleDesc,
            ByAmountAsc,
            ByAmountDesc,
            ByDateAsc,
            ByDateDesc,
            ByCategory,
            ByTypeIncome,
            ByTypeExpense
        }

        public User() { }

        public User(long userId, string? firstName, string? lastName, string? telegramUserName, 
            UserRoles userRole, UserStates userStatus, SortingTypes sortingType)
        {
            UserId = userId;
            FirstName = firstName;
            LastName = lastName;
            TelegramUserName = telegramUserName;
            UserRole = userRole;
            UserStatus = userStatus;

            //SelectedFinancialViewerIndex = 0;
            SelectedSortingType = sortingType;
        }

        public void UpdateData(long? userId = null, string? firstName = null, string? lastName = null, 
            string? telegramUserName = null, UserRoles? userRole = null, UserStates? userStatus = null)
        {
            if(userId != null)
                UserId = userId.Value;

            if (firstName != null) 
                FirstName = firstName;

            if(lastName != null)
                LastName = lastName;

            if(telegramUserName != null)
                TelegramUserName = telegramUserName;

            if (userRole != null)
                UserRole = userRole.Value;

            if(userStatus != null)
                UserStatus = userStatus.Value;
        }

        [Key]
        public long UserId { get; set; }

        [Required]
        private UserRoles _userRole;
        public UserRoles UserRole
        {
            get { return _userRole; }
            set { _userRole = value; OnPropertyChanged(); }
        }

        [Required]
        private UserStates _userStatus;
        public UserStates UserStatus
        {
            get { return _userStatus; }
            set { _userStatus = value; OnPropertyChanged(); }
        }

        private string? _firstName;
        public string? FirstName
        {
            get { return _firstName; }
            set { _firstName = value; OnPropertyChanged(); }
        }

        private string? _lastName;
        public string? LastName
        {
            get { return _lastName; }
            set { _lastName = value; OnPropertyChanged(); }
        }

        private string? _telegramUserName;
        public string? TelegramUserName
        {
            get { return _telegramUserName; }
            set { _telegramUserName = value; OnPropertyChanged(); }
        }

        public int SelectedFinancialViewerIndex { get; set; }

        public SortingTypes SelectedSortingType { get; set; }
    }
}
