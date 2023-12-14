using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;

namespace WindowsFormsApp3
{

    public partial class Form1 : Form
    {
        private decimal totalCost = 0;
        List<string> productsList = new List<string>(); // глобально, чтобы был доступ к нему из разных методов
        private void ExportToCsv(DataGridView dataGridView, string filePath)
        {
            StringBuilder sb = new StringBuilder();

            var headers = dataGridView.Columns.Cast<DataGridViewColumn>();
            sb.AppendLine(string.Join(",", headers.Select(column => $"\"{column.HeaderText}\"")));

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                var cells = row.Cells.Cast<DataGridViewCell>();
                sb.AppendLine(string.Join(",", cells.Select(cell => $"\"{cell.Value}\"")));
            }

            File.WriteAllText(filePath, sb.ToString());
        }
        string FindCheapestStoreForSetProducts(Dictionary<string, int> products)
        {
            string cheapestStore = "";
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string subquery = "";
                foreach (var entry in products)
                {               
                    subquery = $"SELECT ShopID, Price FROM StoreProducts WHERE ProductID = '{entry.Key}' AND Quantity >= {entry.Value}";
                }
                string query = $"SELECT TOP 1 ShopID, SUM(Price) AS TotalPrice FROM ({subquery}) AS T GROUP BY ShopID ORDER BY TotalPrice";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            cheapestStore = reader["ShopID"].ToString();
                        }
                    }
                }
            }
            return cheapestStore;
        }
        public List<string> GetAffordableProductsInStore()
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            List<string> affordableProducts = new List<string>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT ShopID, ProductID, ('" + textBox6.Text + "'/ CAST(Price AS int)) AS Quantity FROM StoreProducts WHERE Price <= '" + textBox6.Text + "'";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataSet dataSet = new DataSet();
                connection.Open();
                adapter.Fill(dataSet, "StoreProducts");
                dataGridView1.DataSource = dataSet.Tables["StoreProducts"];
            }
            return affordableProducts;
        }

        // Метод для получения цены товара из базы данных
        private decimal GetProductPrice(string productName, string shopName)
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT Price FROM StoreProducts WHERE ProductID = @productName And ShopID = @shopName";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@productName", productName);
                    command.Parameters.AddWithValue("@shopName", shopName);
                    connection.Open();
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        return Convert.ToDecimal(result);
                    }
                    else
                    {
                        MessageBox.Show("Товар не найден в базе данных.");
                        return -1; // В случае отсутствия товара возвращаем -1
                    }
                }
            }
        }

        // Метод для получения доступного количества товара из базы данных
        private int GetAvailableQuantity(string productName, string shopName)
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT Quantity FROM StoreProducts WHERE ProductID = @productName And ShopID = @shopName";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@productName", productName);
                    command.Parameters.AddWithValue("@shopName", shopName);
                    connection.Open();
                    object result = command.ExecuteScalar();
                    if (result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        MessageBox.Show("Товар не найден в базе данных.");
                        return 0; // В случае отсутствия товара возвращаем 0
                    }
                }
            }
        }


        // Метод для вывода общей стоимости покупки
        private void ShowTotalCost(decimal totalCost)
        {
            MessageBox.Show("Общая стоимость: " + totalCost.ToString("C2"));
        }

        string FindCheapestStoreForProducts(Dictionary<string, int> products)
        {
            string cheapestStore = "";
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string subquery = "";
                foreach (var entry in products)
                {
                    if (subquery != "") subquery += " INTERSECT ";
                    subquery += $"SELECT ShopID, Price FROM StoreProducts WHERE ProductName = '{entry.Key}' AND Quantity >= {entry.Value}";
                }
                string query = $"SELECT ShopID, SUM(Price) AS TotalPrice FROM ({subquery}) AS T GROUP BY ShopID ORDER BY TotalPrice";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            cheapestStore = reader["ShopID"].ToString();
                        }
                    }
                }
            }
            return cheapestStore;
        }

        public Form1()
        {
            InitializeComponent();
        }

        void GetList()
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM Shops";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataSet dataSet = new DataSet();
                connection.Open();
                adapter.Fill(dataSet, "Shops");           
                dataGridView1.DataSource = dataSet.Tables["Shops"];
            }
        }

        void UpdateProductList()
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM StoreProducts";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataSet dataSet = new DataSet();
                connection.Open();
                adapter.Fill(dataSet, "StoreProducts");
                dataGridView1.DataSource = dataSet.Tables["StoreProducts"];
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetList();
        }

        private void button1_Click_1(object sender, EventArgs e) // Insert Button
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO Shops(ShopID, ShopName) VALUES (@ShopID, @ShopName)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopID", textBox1.Text);
                    command.Parameters.AddWithValue("@ShopName", textBox2.Text);
                    connection.Open();
                    command.ExecuteNonQuery();
                    GetList();
                }
            }
        }

        private void button2_Click_1(object sender, EventArgs e)//Update Button
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=False";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE Shops SET ShopName = @ShopName WHERE ShopID = @ShopID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopName", textBox2.Text);
                    command.Parameters.AddWithValue("@ShopID", textBox1.Text);
                    connection.Open();
                    command.ExecuteNonQuery();
                    GetList();
                }
            }
        }

        private void button3_Click_1(object sender, EventArgs e)//Delete Button
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM Shops WHERE ShopID = @ShopID and ShopName = @ShopName";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopID", textBox1.Text);
                    command.Parameters.AddWithValue("@ShopName", textBox2.Text);
                    connection.Open();
                    command.ExecuteNonQuery();
                    GetList();
                }
            }
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "INSERT INTO StoreProducts(ShopID, ProductID, Quantity, Price) VALUES (@ShopID, @ProductID, @Quantity, @Price)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopID", textBox3.Text);
                    command.Parameters.AddWithValue("@ProductID", textBox4.Text);
                    command.Parameters.AddWithValue("@Quantity", textBox5.Text);
                    command.Parameters.AddWithValue("@Price", textBox6.Text);
                    connection.Open();
                    command.ExecuteNonQuery();
                    UpdateProductList();
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "UPDATE StoreProducts SET Quantity  =  @Quantity, Price  = @Price WHERE ShopID = @ShopID AND ProductID  = @ProductID";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopID", textBox3.Text);
                    command.Parameters.AddWithValue("@ProductID", textBox4.Text);
                    command.Parameters.AddWithValue("@Quantity", textBox5.Text);
                    command.Parameters.AddWithValue("@Price", textBox6.Text);
                    connection.Open();
                    command.ExecuteNonQuery();
                    UpdateProductList();
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "DELETE FROM StoreProducts WHERE ShopID  = @ShopID AND ProductID  = @ProductID AND Quantity  =  @Quantity AND Price  = @Price";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ShopID", textBox3.Text);
                    command.Parameters.AddWithValue("@ProductID", textBox4.Text);
                    command.Parameters.AddWithValue("@Quantity", textBox5.Text);
                    command.Parameters.AddWithValue("@Price", textBox6.Text);
                    connection.Open();
                    command.ExecuteNonQuery();
                    UpdateProductList();
                }
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string connectionString = @"Data Source=XERO\SQLEXPRESS; Initial Catalog=Market Managment; Integrated Security=True";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = "SELECT TOP 1 ShopID FROM StoreProducts WHERE ProductID = '" + textBox4.Text + "' ORDER BY Price ASC";
                 SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                 DataSet dataSet = new DataSet();
                 connection.Open();
                 adapter.Fill(dataSet, "StoreProducts");
                 dataGridView1.DataSource = dataSet.Tables["StoreProducts"];
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            GetAffordableProductsInStore();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string productName = textBox4.Text; // Получаем имя товара из текстового поля
            string shopName = textBox3.Text; // Получаем имя товара из текстового поля
            int quantity = Convert.ToInt32(textBox5.Text);
            if (!int.TryParse(textBox5.Text, out quantity))
            {
                MessageBox.Show("Введите корректное количество товара.");
                return;
            }

            decimal price = GetProductPrice(productName, shopName); // Получаем цену товара из базы данных
            int availableQuantity = GetAvailableQuantity(productName, shopName); // Получаем доступное количество товара из базы данных

            if (availableQuantity >= quantity)
            {
                totalCost += price * quantity; // Обновляем общую стоимость покупки
                ShowTotalCost(totalCost); // Выводим общую стоимость покупки в окно
            }
            else
            {
                MessageBox.Show("Извините, товара недостаточно на складе.");
            }

        }


        private void button10_Click(object sender, EventArgs e)
        {
            totalCost = 0;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            string productInfo = $"{textBox4.Text}: {textBox5.Text}"; // предполагается, что в textBox4 находится название товара, а в textBox5 его количество
            productsList.Add(productInfo);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            Dictionary<string, int> productsDict = new Dictionary<string, int>();

            foreach (var productInfo in productsList)
            {
                string[] parts = productInfo.Split(':');
                string productName = parts[0].Trim();
                int quantity = int.Parse(parts[1].Trim());
                if (productsDict.ContainsKey(productName))
                {
                    productsDict[productName] += quantity;
                }
                else
                {
                    productsDict.Add(productName, quantity);
                }
            }

            string cheapestStore = FindCheapestStoreForSetProducts(productsDict);
            MessageBox.Show($"Лучшая цена в магазине: {cheapestStore}");
        }

        private void button13_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "CSV (*.csv)|*.csv",
                ValidateNames = true
            })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ExportToCsv(dataGridView1, sfd.FileName);
                }
            }
        }
    }
}
