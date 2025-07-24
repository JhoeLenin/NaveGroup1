using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO; // Necesario para Path y Directory
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing; // Necesario para Bitmap, Graphics, Color, Point, RectangleF

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

        private readonly Form1 formulario; // Referencia al formulario
        private readonly string rutaBordes;
        private readonly string rutaColoreados;

        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Escala { get; set; } = 1.0f;
        public float AnguloRotacion { get; set; } = 0f;
        public TipoAvion Tipo { get; private set; }

        // Propiedad de la vida de la nave (manteniendo el nombre 'Vida')
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
        // Funcion booleana para recibir daño - inicia en false
        public bool RecibirDaño(int cantidad)
        {
            Vida -= cantidad; // Resta el daño a la propiedad Vida
            return Vida <= 0; // Devuelve true si la nave fue destruida
        }

        // Constructor que recibe el tipo de avión
        public Naves(TipoAvion tipo, Form1 form)
        {
            this.formulario = form;
            Tipo = tipo;
            string carpetaAvion = tipo.ToString();

            rutaBordes = Path.Combine(BasePath, carpetaAvion, "bordes.txt");
            rutaColoreados = Path.Combine(BasePath, carpetaAvion, "coloreados.txt");

            // Validar que existan los archivos
            if (!File.Exists(rutaBordes) || !File.Exists(rutaColoreados))
            {
                throw new FileNotFoundException($"Archivos no encontrados para el avión {tipo}");
            }

            // La vida se inicializa a 100 por defecto en la declaración de la propiedad
        }

        public void Dibujar(Graphics g, float escala)
        {
            // Regenerar la escala en el cache
            if (bitmapCache == null || escala != lastEscala)
            {
                RegenerarCache(escala);
                lastEscala = escala;
            }

            GraphicsState estadoOriginal = g.Save();
            try
            {
                // Calcular ángulo hacia el cursor
                // Variable tipo cursorpos para obtener la ubicacion del cursor
                Point cursorPos = Form1.Instance.PointToClient(Cursor.Position);
                // Distancia entre la punta del cursor y el centro de la imagen (x,y)
                float dx = cursorPos.X - PosX;
                float dy = cursorPos.Y - PosY;
                // Formula matematica para calcular el angulo de rotacion par el avion
                AnguloRotacion = (float)(Math.Atan2(dy, dx) * (180 / Math.PI)) + 90; // +90 para ajuste

                g.TranslateTransform(PosX, PosY);
                g.RotateTransform(AnguloRotacion);
                g.DrawImage(bitmapCache, -bitmapCache.Width / 2, -bitmapCache.Height / 2);
            }
            finally
            {
                g.Restore(estadoOriginal);
            }
        }
        // Variable bitmap (mapa de bits) para tener una mejor carga de los puntos del avion
        private Bitmap bitmapCache;
        private float lastEscala = -1;
        // Funcion para regenerar cache de las imagenes de aviones
        private void RegenerarCache(float escala)
        {
            if (bitmapCache != null)
                bitmapCache.Dispose();

            var coloreados = LeerColoreados(rutaColoreados);
            var bordes = LeerBordes(rutaBordes);

            // Calcular tamaño necesario
            int maxX = coloreados.Any() ? coloreados.Max(c => c.puntos.Any() ? c.puntos.Max(p => p.X) : 0) : 0;
            int maxY = coloreados.Any() ? coloreados.Max(c => c.puntos.Any() ? c.puntos.Max(p => p.Y) : 0) : 0;

            int width = (int)(maxX * escala) + 10;
            if (width <= 0) width = 10; // Mínimo de 10px para evitar errores
            int height = (int)(maxY * escala) + 10;
            if (height <= 0) height = 10; // Mínimo de 10px para evitar errores

            // Instancia de bitmapcache
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

        // Método para leer los archivos de áreas coloreadas desde un archivo de texto.
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
                    // Se crea el color a partir de los valores RGB
                    var color = Color.FromArgb(
                        int.Parse(colorPart[0]),
                        int.Parse(colorPart[1]),
                        int.Parse(colorPart[2]));
                    // Se convierten los puntos del formato texto a objetos Point
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
        // Método para leer los archivos de bordes desde un archivo de texto.
        private List<List<Point>> LeerBordes(string ruta)
        {
            var grupos = new List<List<Point>>();
            foreach (var linea in File.ReadAllLines(ruta))
            {
                if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("//")) continue;
                // Se convierte cada punto de texto a un objeto Point
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