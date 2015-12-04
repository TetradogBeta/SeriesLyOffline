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
using System.Xml;

namespace SeriesLyOffline2
{
    /// <summary>
    /// Lógica de interacción para Configuracion.xaml
    /// </summary>
    public partial class Configuracion : Window
    {
        static bool chekedPorDefecto;


        public Configuracion()
        {
            InitializeComponent();
            DiscoLogico.UnidadEncontrada += UnidadNueva;
            DiscoLogico.UnidadPerdida += UnidadPerdida;
            clstDiscosLogicos.ItemSelected += PreguntarParaQuitar;
            ckBuscarUnidades.IsChecked = chekedPorDefecto;
            clstDiscosLogicos.AddRange(DiscoLogico.Discos());
            clstRutasHaEvitar.AddRange(DiscoLogico.GetBlackList());
            ckBuscarUnidades.Checked += IniciaBusquedaUnidades;
            ckBuscarUnidades.Unchecked += FinalizaBusquedaUnidades;
            if (!chekedPorDefecto)
                pgrTrabajando.Hide();
        }

        private void FinalizaBusquedaUnidades(object sender, RoutedEventArgs e)
        {
            DiscoLogico.ModoEscaneoEnBuscaDeUnidadesActivas = DiscoLogico.ModoEscaneo.Manual;
            pgrTrabajando.Hide();
            chekedPorDefecto = false;
        }

        private void IniciaBusquedaUnidades(object sender, RoutedEventArgs e)
        {
            DiscoLogico.ModoEscaneoEnBuscaDeUnidadesActivas = DiscoLogico.ModoEscaneo.Continuo;
            pgrTrabajando.Visible();
            chekedPorDefecto = true;
        }
        public static bool BuscarUnidades
        {
            get { return Configuracion.chekedPorDefecto; }
            set { Configuracion.chekedPorDefecto = value; }
        }
        private void PreguntarParaQuitar(object objSelected, Gabriel.Cat.Wpf.ItemArgs arg)
        {
            DiscoLogico disco = objSelected as DiscoLogico;
            new ConfiguradorDiscoLogico(disco).Show();

        }

        private void UnidadPerdida(DiscoLogico disco, DiscoLogicoEventArgs args)
        {
            clstDiscosLogicos.CambiarColor(disco, Colors.LightPink);
        }

        private void UnidadNueva(DiscoLogico disco, DiscoLogicoEventArgs args)
        {

                if (clstDiscosLogicos.Existe(disco))
                    clstDiscosLogicos.CambiarColor(disco, Colors.White);
                else
                    clstDiscosLogicos.Add(disco);
        }
        public static void LoadXml(XmlNode nodoConfiguracionDiscos)
        {
            DiscoLogico.LoadXml(nodoConfiguracionDiscos.LastChild, true);
            if (nodoConfiguracionDiscos.FirstChild.InnerText == true.ToString())
                chekedPorDefecto = true;
        }
        public static XmlNode ToXml()
        {
            text stringXml = "";
            XmlDocument xml = new XmlDocument();
            stringXml+="<ConfiguracionLy>";
            stringXml+="<BuscarUnidades>" + chekedPorDefecto + "</BuscarUnidades>";
            stringXml+=DiscoLogico.ToXml(null).OuterXml;
            stringXml+="</ConfiguracionLy>";
            xml.LoadXml(stringXml);
            xml.Normalize();
            return xml.FirstChild;
            //guardo si escanea unidades logicas
            //guardo la configuracion de cada unidad logica
            //guardo la blackList
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string rutaHaVigilar = null;
            //añade la ruta seleccionada como disco logico


            if (rutaHaVigilar != null)
            {
                DiscoLogico disco = new DiscoLogico(rutaHaVigilar);
                DiscoLogico.AñadirDisco(disco);
                clstDiscosLogicos.Add(disco);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //añade las rutas a la blackList
            Llista<string> blackList = new Llista<string>();
            //cojo la ruta a bloquear y si esta a true los subdirs tambien los pongo :D
       
            DiscoLogico.BlackList(blackList, true);
            clstRutasHaEvitar.Clear();
            clstRutasHaEvitar.AddRange(DiscoLogico.GetBlackList());//asi no estan repetidos y estan por orden :D
            //avisar por evento las rutas nuevas y a eliminar bloqueadas DiscoLogico
            //decir que estan haciendo en el detalle de los dicos logicos
        }

        private void clstRutasHaEvitar_ItemSelected_1(object objSelected, Gabriel.Cat.Wpf.ItemArgs arg)
        {
            //pregunto si quiero quitar la ruta bloqueada de la lista :D
        }
    }
}
