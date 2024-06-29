using AccountingTool;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Financial_Manager.Client.Model
{
    public class SelectedPositionalSortingType : ObservableObject
    {
        private SortingTool.SortTypes _sortingType;
        public SortingTool.SortTypes SortingType
        {
            get { return _sortingType; }
            set { SetProperty(ref _sortingType, value); }
        }

        private SortingTool.PositionalSortingType _positionalSortingType;
        public SortingTool.PositionalSortingType PositionalSortingType
        {
            get { return _positionalSortingType; }
            set { SetProperty(ref _positionalSortingType, value); }
        }
    }
}
