using Gabriel.Cat;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using Gabriel.Cat.Extension;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Xml;

namespace SeriesLyOffline2
{
    public delegate void SerieEncontradaEventHandler(Serie serie);
    public class Serie : IEnumerable<Capitulo>, IClauUnicaPerObjecte
    {
        public enum Estado
        {
            Pendiente, Siguiendo, Acabada
        }
        public static event SerieEncontradaEventHandler SerieNuevaCargada;
        public static readonly string[] extensionesMultimedia = { ".avi", ".mp4", ".mkv", ".mpeg", ".wmv", ".flv", ".m2ts" };//poner mas formatos de video :D
        protected static readonly string[] caracteresXmlSustitutos = { "&lt;", "&gt;", "&amp;", "&quot;", "&apos;" };
        protected static readonly string[] caracteresXmlReservados = { "<", ">", "&", "\"", "\'" };
        static readonly LlistaOrdenada<string, string> diccionarioMultimedia;
        private LlistaOrdenada<IComparable, Capitulo> capitulos;
        private static LlistaOrdenada<string, string> capitulosVistosGuardados;
        private static LlistaOrdenada<string, string> caracteresReservadosXml;
        protected DirectoryInfo dirPadre;
        string idMix;
        bool serieCargada;
        string pathly;
        static Serie()
        {
            capitulosVistosGuardados = new LlistaOrdenada<string, string>();
            diccionarioMultimedia = new LlistaOrdenada<string, string>();
            caracteresReservadosXml = new LlistaOrdenada<string, string>();
            for (int i = 0; i < caracteresXmlReservados.Length; i++)
            {
                caracteresReservadosXml.Afegir(caracteresXmlReservados[i], caracteresXmlSustitutos[i]);
                caracteresReservadosXml.Afegir(caracteresXmlSustitutos[i], caracteresXmlReservados[i]);
            }
            for (int i = 0; i < extensionesMultimedia.Length; i++)
                diccionarioMultimedia.Afegir(extensionesMultimedia[i], extensionesMultimedia[i]);

        }
        public Serie(DirectoryInfo directorio, string idMix)
            : this(directorio)
        {
            this.IdMix = idMix;
        }
        public Serie(DirectoryInfo directorio) : this(directorio, true) { }
        public Serie(DirectoryInfo directorio, bool comprovar)
            : this()
        {
            if (!directorio.CanWrite())
                throw new Exception("El directorio no se puede escribir!!");

            if (comprovar)
                if (!UsarSoloCarpetasMultimedia(directorio))
                    throw new Exception("La carpeta no tiene ningun archivo multimedia");

            dirPadre = directorio;
            pathly = dirPadre.FullName + Path.DirectorySeparatorChar + "capitulosVistosGuardados.ly";
        }
        public Serie(XmlNode nodoXml, string pathDir)
            : this(new DirectoryInfo(pathDir))
        {
            if (Convert.ToInt32(nodoXml.LastChild.InnerText) != 0)
                CargarCapitulos();
        }
        protected Serie()
        {
            this.capitulos = new LlistaOrdenada<IComparable, Capitulo>(); IdMix = "";
            serieCargada = false;
        }

        internal LlistaOrdenada<IComparable, Capitulo> Capitulos
        {
            get
            {
                if (!serieCargada)
                    CargarCapitulos();
                return capitulos;
            }
        }


        public DirectoryInfo Directorio
        {
            get { return dirPadre; }
        }

        public string IdMix
        {
            get
            {

                return idMix;
            }

            set
            {
                idMix = value;
            }
        }
        public virtual bool Empty
        {
            get { return capitulos.Count == 0; }
        }
        public virtual bool CompruebaDisponivilidad()
        {
            return dirPadre.Exists;
        }
        public virtual void QuitarNoDisponibles()
        {
            Llista<Capitulo> capitulosHaQuitar = new Llista<Capitulo>();
            foreach (var capitulo in capitulos)
                if (!capitulo.Value.CampruebaDisponivilidad())
                    capitulosHaQuitar.Afegir(capitulo.Value);
            for (int i = 0; i < capitulosHaQuitar.Count; i++)
                capitulos.Elimina(capitulosHaQuitar[i].Clau());
        }
        public virtual bool CompruebaDireccion(string direccion)
        {
            return CompruebaDireccion(new DirectoryInfo(direccion));
        }
        public virtual bool CompruebaDireccion(DirectoryInfo direccion)
        {
            return dirPadre.FullName.Equals(direccion.FullName);
        }
        public virtual Estado ConsultaEstado()
        {
            Estado estado;
            int numeroDeVistos = 0;
            bool acabado = false;
            capitulos.WhileEach((capitulo) =>
            {
                if (capitulo.Value.Visto)
                    numeroDeVistos++;
                else if (numeroDeVistos > 0)
                    acabado = true;
                return !acabado;
            });
            if (numeroDeVistos == 0)
                estado = Estado.Pendiente;
            else if (numeroDeVistos != capitulos.Count)
                estado = Estado.Siguiendo;
            else
                estado = Estado.Acabada;
            return estado;
        }
        public virtual void CargarCapitulos()
        {
            SortedList<string, string> keyCapitulosPorCargar = new SortedList<string, string>();
            FileInfo[] archivosMultimedia = null;
            Capitulo capitulo = null;
            int numVistos = 0;
            archivosMultimedia = Serie.ArchivosMultimedia(dirPadre);

            capitulos.Buida();
            for (int i = 0; i < archivosMultimedia.Length; i++)
            {
                capitulo = new Capitulo(archivosMultimedia[i]);
                capitulos.Afegir(capitulo.Clau(), capitulo);
                if (capitulosVistosGuardados.Existeix(capitulo.Key))
                {
                    numVistos++;
                    capitulo.Visto = true;
                }
            }

            if (Debugger.IsAttached)
            {
                if (numVistos > 0)
                    Console.WriteLine(" {0} Vistos serie {1}", numVistos, NombreSerie);
            }
            serieCargada = true;
        }

        protected virtual IEnumerable<string> GuardarVistos()
        {
            Llista<string> vistos = new Llista<string>();
            foreach (var capitulo in capitulos)
                if (capitulo.Value.Visto)
                    vistos.Afegir(capitulo.Value.LineaGuardado);
            return vistos;
        }

        public virtual string StringID
        {
            get { return dirPadre.FullName + ";" + idMix; }
        }

        public virtual string NombreSerie { get { return dirPadre.Name; } }

        private Capitulo[] Vistos()
        {
            return capitulos.ValuesToArray().Filtra((capitulo) => { return capitulo.Visto; }).ToTaula();
        }
        public override string ToString()
        {
            string toString = dirPadre.Name;
            if (toString.Length < 5)
            {
                if (dirPadre.Parent != null)
                    toString = dirPadre.Parent.Name + Path.DirectorySeparatorChar + toString;
            }
            return toString;

        }
        public IEnumerator<Capitulo> GetEnumerator()
        {
            return GetCapitulos();
        }
        public virtual IEnumerator<Capitulo> GetCapitulos()
        {
            return Capitulos.ValuesToArray().ObtieneEnumerador();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public virtual IComparable Clau()
        {
            return dirPadre.FullName;
        }
        public static FileInfo[] ArchivosMultimedia(DirectoryInfo dir)
        {
            FileInfo[] files = { };
            //     LlistaOrdenada<string, string> diccionarioMultimedia = new LlistaOrdenada<string, string>(Serie.diccionarioMultimedia);
            try
            {
                if (dir.CanWrite())//me interesa poder escribir
                    files = dir.GetFiles().Filtra((file) =>
                    {
                        return diccionarioMultimedia.Existeix(file.Extension);
                    }).ToTaula();
            }
            catch (Exception ex)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    Console.WriteLine(ex.Message);//peta por acceso denegado
            }
            return files;
        }

        public static bool UsarSoloCarpetasMultimedia(DirectoryInfo dir)
        {
            bool hayMultimedia = false;
            dir.GetFiles().WhileEach((file) =>
            {
                if (Serie.extensionesMultimedia.Existe(file.Extension))
                    hayMultimedia = true;
                return !hayMultimedia;

            });
            return hayMultimedia;
        }
        public static XmlNode ToXml(IEnumerable<Serie> series)
        {
            text stringXml = "";
            XmlDocument nodoXml=new XmlDocument();
            LlistaOrdenada<string, string> capitulosVistos = new LlistaOrdenada<string, string>();
            Llista<string> seriesGuardadas = new Llista<string>();
            LlistaOrdenada<string, Llista<string>> seriesConvinadasGuardadas = new LlistaOrdenada<string, Llista<string>>();
            text serieConvinadaXml = "";
            int numeroVistos = 0;
            string[] campos;
            stringXml+="<SeriesLy>";
            foreach (Serie serie in series)
            {

                foreach (string capituloVisto in serie.GuardarVistos())
                {//obtengo los capitulos que estan vistos :D
                    if (!capitulosVistos.Existeix(capituloVisto))
                        capitulosVistos.Afegir(capituloVisto, capituloVisto);
                    numeroVistos++;
                }

                if ((serie is SerieConvinada))
                {
                    if (!seriesConvinadasGuardadas.Existeix(serie.StringID))
                        seriesConvinadasGuardadas.Afegir(serie.StringID, new Llista<string>());
                    foreach (Serie serieAnidada in ((IEnumerable<Serie>)serie))
                    {
                        seriesConvinadasGuardadas[serie.StringID].Afegir(serieAnidada.Directorio.FullName + ";" + serieAnidada.Vistos().Length);

                    }

                }
                else
                {
                    seriesGuardadas.Afegir(serie.Directorio.FullName + ";" + serie.Vistos().Length);
                }

            }
            stringXml+="<CapitulosVistos>";
            foreach (var capitulo in capitulosVistos)
                stringXml += "<Capitulo>" + capitulo.Value + "</Capitulo>";
            stringXml+="</CapitulosVistos>";

            stringXml+="<Series>";
            for (int i = 0; i < seriesGuardadas.Count; i++)
            {
                campos = seriesGuardadas[i].Split(';');
                stringXml+="<Serie>" + "<Ruta>" + ParsePath(campos[0], caracteresXmlReservados) + "</Ruta>" + "<Vistos>" + campos[1] + "</Vistos>" + "</Serie>";
            }
            foreach (var serieConvinada in seriesConvinadasGuardadas)
            {
                serieConvinadaXml = "<SerieConvinada><Id>" + serieConvinada.Key + "</Id>";
                for (int i = 0; i < serieConvinada.Value.Count; i++)
                {
                    campos = serieConvinada.Value[i].Split(';');   
                    serieConvinadaXml += "<SerieAnidada>" + "<Ruta>" + ParsePath(campos[0],caracteresXmlReservados) + "</Ruta>" + "<Vistos>" + campos[1] + "</Vistos>" + "</SerieAnidada>";
                }
                serieConvinadaXml += "</SerieConvinada>";
                stringXml+=serieConvinadaXml;
            }
            stringXml+="</Series>";
            stringXml+="</SeriesLy>";
            nodoXml.LoadXml(stringXml);
            nodoXml.Normalize();
            return nodoXml.FirstChild;

        }

        public static Serie[] DameSeries(XmlNode nodoSeriesLyXml)
        {
            Llista<Serie> series = new Llista<Serie>();
            Serie serieActual;
            for (int i = 0; i < nodoSeriesLyXml.ChildNodes[0].ChildNodes.Count; i++)//cargo los capitulos vistos :D
                Serie.capitulosVistosGuardados.AfegirORemplaçar(nodoSeriesLyXml.ChildNodes[0].ChildNodes[i].InnerText, nodoSeriesLyXml.ChildNodes[0].ChildNodes[i].InnerText);
            for (int i = 0; i < nodoSeriesLyXml.ChildNodes[1].ChildNodes.Count; i++)
            {
                try
                {


                    if (nodoSeriesLyXml.ChildNodes[1].ChildNodes[i].Name == "Serie")
                    {

                        serieActual = new Serie(nodoSeriesLyXml.ChildNodes[1].ChildNodes[i], ParsePath(nodoSeriesLyXml.ChildNodes[1].ChildNodes[i].FirstChild.InnerText,caracteresXmlSustitutos));/* desescapar caracteres no soportados en el xml por los originales*/
                    }
                    else
                    {
                        serieActual = new SerieConvinada(nodoSeriesLyXml.ChildNodes[1].ChildNodes[i]);/*desescapar caracteres no soportados en el xml por los originales*/
                    }
                    series.Afegir(serieActual);
                    if (SerieNuevaCargada != null)
                        SerieNuevaCargada(serieActual);
                }
                catch { }
            }
            return series.ToTaula();
        }

        protected static string ParsePath(string pathXmlParse,string[] caracteresASustituir)
        {
            string pathSerie = pathXmlParse;
            for (int j = 0; j < caracteresASustituir.Length; j++)
                pathSerie = pathSerie.Replace(caracteresASustituir[j], caracteresReservadosXml[caracteresASustituir[j]]);
            return pathSerie;
        }

        public bool SerieCargada { get { return serieCargada; } }
    }
    public class SerieConvinada : Serie, IEnumerable<Serie>, IEnumerable<Capitulo>
    {
        string nombre;
        LlistaOrdenada<IComparable, Serie> series;
        public SerieConvinada(string nombre)
            : this()
        {
            Nombre = nombre;
        }
        public SerieConvinada()
            : base()
        {
            this.series = new LlistaOrdenada<IComparable, Serie>();
            IdMix = "IdMix:" + MiRandom.Next();
            Nombre = "";
        }

        public SerieConvinada(DirectoryInfo dir)
            : this()
        {
            dirPadre = dir;
            DirectoryInfo[] dirsSeries = dir.GetDirectories().Filtra((dirAComprobar) => { return Serie.UsarSoloCarpetasMultimedia(dirAComprobar); }).ToTaula();
            for (int i = 0; i < dirsSeries.Length; i++)
                Añadir(new Serie(dirsSeries[i], false));
        }

        public SerieConvinada(XmlNode serieXml)
            : this()
        {
            IdMix = serieXml.FirstChild.InnerText;
            for (int i = 1; i < serieXml.ChildNodes.Count; i++)
            {
                Añadir(new Serie(serieXml.ChildNodes[i], ParsePath(serieXml.ChildNodes[i].FirstChild.InnerText,caracteresXmlSustitutos)));
            }
        }
        public string Nombre
        {
            get
            {
                return ToString();
            }

            set
            {
                if (value == null) value = "";
                else if (value.Contains(Path.DirectorySeparatorChar + ""))
                    throw new Exception("No puede contener este caracter");
                nombre = value;
            }
        }
        public override string NombreSerie
        {
            get
            {
                return ToString();
            }
        }
        public override string StringID
        {
            get
            {
                return Nombre + ";" + IdMix;
            }
        }
        public override bool Empty
        {
            get
            {
                return series.Count == 0;
            }
        }
        public bool HaySeries { get { return series.Count != 0; } }

        public override bool CompruebaDisponivilidad()
        {
            bool valido = true;
            series.WhileEach((serie) => { valido = serie.Value.CompruebaDisponivilidad(); return valido; });
            return valido;
        }
        public override void QuitarNoDisponibles()
        {
            //quito las series no disponibles
            IEnumerable<Serie> seriesDisponibles = series.Filtra((serie) => { return serie.Value.CompruebaDisponivilidad(); }).ValuesToArray();
            series.Buida();
            foreach (Serie serieDisponible in seriesDisponibles)
            {
                series.Afegir(serieDisponible.Clau(), serieDisponible);
                //quito los capitulos no disponibles
                serieDisponible.QuitarNoDisponibles();
            }

        }
        public override Estado ConsultaEstado()
        {
            Estado estado;
            int numeroDeVistas = 0;
            bool sinPendientes = true;
            series.WhileEach((serie) =>
            {
                if (serie.Value.ConsultaEstado() != Estado.Pendiente)
                    numeroDeVistas++;
                else if (numeroDeVistas > 0)
                    sinPendientes = false;//ahora se que no esta acabada y esta siguiendose
                return sinPendientes;
            });
            if (numeroDeVistas == 0)
                estado = Estado.Pendiente;
            else if (numeroDeVistas < series.Count)
                estado = Estado.Siguiendo;
            else
                estado = Estado.Acabada;
            return estado;
        }
        public void Añadir(Serie serie)
        {
            if (serie is SerieConvinada)
            {
                Añadir((IEnumerable<Serie>)serie);
            }
            else
            {
                serie.IdMix = this.IdMix;
                series.Afegir(serie.Clau(), serie);
            }
        }
        public void Añadir(IEnumerable<Serie> series)
        {
            foreach (Serie serie in series)
                Añadir(serie);
        }
        public void Quitar(Serie serie)
        {
            if (series.Existeix(serie.Clau()))
            {
                serie.IdMix = "";
                series.Elimina(serie.Clau());
            }
        }
        public override bool CompruebaDireccion(string direccion)
        {
            return CompruebaDireccion(new DirectoryInfo(direccion));
        }
        public override bool CompruebaDireccion(DirectoryInfo direccion)
        {
            bool valido = true;
            series.WhileEach((serie) => { valido = serie.Value.CompruebaDireccion(direccion); return !valido; });//la primera serie quizas da false y ya sale...
            return valido;
        }

        public override void CargarCapitulos()
        {
            foreach (KeyValuePair<IComparable, Serie> serie in series)
                serie.Value.CargarCapitulos();
        }



        IEnumerator<Serie> IEnumerable<Serie>.GetEnumerator()
        {
            return series.ValuesToArray().ObtieneEnumerador();
        }
        public override IEnumerator<Capitulo> GetCapitulos()
        {
            foreach (var serie in series)
                foreach (Capitulo capitulo in serie.Value)
                    yield return capitulo;
        }
        public override IComparable Clau()
        {
            return StringID;
        }
        public override string ToString()
        {
            if (dirPadre != null)
                return base.ToString();
            else if (nombre != "")
                return nombre;
            else
                return "MixSeries";
        }
    }
}