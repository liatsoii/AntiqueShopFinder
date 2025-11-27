using MySql.Data.MySqlClient;
using AntiqueShopFinder.Models;
using System;
using System.Collections.Generic;
using System.Windows;

namespace AntiqueShopFinder.Services
{
    public class DatabaseService
    {
        private readonly string connectionString;

        public DatabaseService()
        {
            connectionString = "Server=127.0.0.1;Port=3306;Database=antique_shops;Uid=root;Pwd=root;";

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ ОШИБКА ПОДКЛЮЧЕНИЯ К БАЗЕ: {ex.Message}\n\n" +
                               "Проверьте:\n" +
                               "• Запущен ли MySQL\n" +
                               "• Правильный ли пароль\n" +
                               "• Существует ли база 'antique_shops'",
                               "Критическая ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // НОВЫЙ МЕТОД: Проверка существования магазина по имени
        public bool ShopExists(string shopName)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM shops WHERE name = @name";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@name", shopName));
                        var result = Convert.ToInt32(command.ExecuteScalar());
                        return result > 0;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при проверке существования магазина: {ex.Message}");
                    return false;
                }
            }
        }

        // НОВЫЙ МЕТОД: Правильный расчет рейтинга магазина
        public decimal CalculateShopRating(int shopId)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            COALESCE(AVG(rating), 0) as average_rating,
                            COUNT(*) as reviews_count
                        FROM reviews 
                        WHERE shop_id = @shopId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@shopId", shopId));

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                decimal averageRating = reader["average_rating"] == DBNull.Value ?
                                    0 : Convert.ToDecimal(reader["average_rating"]);
                                int reviewsCount = Convert.ToInt32(reader["reviews_count"]);

                                return Math.Round(averageRating, 1);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при расчете рейтинга для магазина {shopId}: {ex.Message}");
                }
            }

            return 0;
        }

        // ОБНОВЛЕННЫЙ МЕТОД: Получение всех магазинов с правильным рейтингом
        public List<Shop> GetAllShops()
        {
            var shops = new List<Shop>();

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT s.*, GROUP_CONCAT(c.name) as category_names
                                   FROM shops s
                                   LEFT JOIN shop_categories sc ON s.id = sc.shop_id
                                   LEFT JOIN categories c ON sc.category_id = c.id
                                   GROUP BY s.id";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var shop = new Shop
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Name = reader["name"].ToString(),
                                Address = reader["address"].ToString(),
                                Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
                                Email = reader["email"] == DBNull.Value ? null : reader["email"].ToString(),
                                Website = reader["website"] == DBNull.Value ? null : reader["website"].ToString(),
                                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                                ShopType = reader["shop_type"].ToString(),
                                CreatedAt = Convert.ToDateTime(reader["created_at"])
                            };

                            // Рейтинг теперь рассчитывается на основе отзывов
                            shop.Rating = CalculateShopRating(shop.Id);

                            // Координаты
                            if (reader["latitude"] != DBNull.Value)
                                shop.Latitude = Convert.ToDecimal(reader["latitude"]);
                            if (reader["longitude"] != DBNull.Value)
                                shop.Longitude = Convert.ToDecimal(reader["longitude"]);

                            // Категории
                            if (reader["category_names"] != DBNull.Value)
                            {
                                var categories = reader["category_names"].ToString().Split(',');
                                foreach (var category in categories)
                                {
                                    shop.Categories.Add(category);
                                }
                            }

                            shops.Add(shop);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке магазинов: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return shops;
        }

        public List<Shop> SearchShops(string searchTerm, string shopType, List<string> categories)
        {
            var shops = new List<Shop>();

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = @"
                        SELECT DISTINCT s.*, GROUP_CONCAT(c.name) as category_names
                        FROM shops s
                        LEFT JOIN shop_categories sc ON s.id = sc.shop_id
                        LEFT JOIN categories c ON sc.category_id = c.id
                        WHERE 1=1";

                    var parameters = new List<MySqlParameter>();

                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        query += " AND (s.name LIKE @searchTerm OR s.address LIKE @searchTerm OR s.description LIKE @searchTerm)";
                        parameters.Add(new MySqlParameter("@searchTerm", $"%{searchTerm}%"));
                    }

                    if (!string.IsNullOrEmpty(shopType) && shopType != "all")
                    {
                        query += " AND s.shop_type = @shopType";
                        parameters.Add(new MySqlParameter("@shopType", shopType));
                    }

                    if (categories != null && categories.Count > 0)
                    {
                        query += " AND c.name IN (";
                        for (int i = 0; i < categories.Count; i++)
                        {
                            query += $"@category{i}";
                            if (i < categories.Count - 1) query += ",";
                            parameters.Add(new MySqlParameter($"@category{i}", categories[i]));
                        }
                        query += ")";
                    }

                    query += " GROUP BY s.id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var shop = new Shop
                                {
                                    Id = Convert.ToInt32(reader["id"]),
                                    Name = reader["name"].ToString(),
                                    Address = reader["address"].ToString(),
                                    Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
                                    Email = reader["email"] == DBNull.Value ? null : reader["email"].ToString(),
                                    Website = reader["website"] == DBNull.Value ? null : reader["website"].ToString(),
                                    Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                                    ShopType = reader["shop_type"].ToString(),
                                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                                };

                                // Рассчитываем рейтинг на основе отзывов
                                shop.Rating = CalculateShopRating(shop.Id);

                                // Координаты
                                if (reader["latitude"] != DBNull.Value)
                                    shop.Latitude = Convert.ToDecimal(reader["latitude"]);
                                if (reader["longitude"] != DBNull.Value)
                                    shop.Longitude = Convert.ToDecimal(reader["longitude"]);

                                // Категории
                                if (reader["category_names"] != DBNull.Value)
                                {
                                    var categoryNames = reader["category_names"].ToString().Split(',');
                                    foreach (var categoryName in categoryNames)
                                    {
                                        shop.Categories.Add(categoryName);
                                    }
                                }

                                shops.Add(shop);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при поиске магазинов: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return shops;
        }

        public List<Category> GetAllCategories()
        {
            var categories = new List<Category>();

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT * FROM categories ORDER BY name";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                Name = reader["name"].ToString(),
                                Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке категорий: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return categories;
        }

        public List<Review> GetReviewsByShopId(int shopId)
        {
            var reviews = new List<Review>();

            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "SELECT * FROM reviews WHERE shop_id = @shopId ORDER BY review_date DESC";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@shopId", shopId));

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                reviews.Add(new Review
                                {
                                    Id = Convert.ToInt32(reader["id"]),
                                    ShopId = Convert.ToInt32(reader["shop_id"]),
                                    UserName = reader["user_name"].ToString(),
                                    Rating = Convert.ToInt32(reader["rating"]),
                                    Comment = reader["comment"] == DBNull.Value ? null : reader["comment"].ToString(),
                                    ReviewDate = Convert.ToDateTime(reader["review_date"])
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке отзывов: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return reviews;
        }

        public bool AddReview(Review review)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = @"INSERT INTO reviews (shop_id, user_name, rating, comment, review_date) 
                                   VALUES (@shopId, @userName, @rating, @comment, @reviewDate)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@shopId", review.ShopId));
                        command.Parameters.Add(new MySqlParameter("@userName", review.UserName));
                        command.Parameters.Add(new MySqlParameter("@rating", review.Rating));
                        command.Parameters.Add(new MySqlParameter("@reviewDate", DateTime.Now));

                        if (string.IsNullOrEmpty(review.Comment))
                        {
                            command.Parameters.Add(new MySqlParameter("@comment", DBNull.Value));
                        }
                        else
                        {
                            command.Parameters.Add(new MySqlParameter("@comment", review.Comment));
                        }

                        return command.ExecuteNonQuery() > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении отзыва: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        internal bool RegisterUser(User user)
        {
            throw new NotImplementedException();
        }

        // МЕТОДЫ ДЛЯ АДМИНИСТРАТОРА

        public bool ValidateAdmin(string username, string password)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var query = "SELECT COUNT(*) FROM admins WHERE username = @username AND password = @password";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@username", username));
                        command.Parameters.Add(new MySqlParameter("@password", password));

                        var result = Convert.ToInt32(command.ExecuteScalar());
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // Для демонстрационных целей используем хардкод, если база недоступна
                return username == "admin" && password == "admin";
            }
        }

        public bool AddShop(Shop shop)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Вставляем магазин
                    var query = @"INSERT INTO shops (name, address, phone, email, website, description, shop_type, latitude, longitude, rating, created_at) 
                         VALUES (@name, @address, @phone, @email, @website, @description, @shop_type, @latitude, @longitude, @rating, @created_at);
                         SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@name", shop.Name));
                        command.Parameters.Add(new MySqlParameter("@address", shop.Address));
                        command.Parameters.Add(new MySqlParameter("@phone", shop.Phone ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@email", shop.Email ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@website", shop.Website ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@description", shop.Description ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@shop_type", shop.ShopType));
                        command.Parameters.Add(new MySqlParameter("@latitude", shop.Latitude ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@longitude", shop.Longitude ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@rating", 0.0m)); // Новый магазин с рейтингом 0
                        command.Parameters.Add(new MySqlParameter("@created_at", DateTime.Now));

                        var shopId = Convert.ToInt32(command.ExecuteScalar());

                        // Добавляем категории
                        foreach (var category in shop.Categories)
                        {
                            AddCategoryToShop(shopId, category, connection);
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при добавлении магазина: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        private void AddCategoryToShop(int shopId, string categoryName, MySqlConnection connection)
        {
            try
            {
                // Сначала получаем ID категории по имени
                var getCategoryQuery = "SELECT id FROM categories WHERE name = @name";
                int categoryId;

                using (var command = new MySqlCommand(getCategoryQuery, connection))
                {
                    command.Parameters.Add(new MySqlParameter("@name", categoryName));
                    var result = command.ExecuteScalar();

                    if (result != null)
                    {
                        categoryId = Convert.ToInt32(result);

                        // Добавляем связь магазина с категорией
                        var insertQuery = "INSERT INTO shop_categories (shop_id, category_id) VALUES (@shop_id, @category_id)";
                        using (var insertCommand = new MySqlCommand(insertQuery, connection))
                        {
                            insertCommand.Parameters.Add(new MySqlParameter("@shop_id", shopId));
                            insertCommand.Parameters.Add(new MySqlParameter("@category_id", categoryId));
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем выполнение
                System.Diagnostics.Debug.WriteLine($"Ошибка при добавлении категории {categoryName}: {ex.Message}");
            }
        }

        public bool DeleteShop(int shopId)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Удаляем отзывы магазина (каскадно)
                    var deleteReviewsQuery = "DELETE FROM reviews WHERE shop_id = @id";
                    using (var reviewsCommand = new MySqlCommand(deleteReviewsQuery, connection))
                    {
                        reviewsCommand.Parameters.Add(new MySqlParameter("@id", shopId));
                        reviewsCommand.ExecuteNonQuery();
                    }

                    // Удаляем связи с категориями
                    var deleteCategoriesQuery = "DELETE FROM shop_categories WHERE shop_id = @id";
                    using (var categoriesCommand = new MySqlCommand(deleteCategoriesQuery, connection))
                    {
                        categoriesCommand.Parameters.Add(new MySqlParameter("@id", shopId));
                        categoriesCommand.ExecuteNonQuery();
                    }

                    // Удаляем магазин
                    var deleteShopQuery = "DELETE FROM shops WHERE id = @id";
                    using (var shopCommand = new MySqlCommand(deleteShopQuery, connection))
                    {
                        shopCommand.Parameters.Add(new MySqlParameter("@id", shopId));
                        var result = shopCommand.ExecuteNonQuery();
                        return result > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении магазина: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        public bool UpdateShop(Shop shop)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Обновляем данные магазина
                    var query = @"UPDATE shops 
                                 SET name = @name, address = @address, phone = @phone, 
                                     email = @email, website = @website, description = @description,
                                     shop_type = @shop_type, latitude = @latitude, longitude = @longitude
                                 WHERE id = @id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@id", shop.Id));
                        command.Parameters.Add(new MySqlParameter("@name", shop.Name));
                        command.Parameters.Add(new MySqlParameter("@address", shop.Address));
                        command.Parameters.Add(new MySqlParameter("@phone", shop.Phone ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@email", shop.Email ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@website", shop.Website ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@description", shop.Description ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@shop_type", shop.ShopType));
                        command.Parameters.Add(new MySqlParameter("@latitude", shop.Latitude ?? (object)DBNull.Value));
                        command.Parameters.Add(new MySqlParameter("@longitude", shop.Longitude ?? (object)DBNull.Value));

                        var result = command.ExecuteNonQuery();

                        if (result > 0)
                        {
                            // Удаляем старые категории и добавляем новые
                            DeleteShopCategories(shop.Id, connection);
                            foreach (var category in shop.Categories)
                            {
                                AddCategoryToShop(shop.Id, category, connection);
                            }
                        }

                        return result > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении магазина: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        private void DeleteShopCategories(int shopId, MySqlConnection connection)
        {
            try
            {
                var query = "DELETE FROM shop_categories WHERE shop_id = @shop_id";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.Add(new MySqlParameter("@shop_id", shopId));
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении категорий магазина: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public Shop GetShopById(int shopId)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    var query = @"
                        SELECT s.*, 
                               GROUP_CONCAT(DISTINCT c.name) as category_names
                        FROM shops s
                        LEFT JOIN shop_categories sc ON s.id = sc.shop_id
                        LEFT JOIN categories c ON sc.category_id = c.id
                        WHERE s.id = @id
                        GROUP BY s.id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@id", shopId));

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var shop = new Shop
                                {
                                    Id = Convert.ToInt32(reader["id"]),
                                    Name = reader["name"].ToString(),
                                    Address = reader["address"].ToString(),
                                    Phone = reader["phone"] == DBNull.Value ? null : reader["phone"].ToString(),
                                    Email = reader["email"] == DBNull.Value ? null : reader["email"].ToString(),
                                    Website = reader["website"] == DBNull.Value ? null : reader["website"].ToString(),
                                    Description = reader["description"] == DBNull.Value ? null : reader["description"].ToString(),
                                    ShopType = reader["shop_type"].ToString(),
                                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                                };

                                // Рассчитываем рейтинг на основе отзывов
                                shop.Rating = CalculateShopRating(shop.Id);

                                // Координаты
                                if (reader["latitude"] != DBNull.Value)
                                    shop.Latitude = Convert.ToDecimal(reader["latitude"]);
                                if (reader["longitude"] != DBNull.Value)
                                    shop.Longitude = Convert.ToDecimal(reader["longitude"]);

                                // Категории
                                if (reader["category_names"] != DBNull.Value)
                                {
                                    var categories = reader["category_names"].ToString().Split(',');
                                    foreach (var category in categories)
                                    {
                                        shop.Categories.Add(category);
                                    }
                                }

                                return shop;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке магазина: {ex.Message}", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            return null;
        }

        // НОВЫЙ МЕТОД: Обновление рейтинга магазина в базе данных
        public void UpdateShopRating(int shopId)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    // Рассчитываем средний рейтинг
                    var rating = CalculateShopRating(shopId);

                    // Обновляем рейтинг в таблице shops
                    var query = "UPDATE shops SET rating = @rating WHERE id = @shopId";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add(new MySqlParameter("@rating", rating));
                        command.Parameters.Add(new MySqlParameter("@shopId", shopId));
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении рейтинга магазина {shopId}: {ex.Message}");
                }
            }
        }
    }
}