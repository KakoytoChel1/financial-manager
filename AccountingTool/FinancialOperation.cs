using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AccountingTool
{
    public enum OperationType
    {
        Income,
        Expense
    }

    public enum Currencies
    {
        UAH,
        USD,
        EUR,
        GPB,
        JPY,
        CNY
    }

    public class FinancialOperation : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public FinancialOperation() { }

        public FinancialOperation(string title, double amount, OperationType type, Currencies currency, 
            DateTime createdDate, string? description = null, OperationCategory? operationCategory = null)
        {
            Title = title;
            Amount = amount;
            Type = type;
            Currency = currency;
            CreatedOperationDate = createdDate;
            Description = (!string.IsNullOrWhiteSpace(description)) ? description : "Description not specified.";
            Category = operationCategory;
        }

        public void UpdateData(string? title = null, string? description = null, 
            double? amount = null, OperationType? type = null, 
            Currencies? currency = null, DateTime? createdDate = null)
        {
            if (title != null)
                Title = title;

            if (description != null)
                Description = description;

            if (amount.HasValue)
                Amount = amount.Value;

            if (createdDate.HasValue)
                CreatedOperationDate = createdDate.Value;

            if (type.HasValue)
                Type = type.Value;

            if (currency.HasValue)
                Currency = currency.Value;
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

        private double _amount;
        public double Amount
        {
            get { return _amount; }
            set { _amount = value; OnPropertyChanged(); }
        }

        private OperationType _type;
        public OperationType Type
        {
            get { return _type; }
            set { _type = value; OnPropertyChanged(); }
        }

        private Currencies _currency;
        public Currencies Currency
        {
            get { return _currency; }
            set { _currency = value; OnPropertyChanged(); }
        }

        private DateTime _createdOperationDate;
        public DateTime CreatedOperationDate
        {
            get { return _createdOperationDate; }
            set { _createdOperationDate = value; OnPropertyChanged(); }
        }

        private int? _categoryId;
        public int? CategoryId
        {
            get { return _categoryId; }
            set { _categoryId = value; OnPropertyChanged(); }
        }

        [JsonIgnore]
        private OperationCategory? _category;

        [JsonIgnore]
        public virtual OperationCategory? Category
        {
            get { return _category; }
            set { _category = value; OnPropertyChanged(); }
        }
    }
}
