using System;
using System.Collections.Generic;
using System.Drawing;// Proporciona clases para trabajar con gráficos 2D
using System.Drawing.Drawing2D; // Proporciona funciones más avanzadas para gráficos
using System.IO;// Permite manipular archivos y flujos de datos
using System.Linq;// Permite realizar consultas sobre colecciones

namespace GrupalNaves
{
    // Enum que define los tipos de balas que puede haber en el juego
    public enum TipoBala
    {
        BalaTorreta,
        BalaAvion,
        BalaEnemigo
    }

    public class Bala
    {
        // Ruta base donde están las imágenes de las balas (Assets/Balas)
        private static string BasePath = Path.GetFullPath(
            Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
                "Assets", "Balas"
            )
        );

        // Posición en X de la bala (centro)
        public float PosX { get; set; }
        // Posición en Y de la bala (centro)
        public float PosY { get; set; }
        // Velocidad de desplazamiento por cuadro (frame)
        public float Velocidad { get; set; } = 10f;
        // Ángulo de rotación de la imagen, útil para disparos rotados (e.g., torretas)
        public float AnguloRotacion { get; set; } = 0f;
        // Escala del tamaño de la imagen de la bala
        public float Escala { get; set; } = 0.15f;
        // Tipo de bala (Avión, Torreta, Enemigo)
        public TipoBala Tipo { get; private set; }

        // Constante usada para ajustar la orientación gráfica inicial de las balas
        private const float AjusteAnguloInicial = 90f;
        // Imagen renderizada de la bala
        private Bitmap bitmapCache;
        private float lastEscala = -1;

        // Rutas a los archivos de bordes y color de la bala, para construir su apariencia
        private readonly string rutaBordes;
        private readonly string rutaColoreados;

        // Direcciones de movimiento en el eje X e Y
        private float dx;
        private float dy;

        public bool Activa { get; set; } = true;

        // Rectángulo que representa el área de colisión de la bala
        public RectangleF Bounds
        {
            get
            {
                float size = 10 * Escala; // Tamaño aproximado para detección de colisiones
                return new RectangleF(PosX - size / 2, PosY - size / 2, size, size);
            }
        }

        public Bala(TipoBala tipo, float x, float y, float? angulo = null, float escala = 0.15f)
        {
            Tipo = tipo;
            PosX = x;
            PosY = y;
            Escala = escala;

            // Carpeta correspondiente al tipo de bala
            string carpeta = tipo.ToString();
            // Define las rutas a los archivos que contienen los datos visuales de la bala
            rutaBordes = Path.Combine(BasePath, carpeta, "bordes.txt");
            rutaColoreados = Path.Combine(BasePath, carpeta, "coloreados.txt");

            // Verifica que ambos archivos existan; si no, lanza una excepción
            if (!File.Exists(rutaBordes) || !File.Exists(rutaColoreados))
                throw new FileNotFoundException($"No se encontraron archivos para {tipo}");

            // Si es una bala de torreta, puede tener un ángulo personalizado
            if (tipo == TipoBala.BalaTorreta && angulo.HasValue)
            {
                // Movimiento hacia el ángulo dado
                AnguloRotacion = angulo.Value + AjusteAnguloInicial;
                float radians = (float)(Math.PI / 180 * angulo.Value);
                dx = (float)Math.Cos(radians);
                dy = (float)Math.Sin(radians);
            }
            else if (tipo == TipoBala.BalaAvion)
            {
                dx = 0f;
                dy = -1f; // Hacia arriba
            }
            else if (tipo == TipoBala.BalaEnemigo)
            {
                dx = 0f;
                dy = 1f; // Hacia abajo
            }
        }

        public void Actualizar()
        {
            PosX += dx * Velocidad;
            PosY += dy * Velocidad;
        }
        // Método para regenerar el bitmap cacheado
        public void Dibujar(Graphics g)
        {
            if (bitmapCache == null || Escala != lastEscala)
            {
                RegenerarCache();
                lastEscala = Escala;
            }

            GraphicsState estado = g.Save();
            try
            {
                g.TranslateTransform(PosX, PosY);
                if (Tipo == TipoBala.BalaTorreta)
                {
                    g.RotateTransform(AnguloRotacion); // Apunta hacia el jugador
                }
                g.DrawImage(bitmapCache, -bitmapCache.Width / 2, -bitmapCache.Height / 2);
            }
            finally
            {
                g.Restore(estado);
            }
        }
        // Método para regenerar el bitmap cacheado
        private void RegenerarCache()
        {
            var coloreados = LeerColoreados(rutaColoreados);
            var bordes = LeerBordes(rutaBordes);

            int maxX = coloreados.Max(c => c.puntos.Max(p => p.X)) + 5;
            int maxY = coloreados.Max(c => c.puntos.Max(p => p.Y)) + 5;
            int width = (int)(maxX * Escala);
            int height = (int)(maxY * Escala);

            bitmapCache?.Dispose();
            bitmapCache = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bitmapCache))
            {
                g.Clear(Color.Transparent);
                g.ScaleTransform(Escala, Escala);

                foreach (var (color, puntos) in coloreados)
                {
                    using (var b = new SolidBrush(color))
                    {
                        foreach (var p in puntos)
                        {
                            g.FillRectangle(b, p.X, p.Y, 2, 2);
                        }
                    }
                }

                foreach (var grupo in bordes)
                {
                    if (grupo.Count > 1)
                    {
                        g.DrawPolygon(Pens.Black, grupo.ToArray());
                    }
                }
            }
        }

        // Método para leer los grupos coloreados desde un archivo
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
        //MAU!
        // Método para leer los bordes desde un archivo
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

        public RectangleF ObtenerRect()
        {
            return new RectangleF(PosX - 5, PosY - 5, 10, 10);
        }
        public bool EstaFueraDePantalla(Size tamañoPantalla)
        {
            return PosX < -100 || PosX > tamañoPantalla.Width + 100 ||
                   PosY < -100 || PosY > tamañoPantalla.Height + 100;
        }

    }

}
