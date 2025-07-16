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

        public bool RecibirDaño(int cantidad)
        {
            Vida -= cantidad; // Resta el daño a la propiedad Vida
            return Vida <= 0; // Devuelve true si la nave fue destruida
        }

        // Constructor que recibe el tipo de avión
        public Naves(TipoAvion tipo, Form1 form) // Cambiado a Form1 en lugar de Form
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
            // var coloreados = LeerColoreados(rutaColoreados); // No necesitas leer en cada dibujo
            // var bordes = LeerBordes(rutaBordes); // No necesitas leer en cada dibujo

            if (bitmapCache == null || escala != lastEscala)
            {
                RegenerarCache(escala);
                lastEscala = escala;
            }

            GraphicsState estadoOriginal = g.Save();
            try
            {
                // Calcular ángulo hacia el cursor
                Point cursorPos = Form1.Instance.PointToClient(Cursor.Position);
                float dx = cursorPos.X - PosX;
                float dy = cursorPos.Y - PosY;
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

        private Bitmap bitmapCache;
        private float lastEscala = -1;

        private void RegenerarCache(float escala)
        {
            if (bitmapCache != null)
                bitmapCache.Dispose();

            var coloreados = LeerColoreados(rutaColoreados);
            var bordes = LeerBordes(rutaBordes);

            // Calcular tamaño necesario
            // Asegurarse de que las listas no estén vacías para evitar errores de Max()
            int maxX = coloreados.Any() ? coloreados.Max(c => c.puntos.Any() ? c.puntos.Max(p => p.X) : 0) : 0;
            int maxY = coloreados.Any() ? coloreados.Max(c => c.puntos.Any() ? c.puntos.Max(p => p.Y) : 0) : 0;

            // Asegurarse de que el tamaño mínimo sea razonable para evitar Bitmaps de 0x0
            int width = (int)(maxX * escala) + 10;
            if (width <= 0) width = 10; // Mínimo de 10px para evitar errores
            int height = (int)(maxY * escala) + 10;
            if (height <= 0) height = 10; // Mínimo de 10px para evitar errores


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