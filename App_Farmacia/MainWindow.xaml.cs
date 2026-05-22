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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private string rolUsuarioActual;
        public MainWindow()
        {
            InitializeComponent();
            rolUsuarioActual = "Vendedor"; // Un rol por defecto para pruebas
            AplicarSeguridad();
        }
        public MainWindow(String rol)
        {
            InitializeComponent();
            rolUsuarioActual = rol;
            AplicarSeguridad();

        }

        private void AplicarSeguridad()
        {
            // Según el documento: Vendedor, Gerente, DBA [cite: 21, 22, 23]
            if (rolUsuarioActual == "Vendedor")
            {
                // El vendedor puede insertar pero quizás no ver reportes financieros [cite: 21]
                BtnReportes.Visibility = Visibility.Collapsed;
            }
            else if (rolUsuarioActual == "Gerente")
            {
                // El gerente consulta reportes [cite: 22]
                BtnReportes.Visibility = Visibility.Visible;
            }
        }
        // Navegación al módulo de Clientes
        private void BtnClientes_Click(object sender, RoutedEventArgs e)
        {
             MainFrame.Navigate(new PaginaClientes()); 
        }

        // Navegación al módulo de Reportes
        private void BtnReportes_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PaginaReportes());
        }

        // Asegúrate de tener también este para que el botón Ventas funcione
        private void BtnVentas_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PaginaVentas());
        }

        private void BtnInventario_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new PaginaStock());
        }
    }
    }