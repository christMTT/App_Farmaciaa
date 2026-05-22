using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace App_Farmacia
{
    public partial class PaginaClientes : Page
    {
        public PaginaClientes()
        {
            InitializeComponent();
            CargarClientes();
        }

        private void CargarClientes(string filtro = "")
        {
            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    string query = "SELECT * FROM vw_verClientes";

                    if (!string.IsNullOrEmpty(filtro))
                    {
                        query += " WHERE Nombre LIKE @busqueda OR NroDocumento LIKE @busqueda";
                    }

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    if (!string.IsNullOrEmpty(filtro))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@busqueda", "%" + filtro + "%");
                    }

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgClientes.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar clientes: " + ex.Message);
                }
            }
        }

        // Este es el evento que llama al TextBox
        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            // Si txtBuscarCliente sigue en rojo, dale "Guardar todo" (Ctrl+Shift+S)
            // para que Visual Studio refresque el archivo autogenerado.
            dgHistorial.ItemsSource = null;
            CargarClientes(txtBuscarCliente.Text);
        }

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtDocumento.Text))
            {
                MessageBox.Show("Faltan datos obligatorios.");
                return;
            }

            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_crearNuevoCliente", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@Apellido", txtApellido.Text);
                    cmd.Parameters.AddWithValue("@NroDocumento", txtDocumento.Text);
                    cmd.Parameters.AddWithValue("@Correo", txtCorreo.Text);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Cliente registrado.");

                    CargarClientes();
                    txtNombre.Clear(); txtApellido.Clear(); txtDocumento.Clear(); txtCorreo.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Usamos ?. para evitar errores si la selección es nula al momento de buscar
            if (dgClientes.SelectedItem is DataRowView filaSeleccionada)
            {
                // El nombre entre comillas debe ser EXACTO al de la vista vw_verClientes
                string documento = filaSeleccionada["NroDocumento"].ToString();
                CargarHistorialPorCliente(documento);
            }
        }

        private void CargarHistorialPorCliente(string dni)
        {
            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    // Filtramos la vista 'HistorialDeCompras' por 'ClienteDocumento'
                    string sql = "SELECT Fecha, Total, Sucursal FROM vw_HistorialDeCompras WHERE ClienteDocumento = @dni";

                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@dni", dni);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    dgHistorial.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar historial: " + ex.Message);
                }
            }
        }
    }
}
