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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace App_Farmacia
{
    public partial class PaginaStock : Page
    {
        private int idSeleccionado = 0;

        public PaginaStock()
        {
            InitializeComponent();
            CargarTabla();
            CargarCategorias();
        }

        private void CargarCategorias()
        {
            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("SELECT ID_Categoria, Nombre FROM Categoria", con);
                    cmd.CommandTimeout = 120;
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cbCategoria.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar categorías: " + ex.Message);
                }
            }
        }

        private void CargarTabla(string nombre = "")
        {
            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("sp_BuscarProducto", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;
                    cmd.Parameters.AddWithValue("@nombre", nombre);
                    cmd.Parameters.AddWithValue("@idSucursal", Sesion.IdSucursal);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgStock.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar tabla: " + ex.Message);
                }
            }
        }

        private void txtBusqueda_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            CargarTabla(txtBusqueda.Text);
        }

        private void BtnCrear_Click(object sender, RoutedEventArgs e)
        {
            if (cbCategoria.SelectedValue == null)
            {
                MessageBox.Show("Debe seleccionar una categoría para el producto.");
                return;
            }

            if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad < 0)
            {
                MessageBox.Show("Ingrese una cantidad válida (mayor o igual a 0).");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
                {
                    con.Open();

                    // Verificar si ya existe un producto con mismo nombre Y presentación
                    SqlCommand cmdVerificar = new SqlCommand(
                        "SELECT COUNT(*) FROM Producto WHERE Nombre = @Nombre AND Presentacion = @Presentacion", con);
                    cmdVerificar.Parameters.AddWithValue("@Nombre", txtNombre.Text);
                    cmdVerificar.Parameters.AddWithValue("@Presentacion", txtPresentacion.Text);
                    cmdVerificar.CommandTimeout = 120;

                    int existe = (int)cmdVerificar.ExecuteScalar();

                    if (existe > 0)
                    {
                        MessageBox.Show("Ya existe un producto con ese nombre y presentación. Use 'Añadir a existente' para reabastecer stock.");
                        return;
                    }

                    SqlCommand cmd = new SqlCommand("sp_CrearProducto", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;

                    cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@Presentacion", txtPresentacion.Text);
                    cmd.Parameters.AddWithValue("@Precio", decimal.Parse(txtPrecio.Text));
                    cmd.Parameters.AddWithValue("@Stock", cantidad);
                    cmd.Parameters.AddWithValue("@idSucursal", Sesion.IdSucursal);
                    cmd.Parameters.AddWithValue("@idCategoria", cbCategoria.SelectedValue);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Producto creado y asignado a sucursal.");

                    _ = Datos.Auditoria.Instancia.RegistrarEdicionAsync(
                        entidad: "Producto",
                        entidadId: 0,
                        campo: "creacion",
                        valorAnterior: null,
                        valorNuevo: txtNombre.Text + " - " + txtPresentacion.Text
                    );

                    CargarTabla();
                    LimpiarCampos();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en SQL: " + ex.Message);
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (idSeleccionado == 0) { MessageBox.Show("Seleccione un producto de la tabla"); return; }

            if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad < 0)
            {
                MessageBox.Show("Ingrese una cantidad válida (mayor o igual a 0).");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_ActualizarStock", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 120;

                    cmd.Parameters.AddWithValue("@idProducto", idSeleccionado);
                    cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text);
                    cmd.Parameters.AddWithValue("@NuevaCantidad", cantidad);
                    cmd.Parameters.AddWithValue("@idSucursal", Sesion.IdSucursal);

                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Stock actualizado exitosamente.");

                    _ = Datos.Auditoria.Instancia.RegistrarEdicionAsync(
                        entidad: "Producto",
                        entidadId: idSeleccionado,
                        campo: "stock",
                        valorAnterior: null,
                        valorNuevo: cantidad
                    );

                    CargarTabla();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar: " + ex.Message);
            }
        }

        private void dgStock_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgStock.SelectedItem is DataRowView fila)
            {
                idSeleccionado = (int)fila["ID_Producto"];
                txtNombre.Text = fila["Nombre"].ToString();
                txtPrecio.Text = fila["Precio"].ToString();
                txtPresentacion.Text = fila["Presentacion"].ToString();
            }
        }

        private void LimpiarCampos()
        {
            idSeleccionado = 0;
            txtNombre.Clear();
            txtPresentacion.Clear();
            txtPrecio.Text = "0.00";
            txtCantidad.Text = "0";
            cbCategoria.SelectedIndex = -1;
        }
    }
}