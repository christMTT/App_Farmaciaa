using Microsoft.Data.SqlClient;
using MongoDB.Bson;
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

namespace App_Farmacia
{
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
        private decimal descuento = 0;
        private decimal totalConDescuento = 0;
        private int idClienteSeleccionado = 11;
        private bool _ventaConReceta = false;

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

                // VALIDACIÓN DE STOCK DISPONIBLE
                int stockDisponible = 0;
                using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
                {
                    try
                    {
                        con.Open();
                        string sql = "SELECT Stock FROM ProductoSucursales WHERE ID_Producto = @idProducto AND ID_Sucursal = @idSucursal";
                        SqlCommand cmd = new SqlCommand(sql, con);
                        cmd.Parameters.AddWithValue("@idProducto", (int)fila["ID_Producto"]);
                        cmd.Parameters.AddWithValue("@idSucursal", Sesion.IdSucursal);

                        object resultado = cmd.ExecuteScalar();
                        stockDisponible = resultado != null ? Convert.ToInt32(resultado) : 0;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al verificar stock: " + ex.Message);
                        return;
                    }
                }

                // Considerar también lo que ya está en el carrito para ese producto
                int cantidadEnCarrito = 0;
                foreach (var c in carrito)
                {
                    if (c.Id == (int)fila["ID_Producto"])
                        cantidadEnCarrito += c.Cant;
                }

                if (cantidad + cantidadEnCarrito > stockDisponible)
                {
                    MessageBox.Show($"Stock insuficiente. Solo hay {stockDisponible} unidades disponibles" +
                        (cantidadEnCarrito > 0 ? $" y ya tiene {cantidadEnCarrito} en el carrito." : "."));
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
                        descuento = r.GetDecimal(r.GetOrdinal("MontoDescuento"));
                        totalConDescuento = total - descuento;

                        lblDescuento.Text = $"Descuento ({r["Nombre"]}): -{descuento:C}";
                        lblDescuento.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        descuento = 0;
                        totalConDescuento = total;
                        lblDescuento.Visibility = Visibility.Collapsed;
                    }

                    lblSubtotal.Text = $"Subtotal: {total:C}";
                    lblTotal.Text = $"TOTAL: {totalConDescuento:C}";
                }
                catch (Exception ex)
                {
                    descuento = 0;
                    totalConDescuento = total;
                    lblSubtotal.Text = $"Subtotal: {total:C}";
                    lblTotal.Text = $"TOTAL: {total:C}";
                    lblDescuento.Visibility = Visibility.Collapsed;
                    MessageBox.Show("Error al obtener descuento: " + ex.Message);
                }
            }
        }

        private void ActualizarInterfaz()
        {
            dgCarrito.Items.Refresh();
            lblSubtotal.Text = $"Subtotal: {total:C}";
            AplicarDescuento();
        }

        private void btnReceta_Click(object sender, RoutedEventArgs e)
        {
            _ventaConReceta = !_ventaConReceta;

            if (_ventaConReceta)
            {
                btnReceta.Content = "✓ Con receta";
                btnReceta.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FAEEDA"));

                panelReceta.Visibility = Visibility.Visible;
            }
            else
            {
                btnReceta.Content = "Venta con receta";
                btnReceta.Background = Brushes.Transparent;

                panelReceta.Visibility = Visibility.Collapsed;

                txtDoctor.Clear();
                txtConsultorio.Clear();
            }
        }

        private async void btnFinalizar_Click(object sender, RoutedEventArgs e)
        {
            if (carrito.Count == 0) { MessageBox.Show("El carrito está vacío."); return; }

            var itemsParaAuditoria = new List<ElementoCarrito>(carrito);
            int idFactura = 0;
            bool ventaExitosa = false;

            using (SqlConnection con = new SqlConnection(Datos.conexion.Cadena))
            {
                con.Open();
                try
                {
                    // sin BeginTransaction — el SP ya maneja la suya
                    SqlCommand cmdF = new SqlCommand("sp_InsertarFactura", con);
                    cmdF.CommandType = CommandType.StoredProcedure;
                    cmdF.Parameters.AddWithValue("@idCliente", idClienteSeleccionado);
                    cmdF.Parameters.AddWithValue("@idSucursal", Sesion.IdSucursal);
                    cmdF.Parameters.AddWithValue("@idUsuario", Sesion.IdUsuario);
                    cmdF.Parameters.AddWithValue("@total", totalConDescuento);
                    cmdF.ExecuteNonQuery();

                    idFactura = Convert.ToInt32(cmdF.ExecuteScalar());

                    foreach (var item in carrito)
                    {
                        SqlCommand cmdD = new SqlCommand("sp_InsertarDetalleFactura", con);
                        cmdD.CommandType = CommandType.StoredProcedure;
                        cmdD.Parameters.AddWithValue("@subtotal", item.Subtotal);
                        cmdD.Parameters.AddWithValue("@cantidad", item.Cant);
                        cmdD.Parameters.AddWithValue("@PrecioUnit", item.Precio);
                        cmdD.Parameters.AddWithValue("@idFactura", idFactura);
                        cmdD.Parameters.AddWithValue("@idProducto", item.Id);
                        cmdD.ExecuteNonQuery();
                    }

                    ventaExitosa = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al finalizar venta: " + ex.Message);
                }
            }

            if (!ventaExitosa) return;

            if (_ventaConReceta)
            {
                var productosReceta = new BsonArray();
                foreach (var item in itemsParaAuditoria)
                {
                    productosReceta.Add(new BsonDocument
                    {
                        ["producto_id"] = item.Id,
                        ["producto_nombre"] = item.Nombre,
                        ["cantidad"] = item.Cant,
                        ["precio_unit"] = (double)item.Precio
                    });
                }

                await Datos.Auditoria.Instancia.RegistrarRecetaAsync( idFactura, productosReceta,txtDoctor.Text, txtConsultorio.Text);
            }

            if (descuento > 0)
                MessageBox.Show($"Venta guardada.\nDescuento: {descuento:C}\nTotal: {totalConDescuento:C}");
            else
                MessageBox.Show("Venta guardada con éxito.");

            _ventaConReceta = false;
            btnReceta.Content = "Venta con receta";
            btnReceta.Background = Brushes.Transparent;
            LimpiarVenta();
        }


        private void LimpiarVenta()
        {
            carrito.Clear();
            total = 0;
            descuento = 0;
            totalConDescuento = 0;
            idClienteSeleccionado = 1;

            txtDoctor.Clear();
            txtDniCliente.Clear();
            txtBusqueda.Clear();
            txtCantidad.Text = "1";
            txtConsultorio.Clear();
      

            lblNombreCliente.Text = "Cliente: Público General";
            lblNombreCliente.Foreground = Brushes.Gray;
            lblDescuento.Visibility = Visibility.Collapsed;

            ActualizarInterfaz();
        }

        private void dgBusqueda_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }
}