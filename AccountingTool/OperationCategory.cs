using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AccountingTool
{
    public class OperationCategory : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public OperationCategory() { }

        public OperationCategory(string title, string? description = null)
        {
            Title = title;
            Description = (!string.IsNullOrWhiteSpace(description)) ? description : "Description not specified.";
        }

        public void UpdateData(string? title = null,  string? description = null)
        {
            if (title != null)
                Title = title;

            if(description != null)
                Description = description;
        }

        public int Id { get; set; }

        private string _title = null!;
        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged(); }
        }

        private string? _description;
        public string? Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        public virtual ObservableCollection<FinancialOperation> FinancialOperations { get; set; } = new ObservableCollection<FinancialOperation>();
    }
}
