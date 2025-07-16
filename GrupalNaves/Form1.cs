using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq; // Necesario para .ToList() en las colecciones

namespace GrupalNaves
{
    public partial class Form1 : Form
    {
        // Singleton para acceder a la instancia del formulario desde otras clases
        private static Form1 instance;
        // Constructor estático para inicializar la instancia
        public static Form1 Instance => instance;

        private Naves naveJugador;
        private TipoAvion avionSeleccionado = TipoAvion.Avion1;
        private Movimiento gestorMovimiento;

        // Listas de elementos del juego
        private List<Bala> balasEnemigos = new List<Bala>();
        private List<Obstaculos> listaObstaculos;
        private List<Torre> torres;
        private List<Bala> balasTorreta;
        private List<Bala> balasJugador;
        private List<AvionEnemigo> enemigos;

        // Temporizadores del juego
        private System.Windows.Forms.Timer timerDisparoTorreta;
        private System.Windows.Forms.Timer timerActualizacionJuego; // Timer principal del juego
        private System.Windows.Forms.Timer timerGeneracionEnemigos;

        // Elementos visuales
        private FondoJuego fondoJuego;

        // Instancia del HUD (ahora para dibujo directo)
        private HUD hudJuego;

        // Referencia al menú principal
        private Menu mainMenu; // <--- NUEVO: Para poder volver al menú principal

        public Form1()
        {
            instance = this;
            InitializeComponent();
            Debug.WriteLine($"Tamaño real del cliente: {this.ClientSize}");

            // Configuración optimizada para reducir parpadeo
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            this.DoubleBuffered = true;
            this.ClientSize = new Size(1920, 1080);
            this.Text = "Juego de Naves Espaciales";
            this.KeyPreview = true;
            this.Resize += Form1_Resize;

            // Cargar fondo del juego
            try
            {
                fondoJuego = FondoJuego.CrearDesdeAssets("fondo.png", this.ClientSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el fondo: {ex.Message}");
                fondoJuego = null;
            }

            // Inicializar listas
            listaObstaculos = new List<Obstaculos>();
            balasJugador = new List<Bala>();
            torres = new List<Torre>();
            enemigos = new List<AvionEnemigo>();

            // Eventos del formulario
            this.Paint += DibujarElementosJuego;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_DebugKeyUp; // Mantener para depuración

            // Mostrar menú de selección de nave
            mainMenu = new Menu(); // <--- CAMBIO: Inicializar el campo mainMenu
            mainMenu.NaveSeleccionada += OnNaveSeleccionada;
            mainMenu.MostrarMenu();

            // Inicializar el HUD (solo los datos, no los controles visuales)
            hudJuego = new HUD(this, 100); // 100 es la vida inicial por defecto
        }

        // Método que se ejecuta al pintar el fondo del formulario
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            fondoJuego?.Dibujar(e.Graphics);
        }

        // Método que se ejecuta cuando el formulario cambia de tamaño
        private void Form1_Resize(object sender, EventArgs e)
        {
            fondoJuego?.CambiarTamaño(this.ClientSize);
            this.Invalidate();
        }

        private void Form1_DebugKeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Tecla presionada: {e.KeyCode}");
        }

        private void Form1_DebugKeyUp(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Tecla soltada: {e.KeyCode}");
        }

        private void OnNaveSeleccionada(TipoAvion tipo)
        {
            this.avionSeleccionado = tipo;
            InicializarJuego();

            // Preparar el formulario para recibir input
            this.Focus();
            this.ActiveControl = null;
            this.Select();
            this.Invalidate();
        }

        private void InicializarJuego()
        {
            // Inicializar torres con posiciones visibles
            torres.Clear();
            try
            {
                var torreIzquierda = new Torre(150, 150, 0.2f) { AjusteAngulo = 90f };
                torres.Add(torreIzquierda);

                var torreDerecha = new Torre(this.ClientSize.Width - 250, 150, 0.2f) { AjusteAngulo = 90f };
                torres.Add(torreDerecha);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar torres: {ex.Message}");
            }

            balasTorreta = new List<Bala>();

            // Inicializar nave jugador
            try
            {
                naveJugador = new Naves(avionSeleccionado, this)
                {
                    PosX = this.ClientSize.Width / 2,
                    PosY = this.ClientSize.Height - 200,
                    Escala = 0.6f
                };
                hudJuego?.ActualizarVida(naveJugador.Vida);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la nave: {ex.Message}");
                naveJugador = null;
                this.Close();
                return;
            }

            // Limpiar y crear obstáculos
            listaObstaculos.Clear();
            listaObstaculos.Add(new Obstaculos(50, 50, 260, 260, Color.DarkSlateGray, Color.Black, 2.0f));
            listaObstaculos.Add(new Obstaculos(350, 300, 260, 260, Color.Firebrick, Color.DarkRed, 3.0f));
            listaObstaculos.Add(new Obstaculos(700, 50, 260, 260, Color.ForestGreen, Color.DarkGreen, 2.0f));
            listaObstaculos.Add(new Obstaculos(10, 400, 260, 260, Color.Gold, Color.DarkGoldenrod, 1.0f));

            // Iniciar sistema de movimiento
            if (naveJugador != null)
            {
                gestorMovimiento = new Movimiento(naveJugador, this);
                gestorMovimiento.IniciarMovimiento();
            }

            // Configurar el temporizador para disparos de torretas
            timerDisparoTorreta = new System.Windows.Forms.Timer();
            timerDisparoTorreta.Interval = 1500; // Cada 1.5 segundos
            timerDisparoTorreta.Tick += TimerDisparoTorreta_Tick;
            timerDisparoTorreta.Start();

            // Configurar el temporizador principal del juego
            if (timerActualizacionJuego == null)
            {
                timerActualizacionJuego = new System.Windows.Forms.Timer();
                timerActualizacionJuego.Interval = 15; // Aproximadamente 60 FPS
                timerActualizacionJuego.Tick += TimerActualizacionJuego_Tick;
            }
            timerActualizacionJuego.Start();

            // *** Punto clave para la generación de enemigos ***
            // Asegurarse de que la lista de enemigos esté limpia antes de empezar a añadir nuevos
            enemigos.Clear();
            // Configurar temporizador para generación de enemigos
            if (timerGeneracionEnemigos == null) // Evitar crear múltiples instancias
            {
                timerGeneracionEnemigos = new System.Windows.Forms.Timer();
                timerGeneracionEnemigos.Interval = 5000; // Cada 5 segundos
                timerGeneracionEnemigos.Tick += TimerGeneracionEnemigos_Tick;
            }
            timerGeneracionEnemigos.Start(); // ¡Asegúrate de que este timer se inicie!
            Debug.WriteLine("Timer de generación de enemigos iniciado."); // Para depuración
        }

        public void AgregarBalaEnemigo(Bala bala)
        {
            balasEnemigos.Add(bala);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && naveJugador != null)
            {
                if (balasJugador == null)
                    balasJugador = new List<Bala>();

                float centroX = naveJugador.PosX;
                float centroY = naveJugador.PosY - (50 * naveJugador.Escala);

                Point cursorPos = this.PointToClient(Cursor.Position);
                var bala = new Bala(TipoBala.BalaAvion, centroX, centroY, null, 0.15f, cursorPos);
                balasJugador.Add(bala);
            }
        }

        private void TimerDisparoTorreta_Tick(object sender, EventArgs e)
        {
            if (torres == null || naveJugador == null) return;

            foreach (var torre in torres)
            {
                float centroX = torre.PosX + (torre.bitmapCache?.Width ?? 0) / 2f;
                float centroY = torre.PosY + (torre.bitmapCache?.Height ?? 0) / 2f;

                var bala = new Bala(TipoBala.BalaTorreta, centroX, centroY, torre.AnguloRotacion);
                balasTorreta.Add(bala);
            }
        }

        // Timer principal para toda la lógica del juego y el redibujado
        private void TimerActualizacionJuego_Tick(object sender, EventArgs e)
        {
            // Actualizar lógica de balas
            for (int i = balasTorreta.Count - 1; i >= 0; i--)
            {
                balasTorreta[i].Actualizar();
                if (balasTorreta[i].EstaFueraDePantalla(this.ClientSize))
                {
                    balasTorreta.RemoveAt(i);
                }
            }

            if (balasJugador != null)
            {
                for (int i = balasJugador.Count - 1; i >= 0; i--)
                {
                    balasJugador[i].Actualizar();
                    if (balasJugador[i].EstaFueraDePantalla(this.ClientSize))
                    {
                        balasJugador.RemoveAt(i);
                    }
                }
            }

            for (int i = balasEnemigos.Count - 1; i >= 0; i--)
            {
                balasEnemigos[i].Actualizar();
                if (balasEnemigos[i].EstaFueraDePantalla(this.ClientSize))
                {
                    balasEnemigos.RemoveAt(i);
                }
            }

            // Actualizar enemigos
            if (enemigos != null && naveJugador != null)
            {
                foreach (var enemigo in enemigos)
                {
                    enemigo.Actualizar(naveJugador);
                }
            }

            // Verificar colisiones
            VerificarColisiones();

            // Forzar el redibujado de todo el formulario una vez por tick
            this.Invalidate();
        }

        private void TimerGeneracionEnemigos_Tick(object sender, EventArgs e)
        {
            if (naveJugador == null || enemigos == null)
            {
                Debug.WriteLine("No se pueden generar enemigos: Nave Jugador o lista de enemigos es nula.");
                return;
            }

            Random rnd = new Random();
            float x = rnd.Next(100, this.ClientSize.Width - 100);
            float y = rnd.Next(50, 200);

            try
            {
                var enemigo = new AvionEnemigo(x, y, naveJugador);
                enemigos.Add(enemigo);
                Debug.WriteLine($"Enemigo generado en: X={x}, Y={y}. Total de enemigos: {enemigos.Count}"); // Para depuración
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al crear enemigo: {ex.Message}");
                // Si hay un error al cargar el asset del enemigo, no se creará
                MessageBox.Show($"Error al generar enemigo: {ex.Message}. Asegúrate de que los archivos 'enemigo_bordes.txt' y 'enemigo_coloreados.txt' estén en la ruta correcta.");
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        private void DibujarElementosJuego(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            // Dibujar elementos del juego
            // *** Asegúrate de que esta línea esté presente y sin comentarios ***
            enemigos?.ForEach(enemigo => enemigo.Dibujar(g));
            balasEnemigos.ForEach(bala => bala.Dibujar(g));

            if (torres != null)
            {
                foreach (var torre in torres)
                {
                    if (naveJugador != null)
                    {
                        float centroTorreX = torre.PosX + (torre.bitmapCache?.Width ?? 0) / 2f;
                        float centroTorreY = torre.PosY + (torre.bitmapCache?.Height ?? 0) / 2f;
                        float centroAvionX = naveJugador.PosX;
                        float centroAvionY = naveJugador.PosY;

                        float dx = centroAvionX - centroTorreX;
                        float dy = centroAvionY - centroTorreY;
                        torre.AnguloRotacion = (float)(Math.Atan2(dy, dx) * (180 / Math.PI));
                    }
                    torre.Dibujar(g);

                    using (var brush = new SolidBrush(Color.Red))
                    {
                        g.FillEllipse(brush, torre.PosX - 3, torre.PosY - 3, 6, 6);
                    }
                }
            }

            balasTorreta.ForEach(bala => bala.Dibujar(g));
            balasJugador?.ForEach(bala => bala.Dibujar(g));

            /*listaObstaculos?.ForEach(obstaculo => obstaculo.Dibujar(g));*/

            naveJugador?.Dibujar(g, naveJugador.Escala);

            hudJuego?.Dibujar(g);
        }

        private void VerificarColisiones()
        {
            if (naveJugador == null) return;

            // Colisiones con enemigos
            for (int i = enemigos.Count - 1; i >= 0; i--)
            {
                var enemigo = enemigos[i];

                // Jugador vs Enemigo
                if (naveJugador.Bounds.IntersectsWith(enemigo.Bounds))
                {
                    bool naveDestruida = naveJugador.RecibirDaño(enemigo.DañoColision);
                    enemigos.RemoveAt(i);
                    hudJuego?.ActualizarVida(naveJugador.Vida);
                    if (naveDestruida)
                    {
                        MostrarMenuGameOver(); // <--- CAMBIO: Redirigir al menú de Game Over
                        return; // Salir de este método ya que el juego ha terminado
                    }
                    continue;
                }

                // Balas Jugador vs Enemigo
                if (balasJugador != null)
                {
                    for (int j = balasJugador.Count - 1; j >= 0; j--)
                    {
                        if (enemigo.Bounds.IntersectsWith(balasJugador[j].Bounds))
                        {
                            bool enemigoDestruido = enemigo.RecibirDaño(20);
                            balasJugador.RemoveAt(j);
                            if (enemigoDestruido)
                            {
                                enemigos.RemoveAt(i);
                                hudJuego?.AgregarPuntaje(100);
                                break;
                            }
                        }
                    }
                }

                // Balas Enemigo vs Jugador
                foreach (var bala in enemigo.Balas.ToList())
                {
                    if (naveJugador.Bounds.IntersectsWith(bala.Bounds))
                    {
                        bool naveDestruida = naveJugador.RecibirDaño(10);
                        enemigo.Balas.Remove(bala);
                        hudJuego?.ActualizarVida(naveJugador.Vida);
                        if (naveDestruida)
                        {
                            MostrarMenuGameOver(); // <--- CAMBIO: Redirigir al menú de Game Over
                            return; // Salir de este método
                        }
                    }
                }
            }

            // Colisiones con balas de torreta
            for (int i = balasTorreta.Count - 1; i >= 0; i--)
            {
                if (naveJugador.Bounds.IntersectsWith(balasTorreta[i].Bounds))
                {
                    bool naveDestruida = naveJugador.RecibirDaño(10);
                    balasTorreta.RemoveAt(i);
                    hudJuego?.ActualizarVida(naveJugador.Vida);
                    if (naveDestruida)
                    {
                        MostrarMenuGameOver(); // <--- CAMBIO: Redirigir al menú de Game Over
                        return; // Salir de este método
                    }
                }
            }

            // Colisión balas jugador vs torres
            if (balasJugador != null)
            {
                for (int i = balasJugador.Count - 1; i >= 0; i--)
                {
                    for (int j = torres.Count - 1; j >= 0; j--)
                    {
                        if (balasJugador[i].Bounds.IntersectsWith(torres[j].Bounds))
                        {
                            bool torreDestruida = torres[j].RecibirDaño(20);
                            balasJugador.RemoveAt(i);
                            if (torreDestruida)
                            {
                                torres.RemoveAt(j);
                                hudJuego?.AgregarPuntaje(500);
                            }
                            break;
                        }
                    }
                }
            }

            // Colisiones con balas enemigas independientes (si las hay)
            for (int i = balasEnemigos.Count - 1; i >= 0; i--)
            {
                if (naveJugador.Bounds.IntersectsWith(balasEnemigos[i].Bounds))
                {
                    bool naveDestruida = naveJugador.RecibirDaño(10);
                    balasEnemigos.RemoveAt(i);
                    hudJuego?.ActualizarVida(naveJugador.Vida);
                    if (naveDestruida)
                    {
                        MostrarMenuGameOver(); // <--- CAMBIO: Redirigir al menú de Game Over
                        return; // Salir de este método
                    }
                }
            }

            // Colisión jugador vs torres (sin daño, solo para interacción)
            foreach (var torre in torres)
            {
                if (naveJugador.Bounds.IntersectsWith(torre.Bounds))
                {
                    // Lógica para empuje o rebote si es necesario
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            fondoJuego?.Dispose();
            gestorMovimiento?.DetenerMovimiento();

            timerActualizacionJuego?.Stop();
            timerActualizacionJuego?.Dispose();

            timerDisparoTorreta?.Stop();
            timerDisparoTorreta?.Dispose();

            timerGeneracionEnemigos?.Stop();
            timerGeneracionEnemigos?.Dispose();

            hudJuego?.Dispose();
        }

        /// <summary>
        /// Muestra el menú de Game Over y maneja las opciones del usuario.
        /// </summary>
        private void MostrarMenuGameOver()
        {
            // Detener todos los timers del juego
            timerActualizacionJuego?.Stop();
            timerDisparoTorreta?.Stop();
            timerGeneracionEnemigos?.Stop();
            gestorMovimiento?.DetenerMovimiento(); // Detener también el movimiento de la nave

            // Ocultar el formulario de juego actual
            this.Hide();

            var gameOverMenu = new MenuGameOver();

            gameOverMenu.ReiniciarJuego += () =>
            {
                // Reiniciar el juego: Limpiar listas, reiniciar nave, etc.
                ReiniciarEstadoJuego();
                this.Show(); // Mostrar Form1 de nuevo
            };

            gameOverMenu.VolverAlMenuPrincipal += () =>
            {
                this.Close(); // Cerrar el formulario de juego actual
                // Para volver al menú principal de forma limpia, lo más sencillo es reiniciar la aplicación.
                // Esto hará que Program.cs se ejecute de nuevo y muestre el Menu inicial.
                Application.Restart();
            };

            gameOverMenu.SalirDelJuego += () =>
            {
                Application.Exit(); // Cierra completamente la aplicación
            };

            gameOverMenu.MostrarMenuGameOver();
        }

        /// <summary>
        /// Restablece el estado del juego para un nuevo inicio.
        /// </summary>
        private void ReiniciarEstadoJuego()
        {
            // Limpiar todas las listas de elementos del juego
            balasEnemigos.Clear();
            listaObstaculos.Clear();
            torres.Clear();
            balasTorreta.Clear();
            balasJugador.Clear();
            enemigos.Clear();

            // Reiniciar la nave del jugador a su estado inicial
            if (naveJugador != null)
            {
                naveJugador.Vida = 100; // O la vida inicial que desees
                naveJugador.PosX = this.ClientSize.Width / 2;
                naveJugador.PosY = this.ClientSize.Height - 200;
                // Puedes también reestablecer su tipo si quieres que el jugador re-seleccione
                // this.avionSeleccionado = TipoAvion.Avion1; // O dejar el último seleccionado
            }

            // Reiniciar el HUD
            hudJuego = new HUD(this, naveJugador.Vida); // Crear una nueva instancia de HUD o resetear la existente

            // Volver a inicializar el juego (esto configura las torres, nave, timers, etc.)
            InicializarJuego();
            this.Focus();
            this.ActiveControl = null;
            this.Select();
            this.Invalidate();
        }
    }
}