using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

namespace GrupalNaves
{
    public class Torre
    {
        private static string BasePath = Path.GetFullPath(
            Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
                "Assets",
                "Torres",
                "Torreta"
            )
        );

        private readonly string rutaBordes;
        private readonly string rutaColoreados;

        public int PosX { get; set; }
        public int PosY { get; set; }
        public float Escala { get; set; } = 0.2f; // Por defecto, torre pequeña
        public float AnguloRotacion { get; set; } = 0f;
        public float AjusteAngulo { get; set; } = 0f;
        // Propiedades de la Torre

        public int Vida { get; set; } = 50;
        public RectangleF Bounds
        {
            get
            {
                float width = (bitmapCache?.Width ?? 100) * Escala;
                float height = (bitmapCache?.Height ?? 100) * Escala;
                return new RectangleF(PosX, PosY, width, height);
            }
        }
        // Propiedad para determinar si la torre está activa (no destruida)

        public bool RecibirDaño(int cantidad)
        {
            Vida -= cantidad;
            return Vida <= 0; // Devuelve true si la torre fue destruida
        }

        // Constructor que recibe la posición y escala de la torre
        public Torre(int x, int y, float escala = 0.2f)
        {
            PosX = x;
            PosY = y;
            Escala = escala;

            rutaBordes = Path.Combine(BasePath, "bordes.txt");
            rutaColoreados = Path.Combine(BasePath, "coloreados.txt");

            if (!File.Exists(rutaBordes) || !File.Exists(rutaColoreados))
            {
                throw new FileNotFoundException("Faltan archivos de la torre: bordes o coloreados.");
            }
        }
        public Bitmap bitmapCache { get; private set; }
        private float lastEscala = -1;

        public void Dibujar(Graphics g)
        {
            if (bitmapCache == null || Escala != lastEscala)
            {
                RegenerarCache();
                lastEscala = Escala;
            }

            GraphicsState estadoOriginal = g.Save();
            try
            {
                // Calcular el centro del bitmap cacheado
                float centerX = bitmapCache.Width / 2f;
                float centerY = bitmapCache.Height / 2f;

                // Primero mover al punto de posición (PosX, PosY)
                g.TranslateTransform(PosX, PosY);

                // Luego mover al centro de la imagen para rotar
                g.TranslateTransform(centerX, centerY);

                // Aplicar rotación
                g.RotateTransform(AnguloRotacion + AjusteAngulo);

                // Dibujar centrado
                g.DrawImage(bitmapCache, -centerX, -centerY);
            }
            finally
            {
                g.Restore(estadoOriginal);
            }
        }

        private void RegenerarCache()
        {
            if (bitmapCache != null)
                bitmapCache.Dispose();

            var coloreados = LeerColoreados(rutaColoreados);
            var bordes = LeerBordes(rutaBordes);

            // Calcular tamaño real basado en los puntos
            int maxX = coloreados.Max(c => c.puntos.Max(p => p.X)) + 5;
            int maxY = coloreados.Max(c => c.puntos.Max(p => p.Y)) + 5;

            // Aplicar escala al tamaño del bitmap
            int width = (int)(maxX * Escala);
            int height = (int)(maxY * Escala);

            bitmapCache = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmapCache))
            {
                g.Clear(Color.Transparent);
                g.ScaleTransform(Escala, Escala);

                // Dibujar coloreados
                foreach (var (color, puntos) in coloreados)
                {
                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        foreach (var p in puntos)
                        {
                            g.FillRectangle(brush, p.X, p.Y, 2, 2);
                        }
                    }
                }

                // Dibujar bordes
                foreach (var grupo in bordes)
                {
                    if (grupo.Count > 1)
                    {
                        g.DrawPolygon(Pens.Black, grupo.ToArray());
                    }
                }
            }
        }

        private List<(Color color, List<Point> puntos)> LeerColoreados(string ruta)
        {
            var grupos = new List<(Color, List<Point>)>();
            using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                string linea;
                while ((linea = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("//")) continue;
                    var partes = linea.Split(' ');
                    var colorPart = partes[0].Split(',');
                    var color = Color.FromArgb(
                        int.Parse(colorPart[0]),
                        int.Parse(colorPart[1]),
                        int.Parse(colorPart[2]));
                    var puntos = partes.Skip(1).Select(p =>
                    {
                        var coords = p.Split(',');
                        return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
                    }).ToList();
                    grupos.Add((color, puntos));
                }
            }
            return grupos;
        }
        /// funcion///
        private List<List<Point>> LeerBordes(string ruta)
        {
            // Lista principal que contendrá varios grupos de puntos
            var grupos = new List<List<Point>>();

            // Recorre cada línea del archivo ubicado en la ruta especificada
            foreach (var linea in File.ReadAllLines(ruta))
            {
                if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("//")) continue;
                // Divide la línea por espacios para obtener cada punto
                var puntos = linea.Split(' ')
                                  .Select(p =>
                                  {
                                      // Divide cada punto por coma para separar coordenadas X e Y
                                      var coords = p.Split(',');

                                      // Crea un objeto Point a partir de las coordenadas y lo retorna
                                      return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
                                  }).ToList(); // Convierte la colección a una lista
                // Agrega el grupo de puntos a la lista principal
                grupos.Add(puntos);
            }
            return grupos;
        }
    }
}
