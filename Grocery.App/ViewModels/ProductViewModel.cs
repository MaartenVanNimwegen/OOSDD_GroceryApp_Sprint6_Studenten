using CommunityToolkit.Mvvm.Input;
using Grocery.App.Views;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.Collections.ObjectModel;

namespace Grocery.App.ViewModels
{
    public partial class ProductViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        public ObservableCollection<Product> Products { get; set; } = new();

        public ProductViewModel(IProductService productService)
        {
            _productService = productService;
        }

        [RelayCommand]
        public void Load()
        {
            var items = _productService.GetAll();
            Products.Clear();
            foreach (var p in items) Products.Add(p);
        }

        [RelayCommand]
        private Task GoToNewProductAsync() => Shell.Current.GoToAsync(nameof(NewProductView));

    }
}
