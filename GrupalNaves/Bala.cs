using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GrupalNaves
{
    public enum TipoBala
    {
        BalaTorreta,
        BalaAvion,
        BalaEnemigo
    }

    public class Bala
    {
        // Ruta base mejorada que funciona en cualquier entorno
        private static string BasePath = GetAssetsPath();
        // Cache global para evitar recargar imágenes repetidamente
        private static Dictionary<TipoBala, Bitmap> cacheGlobal = new Dictionary<TipoBala, Bitmap>();

        // Método para obtener la ruta correcta a los assets
        private static string GetAssetsPath()
        {
            try
            {
                // Primero intentamos con la ruta de desarrollo (tu ruta local)
                string devPath = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "..", "..", "..", "Assets", "Balas");

                if (Directory.Exists(devPath))
                    return devPath;

                // Si no existe, probamos con la ruta de ejecución (bin/Debug)
                string execPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Assets", "Balas");

                if (Directory.Exists(execPath))
                    return execPath;

                // Si no existe, probamos con la ruta de publicación (bin/Release)
                string releasePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "..", "Assets", "Balas");

                if (Directory.Exists(releasePath))
                    return releasePath;

                // Último recurso: ruta absoluta (solo para debug)
                string absolutePath = @"C:\Users\PC\source\repos\NaveGroup1\GrupalNaves\Assets\Balas";
                if (Directory.Exists(absolutePath))
                {
                    Debug.WriteLine("ADVERTENCIA: Usando ruta absoluta - solo para desarrollo");
                    return absolutePath;
                }

                throw new DirectoryNotFoundException("No se pudo encontrar el directorio de assets");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al determinar ruta de assets: {ex.Message}");
                throw;
            }
        }

        // Resto de la clase Bala (igual que antes)
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Velocidad { get; set; } = 10f;
        public float AnguloRotacion { get; set; } = 0f;
        public float Escala { get; set; } = 0.15f;
        public TipoBala Tipo { get; private set; }
        public bool Activa { get; set; } = true;

        private Bitmap bitmapCache;
        private float lastEscala = -1;
        private readonly string rutaBordes;
        private readonly string rutaColoreados;
        private float dx;
        private float dy;

        public RectangleF Bounds
        {
            get
            {
                // Devuelve el área ocupada por la bala para propósitos de colisión
                float size = 10 * Escala;
                return new RectangleF(PosX - size / 2, PosY - size / 2, size, size);
            }
        }

        public Bala(TipoBala tipo, float x, float y, float? angulo = null, float escala = 0.15f, PointF? cursorPos = null)
        {
            Tipo = tipo;
            PosX = x;
            PosY = y;
            Escala = escala;

            // Configuración de rutas a archivos de recursos de la bala (bordes y colores)
            string carpeta = tipo.ToString();
            rutaBordes = Path.Combine(BasePath, carpeta, "bordes.txt");
            rutaColoreados = Path.Combine(BasePath, carpeta, "coloreados.txt");

            // Mensajes de depuración para verificar las rutas
            Debug.WriteLine($"Intentando cargar balas desde: {BasePath}");
            Debug.WriteLine($"Ruta bordes: {rutaBordes}");
            Debug.WriteLine($"Ruta colores: {rutaColoreados}");

            // Validación de existencia de archivos
            if (!File.Exists(rutaBordes) || !File.Exists(rutaColoreados))
            {
                string errorMsg = $"Archivos de bala no encontrados. Buscados en:\n" +
                                $"Bordes: {rutaBordes}\n" +
                                $"Colores: {rutaColoreados}\n" +
                                $"BasePath: {BasePath}\n" +
                                $"Directorio existe: {Directory.Exists(BasePath)}";

                Debug.WriteLine(errorMsg);
                throw new FileNotFoundException(errorMsg);
            }
            // Configura la dirección en la que se moverá la bala
            ConfigurarDireccionMovimiento(angulo, cursorPos);
        }

        private void VerificarArchivosBalas()
        {
            try
            {
                // Verifica si el directorio base y los archivos requeridos existen
                if (!Directory.Exists(BasePath))
                    throw new DirectoryNotFoundException($"Directorio de balas no encontrado: {BasePath}");

                if (!File.Exists(rutaBordes))
                    throw new FileNotFoundException($"Archivo de bordes no encontrado: {rutaBordes}");

                if (!File.Exists(rutaColoreados))
                    throw new FileNotFoundException($"Archivo de colores no encontrado: {rutaColoreados}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al cargar recursos de bala: {ex.Message}");
                // Si falla, se crea una representación visual básica de la bala
                CrearBitmapDeFallback();
                throw; // Relanzar la excepción para manejo superior
            }
        }

        private void CrearBitmapDeFallback()
        {
            // Crea una imagen básica de bala (círculo rojo) si fallan los archivos
            bitmapCache = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(bitmapCache))
            {
                g.Clear(Color.Transparent);
                g.FillEllipse(Brushes.Red, 0, 0, 20, 20);
            }
        }

        private void ConfigurarDireccionMovimiento(float? angulo, PointF? cursorPos)
        {
            if (Tipo == TipoBala.BalaAvion && cursorPos.HasValue)
            {
                // Calcula la dirección de movimiento hacia el cursor
                float deltaX = cursorPos.Value.X - PosX;
                float deltaY = cursorPos.Value.Y - PosY;

                // Normalizar vector
                float length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (length > 0)
                {
                    dx = deltaX / length;
                    dy = deltaY / length;
                }
                else
                {
                    dx = 0;
                    dy = -1;// Dirección por defecto (hacia arriba)
                }

                // Ángulo en grados (convertido de radianes)
                AnguloRotacion = (float)(Math.Atan2(dy, dx) * (180 / Math.PI));
            }
            else if ((Tipo == TipoBala.BalaTorreta || Tipo == TipoBala.BalaEnemigo) && angulo.HasValue)
            {
                AnguloRotacion = angulo.Value;
                float radians = (float)(Math.PI / 180 * angulo.Value);
                dx = (float)Math.Cos(radians);
                dy = (float)Math.Sin(radians);
            }
            else if (Tipo == TipoBala.BalaAvion) // Disparo recto hacia arriba por defecto
            {
                dx = 0f;
                dy = -1f;
                AnguloRotacion = -90; // Apuntando hacia arriba
            }
        }

        public void Actualizar()
        {
            PosX += dx * Velocidad;
            PosY += dy * Velocidad;
        }

        public void Dibujar(Graphics g)
        {
            if (bitmapCache == null)
            {
                if (!cacheGlobal.TryGetValue(Tipo, out bitmapCache))
                {
                    RegenerarCache(); // genera una vez por tipo y escala fija
                    cacheGlobal[Tipo] = bitmapCache; // ¡NO clones!
                }
            }

            GraphicsState estado = g.Save();
            try
            {
                g.TranslateTransform(PosX, PosY);
                g.RotateTransform(AnguloRotacion + 90); // depende de cómo está orientada la imagen
                g.DrawImage(bitmapCache, -bitmapCache.Width / 2, -bitmapCache.Height / 2);
            }
            finally
            {
                g.Restore(estado);
            }
        }

        private void RegenerarCache()
        {
            try
            {
                // Lee los datos de color y puntos desde un archivo.
                var coloreados = LeerColoreados(rutaColoreados);
                // Lee los bordes desde otro archivo.
                var bordes = LeerBordes(rutaBordes);

                // Calcula el ancho y alto máximos considerando una escala.
                int maxX = coloreados.Max(c => c.puntos.Max(p => p.X)) + 5;
                int maxY = coloreados.Max(c => c.puntos.Max(p => p.Y)) + 5;
                int width = (int)(maxX * Escala);
                int height = (int)(maxY * Escala);

                // Libera la memoria de un posible bitmap anterior.
                bitmapCache?.Dispose();
                // Crea un nuevo bitmap con las dimensiones calculadas.
                bitmapCache = new Bitmap(width, height);

                // Dibuja en el nuevo bitmap.
                using (var g = Graphics.FromImage(bitmapCache))
                {
                    // Limpia con fondo transparente.
                    g.Clear(Color.Transparent);
                    // Aplica la escala de dibujo.
                    g.ScaleTransform(Escala, Escala);

                    // Dibuja los puntos coloreados.
                    foreach (var (color, puntos) in coloreados)
                    {
                        using (var b = new SolidBrush(color))
                        {
                            foreach (var p in puntos)
                            {
                                g.FillRectangle(b, p.X, p.Y, 2, 2);// Dibuja pequeños cuadrados.
                            }
                        }
                    }

                    // Dibuja los bordes si tienen más de un punto.
                    foreach (var grupo in bordes)
                    {
                        if (grupo.Count > 1)
                        {
                            g.DrawPolygon(Pens.Black, grupo.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Muestra un mensaje de error y genera un bitmap alternativo.
                Debug.WriteLine($"Error al regenerar cache de bala: {ex.Message}");
                CrearBitmapDeFallback();
            }
        }

        private List<(Color color, List<Point> puntos)> LeerColoreados(string ruta)
        {
            var grupos = new List<(Color, List<Point>)>();
            try
            {
                // Abre el archivo en modo lectura compartida.
                using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    string linea;
                    while ((linea = sr.ReadLine()) != null)
                    {
                        // Ignora líneas vacías o comentarios.
                        if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("//")) continue;

                        // Obtiene el color y los puntos de la línea.
                        var partes = linea.Split(' ');
                        var colorPart = partes[0].Split(',');
                        var color = Color.FromArgb(
                            int.Parse(colorPart[0]),
                            int.Parse(colorPart[1]),
                            int.Parse(colorPart[2]));

                        // Convierte los datos de puntos a una lista de coordenadas.
                        var puntos = partes.Skip(1).Select(p =>
                        {
                            var coords = p.Split(',');
                            return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
                        }).ToList();
                        // Agrega el grupo leído.
                        grupos.Add((color, puntos));
                    }
                }
            }
            catch (Exception ex)
            {
                // Informa el error al leer y relanza la excepción.
                Debug.WriteLine($"Error al leer coloreados: {ex.Message}");
                throw;
            }
            return grupos;
        }

        private List<List<Point>> LeerBordes(string ruta)
        {
            var grupos = new List<List<Point>>();
            try
            {
                // Lee todas las líneas del archivo.
                foreach (var linea in File.ReadAllLines(ruta))
                {
                    // Ignora líneas vacías o comentarios.
                    if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("//")) continue;

                    // Convierte los datos a una lista de puntos.
                    var puntos = linea.Split(' ')
                                    .Select(p =>
                                    {
                                        var coords = p.Split(',');
                                        return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
                                    }).ToList();

                    // Agrega el grupo de puntos.
                    grupos.Add(puntos);
                }
            }
            catch (Exception ex)
            {
                // Informa el error al leer y relanza la excepción.
                Debug.WriteLine($"Error al leer bordes: {ex.Message}");
                throw;
            }
            return grupos;
        }

        public bool EstaFueraDePantalla(Size tamañoPantalla)
        {
            // Verifica si la bala está fuera del área visible con un margen de 100px.
            return PosX < -100 || PosX > tamañoPantalla.Width + 100 ||
                   PosY < -100 || PosY > tamañoPantalla.Height + 100;
        }
    }
}