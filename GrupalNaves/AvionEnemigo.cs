using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GrupalNaves
{
    public class AvionEnemigo
    {
        private static string BasePath = Path.GetFullPath(
            Path.Combine(
                Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName,
                "Assets", "Naves", "Enemigo"
            )
        );

        private readonly string rutaBordes;
        private readonly string rutaColoreados;

        public float PosX { get; set; }
        public float PosY { get; set; }
        public float Escala { get; set; } = 0.4f;
        public float Velocidad { get; set; } = 2f;
        public int Vida { get; set; } = 50;
        public int DañoColision { get; set; } = 20;
        public bool Activo { get; set; } = true;

        // Temporizador para disparos
        private int intervaloDisparo = 2000; // 2 segundos
        private DateTime ultimoDisparo;
        public List<Bala> Balas { get; private set; }

        private Bitmap bitmapCache;
        private float lastEscala = -1;
        private float AnguloRotacion = 0f;

        public RectangleF Bounds
        {
            get
            {
                if (bitmapCache == null)
                    return RectangleF.Empty;

                float width = bitmapCache.Width;
                float height = bitmapCache.Height;

                // Como dibujas el enemigo desde el centro:
                return new RectangleF(PosX, PosY, width, height);
            }
        }
        // constructor que recibe la posición inicial del avión enemigo
        public AvionEnemigo(float posX, float posY, Naves jugador)
        {
            PosX = posX;
            PosY = posY;
            Balas = new List<Bala>();
            ultimoDisparo = DateTime.Now;

            rutaBordes = Path.Combine(BasePath, "bordes.txt");
            rutaColoreados = Path.Combine(BasePath, "coloreados.txt");

            if (!File.Exists(rutaBordes) || !File.Exists(rutaColoreados))
            {
                throw new FileNotFoundException("Archivos de avión enemigo no encontrados");
            }

            // Apuntar al jugador desde el principio
            float dx = jugador.PosX - PosX;
            float dy = jugador.PosY - PosY;
            AnguloRotacion = (float)(Math.Atan2(dy, dx) * (180 / Math.PI));
        }


        public void Actualizar(Naves jugador)
        {
            if (!Activo) return;

            // Movimiento similar a torretas (persecución suave)
            float centroEnemigoX = PosX + (bitmapCache?.Width ?? 0) / 2f * Escala;
            float centroEnemigoY = PosY + (bitmapCache?.Height ?? 0) / 2f * Escala;

            float centroJugadorX = jugador.PosX;
            float centroJugadorY = jugador.PosY;

            // Calcular dirección hacia el jugador
            float dx = centroJugadorX - centroEnemigoX;
            float dy = centroJugadorY - centroEnemigoY;
            float distancia = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distancia > 0)
            {
                dx /= distancia;
                dy /= distancia;

                // Movimiento más lento cuando está cerca
                float factorVelocidad = Math.Min(1, distancia / 200f);
                PosX += dx * Velocidad * factorVelocidad;
                PosY += dy * Velocidad * factorVelocidad;
            }

            // Calcular ángulo hacia el jugador
            float anguloObjetivo = (float)(Math.Atan2(dy, dx) * (180 / Math.PI));

            // Asegura que ambos ángulos estén entre -180 y 180
            float diferencia = ((anguloObjetivo - AnguloRotacion + 540) % 360) - 180;

            // Limita el giro máximo por frame (en grados)
            float velocidadGiro = 0.5f;

            if (Math.Abs(diferencia) < velocidadGiro)
            {
                AnguloRotacion = anguloObjetivo; // Ya casi igual
            }
            else
            {
                AnguloRotacion += Math.Sign(diferencia) * velocidadGiro;
            }


            // Disparar si es tiempo
            if ((DateTime.Now - ultimoDisparo).TotalMilliseconds >= intervaloDisparo)
            {
                Disparar(jugador);
                ultimoDisparo = DateTime.Now;
            }

            // Actualizar balas
            for (int i = Balas.Count - 1; i >= 0; i--)
            {
                Balas[i].Actualizar();
                if (Balas[i].EstaFueraDePantalla(Form1.ActiveForm?.ClientSize ?? new Size(1920, 1080)))
                {
                    Balas.RemoveAt(i);
                }
            }
        }

        private void Disparar(Naves jugador)
        {
            // Calcular ángulo hacia el jugador (como las torretas)
            float centroEnemigoX = PosX + (bitmapCache?.Width ?? 0) / 2f * Escala;
            float centroEnemigoY = PosY + (bitmapCache?.Height ?? 0) / 2f * Escala;

            float centroJugadorX = jugador.PosX;
            float centroJugadorY = jugador.PosY;

            float dx = centroJugadorX - centroEnemigoX;
            float dy = centroJugadorY - centroEnemigoY;
            float angulo = (float)(Math.Atan2(dy, dx) * (180 / Math.PI));

            // Crear bala en la posición del enemigo apuntando al jugador
            var bala = new Bala(TipoBala.BalaEnemigo, centroEnemigoX, centroEnemigoY, angulo);
            Form1.Instance?.AgregarBalaEnemigo(bala);
        }

        public bool RecibirDaño(int cantidad)
        {
            Vida -= cantidad;
            if (Vida <= 0)
            {
                Activo = false;
                return true; // Enemigo destruido
            }
            return false;
        }

        public void Dibujar(Graphics g)
        {
            if (!Activo) return;

            if (bitmapCache == null || Escala != lastEscala)
            {
                RegenerarCache();
                lastEscala = Escala;
            }

            GraphicsState estado = g.Save();
            try
            {
                // Calcular centro para rotación
                float centerX = bitmapCache.Width / 2f * Escala;
                float centerY = bitmapCache.Height / 2f * Escala;

                g.TranslateTransform(PosX + centerX, PosY + centerY);
                g.RotateTransform(AnguloRotacion - 90); // +90 para ajuste como las torretas
                g.DrawImage(bitmapCache, -centerX, -centerY);
            }
            finally
            {
                g.Restore(estado);
            }

            // Dibujar balas
            foreach (var bala in Balas)
            {
                bala.Dibujar(g);
            }
        }

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