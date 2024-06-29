using AccountingTool;

namespace Financial_Manager.Client.Model
{
    public class SelectionCurrencyItem
    {
        public string? Title { get; set; }

        public string? IconPath { get; set; }

        public Currencies? Currency { get; set; }
    }
}
