using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrupalNaves
{

    public class Naves
    {
        private static string BasePath = Path.GetFullPath(
            Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
            "Assets",
            "Naves"
            )
        );
        private readonly string rutaBordes;
        private readonly string rutaColoreados;

        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Escala { get; set; } = 1.0f;
        public float AnguloRotacion { get; set; } = 0f;
        public TipoAvion Tipo { get; private set; }

        // Propiedades de las Naves
        public int Vida { get; set; } = 100;
        public RectangleF Bounds
        {
            get
            {
                // Ajusta estos valores según el tamaño real de tu nave
                float width = 100 * Escala;
                float height = 100 * Escala;
                return new RectangleF(PosX - width / 2, PosY - height / 2, width, height);
            }
        }
        // Propiedad para determinar si la nave está activa (no destruida)
        public bool RecibirDaño(int cantidad)
        {
            Vida -= cantidad;
            return Vida <= 0; // Devuelve true si la nave fue destruida
        }

        // Constructor que recibe el tipo de avión
        public Naves(TipoAvion tipo)
        {
            Tipo = tipo;
            string carpetaAvion = tipo.ToString();

            rutaBordes = Path.Combine(BasePath, carpetaAvion, "bordes.txt");
            rutaColoreados = Path.Combine(BasePath, carpetaAvion, "coloreados.txt");

            // Validar que existan los archivos
            if (!File.Exists(rutaBordes) || !File.Exists(rutaColoreados))
            {
                throw new FileNotFoundException($"Archivos no encontrados para el avión {tipo}");
            }
        }

        public void Dibujar(Graphics g, float escala)
        {
            var coloreados = LeerColoreados(rutaColoreados);
            var bordes = LeerBordes(rutaBordes);

            // Usar un bitmap en caché para renderizado más rápido
            if (bitmapCache == null || escala != lastEscala)
            {
                RegenerarCache(escala);
                lastEscala = escala;
            }

            GraphicsState estadoOriginal = g.Save();
            try
            {
                g.TranslateTransform(PosX, PosY);
                g.RotateTransform(AnguloRotacion);
                g.DrawImage(bitmapCache, -bitmapCache.Width / 2, -bitmapCache.Height / 2);
            }
            finally
            {
                g.Restore(estadoOriginal);
            }
        }

        private Bitmap bitmapCache;
        private float lastEscala = -1;

        private void RegenerarCache(float escala)
        {
            if (bitmapCache != null)
                bitmapCache.Dispose();

            var coloreados = LeerColoreados(rutaColoreados);
            var bordes = LeerBordes(rutaBordes);

            // Calcular tamaño necesario
            int maxX = coloreados.Max(c => c.puntos.Max(p => p.X));
            int maxY = coloreados.Max(c => c.puntos.Max(p => p.Y));
            int width = (int)(maxX * escala) + 10;
            int height = (int)(maxY * escala) + 10;

            bitmapCache = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmapCache))
            {
                g.Clear(Color.Transparent);
                g.ScaleTransform(escala, escala);

                // Dibujar coloreados optimizado
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

                // Dibujar bordes optimizado
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

        private List<List<Point>> LeerBordes(string ruta)
        {
            var grupos = new List<List<Point>>();
            foreach (var linea in File.ReadAllLines(ruta))
            {
                if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("//")) continue;
                var puntos = linea.Split(' ')
                                  .Select(p =>
                                  {
                                      var coords = p.Split(',');
                                      return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
                                  }).ToList();
                grupos.Add(puntos);
            }
            return grupos;
        }
    }
}