
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

namespace SeriesLyOffline_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        LlistaOrdenada<IComparable, Serie> series;
        LlistaOrdenada<IComparable, SerieConvinada> seriesConvinadas;
        private bool acabadoDeCargar;
        readonly string PATHDATOSLY = Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar + "datos.ly";
        public MainWindow()
        {
            if (new DirectoryInfo(Path.GetDirectoryName(PATHDATOSLY)).CanWrite() || MessageBox.Show("No se puede escribir en el directorio, eso puede imperdir guardar y/o actualizar la base de datos, Si quieres puedes usarlo sabiendo que se puede perder el trabajo hecho", "Problema con los permisos de escritura", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                series = new LlistaOrdenada<IComparable, Serie>();
                seriesConvinadas = new LlistaOrdenada<IComparable, SerieConvinada>();
                InitializeComponent();
                clstSeries.Sorted = true;
                imagen.SetImage(Imagenes.mainlogo2);
                clstSeries.Tipo = ColorListBox.TipoSeleccion.One;
                //pongo a buscar unidades nuevas y quitadas y actualizadas :D
                DiscoLogico.VelocidadPorDefecto = DiscoLogico.VelocidadEscaneo.Normal;
                DiscoLogico.MetodoParaFiltrarPorDefecto = Serie.ArchivosMultimedia;
                DiscoLogico.ConservarDiscosPerdidos = true;
                DiscoLogico.VerMensajesDebugger = false;
                DiscoLogico.UsarObjetosIOPorDefecto = true; //uso objetos io??

                DiscoLogico.CarpetaVigilada += PonDirectorioSiNoEsta;
                DiscoLogico.CarpetaNoVigilada += QuitaDirectorioSiEsta;
                DiscoLogico.DirectorioPerdido += QuitaDirectorioSiEsta;
                DiscoLogico.CarpetaBloqueada += QuitaDirectorioSiEsta;
               
                Serie.SerieNuevaCargada += CargarSerieDelXml;//asi puede ir mas rapido :D al ir en paralelo :D
                clstSeries.ItemSelected += VisualizarSerie;
                Closing += GuardaSeries;
                //si seleccionan mas de uno (dándole a control sino hace click normal!)  ofrece para unirlas todas y poder ponerle un nombre cuando pulsa M (despues de seleccionarlas)
                Keyboard.AddKeyDownHandler(this, SiPulsaControl);
                Load();
            }
            else
            {
                //no puedo escribir y el usuario no quiere usarla asi que la cierro
                this.Close();
            }
        }

        private void Load()
        {
            XmlDocument xml;
            if (File.Exists(PATHDATOSLY))
            {

                MiPool<object>.AñadirFaena((obj) =>
                {
                    try
                    {

                        xml = new XmlDocument();
                        xml.LoadXml(File.ReadAllText(PATHDATOSLY));
                        Configuracion.LoadXml(xml.FirstChild.FirstChild);
                        Serie.DameSeries(xml.FirstChild.LastChild);
                        acabadoDeCargar = true;
                        if (Configuracion.BuscarUnidades)
                            DiscoLogico.BuscaUnidadesNuevasAsync();//asi no se adelanta!!
                    }
                    finally
                    {
                    }
                }, null);
                MiPool<object>.IniciaFaena();
            }
            else { acabadoDeCargar = true; DiscoLogico.BuscaUnidadesNuevasAsync(); }
        }
        private void QuitaDirectorioSiEsta(DiscoLogico disco, IOArgs dir)
        {
            Action act;
            if (series.Existeix(dir.Path)||ExisteDirEnUnMix(new DirectoryInfo(dir.Path)))
            {
                act = () =>
                {
                    clstSeries.Remove(series[dir.Path]);
                    clstSeries.VolverASeleccionar();
                    series.Elimina(dir.Path);
                };
                Dispatcher.BeginInvoke(act);
            }
        }

        private void PonDirectorioSiNoEsta(DiscoLogico disco, IOArgs dir)
        {
            Action act;
            Serie serieAPoner;
            if (!series.Existeix(dir.Path)&&!ExisteDirEnUnMix(new DirectoryInfo(dir.Path)))
            {
                act = () =>
                {
                    serieAPoner = CargaCarpeta(new DirectoryInfo(dir.Path));
                    if (serieAPoner!=null)
                    {
                        series.Afegir(dir.Path, serieAPoner);
                        clstSeries.Add(serieAPoner, DameColor(serieAPoner));
                    }
                };
                Dispatcher.BeginInvoke(act);
            }
        }
        private void SiPulsaControl(object sender, KeyEventArgs e)
        {
            CapituloViewer capitulo;
            Serie[] seriesAConvinar;
            PedirNombreSerieWindow nombre;
            SerieConvinada serieConvinada;

            switch (e.Key)
            {
                case Key.S://poner a cargar



                    if (serieViewer.SerieAVisualizar != null)
                    {

                        for(int i=0;i<serieViewer.ColeccionControles.Count;i++)
                           ((CapituloViewer)serieViewer.ColeccionControles[i]).Visto = true;
                    }



                    break;//selecciona todos
                case Key.D://poner a cargar

                    if (serieViewer.SerieAVisualizar != null)
                    {
                        for (int i = 0; i < serieViewer.ColeccionControles.Count; i++)
                            ((CapituloViewer)serieViewer.ColeccionControles[i]).Visto = false;
                    }

                    break;//deselecciona todos

                case Key.I://poner a cargar
                    if (serieViewer.SerieAVisualizar != null)
                    {

                        for (int i = 0; i < serieViewer.ColeccionControles.Count; i++)
                        {
                            capitulo = ((CapituloViewer)serieViewer.ColeccionControles[i]);
                            capitulo.Visto = !capitulo.Visto;
                        }

                    }

                    break;//invierte seleccion
                case Key.M:
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
                                series.Elimina(seriesAConvinar[i].Clau());
                            series.Afegir(serieConvinada);
                            clstSeries.Remove(seriesAConvinar);
                            clstSeries.Add(serieConvinada, DameColor(serieConvinada));
                            clstSeries.SelectItem(serieConvinada);
                            seriesConvinadas.Afegir(serieConvinada);
                        }
                    }
                    else if (serieViewer.SerieAVisualizar is SerieConvinada)
                    {
                        clstSeries.Remove((object)serieViewer.SerieAVisualizar);
                        series.Elimina(serieViewer.SerieAVisualizar.Clau());
                        seriesConvinadas.Elimina(serieViewer.SerieAVisualizar.Clau());
                        foreach (Serie serie in (IEnumerable<Serie>)serieViewer.SerieAVisualizar)
                        {
                            if (serie != null)
                            {
                                clstSeries.Add(serie, DameColor(serie));
                                if (!series.Existeix(serie.Clau()))
                                    series.Afegir(serie.Clau(), serie);


                            }

                        }

                    }


                    clstSeries.SelectAt(0);
                    break;
            }

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

        private void CargarSerieDelXml(Serie serie)
        {
            Action act;
            if (serie is SerieConvinada)
            {
            	if(!seriesConvinadas.Existeix(serie.Clau()))
            		seriesConvinadas.Afegir(serie as SerieConvinada);
            }
            if (!series.Existeix(serie.Clau()))
            {
                series.Afegir(serie);
                act = () => { clstSeries.Add(serie, DameColor(serie)); };
                Dispatcher.BeginInvoke(act);
            }
        }

        private void VisualizarSerie(object objSelected, ItemArgs arg)
        {
            if (clstSeries.Tipo != ColorListBox.TipoSeleccion.More)
            {
                Serie serie = objSelected as Serie;
                if (serie != null)
                {
                    if (serieViewer.SerieAVisualizar != null)
                    {
                        clstSeries.CambiarColor(serieViewer.SerieAVisualizar, DameColor(serieViewer.SerieAVisualizar));
                    }
                    serieViewer.SerieAVisualizar = serie;
                    clstSeries.CambiarColor(serie, DameColor(serie));
                }
            }
        }

		bool ExisteDirEnUnMix(DirectoryInfo directorio)
		{
			bool estaSinUso=true;
			seriesConvinadas.WhileEach(serieHaMirar => {
				estaSinUso = !serieHaMirar.Value.CompruebaDireccion(directorio);
				return estaSinUso;
			});
			return !estaSinUso;
		}

        private Serie CargaCarpeta(DirectoryInfo directorio)
        {
            Serie serie = null;
            bool estaSinUso=!series.Existeix(directorio.FullName);
            if(estaSinUso)
         		estaSinUso= !ExisteDirEnUnMix(directorio);
            
            if (estaSinUso)
                try
                {

                    serie = new Serie(directorio);
                    if (Debugger.IsAttached)
                    {
                        Console.WriteLine("Serie Nueva {0} ;", serie);
                    }
                }
                catch { }
            return serie;
        }

        private void GuardaSeries(object sender, CancelEventArgs e)
        {
            FileStream fs;
            StreamWriter sw;
            DiscoLogico.AcabaTodaActividad();
            if (new DirectoryInfo(Path.GetDirectoryName(PATHDATOSLY)).CanWrite())
            {
                if (acabadoDeCargar)
                {
                   
                    fs = new FileStream(PATHDATOSLY, FileMode.Create);
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


                        sw.WriteLine(Serie.ToXml(series.ValuesToArray()).OuterXml);
                        if (Debugger.IsAttached)
                        {
                            Console.WriteLine("fin guardar series");
                        }
                    }
                    finally
                    {
                        sw.WriteLine("</SeriesLyOffLine>");
                        sw.Close();
                        fs.Close();
                    }
                }
            }

        }

        private void imagen_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            //abrir ventana configuracion disco logico
            new Configuracion().Show();
        }


    }
}
