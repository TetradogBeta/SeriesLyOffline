
using System.CodeDom;
using SeriesLyOffline2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Gabriel.Cat.Wpf;
using Gabriel.Cat.Extension;
using System.Diagnostics;
using Gabriel.Cat;
using System.Xml;
using System.Threading;
namespace SeriesLyOffline_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly string pathDatosLy = Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + "datos.ly";
        ListaUnica<Serie> seriesList;
        ListaUnica<SerieConvinada> seriesConvinadas;
        Semaphore semaforSeries;
        bool acabadoDeCargar;
        public MainWindow()
        {
            if (PuedeInicializar())
            {
                InitializeComponent();
                imagen.SetImage(Imagenes.mainlogo2);
                seriesList = new ListaUnica<Serie>();
                seriesConvinadas = new ListaUnica<SerieConvinada>();
                semaforSeries = new Semaphore(1, 1);
                //configuro Disco logico para SeriesLy
                DiscoLogico.MetodoParaFiltrarPorDefecto = Serie.ArchivosMultimedia;//solo quiero que me de carpetas multimedia
                DiscoLogico.DirectorioNuevo += CarpetaEncontrada;
                DiscoLogico.DirectorioPerdido += CarpetaPerdida;
                DiscoLogico.ConservarDiscosPerdidos = true;
                DiscoLogico.UsarObjetosIOPorDefecto = true;
                //Configuro Serie para cuando carge las series
                Serie.SerieNuevaCargada += SerieNueva;
                //Configuro la lista donde estaran las series
                clstSeries.Tipo = ColorListBox.TipoSeleccion.One;
                clstSeries.Sorted = true;
                clstSeries.ItemSelected += VisualizaSerie;
                LoadBD();
                Closed += GuardaBD;
                Keyboard.AddKeyDownHandler(this, SiPulsaControl);

            }
            else
            {
                this.Close();
            }
        }
        private void imagen_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            //abrir ventana configuracion disco logico
            new Configuracion().Show();
        }

        #region BD
        private void GuardaBD(object sender, EventArgs e)
        {
            FileStream fs;
            StreamWriter sw;
            DiscoLogico.AcabaTodaActividad();
            if (new DirectoryInfo(Path.GetDirectoryName(pathDatosLy)).CanWrite())
            {
                if (acabadoDeCargar)
                {

                    fs = new FileStream(pathDatosLy, FileMode.Create);
                    sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                    try
                    {
                        sw.WriteLine("<SeriesLyOffLine>");
                        sw.WriteLine(Configuracion.ToXml().OuterXml);
                        if (Debugger.IsAttached)
                        {
                            Console.WriteLine("inicio guardar series");
                        }
                        //si si aun no a cargado las series guardadas no las borra!!


                        sw.WriteLine(Serie.ToXml(seriesList.ToArray()).OuterXml);
                        if (Debugger.IsAttached)
                        {
                            Console.WriteLine("fin guardar series");
                        }
                        sw.WriteLine("</SeriesLyOffLine>");
                    }
                    catch { }
                    finally
                    {

                        sw.Close();
                        fs.Close();
                    }
                }
            }

        }
        private void LoadBD()
        {
            bool inicializarBD = !File.Exists(pathDatosLy);
            XmlDocument xml;
            acabadoDeCargar = inicializarBD;
            if (!inicializarBD)
            {
                MiPool<object>.AñadirFaena((obj) =>
             {
                 //cargo el xml
                 try
                 {
                     xml = new XmlDocument();
                     xml.LoadXml(File.ReadAllText(pathDatosLy));
                     Configuracion.LoadXml(xml.FirstChild.FirstChild);
                     Serie.DameSeries(xml.FirstChild.LastChild);
                     acabadoDeCargar = true;
                     if (Configuracion.BuscarUnidades)
                         DiscoLogico.BuscaUnidadesNuevasAsync();//asi no se adelanta!!

                 }
                 catch
                 {
                     MessageBox.Show("Error al cargar la base de datos", "Atencion", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                     InicializarBD();
                 }
             }, null);
                MiPool<object>.IniciaFaena();


            }
            else
                InicializarBD();
        }
        private void InicializarBD()
        {
            DiscoLogico.BuscaUnidadesNuevasAsync();
        }
        #endregion
        private void SiPulsaControl(object sender, KeyEventArgs e)
        {
            CapituloViewer capitulo;
            Serie[] seriesAConvinar;
            PedirNombreSerieWindow nombre;
            SerieConvinada serieConvinada;

            switch (e.Key)
            {
                case Key.S:



                    if (serieViewer.SerieAVisualizar != null)
                    {

                        for (int i = 0; i < serieViewer.ColeccionControles.Count; i++)
                            ((CapituloViewer)serieViewer.ColeccionControles[i]).Visto = true;
                    }



                    break;
                case Key.D:

                    if (serieViewer.SerieAVisualizar != null)
                    {
                        for (int i = 0; i < serieViewer.ColeccionControles.Count; i++)
                            ((CapituloViewer)serieViewer.ColeccionControles[i]).Visto = false;
                    }

                    break;

                case Key.I:
                    if (serieViewer.SerieAVisualizar != null)
                    {

                        for (int i = 0; i < serieViewer.ColeccionControles.Count; i++)
                        {
                            capitulo = ((CapituloViewer)serieViewer.ColeccionControles[i]);
                            capitulo.Visto = !capitulo.Visto;
                        }

                    }

                    break;
                case Key.M:
                    semaforSeries.WaitOne();
                    //si solo hay uno si es mixted se desmixta XD
                    //saca un control para poner nombre
                    seriesAConvinar = clstSeries.SelectedItems().Casting<Serie>().ToArray();
                    if (seriesAConvinar.Length > 1)
                    {
                        nombre = new PedirNombreSerieWindow();
                        nombre.PonFocoEscritura();
                        nombre.ShowDialog();


                        if (!String.IsNullOrEmpty(nombre.NombreMix))
                        {
                            serieConvinada = new SerieConvinada(nombre.NombreMix);
                            serieConvinada.Añadir(seriesAConvinar);
                            for (int i = 0; i < seriesAConvinar.Length; i++)
                                seriesList.Elimina(seriesAConvinar[i].Clau());
                            seriesList.Añadir((Serie)serieConvinada);
                            clstSeries.Remove(seriesAConvinar);
                            clstSeries.Add(serieConvinada, DameColor(serieConvinada));
                            clstSeries.SelectItem(serieConvinada);
                            seriesConvinadas.Añadir(serieConvinada);
                        }
                    }
                    else if (serieViewer.SerieAVisualizar is SerieConvinada)
                    {
                        clstSeries.Remove((object)serieViewer.SerieAVisualizar);
                        seriesList.Elimina(serieViewer.SerieAVisualizar.Clau());
                        seriesConvinadas.Elimina(serieViewer.SerieAVisualizar.Clau());
                        foreach (Serie serie in (IEnumerable<Serie>)serieViewer.SerieAVisualizar)
                        {
                            if (serie != null)
                            {
                                clstSeries.Add(serie, DameColor(serie));
                                if (!seriesList.Existe(serie.Clau()))
                                    seriesList.Añadir(serie);


                            }

                        }

                    }


                    clstSeries.SelectAt(0);
                    semaforSeries.Release();
                    break;
            }

        }

        private void VisualizaSerie(object objSelected, ItemArgs arg)
        {
            Serie serieSeleccionada = objSelected as Serie;
            if (serieSeleccionada != null)
            {
                if (serieViewer.SerieAVisualizar != null)
                {
                    clstSeries.CambiarColor(serieViewer.SerieAVisualizar, DameColor(serieViewer.SerieAVisualizar));
                }
                serieViewer.SerieAVisualizar = serieSeleccionada;
            }
        }

        private void SerieNueva(Serie serie)
        {
            semaforSeries.WaitOne();
            if (!seriesList.Existe(serie) && !EstaEnUnMix(serie))
            {
                if (serie is SerieConvinada)
                    seriesConvinadas.Añadir(serie as SerieConvinada);
                seriesList.Añadir(serie);
                clstSeries.Add(serie, DameColor(serie));

            }
            semaforSeries.Release();
        }

        private bool EstaEnUnMix(Serie serie)
        {
            return EstaEnUnMix(serie.Directorio.FullName);
        }
        private bool EstaEnUnMix(string path)
        {
            bool estaEnUnMix = false;
            seriesConvinadas.WhileEach((serieConvinada) =>
            {
                estaEnUnMix = serieConvinada.CompruebaDireccion(path);
                return !estaEnUnMix;
            });
            return estaEnUnMix;
        }
        private Color DameColor(Serie serie)
        {
            Color color = Colors.Wheat;
            switch (serie.ConsultaEstado())
            {
                case Serie.Estado.Pendiente: color = Colors.White; break;
                case Serie.Estado.Siguiendo: color = Colors.Green; break;
                case Serie.Estado.Acabada: color = Colors.LightPink; break;

            }

            return color;
        }

        private bool PuedeInicializar()
        {
            return new DirectoryInfo(Environment.CurrentDirectory).CanWrite() || MessageBox.Show("No se puede escribir en el directorio, eso puede imperdir guardar y/o actualizar la base de datos, Si quieres puedes usarlo sabiendo que se puede perder el trabajo hecho", "Problema con los permisos de escritura", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes;
        }
        private void CarpetaEncontrada(DiscoLogico disco, IOArgs archivo)
        {
            if (!seriesList.ExisteClave(archivo.Path))
            {

                SerieNueva(new Serie(new DirectoryInfo(archivo.Path),false));
            }
        }

        private void CarpetaPerdida(DiscoLogico disco, IOArgs archivo)
        {
            bool acabado = false;
            semaforSeries.WaitOne();
            if (seriesList.ExisteClave(archivo.Path))
            {
                clstSeries.Remove(seriesList[archivo.Path]);
                seriesList.EliminaClave(archivo.Path);
            }
            else if (EstaEnUnMix(archivo.Path))
            {
                seriesConvinadas.WhileEach((serieConvinada) =>
                {
                    if (serieConvinada.CompruebaDireccion(archivo.Path))
                    {
                        serieConvinada.QuitarNoDisponibles();
                        if (!serieConvinada.HaySeries)
                        {
                            seriesList.Elimina(serieConvinada.Clau());
                            clstSeries.Remove(serieConvinada);
                        }
                        acabado = true;
                    }
                    return !acabado;
                });
            }

            semaforSeries.Release();
        }


    }
}
