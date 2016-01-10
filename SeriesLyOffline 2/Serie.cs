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
        static readonly string[] extensionesMultimedia = { ".avi", ".mp4", ".mkv", ".mpeg", ".wmv", ".flv", ".m2ts" };//poner mas formatos de video :D
        public static readonly LlistaOrdenada<string, string> diccionarioMultimedia;
        private static LlistaOrdenada<string, string> capitulosVistosGuardados;

        private LlistaOrdenada<IComparable, Capitulo> capitulos;
        protected DirectoryInfo dirPadre;
        string idMix;
        bool serieCargada;

        static Serie()
        {
            capitulosVistosGuardados = new LlistaOrdenada<string, string>();
            diccionarioMultimedia = new LlistaOrdenada<string, string>();
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
            if (!directorio.CanRead())
                throw new Exception("El directorio no se puede leer!!");

            if (comprovar)
                if (!UsarSoloCarpetasMultimedia(directorio))
                    throw new Exception("La carpeta no tiene ningun archivo multimedia");

            dirPadre = directorio;
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

            foreach (KeyValuePair<IComparable,Capitulo> capitulo in capitulos)
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
            foreach (KeyValuePair<IComparable,Capitulo> capitulo in capitulos)
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
            return capitulos.ValuesToArray().ObtieneEnumerador();
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
            try
            {
                if (dir.CanRead())//me interesa poder leer
                    files = dir.GetFiles(extensionesMultimedia);
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
            return dir.GetFiles(extensionesMultimedia).Length > 0;
        }
        public static XmlNode ToXml(IEnumerable<Serie> series)
        {
            text stringXml = "";
            XmlDocument nodoXml = new XmlDocument();
            LlistaOrdenada<string, string> capitulosVistos = new LlistaOrdenada<string, string>();
            Llista<string> seriesGuardadas = new Llista<string>();
            LlistaOrdenada<string, Llista<string>> seriesConvinadasGuardadas = new LlistaOrdenada<string, Llista<string>>();
            text serieConvinadaXml = "";
            int numeroVistos = 0;
            string[] campos;
            stringXml += "<SeriesLy>";
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
            stringXml += "<CapitulosVistos>";
            foreach (KeyValuePair<string,string> capitulo in capitulosVistos)
                stringXml += "<Capitulo>" + capitulo.Value + "</Capitulo>";
            stringXml += "</CapitulosVistos>";

            stringXml += "<Series>";
            for (int i = 0; i < seriesGuardadas.Count; i++)
            {
                campos = seriesGuardadas[i].Split(';');
                stringXml += "<Serie>" + "<Ruta>" + campos[0].EscaparCaracteresXML() + "</Ruta>" + "<Vistos>" + campos[1] + "</Vistos>" + "</Serie>";
            }
            foreach (KeyValuePair<string, Llista<string>> serieConvinada in seriesConvinadasGuardadas)
            {
                serieConvinadaXml = "<SerieConvinada><Id>" + serieConvinada.Key.EscaparCaracteresXML()  + "</Id>";
                for (int i = 0; i < serieConvinada.Value.Count; i++)
                {
                    campos = serieConvinada.Value[i].Split(';');
                    serieConvinadaXml += "<SerieAnidada>" + "<Ruta>" + campos[0].EscaparCaracteresXML() + "</Ruta>" + "<Vistos>" + campos[1] + "</Vistos>" + "</SerieAnidada>";
                }
                serieConvinadaXml += "</SerieConvinada>";
                stringXml += serieConvinadaXml;
            }
            stringXml += "</Series>";
            stringXml += "</SeriesLy>";
            nodoXml.LoadXml(stringXml);
            nodoXml.Normalize();
            return nodoXml.FirstChild;

        }

        public static Serie[] DameSeries(XmlNode nodoSeriesLyXml)
        {
            Llista<Serie> series = new Llista<Serie>();
            XmlNode nodosCapitulosVistos = nodoSeriesLyXml.ChildNodes[0];
            XmlNode nodosSeries = nodoSeriesLyXml.ChildNodes[1];
            XmlNode nodoSerie;
            Serie serieActual;
            for (int i = 0; i < nodosCapitulosVistos.ChildNodes.Count; i++)//cargo los capitulos vistos :D
                Serie.capitulosVistosGuardados.AfegirORemplaçar(nodosCapitulosVistos.ChildNodes[i].InnerText, nodosCapitulosVistos.ChildNodes[i].InnerText);
            for (int i = 0; i < nodosSeries.ChildNodes.Count; i++)
            {
                try
                {
                    nodoSerie = nodosSeries.ChildNodes[i];

                    if (nodoSerie.Name == "Serie")
                    {

                        serieActual = new Serie(nodoSerie, nodoSerie.FirstChild.InnerText.DescaparCaracteresXML());/* desescapar caracteres no soportados en el xml por los originales*/
                    }
                    else
                    {
                        serieActual = new SerieConvinada(nodoSerie);/*desescapar caracteres no soportados en el xml por los originales*/
                    }
                    series.Afegir(serieActual);
                    if (SerieNuevaCargada != null)
                        SerieNuevaCargada(serieActual);
                }
                catch{}
            }
            return series.ToTaula();
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
            DirectoryInfo[] dirsSeries;
            dirPadre = dir;
            dirsSeries = dir.GetDirectories().Filtra((dirAComprobar) => { return Serie.UsarSoloCarpetasMultimedia(dirAComprobar); }).ToTaula();
            for (int i = 0; i < dirsSeries.Length; i++)
                Añadir(new Serie(dirsSeries[i], false));
        }

        public SerieConvinada(XmlNode serieXml)
            : this()
        {
            string[] camposId = serieXml.FirstChild.InnerText.DescaparCaracteresXML().Split(';');
            IdMix = camposId[1];
            Nombre = camposId[0];
            for (int i = 1; i < serieXml.ChildNodes.Count; i++)
            {
                Añadir(new Serie(serieXml.ChildNodes[i], serieXml.ChildNodes[i].FirstChild.InnerText.DescaparCaracteresXML()));
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
            Serie[] seriesDisponibles = series.Filtra((serie) => { return serie.Value.CompruebaDisponivilidad(); }).ValuesToArray();
            series.Buida();
            for(int i = 0; i < seriesDisponibles.Length; i++) { 
                series.Afegir(seriesDisponibles[i].Clau(), seriesDisponibles[i]);
            //quito los capitulos no disponibles
            seriesDisponibles[i].QuitarNoDisponibles();
            }

        }
        public override Estado ConsultaEstado()
        {
            Estado estado, estadoSerie;
            int numeroDeVistas = 0,numSeries=0;
            bool seEstaSiguiendoUnaSerie = false;
            series.WhileEach((serie) =>
            {
                numSeries++;
                estadoSerie = serie.Value.ConsultaEstado();
                switch (estadoSerie)
                {
                    case Estado.Acabada:numeroDeVistas++; break;
                    case Estado.Siguiendo:seEstaSiguiendoUnaSerie = true; break;
                }
                if (numeroDeVistas>0&&numSeries != numeroDeVistas)
                    seEstaSiguiendoUnaSerie = true;
                return !seEstaSiguiendoUnaSerie;
            });
            if (seEstaSiguiendoUnaSerie)
                estado = Estado.Siguiendo;
            else if (numeroDeVistas == 0)
                estado = Estado.Pendiente;
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
        	return series.Existeix(direccion.FullName);
        }

        public override void CargarCapitulos()
        {
            foreach (KeyValuePair<IComparable, Serie> serie in series)
                serie.Value.CargarCapitulos();
        }

        protected override IEnumerable<string> GuardarVistos()
        {
            List<string> listaVistos = new List<string>();
            foreach (Capitulo capitulo in this)
                if (capitulo.Visto)
                    listaVistos.Add(capitulo.LineaGuardado);
            return listaVistos;
        }

        IEnumerator<Serie> IEnumerable<Serie>.GetEnumerator()
        {
            return series.ValuesToArray().ObtieneEnumerador();
        }
        public override IEnumerator<Capitulo> GetCapitulos()
        {
            foreach (KeyValuePair<IComparable, Serie> serie in series)
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