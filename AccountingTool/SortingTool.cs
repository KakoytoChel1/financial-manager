namespace AccountingTool
{
    public static class SortingTool
    {
        public enum SortTypes
        {
            Ascending,
            Descending
        }

        public enum PositionalSortingType
        {
            ByTitle,
            ByAmount,
            ByDate
        }

        public static IEnumerable<FinancialOperation> SortFinancialsByTitle(IEnumerable<FinancialOperation> financialOperations, SortTypes sortType)
        {
            if (sortType == SortTypes.Ascending)
                return financialOperations.OrderBy(op => op.Title);

            else
                return financialOperations.OrderByDescending(op => op.Title);
        }

        public static IEnumerable<OperationCategory> SortCategoriesByTitle(IEnumerable<OperationCategory> operationCategories, SortTypes sortType)
        {
            if (sortType == SortTypes.Ascending)
                return operationCategories.OrderBy(op => op.Title);

            else
                return operationCategories.OrderByDescending(op => op.Title);
        }

        public static IEnumerable<FinancialOperation> SortByAmount(IEnumerable<FinancialOperation> financialOperations, SortTypes sortType)
        {
            if (sortType == SortTypes.Ascending)
                return financialOperations.OrderBy(op => op.Amount);

            else
                return financialOperations.OrderByDescending(op => op.Amount);
        }

        public static IEnumerable<FinancialOperation> SortByCreatedDate(IEnumerable<FinancialOperation> financialOperations, SortTypes sortType)
        {
            if (sortType == SortTypes.Ascending)
                return financialOperations.OrderBy(op => op.CreatedOperationDate);

            else
                return financialOperations.OrderByDescending(op => op.CreatedOperationDate);
        }

        public static IEnumerable<FinancialOperation> SortByIncome(IEnumerable<FinancialOperation> financialOperations)
        {
            return financialOperations.Where(op => op.Type == OperationType.Income);
        }

        public static IEnumerable<FinancialOperation> SortByExpense(IEnumerable<FinancialOperation> financialOperations)
        {
            return financialOperations.Where(op => op.Type == OperationType.Expense);
        }

        public static IEnumerable<FinancialOperation> SortByCurrency(IEnumerable<FinancialOperation> financialOperations, Currencies currency)
        {
            return financialOperations.Where(op => op.Currency == currency);
        }

        public static IEnumerable<FinancialOperation> SortByCategory(IEnumerable<FinancialOperation> financialOperations,
            int categoryId)
        {
            return financialOperations.Where(op => op.CategoryId == categoryId);
        }
    }
}
