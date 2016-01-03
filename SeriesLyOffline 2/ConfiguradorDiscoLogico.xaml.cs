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
            cmbModoEscaneo.SelectionChanged += CambiaModoEscaneo;
            cmbVelocidadEscaneo.SelectionChanged += CambiaVelocidadEscaneo;
            txtRutaSeleccionada.Text = disco.Root;
            incrementoPorCiclo.Text = disco.IncrementoComprbarBDUnidad+"";
            intervalInicial.Text = disco.IntervalComprobarBDUnidad+"";
            ciclosParaCambio.Text = disco.NumCiclosMaxComprobarBDUnidadSinCambios + "";
            incrementoPorCiclo.TextChanged += ValidaTexto;
            intervalInicial.TextChanged += ValidaTexto;
            ciclosParaCambio.TextChanged += ValidaTexto;
            incrementoPorCiclo.AcceptsReturn = true;
            intervalInicial.AcceptsReturn = true;
            ciclosParaCambio.AcceptsReturn = true;
        
        }

        private void ValidaTexto(object sender, TextChangedEventArgs e)
        {
            try
            {
                string numero = ((TextBox)sender).Text;
                Convert.ToInt32(numero);
            }
            catch {
                MessageBox.Show("Solo se pueden usar numeros enteros");
            }
        }

        private void CambiaVelocidadEscaneo(object sender, SelectionChangedEventArgs e)
        {
            disco.Velocidad = (DiscoLogico.VelocidadEscaneo)Enum.Parse(typeof(DiscoLogico.VelocidadEscaneo), cmbVelocidadEscaneo.SelectedItem as string);
        }

        private void CambiaModoEscaneo(object sender, SelectionChangedEventArgs e)
        {
            disco.ModoEscaneoBD = (DiscoLogico.ModoEscaneo)Enum.Parse(typeof(DiscoLogico.ModoEscaneo), cmbModoEscaneo.SelectedItem as string);
            incrementoPorCiclo.IsEnabled = false;
            intervalInicial.IsEnabled = false;
            ciclosParaCambio.IsEnabled = false;
            switch (disco.ModoEscaneoBD) {
                case DiscoLogico.ModoEscaneo.CadaXTiempo:
                    intervalInicial.IsEnabled = true; break;
                case DiscoLogico.ModoEscaneo.CadaXTiempoIncrementandolo:
                    intervalInicial.IsEnabled = true;
                    incrementoPorCiclo.IsEnabled = true; break;
                case DiscoLogico.ModoEscaneo.CadaXTiempoUnosXCiclosLuegoManual:
                    incrementoPorCiclo.IsEnabled = true;
                    intervalInicial.IsEnabled = true;
                    ciclosParaCambio.IsEnabled = true; break;
            }
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
        }
        private void pgrTrabajando_LeftUpClick_1(object sender, MouseButtonEventArgs e)
        {
            disco.EscanearAsync();
            MessageBox.Show("Se ha puesto a escanear");
        }
    }
}
