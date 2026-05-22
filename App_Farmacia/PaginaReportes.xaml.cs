using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace App_Farmacia
{
    public partial class PaginaReportes : Page
    {
        public PaginaReportes()
        {
            InitializeComponent();
            CargarTodosLosReportes();
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarTodosLosReportes();
        }

        private void CargarTodosLosReportes()
        {
            CargarDataGrid("SELECT * FROM vw_TopProductos", dgTopProductos);
            CargarDataGrid("SELECT * FROM vw_TotalSucursales", dgTotalSucursales);
            CargarDataGrid("SELECT * FROM vw_VentasDelDia", dgVentasDia);
        }

        private void CargarDataGrid(string query, DataGrid dg)
        {
            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dg.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar {dg.Name}: " + ex.Message);
                }
            }
        }
    }
}
