using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace App_Farmacia
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            CargarSucursales();
        }
        private void CargarSucursales()
        {
            using (SqlConnection con = new SqlConnection(Datos.conexion.CadenaLogin))
            {
                try
                {
                    con.Open();
                    // Consulta a tu tabla de la base de datos Farmacia1
                    string query = "SELECT ID_Sucursal, Nombre FROM Sucursales";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cbSucursal.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar sucursales: " + ex.Message);
                }
            }
        }
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Validación básica de selección de sucursal
            if (cbSucursal.SelectedValue == null)
            {
                MessageBox.Show("Por favor, seleccione una sucursal.");
                return;
            }

            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_ValidarAcceso", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@usuario", txtUsuario.Text);
                    cmd.Parameters.AddWithValue("@password", txtPassword.Password);

                    // Cambiamos ExecuteScalar por ExecuteReader para traer múltiples columnas (ID y Rol)
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // LEEMOS LOS DATOS DEL USUARIO
                            Sesion.IdUsuario = reader.GetInt32(0); // El ID_Usuarios (Primera columna)
                            string rolObtenido = reader.GetString(1); // El Rol (Segunda columna)

                            // GUARDAMOS EL RESTO DE LA SESIÓN
                            Sesion.IdSucursal = (int)cbSucursal.SelectedValue;
                            Sesion.Rol = rolObtenido;
                            Sesion.NombreUsuario = txtUsuario.Text;

                            // Entramos al sistema
                            MainWindow principal = new MainWindow(rolObtenido);
                            principal.Show();
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Credenciales o Sucursal incorrectas.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error de conexión: " + ex.Message);
                }
            }
        }
    }
   
}
