using AntiqueShopFinder.Models;
using AntiqueShopFinder.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AntiqueShopFinder
{
    public partial class AddShopWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private List<Category> _allCategories;
        private List<Shop> _existingShops;

        public AddShopWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            LoadExistingShops();
            LoadCategories();
        }

        private void LoadExistingShops()
        {
            try
            {
                _existingShops = _databaseService.GetAllShops();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке существующих магазинов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                _existingShops = new List<Shop>();
            }
        }

        private void LoadCategories()
        {
            try
            {
                _allCategories = _databaseService.GetAllCategories();

                foreach (var category in _allCategories)
                {
                    var checkBox = new CheckBox
                    {
                        Content = category.Name,
                        Tag = category.Name,
                        FontSize = 13,
                        Margin = new Thickness(0, 0, 10, 5)
                    };

                    CategoriesPanel.Children.Add(checkBox);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик для запрета вставки из буфера обмена
        private void PhoneTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            // Полностью запрещаем вставку
            e.CancelCommand();
        }

        // Обработчик для ввода только разрешенных символов
        private void PhoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры, пробелы, скобки, плюс и дефис
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != ' ' && c != '(' && c != ')' && c != '+' && c != '-')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        // НОВЫЙ МЕТОД: Проверка уникальности названия магазина
        private bool IsShopNameUnique(string shopName)
        {
            return !_existingShops.Any(shop =>
                shop.Name.Equals(shopName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private void AddShopButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация обязательных полей
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    ShowError("Введите название магазина");
                    return;
                }

                if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
                {
                    ShowError("Введите адрес магазина");
                    return;
                }

                // ПРОВЕРКА УНИКАЛЬНОСТИ НАЗВАНИЯ МАГАЗИНА
                string shopName = NameTextBox.Text.Trim();
                if (!IsShopNameUnique(shopName))
                {
                    ShowError("Магазин с таким названием уже существует. Введите уникальное название.");
                    return;
                }

                // Создаем новый магазин
                var shop = new Shop
                {
                    Name = shopName,
                    Address = AddressTextBox.Text.Trim(),
                    ShopType = (ShopTypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "antique",
                    Description = string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim(),
                    Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim(),
                    Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim(),
                    Website = string.IsNullOrWhiteSpace(WebsiteTextBox.Text) ? null : WebsiteTextBox.Text.Trim(),
                    Rating = 0, // Новый магазин без рейтинга
                    CreatedAt = DateTime.Now
                };

                // Добавляем выбранные категории
                foreach (CheckBox checkBox in CategoriesPanel.Children)
                {
                    if (checkBox.IsChecked == true)
                    {
                        shop.Categories.Add(checkBox.Tag.ToString());
                    }
                }

                // Проверяем, что выбрана хотя бы одна категория
                if (shop.Categories.Count == 0)
                {
                    ShowError("Выберите хотя бы одну категорию");
                    return;
                }

                // Сохраняем магазин в базу
                if (_databaseService.AddShop(shop))
                {
                    MessageBox.Show("Магазин успешно добавлен!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowError("Не удалось добавить магазин. Проверьте введенные данные.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка при добавлении магазина: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ShowError(string message)
        {
            ErrorTextBlock.Text = message;
            ErrorTextBlock.Visibility = Visibility.Visible;
        }

        private void ClearError()
        {
            ErrorTextBlock.Visibility = Visibility.Collapsed;
        }

        // Обработчики для очистки ошибок при вводе
        private void NameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearError();
        }

        private void AddressTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearError();
        }
    }
}