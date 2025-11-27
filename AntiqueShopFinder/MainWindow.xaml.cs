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
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _databaseService;
        private List<Shop> _allShops;
        private List<Category> _allCategories;
        private Shop _selectedShop;
        private bool _isDataLoaded = false;
        private bool _isAdminLoggedIn = false;
        private string _currentSortOrder = "desc"; // "asc", "desc"

        public MainWindow()
        {
            InitializeComponent();
            _databaseService = new DatabaseService();
            this.Loaded += MainWindow_Loaded;
            MainTabControl.SelectionChanged += MainTabControl_SelectionChanged;

            // Скрываем кнопку добавления магазина до входа
            AddShopButton.Visibility = Visibility.Collapsed;
        }

        // НОВЫЙ МЕТОД: Сброс полей отзыва
        private void ResetReviewFields()
        {
            ReviewUserName.Text = "";
            ReviewComment.Text = "";
            ReviewRating.SelectedIndex = 0; // Устанавливаем рейтинг на 5

            // Показываем placeholder тексты
            ReviewUserNamePlaceholder.Visibility = Visibility.Visible;
            ReviewCommentPlaceholder.Visibility = Visibility.Visible;
        }

        // Обработчики для placeholder текста
        private void ReviewUserName_GotFocus(object sender, RoutedEventArgs e)
        {
            ReviewUserNamePlaceholder.Visibility = Visibility.Collapsed;
        }

        private void ReviewUserName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ReviewUserName.Text))
            {
                ReviewUserNamePlaceholder.Visibility = Visibility.Visible;
            }
        }

        private void ReviewComment_GotFocus(object sender, RoutedEventArgs e)
        {
            ReviewCommentPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void ReviewComment_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ReviewComment.Text))
            {
                ReviewCommentPlaceholder.Visibility = Visibility.Visible;
            }
        }

        // МЕТОД ДЛЯ ПЕРЕКЛЮЧЕНИЯ СОРТИРОВКИ
        private void SortToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Переключаем между убыванием и возрастанием
            if (_currentSortOrder == "desc")
            {
                _currentSortOrder = "asc";
                SortToggleButton.Content = "↑ Рейтинг";
                SortToggleButton.ToolTip = "По возрастанию рейтинга";
            }
            else
            {
                _currentSortOrder = "desc";
                SortToggleButton.Content = "↓ Рейтинг";
                SortToggleButton.ToolTip = "По убыванию рейтинга";
            }

            ApplySorting();
        }

        private void ApplySorting()
        {
            if (!_isDataLoaded) return;

            var currentShops = ShopsItemsControl.ItemsSource as IEnumerable<Shop> ?? _allShops;
            if (currentShops == null) return;

            List<Shop> sortedShops;

            switch (_currentSortOrder)
            {
                case "asc":
                    sortedShops = currentShops.OrderBy(shop => shop.Rating).ToList();
                    break;
                case "desc":
                    sortedShops = currentShops.OrderByDescending(shop => shop.Rating).ToList();
                    break;
                default:
                    sortedShops = currentShops.ToList();
                    break;
            }

            ShopsItemsControl.ItemsSource = sortedShops;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isDataLoaded)
            {
                LoadData();
            }
        }

        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != MainTabControl) return;

            if (MainTabControl.SelectedItem == SearchTab)
            {
                // Показываем layout для поиска (два столбца)
                SearchGrid.Visibility = Visibility.Visible;
                AdminScrollViewer.Visibility = Visibility.Collapsed;
                LoadShopsForSearch();
            }
            else if (MainTabControl.SelectedItem == AdminTab)
            {
                // Показываем layout для админа (один столбец на всю ширину)
                SearchGrid.Visibility = Visibility.Collapsed;
                AdminScrollViewer.Visibility = Visibility.Visible;

                if (_isAdminLoggedIn)
                {
                    LoadShopsForAdmin();
                }
                else
                {
                    // Если не авторизован, очищаем список магазинов
                    AdminShopsItemsControl.ItemsSource = null;
                }
            }
        }

        private void LoadData()
        {
            try
            {
                _allShops = _databaseService.GetAllShops();
                _allCategories = _databaseService.GetAllCategories();

                // Заполняем комбобокс категорий
                CategoryComboBox.Items.Clear();
                CategoryComboBox.Items.Add(new ComboBoxItem
                {
                    Content = "Все категории",
                    Tag = "all",
                    IsSelected = true
                });

                foreach (var category in _allCategories)
                {
                    CategoryComboBox.Items.Add(new ComboBoxItem
                    {
                        Content = category.Name,
                        Tag = category.Name
                    });
                }

                _isDataLoaded = true;
                LoadShopsForSearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadShopsForSearch()
        {
            if (!_isDataLoaded) return;

            try
            {
                string searchTerm = SearchTextBox.Text ?? "";
                string shopType = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "all";

                var selectedCategories = new List<string>();
                if (CategoryComboBox.SelectedItem is ComboBoxItem categoryItem &&
                    categoryItem.Tag?.ToString() != "all")
                {
                    selectedCategories.Add(categoryItem.Tag.ToString());
                }

                var filteredShops = _databaseService.SearchShops(searchTerm, shopType, selectedCategories);

                // Обновляем рейтинг для каждого магазина
                foreach (var shop in filteredShops)
                {
                    shop.Rating = CalculateShopRating(shop.Id);
                }

                // Применяем сортировку если она активна
                if (_currentSortOrder != "none")
                {
                    ApplySortingToShops(ref filteredShops);
                }

                ShopsItemsControl.ItemsSource = filteredShops;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке магазинов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySortingToShops(ref List<Shop> shops)
        {
            switch (_currentSortOrder)
            {
                case "asc":
                    shops = shops.OrderBy(shop => shop.Rating).ToList();
                    break;
                case "desc":
                    shops = shops.OrderByDescending(shop => shop.Rating).ToList();
                    break;
            }
        }

        private void LoadShopsForAdmin()
        {
            if (!_isDataLoaded) return;

            try
            {
                _allShops = _databaseService.GetAllShops();

                // Обновляем рейтинг для каждого магазина
                foreach (var shop in _allShops)
                {
                    shop.Rating = CalculateShopRating(shop.Id);
                }

                AdminShopsItemsControl.ItemsSource = _allShops;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке магазинов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateShopRating(int shopId)
        {
            try
            {
                var reviews = _databaseService.GetReviewsByShopId(shopId);
                if (reviews == null || reviews.Count == 0)
                    return 0;

                decimal totalRating = 0;
                foreach (var review in reviews)
                {
                    totalRating += review.Rating;
                }

                return Math.Round(totalRating / reviews.Count, 1);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private void SearchShops()
        {
            if (!_isDataLoaded) return;
            LoadShopsForSearch();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchShops();
        }

        private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchShops();
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SearchShops();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            TypeComboBox.SelectedIndex = 0;
            CategoryComboBox.SelectedIndex = 0;
            DetailsPanel.Visibility = Visibility.Collapsed;
            _selectedShop = null;

            // Сбрасываем сортировку
            _currentSortOrder = "desc";
            SortToggleButton.Content = "↓ Рейтинг";
            SortToggleButton.ToolTip = "По убыванию рейтинга";
            LoadShopsForSearch();
        }

        private void ShopItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is Shop shop)
                {
                    ShowShopDetails(shop);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе магазина: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowShopDetails(Shop shop)
        {
            _selectedShop = shop;

            try
            {
                var reviews = _databaseService.GetReviewsByShopId(shop.Id);
                shop.Reviews = reviews;

                // Рассчитываем актуальный рейтинг
                shop.Rating = CalculateShopRating(shop.Id);

                DetailName.Text = shop.Name;
                DetailRating.Text = shop.Rating.ToString("0.0");
                DetailReviewsCount.Text = $"({reviews.Count} отзывов)";
                DetailAddress.Text = shop.Address;
                DetailDescription.Text = shop.Description ?? "Описание отсутствует";

                // Контактная информация
                SetContactVisibility(DetailPhone, shop.Phone, PhonePanel);
                SetContactVisibility(DetailEmail, shop.Email, EmailPanel);
                SetContactVisibility(DetailWebsite, shop.Website, WebsitePanel);

                if (!string.IsNullOrEmpty(shop.Phone))
                    DetailPhone.Text = shop.Phone;
                if (!string.IsNullOrEmpty(shop.Email))
                    DetailEmail.Text = shop.Email;
                if (!string.IsNullOrEmpty(shop.Website))
                    DetailWebsite.Text = shop.Website;

                // Категории - исправленное отображение
                if (shop.Categories != null && shop.Categories.Count > 0)
                {
                    DetailCategoriesItemsControl.ItemsSource = shop.Categories;
                    CategoriesPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    CategoriesPanel.Visibility = Visibility.Collapsed;
                }

                ReviewsItemsControl.ItemsSource = shop.Reviews;
                DetailsPanel.Visibility = Visibility.Visible;

                // СБРАСЫВАЕМ ПОЛЯ ОТЗЫВА ПРИ ПЕРЕХОДЕ НА НОВЫЙ МАГАЗИН
                ResetReviewFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке деталей магазина: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetContactVisibility(TextBlock textBlock, string value, StackPanel panel)
        {
            if (!string.IsNullOrEmpty(value))
            {
                textBlock.Text = value;
                panel.Visibility = Visibility.Visible;
            }
            else
            {
                panel.Visibility = Visibility.Collapsed;
            }
        }

        private void AddReview_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedShop == null)
            {
                MessageBox.Show("Выберите магазин для добавления отзыва", "Информация",
                              MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(ReviewUserName.Text))
            {
                MessageBox.Show("Введите ваше имя", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int rating = 5;
                if (ReviewRating.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                {
                    if (int.TryParse(selectedItem.Tag.ToString(), out int parsedRating))
                    {
                        rating = parsedRating;
                    }
                }

                var review = new Review
                {
                    ShopId = _selectedShop.Id,
                    UserName = ReviewUserName.Text.Trim(),
                    Rating = rating,
                    Comment = string.IsNullOrWhiteSpace(ReviewComment.Text) ? null : ReviewComment.Text.Trim(),
                    ReviewDate = DateTime.Now
                };

                if (_databaseService.AddReview(review))
                {
                    MessageBox.Show("Отзыв успешно добавлен!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем отзывы и рейтинг
                    var reviews = _databaseService.GetReviewsByShopId(_selectedShop.Id);
                    _selectedShop.Reviews = reviews;
                    _selectedShop.Rating = CalculateShopRating(_selectedShop.Id);

                    ReviewsItemsControl.ItemsSource = reviews;
                    DetailRating.Text = _selectedShop.Rating.ToString("0.0");

                    // ОЧИСТКА ФОРМЫ ОТЗЫВА
                    ResetReviewFields();

                    // Обновляем список магазинов с сохранением сортировки
                    LoadShopsForSearch();
                    if (_isAdminLoggedIn)
                    {
                        LoadShopsForAdmin();
                    }
                }
                else
                {
                    MessageBox.Show("Не удалось добавить отзыв", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении отзыва: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdminLoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = AdminLoginTextBox.Text.Trim();
            string password = AdminPasswordTextBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_databaseService.ValidateAdmin(username, password))
                {
                    _isAdminLoggedIn = true;
                    AdminLoginPanel.Visibility = Visibility.Collapsed;
                    AdminPanel.Visibility = Visibility.Visible;
                    AddShopButton.Visibility = Visibility.Visible;

                    // Очищаем поля ввода пароля
                    AdminPasswordTextBox.Password = "";

                    LoadShopsForAdmin();

                    MessageBox.Show("Успешный вход в панель администратора!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    AdminPasswordTextBox.Password = "";
                    AdminPasswordTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdminLogoutButton_Click(object sender, RoutedEventArgs e)
        {
            _isAdminLoggedIn = false;
            AdminLoginPanel.Visibility = Visibility.Visible;
            AdminPanel.Visibility = Visibility.Collapsed;
            AddShopButton.Visibility = Visibility.Collapsed;

            // Очищаем список магазинов в админке
            AdminShopsItemsControl.ItemsSource = null;

            // Очищаем поля ввода
            AdminLoginTextBox.Text = "";
            AdminPasswordTextBox.Password = "";

            MessageBox.Show("Вы вышли из панели администратора", "Информация",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddShopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminLoggedIn)
            {
                MessageBox.Show("Для добавления магазина необходимо войти в систему", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var addShopWindow = new AddShopWindow();
                addShopWindow.Owner = this;

                if (addShopWindow.ShowDialog() == true)
                {
                    // Мгновенно обновляем данные
                    LoadData();
                    LoadShopsForAdmin();

                    MessageBox.Show("Магазин успешно добавлен!", "Успех",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении магазина: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteShopButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isAdminLoggedIn)
            {
                MessageBox.Show("Для удаления магазина необходимо войти в систему", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (sender is Button button && button.Tag is int shopId)
                {
                    var shop = _allShops.FirstOrDefault(s => s.Id == shopId);
                    if (shop == null) return;

                    var result = MessageBox.Show($"Вы уверены, что хотите удалить магазин \"{shop.Name}\"?",
                                               "Подтверждение удаления",
                                               MessageBoxButton.YesNo,
                                               MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        if (_databaseService.DeleteShop(shopId))
                        {
                            // Мгновенно обновляем данные
                            LoadData();
                            LoadShopsForAdmin();

                            // Скрываем детали если удаленный магазин был выбран
                            if (_selectedShop != null && _selectedShop.Id == shopId)
                            {
                                DetailsPanel.Visibility = Visibility.Collapsed;
                                _selectedShop = null;
                            }

                            MessageBox.Show("Магазин успешно удален!", "Успех",
                                          MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить магазин", "Ошибка",
                                          MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении магазина: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}