using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for PedirNombreSerieWindow.xaml
    /// </summary>
    public partial class PedirNombreSerieWindow : Window
    {
        bool guardado = false;
        Semaphore semafor = new Semaphore(0,1);
        public PedirNombreSerieWindow()
        {
            InitializeComponent();
            txtNombre.PreviewKeyUp += EnterPresionado;
        }

        private void EnterPresionado(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button_Click_1(null, null);
       
            }
        }


        public string NombreMix { get { return txtNombre.Text; } }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(NombreMix))
                if (MessageBox.Show("Se pondra MixSeries si no pones un nombre, te parece bien?", "Atencion", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    txtNombre.Text = "MixSeries";
            if (!String.IsNullOrEmpty(NombreMix))
            {
                guardado = true;
                this.Close();
            }
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!guardado)
                e.Cancel = MessageBox.Show("Desea cancelar el mix?", "Atencion", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No;
            try { semafor.Release(); }
            catch { }
        }
        public void PonFocoEscritura()
        {
            txtNombre.Focus();
        }

    }
}
