using Gabriel.Cat.Extension;
using Gabriel.Cat.Wpf;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace SeriesLyOffline2
{
    /// <summary>
    /// Interaction logic for CapituloViewer.xaml
    /// </summary>
    public partial class CapituloViewer : UserControl,IComparable,IComparable<CapituloViewer>
    {
        Capitulo capitulo;
        public CapituloViewer()
        {
           
            //Hacer que se redimensione bien :D solo modifica la longitud de la etiqueta hay un tamaño minimo
            InitializeComponent();
            imgVisto.SetImage(Imagenes.NoVisto);
            imgPlay.SetImage(Imagenes.Play);
            //poner clic visto para cambiar a no visto
            //clic img play para abrir el archivo

        }
        public CapituloViewer(Capitulo capitulo):this()
        {
            this.Capitulo = capitulo;
        }

        public bool Visto
        {
            get { return capitulo.Visto; }
            set {

                    capitulo.Visto = value;
                    //pongo la imagen que toque
                    if (capitulo.Visto)
                        imgVisto.SetImage(Imagenes.Visto);
                    else
                        imgVisto.SetImage(Imagenes.NoVisto);
                
            }
        }
        public Capitulo Capitulo
        {
            get { return capitulo; }
            set { capitulo = value;
                Visto = capitulo.Visto;
                txtNombreSerie.Text = capitulo.Nombre;
                imgCapitulo.SetImage(capitulo.ArchivoMultimedia.Miniatura());
            }
        }

        private void imgVisto_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Visto =!Visto;

        }

        private void imgPlay_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            capitulo.Play();
            Visto = true;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as CapituloViewer);      
        }


        public int CompareTo(CapituloViewer other)
        {
            if (other != null)
                return Capitulo.CompareTo(other.Capitulo);
            else return -1;
        }
    }
}
