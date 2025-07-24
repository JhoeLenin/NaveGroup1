using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO; // Necesario para Path y Directory
using System.Linq; // Necesario para .ToList()
using System.Drawing.Drawing2D; // ¡Esta directiva es necesaria para GraphicsState!

namespace GrupalNaves
{
    public class AvionEnemigo
    {
        private static string BasePath = Path.GetFullPath(
            Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
                "Assets",
                "Enemigos" // Ajusta esta ruta si tus assets de enemigos están en otro lugar
            )
        );

        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Escala { get; set; } = 0.3f;
        public float AnguloRotacionEnemigo { get; set; } = 90f;
        public float AnguloRotacion { get; set; } = 0f;
        public int Vida { get; set; } = 50; // Vida del enemigo
        public int DañoColision { get; private set; } = 30; // Daño al chocar con el jugador

        private Naves naveJugadorObjetivo; // Referencia a la nave del jugador
        public List<Bala> Balas { get; private set; } // Balas disparadas por este enemigo

        private System.Windows.Forms.Timer timerDisparo; // Temporizador para que este enemigo dispare
        private DateTime lastShotTime; // Para controlar el tiempo del último disparo

        // Bounds para colisiones
        public RectangleF Bounds
        {
            get
            {
                // Ajusta estos valores según el tamaño real de tu enemigo
                float width = 200 * Escala; // Ejemplo: 80px de ancho
                float height = 200 * Escala; // Ejemplo: 80px de alto
                return new RectangleF(PosX - width / 2, PosY - height / 2, width, height);
            }
        }

        public AvionEnemigo(float x, float y, Naves targetNave)
        {
            PosX = x;
            PosY = y;
            this.naveJugadorObjetivo = targetNave;
            this.Balas = new List<Bala>(); // Inicializa la lista de balas del enemigo

            // Inicializar timer de disparo
            timerDisparo = new System.Windows.Forms.Timer();
            timerDisparo.Interval = new Random().Next(1500, 4000); // Dispara cada 1.5 - 4 segundos
            timerDisparo.Tick += TimerDisparo_Tick;
            timerDisparo.Start();
            lastShotTime = DateTime.Now; // Inicializa el tiempo del último disparo

            RegenerarCache(Escala); // Generar el bitmap cache al crear el enemigo
        }

        // Método para actualizar la posición y lógica del enemigo
        public void Actualizar(Naves jugador)
        {
            // Lógica de movimiento: por ejemplo, seguir al jugador
            if (jugador != null)
            {
                float velocidad = 2f; // Velocidad de movimiento del enemigo
                float dx = jugador.PosX - PosX;
                float dy = jugador.PosY - PosY;

                float distancia = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distancia > 5) // Mover solo si no está demasiado cerca
                {
                    PosX += (dx / distancia) * velocidad;
                    PosY += (dy / distancia) * velocidad;
                }

                // Calcular ángulo de rotación para que mire al jugador
                AnguloRotacion = (float)(Math.Atan2(dy, dx) * (180 / Math.PI)) - AnguloRotacionEnemigo;
            }
        }

        // Dibuja el enemigo
        public void Dibujar(Graphics g)
        {
            if (bitmapCache == null) return;

            // Declarar GraphicsState fuera del bloque try para que sea accesible en finally
            GraphicsState estadoOriginal = null;
            try
            {
                estadoOriginal = g.Save(); // Guardar el estado original del Graphics
                g.TranslateTransform(PosX, PosY);
                g.RotateTransform(AnguloRotacion);
                g.DrawImage(bitmapCache, -bitmapCache.Width / 2, -bitmapCache.Height / 2);
            }
            finally
            {
                // Solo restaurar si el estado se guardó correctamente
                if (estadoOriginal != null)
                {
                    g.Restore(estadoOriginal);
                }
            }
        }

        // Método para que el enemigo reciba daño
        public bool RecibirDaño(int cantidad)
        {
            Vida -= cantidad;
            return Vida <= 0; // Devuelve true si el enemigo ha sido destruido
        }

        // Evento de disparo del enemigo
        private void TimerDisparo_Tick(object sender, EventArgs e)
        {
            if (naveJugadorObjetivo == null) return;

            // Evitar disparos excesivamente rápidos si el timer se activa varias veces
            if ((DateTime.Now - lastShotTime).TotalMilliseconds < timerDisparo.Interval - 100) return;

            // Calcular posición de la bala (ej: desde el centro del enemigo)
            float centroX = PosX + (bitmapCache?.Width ?? 0) / 2f;
            float centroY = PosY + (bitmapCache?.Height ?? 0) / 2f;

            // Calcular ángulo hacia el jugador
            float dx = naveJugadorObjetivo.PosX - centroX;
            float dy = naveJugadorObjetivo.PosY - centroY;
            float angulo = (float)(Math.Atan2(dy, dx) * (180 / Math.PI));

            var balaEnemiga = new Bala(TipoBala.BalaEnemigo, centroX, centroY, angulo);

            // ¡Aquí es donde agregas la bala a la lista global en Form1!
            // Usa el singleton Form1.Instance para acceder al método.
            Form1.Instance.AgregarBalaEnemigo(balaEnemiga);

            lastShotTime = DateTime.Now; // Actualiza el tiempo del último disparo
        }

        // ---- Lógica de carga de sprites (similar a Naves.cs, asegurate de que exista) ----
        private Bitmap bitmapCache;
        // private float lastEscala = -1; // No es necesario si la escala es fija para el enemigo

        private void RegenerarCache(float escala)
        {
            if (bitmapCache != null)
                bitmapCache.Dispose();

            // Asume que tienes un archivo "enemigo.txt" o similar en la carpeta Assets/Enemigos
            string rutaBordes = Path.GetFullPath(
            Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
            "Assets",
            "Naves",
            "Enemigo",
            "bordes.txt"
            )
        ); // O el nombre de tu archivo de bordes de enemigo
            string rutaColoreados = Path.GetFullPath(
            Path.Combine(
            Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
            "Assets",
            "Naves",
            "Enemigo",
            "coloreados.txt"
            )
        ); // O el nombre de tu archivo de coloreados de enemigo

            if (!File.Exists(rutaBordes) || !File.Exists(rutaColoreados))
            {
                // Fallback o lanzar excepción si los archivos no existen
                // Debug.WriteLine($"Advertencia: Archivos de enemigo no encontrados en {BasePath}");
                return;
            }
            // Lee los datos de colores y bordes desde los archivos correspondientes
            var coloreados = LeerColoreados(rutaColoreados);
            var bordes = LeerBordes(rutaBordes);

            // Calcula el ancho máximo entre todos los puntos coloreados
            int maxX = coloreados.Any() ? coloreados.Max(c => c.puntos.Any() ? c.puntos.Max(p => p.X) : 0) : 0;
            // Calcula la altura máxima entre todos los puntos coloreados
            int maxY = coloreados.Any() ? coloreados.Max(c => c.puntos.Any() ? c.puntos.Max(p => p.Y) : 0) : 0;

            // Calcula el ancho del bitmap final en base al valor máximo de X y una escala
            int width = (int)(maxX * escala) + 10;
            if (width <= 0) width = 10;// Asegura que tenga al menos 10 píxeles de ancho
            // Calcula la altura del bitmap final en base al valor máximo de Y y una escala
            int height = (int)(maxY * escala) + 10;
            if (height <= 0) height = 10;// Asegura que tenga al menos 10 píxeles de alto

            // Crea un nuevo bitmap con las dimensiones calculadas
            bitmapCache = new Bitmap(width, height);
            // Dibuja sobre el bitmap usando gráficos
            using (var g = Graphics.FromImage(bitmapCache))
            {
                g.Clear(Color.Transparent);// Limpia el fondo con transparencia
                g.ScaleTransform(escala, escala);// Aplica la escala a los dibujos

                // Dibuja cada grupo de puntos con su color correspondiente
                foreach (var (color, puntos) in coloreados)
                {
                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        foreach (var p in puntos)
                        {
                            g.FillRectangle(brush, p.X, p.Y, 2, 2);// Dibuja un rectángulo de 2x2 en cada punto
                        }
                    }
                }
                // Dibuja los bordes usando líneas conectadas si hay más de un punto
                foreach (var grupo in bordes)
                {
                    if (grupo.Count > 1)
                    {
                        g.DrawPolygon(Pens.Black, grupo.ToArray());// Dibuja un polígono con los puntos del grupo
                    }
                }
            }
        }
        // Método que lee puntos coloreados desde un archivo y los agrupa por color
        private List<(Color color, List<Point> puntos)> LeerColoreados(string ruta)
        {
            var grupos = new List<(Color, List<Point>)>();
            if (!File.Exists(ruta)) return grupos; // Retorna vacío si el archivo no existe

            using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                string linea;
                while ((linea = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("//")) continue; // Ignora líneas vacías o comentarios
                    var partes = linea.Split(' ');// Divide en partes por espacio
                    var colorPart = partes[0].Split(',');// La primera parte es el color en formato R,G,B

                    // Crea el color a partir de los componentes RGB
                    var color = Color.FromArgb(
                        int.Parse(colorPart[0]),
                        int.Parse(colorPart[1]),
                        int.Parse(colorPart[2]));

                    // Convierte el resto de partes en puntos X,Y
                    var puntos = partes.Skip(1).Select(p =>
                    {
                        var coords = p.Split(',');
                        return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
                    }).ToList();
                    // Agrega el grupo de color y sus puntos
                    grupos.Add((color, puntos));
                }
            }
            return grupos;
        }
        // Método que lee los bordes desde un archivo y los agrupa en listas de puntos
        private List<List<Point>> LeerBordes(string ruta) 
        {
            var grupos = new List<List<Point>>();
            if (!File.Exists(ruta)) return grupos;// Retorna vacío si el archivo no existe

            // Lee todas las líneas del archivo
            foreach (var linea in File.ReadAllLines(ruta))
            {
                if (string.IsNullOrWhiteSpace(linea) || linea.StartsWith("//")) continue; // Ignora líneas vacías o comentarios
                var puntos = linea.Split(' ')
                                    .Select(p =>
                                    {
                                        var coords = p.Split(',');
                                        return new Point(int.Parse(coords[0]), int.Parse(coords[1]));
                                    }).ToList();
                grupos.Add(puntos); // Agrega el grupo de puntos
            }
            return grupos;
        }
    }
}