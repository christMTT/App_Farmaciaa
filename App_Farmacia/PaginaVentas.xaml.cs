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
    // Clase para representar los productos en el carrito
    public class ElementoCarrito
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Cant { get; set; }
        public decimal Subtotal => Precio * Cant;
    }

    public partial class PaginaVentas : Page
    {
        private List<ElementoCarrito> carrito = new List<ElementoCarrito>();
        private decimal total = 0;
        private decimal descuento = 0;         // 👈 NUEVO
        private decimal totalConDescuento = 0; // 👈 NUEVO
        private int idClienteSeleccionado = 11;

        public PaginaVentas()
        {
            InitializeComponent();
            dgCarrito.ItemsSource = carrito;
        }

        // ================================
        // LÓGICA DE CLIENTE
        // ================================

        private void btnValidarCliente_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDniCliente.Text))
            {
                MessageBox.Show("Ingrese un DNI para validar.");
                return;
            }

            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    string sql = "SELECT ID_Cliente, Primer_Nombre + ' ' + Primer_Apellido FROM Cliente WHERE NroDocumento = @dni";
                    SqlCommand cmd = new SqlCommand(sql, con);
                    cmd.Parameters.AddWithValue("@dni", txtDniCliente.Text);

                    con.Open();
                    SqlDataReader r = cmd.ExecuteReader();

                    if (r.Read())
                    {
                        idClienteSeleccionado = r.GetInt32(0);
                        lblNombreCliente.Text = "Cliente: " + r.GetString(1);
                        lblNombreCliente.Foreground = Brushes.Blue;
                    }
                    else
                    {
                        MessageBox.Show("Cliente no encontrado. Se usará 'Público General'.");
                        idClienteSeleccionado = 1;
                        lblNombreCliente.Text = "Cliente: Público General";
                        lblNombreCliente.Foreground = Brushes.Gray;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al buscar cliente: " + ex.Message);
                }
            }
        }

        // ================================
        // LÓGICA DE PRODUCTOS
        // ================================

        private void txtBusqueda_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    SqlCommand cmd = new SqlCommand("sp_BuscarProducto", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nombre", txtBusqueda.Text);
                    cmd.Parameters.AddWithValue("@idSucursal", Sesion.IdSucursal);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgBusqueda.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al buscar producto: " + ex.Message);
                }
            }
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (dgBusqueda.SelectedItem is DataRowView fila)
            {
                if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
                {
                    MessageBox.Show("Ingrese una cantidad válida.");
                    return;
                }

                var item = new ElementoCarrito
                {
                    Id = (int)fila["ID_Producto"],
                    Nombre = fila["Nombre"].ToString(),
                    Precio = (decimal)fila["Precio"],
                    Cant = cantidad
                };

                carrito.Add(item);
                total += item.Subtotal;
                ActualizarInterfaz();
            }
            else
            {
                MessageBox.Show("Seleccione un producto de la lista.");
            }
        }

        // ================================
        // LÓGICA DE DESCUENTOS
        // ================================

        private void AplicarDescuento()
        {
            // Si el carrito está vacío no hay nada que descontar
            if (total == 0)
            {
                descuento = 0;
                totalConDescuento = 0;
                lblDescuento.Visibility = Visibility.Collapsed;
                lblTotal.Text = "TOTAL: $0.00";
                return;
            }

            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                try
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("sp_ObtenerDescuento", con);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Total", total);

                    SqlDataReader r = cmd.ExecuteReader();

                    if (r.Read())
                    {
                        // Hay promoción activa
                        descuento = r.GetDecimal(r.GetOrdinal("MontoDescuento"));
                        totalConDescuento = total - descuento;

                        lblDescuento.Text = $"Descuento ({r["Nombre"]}): -{descuento:C}";
                        lblDescuento.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        // Sin promoción activa
                        descuento = 0;
                        totalConDescuento = total;
                        lblDescuento.Visibility = Visibility.Collapsed;
                    }

                    lblSubtotal.Text = $"Subtotal: {total:C}";
                    lblTotal.Text = $"TOTAL: {totalConDescuento:C}";
                }
                catch (Exception ex)
                {
                    // Si falla el descuento, seguimos con el total normal
                    descuento = 0;
                    totalConDescuento = total;
                    lblSubtotal.Text = $"Subtotal: {total:C}";
                    lblTotal.Text = $"TOTAL: {total:C}";
                    lblDescuento.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Error al obtener descuento: " + ex.Message);
                }
            }
        }

        // ================================
        // ACTUALIZAR INTERFAZ
        // ================================

        private void ActualizarInterfaz()
        {
            dgCarrito.Items.Refresh();
            lblSubtotal.Text = $"Subtotal: {total:C}";
            AplicarDescuento(); // Recalcula el descuento cada vez que cambia el carrito
        }

        // ================================
        // FINALIZAR VENTA
        // ================================

        private void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0)
            {
                MessageBox.Show("El carrito está vacío.");
                return;
            }

            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                con.Open();
                SqlTransaction tra = con.BeginTransaction();

                try
                {
                    // 1. Insertar Factura
                    SqlCommand cmdF = new SqlCommand("sp_InsertarFactura", con, tra);
                    cmdF.CommandType = CommandType.StoredProcedure;

                    cmdF.Parameters.AddWithValue("@idCliente", idClienteSeleccionado);
                    cmdF.Parameters.AddWithValue("@idSucursal", Sesion.IdSucursal);
                    cmdF.Parameters.AddWithValue("@idUsuario", Sesion.IdUsuario);
                    cmdF.Parameters.AddWithValue("@total", totalConDescuento); // 👈 CAMBIADO
                    cmdF.ExecuteNonQuery();

                    // 2. Recuperar ID de la factura recién creada
                    SqlCommand cmdId = new SqlCommand(
                        "SELECT TOP 1 ID_Factura FROM Factura ORDER BY ID_Factura DESC", con, tra);
                    int idFactura = (int)cmdId.ExecuteScalar();

                    // 3. Insertar Detalles
                    foreach (var item in carrito)
                    {
                        SqlCommand cmdD = new SqlCommand("sp_InsertarDetalleFactura", con, tra);
                        cmdD.CommandType = CommandType.StoredProcedure;

                        cmdD.Parameters.AddWithValue("@subtotal", item.Subtotal);
                        cmdD.Parameters.AddWithValue("@cantidad", item.Cant);
                        cmdD.Parameters.AddWithValue("@PrecioUnit", item.Precio);
                        cmdD.Parameters.AddWithValue("@idFactura", idFactura);
                        cmdD.Parameters.AddWithValue("@idProducto", item.Id);

                        cmdD.ExecuteNonQuery();
                    }

                    tra.Commit();

                    // Mensaje con resumen de la venta
                    if (descuento > 0)
                        MessageBox.Show($"Venta guardada.\nDescuento aplicado: {descuento:C}\nTotal cobrado: {totalConDescuento:C}");
                    else
                        MessageBox.Show("Venta guardada con éxito.");

                    LimpiarVenta();
                }
                catch (Exception ex)
                {
                    tra.Rollback();
                    MessageBox.Show("Error al finalizar venta: " + ex.Message);
                }
            }
        }

        // ================================
        // LIMPIAR VENTA
        // ================================

        private void LimpiarVenta()
        {
            carrito.Clear();
            total = 0;
            descuento = 0;  // 👈 NUEVO
            totalConDescuento = 0;  // 👈 NUEVO
            idClienteSeleccionado = 1;

            txtDniCliente.Clear();
            txtBusqueda.Clear();
            txtCantidad.Text = "1";

            lblNombreCliente.Text = "Cliente: Público General";
            lblNombreCliente.Foreground = Brushes.Gray;
            lblDescuento.Visibility = Visibility.Collapsed; // 👈 NUEVO

            ActualizarInterfaz();
        }

        private void dgBusqueda_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }
}