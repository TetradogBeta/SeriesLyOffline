using Gabriel.Cat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SeriesLyOffline2
{
    /// <summary>
    /// Lógica de interacción para ConfiguradorDiscoLogico.xaml
    /// </summary>
    public partial class ConfiguradorDiscoLogico : Window
    {
        //el color cambia con lo que esta pasando en el disco
        DiscoLogico disco;
        public event EventHandler CambioNombreDisco;
        public ConfiguradorDiscoLogico(DiscoLogico disco)
        {
            InitializeComponent();
            this.disco = disco;
            disco.EstadoCambiado += PonColorEstado;
            PonColorEstado(disco, disco.EstadoBD);
            cmbModoEscaneo.ItemsSource = Enum.GetNames((typeof(DiscoLogico.ModoEscaneo)));
            cmbVelocidadEscaneo.ItemsSource = Enum.GetNames((typeof(DiscoLogico.VelocidadEscaneo)));
            cmbModoEscaneo.SelectedItem = disco.ModoEscaneoBD.ToString();
            cmbVelocidadEscaneo.SelectedItem = disco.Velocidad.ToString();
            cmbModoEscaneo.SelectionChanged += CambiarModoEscaneo;
            cmbVelocidadEscaneo.SelectionChanged += CambiaVelocidadEscaneo;
            txtRutaSeleccionada.Text = disco.Root;
            incrementoPorCiclo.Text = disco.IncrementoComprbarBDUnidad+"";
            intervalInicial.Text = disco.IntervalComprobarBDUnidad+"";
            ciclosParaCambio.Text = disco.NumCiclosMaxComprobarBDUnidadSinCambios + "";
            incrementoPorCiclo.TextChanged += ValidaTexto;
            intervalInicial.TextChanged += ValidaTexto;
            ciclosParaCambio.TextChanged += ValidaTexto;
            CambiarModoEscaneo(disco.ModoEscaneoBD);
            this.pgrTrabajando.MouseLeftButtonUp += pgrTrabajando_LeftUpClick_1;


        }

        private void ValidaTexto(object sender, TextChangedEventArgs e)
        {
            try
            {

                System.Windows.Controls.TextBox txtSender = sender as System.Windows.Controls.TextBox;
                Convert.ToInt32(txtSender.Text);
            }
            catch (FormatException){
                System.Windows.MessageBox.Show("Solo se pueden usar numeros enteros");
            }
        }

        private void CambiaVelocidadEscaneo(object sender, SelectionChangedEventArgs e)
        {
            disco.Velocidad = (DiscoLogico.VelocidadEscaneo)Enum.Parse(typeof(DiscoLogico.VelocidadEscaneo), cmbVelocidadEscaneo.SelectedItem as string);
        }

        private void CambiarModoEscaneo(object sender, SelectionChangedEventArgs e)
        {
            CambiarModoEscaneo((DiscoLogico.ModoEscaneo)Enum.Parse(typeof(DiscoLogico.ModoEscaneo), cmbModoEscaneo.SelectedItem as string));

        }
        private void CambiarModoEscaneo(DiscoLogico.ModoEscaneo modo)
        {
            disco.ModoEscaneoBD = modo;
            incrementoPorCiclo.IsEnabled = false;
            intervalInicial.IsEnabled = false;
            ciclosParaCambio.IsEnabled = false;
            //falta que al cambiarlo se cambie en el disco...
            /*
            switch (disco.ModoEscaneoBD)
            {
                case DiscoLogico.ModoEscaneo.CadaXTiempo:
                    intervalInicial.IsEnabled = true; break;
                case DiscoLogico.ModoEscaneo.CadaXTiempoIncrementandolo:
                    intervalInicial.IsEnabled = true;
                    incrementoPorCiclo.IsEnabled = true; break;
                case DiscoLogico.ModoEscaneo.CadaXTiempoUnosXCiclosLuegoManual:
                    incrementoPorCiclo.IsEnabled = true;
                    intervalInicial.IsEnabled = true;
                    ciclosParaCambio.IsEnabled = true; break;
            }*/
        }
        private void PonColorEstado(DiscoLogico disco, DiscoLogico.EstadoEscaneo estado)
        {
            Brush color=Brushes.AliceBlue;
            Action act;
            switch (estado)
            {
                case DiscoLogico.EstadoEscaneo.Acabado:
                    color = Brushes.Red;
                    break;
                case DiscoLogico.EstadoEscaneo.ComprovandoIntegridad:
                    color = Brushes.Blue;
                    break;
                case DiscoLogico.EstadoEscaneo.Escaneando:
                    color = Brushes.BlueViolet;
                    break;
                case DiscoLogico.EstadoEscaneo.Esperando:
                    color = Brushes.LightGray;
                    break;
            }
            act = () => pgrTrabajando.Foreground = color;
            Dispatcher.BeginInvoke(act);
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //navega entre las carpetas
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if(!DiscoLogico.EstaLaRuta(folderBrowser.SelectedPath))
                {
                    disco.Root = folderBrowser.SelectedPath;
                    txtRutaSeleccionada.Text = disco.Root;
                    if (CambioNombreDisco != null)
                        CambioNombreDisco(this, new EventArgs());
                }
                else
                {
                    System.Windows.MessageBox.Show("La ruta ya esta activa actualmente");
                }
            }
        }
        private void pgrTrabajando_LeftUpClick_1(object sender, MouseButtonEventArgs e)
        {
            disco.EscanearAsync();
            System.Windows.MessageBox.Show("Se ha puesto a escanear");
        }
    }
}
