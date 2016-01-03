using Gabriel.Cat;
using System.IO;
using System;
using Gabriel.Cat.Extension;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Collections.Generic;

namespace SeriesLyOffline2
{
    public class Capitulo:IClauUnicaPerObjecte,IComparable,IComparable<Capitulo>
    {

        FileInfo capitulo;
        bool visto;
        string clau;

        public Capitulo(FileInfo capitulo)
        {
            this.ArchivoMultimedia = capitulo;
            if (!Serie.extensionesMultimedia.Existe(capitulo.Extension))
                throw new Exception("No es un archivo multimedia compatible!");
        }
        public Capitulo(FileInfo capitulo,bool visto):this(capitulo)
        { Visto = visto; }
        public FileInfo ArchivoMultimedia
        {
            get
            {
                return capitulo;
            }

            set
            {
                if (value == null)
                    throw new NullReferenceException();
                else if (!Serie.extensionesMultimedia.Existe(value.Extension))
                    throw new Exception("No es un archivo multimedia compatible!");
                capitulo = value;
                clau = null;
            }
        }

        public bool Visto
        {
            get
            {
                return visto;
            }

            set
            {
                visto = value;
                if (visto&&clau==null)
                    clau = Key; 
            }
        }


        public string Key
        {
            get
            {
                if (clau == null)
                {
                   clau = DameId(ArchivoMultimedia);  
                }

                return clau;
            }

        }
        public  string Nombre
        {
            get {
                return ArchivoMultimedia.Name.Split('.')[0];//ahora tengo el nombre sin extension;
            }
        }

        public string LineaGuardado { 
            get { return Key; }
        }

   

        public void Play()
        {
            ArchivoMultimedia.Abrir();
        }

        public IComparable Clau()
        {
           return ArchivoMultimedia.FullName;
        }
        public bool CampruebaDisponivilidad()
        {
            return ArchivoMultimedia.Exists;
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as Capitulo);

        }
        public int CompareTo(Capitulo other)
        {
            if (other != null)
                return Nombre.CompareTo(other.Nombre);
            else return -1;
        }

        public static string DameId(FileInfo fileInfo)
        {
            return fileInfo.IdUnicoRapido();
        }
    }
}