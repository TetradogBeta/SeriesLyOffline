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
using Gabriel.Cat.Wpf;
using Gabriel.Cat.Extension;
using Gabriel.Cat;

namespace SeriesLyOffline2
{
    /// <summary>
    /// Interaction logic for SeriesViewer.xaml
    /// </summary>
    public partial class SeriesViewer : UserControl,IEnumerable<CapituloViewer>
    {
        Serie serieAVisualizar;
        Pila<CapituloViewer> capitulosCargados;
        public SeriesViewer()
        {
            InitializeComponent();
            capitulosCargados = new Pila<CapituloViewer>();
        }
        public UIElementCollection ColeccionControles
        {
            get { return stkCapitulos.Children; }
        }
        public Serie SerieAVisualizar
        {
            get
            {
                return serieAVisualizar;
            }

            set
            {
                CapituloViewer capitulo = null,capituloNoVisto=null;
                serieAVisualizar = value;
                gbCapitulosVisualizados.Header = SerieAVisualizar.NombreSerie;
                capitulosCargados.Push(stkCapitulos.Children.OfType<CapituloViewer>());
                stkCapitulos.Children.Clear();
                foreach(Capitulo capituloHaPoner in serieAVisualizar) {
                    if (capitulosCargados.Count > 0)//si ya hay los reutilizo
                    {
                        capitulo = capitulosCargados.Pop();
                        capitulo.Capitulo = capituloHaPoner;

                    }
                    else
                    {//si no hay los creo y añado
                        capitulo = new CapituloViewer(capituloHaPoner);//asi hago el minimo de news de controles :D
                        capitulo.Width = Width;
                        capitulo.Height = Height / 10;

                    }
                    stkCapitulos.Children.Add(capitulo);
                    if (!capitulo.Visto && capituloNoVisto != null)
                        capituloNoVisto = capitulo;
                }
                
              //  if(capitulo!=null)
               // stkCapitulos.HeightItem(capitulo);//en teoria lo posiciona...
                
            }
        }
        
        public IEnumerator<CapituloViewer> GetEnumerator()
        {
            return stkCapitulos.Children.OfType<CapituloViewer>().ObtieneEnumerador();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
