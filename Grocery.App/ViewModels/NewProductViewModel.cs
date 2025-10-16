using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Grocery.Core.Interfaces.Services;
using Grocery.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Grocery.App.ViewModels;

// Zorg dat je CommunityToolkit.Mvvm >= 8.2 gebruikt
public partial class NewProductViewModel : ObservableValidator
{
    private readonly IProductService _productService; // implementeer AddAsync / Add

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Naam is verplicht.")]
    private string? name;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, int.MaxValue, ErrorMessage = "Voorraad moet ≥ 0 zijn.")]
    private int stock;

    // MAUI DatePicker werkt met DateTime. We mappen naar DateOnly in Save.
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(NewProductViewModel), nameof(ValidateShelfLife))]
    private DateTime shelfLifeDate = DateTime.Today;

    // Text-binding voor prijs zodat komma/locale werkt.
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(NewProductViewModel), nameof(ValidatePriceText))]
    private string priceText = "0,00";

    [ObservableProperty]
    private string validationSummary = string.Empty;

    public NewProductViewModel(IProductService productService)
    {
        _productService = productService;
    }

    public static ValidationResult? ValidateShelfLife(DateTime value, ValidationContext _)
        => value == default ? new ValidationResult("THT is verplicht.") : ValidationResult.Success;

    public static ValidationResult? ValidatePriceText(string? value, ValidationContext _)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new ValidationResult("Prijs is verplicht.");

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var d))
            return new ValidationResult("Ongeldige prijs.");

        if (d < 0 || d > 999.99m)
            return new ValidationResult("Prijs moet tussen 0 en 999,99 liggen.");

        // maximaal 2 decimalen
        var decimals = BitConverter.GetBytes(decimal.GetBits(d)[3])[2];
        return decimals <= 2 ? ValidationResult.Success : new ValidationResult("Max. 2 decimalen.");
    }

    [RelayCommand]
    private void Save()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            ValidationSummary = string.Join(Environment.NewLine,
                GetErrors(nameof(Name)).Cast<ValidationResult>().Select(e => "• " + e.ErrorMessage)
                .Concat(GetErrors(nameof(Stock)).Cast<ValidationResult>().Select(e => "• " + e.ErrorMessage))
                .Concat(GetErrors(nameof(ShelfLifeDate)).Cast<ValidationResult>().Select(e => "• " + e.ErrorMessage))
                .Concat(GetErrors(nameof(PriceText)).Cast<ValidationResult>().Select(e => "• " + e.ErrorMessage)));
            return;
        }

        // Convert naar domeinmodel
        decimal price = decimal.Parse(PriceText, CultureInfo.CurrentCulture);
        var product = new Product(
            id: 0, // nieuw item
            name: Name!.Trim(),
            stock: Stock,
            shelfLife: DateOnly.FromDateTime(ShelfLifeDate.Date),
            price: price
        );

        _productService.Add(product); // implementeer in jouw service/repo
        Shell.Current.GoToAsync("..");     // terug naar lijst
    }

    [RelayCommand]
    private void Cancel() => Shell.Current.GoToAsync("..");
}
