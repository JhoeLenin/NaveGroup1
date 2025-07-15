using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;


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
        // Lista de balas disparadas por el jugador (asi no desaparecen junto con el avion que las instancia)
        private List<Bala> balasEnemigos = new List<Bala>();
        // Lista de obstáculos
        private List<Obstaculos> listaObstaculos;
        // Lista de torres (torretas)
        private List<Torre> torres;
        // Lista de balas disparadas por las torretas
        private List<Bala> balasTorreta;
        // Temporizador para disparos de torretas
        private System.Windows.Forms.Timer timerDisparoTorreta;
        // Temporizador para actualizar las balas
        private System.Windows.Forms.Timer timerActualizacionBalas;

        private List<Bala> balasJugador;
        // Bitmap de fondo (opcional, si se desea un fondo estático)
        private FondoJuego fondoJuego;

        // Lista de enemigos (aviones enemigos)
        private List<AvionEnemigo> enemigos;
        private System.Windows.Forms.Timer timerGeneracionEnemigos;

        // Propiedad para acceder a la lista de balas del jugador
        public void AgregarBalaEnemigo(Bala bala)
        {
            balasEnemigos.Add(bala);
        }

        public Form1()
        {
            instance = this; // ¡Asigna la instancia estática aquí!
            InitializeComponent();
            Debug.WriteLine($"Tamaño real del cliente: {this.ClientSize}");

            // Configuración optimizada para reducir parpadeo
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            // Configuración de la ventana
            this.DoubleBuffered = true;
            this.ClientSize = new Size(1920, 1080);
            this.Text = "Juego de Naves Espaciales";
            this.KeyPreview = true;
            this.Resize += Form1_Resize; // Nuevo evento para redimensionamiento

            // Cargar fondo usando la nueva clase
            try
            {
                fondoJuego = FondoJuego.CrearDesdeAssets("fondo.png", this.ClientSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el fondo: {ex.Message}");
                fondoJuego = null;
            }

            // Inicializar componentes
            listaObstaculos = new List<Obstaculos>();
            balasJugador = new List<Bala>();

            // Eventos
            this.Paint += DibujarElementosJuego;
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_DebugKeyUp;

            // Mostrar menú de selección
            Menu menuSeleccion = new Menu();
            menuSeleccion.NaveSeleccionada += OnNaveSeleccionada;
            menuSeleccion.MostrarMenu();
        }

        // Método que se ejecuta al pintar el formulario
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Solo dibujamos el fondo, no llamamos al método base para evitar parpadeo
            fondoJuego?.Dibujar(e.Graphics);
        }

        // Método que se ejecuta cuando el formulario cambia de tamaño
        private void Form1_Resize(object sender, EventArgs e)
        {
            fondoJuego?.CambiarTamaño(this.ClientSize);
            this.Invalidate(); // Redibujar
        }

        // Método que se ejecuta cuando se presiona una tecla mientras el formulario tiene el foco
        private void Form1_DebugKeyDown(object sender, KeyEventArgs e)
        {
            // Imprime en la consola de depuración qué tecla fue presionada
            Debug.WriteLine($"Tecla presionada: {e.KeyCode}");
            // Verifica si la tecla presionada es la barra espaciadora y si la nave del jugador existe
            if (e.KeyCode == Keys.Space && naveJugador != null)
            {
                // Si la lista de balas del jugador aún no fue creada, la inicializa
                if (balasJugador == null)
                {
                    balasJugador = new List<Bala>();
                    Debug.WriteLine("Lista de balas del jugador inicializada");
                }

                // Calcula la posición inicial de la bala: centrada en la nave y un poco más arriba (para simular que sale del frente)
                float centroX = naveJugador.PosX;
                float centroY = naveJugador.PosY - (50 * naveJugador.Escala);

                Debug.WriteLine($"Creando bala en ({centroX}, {centroY})");
                // Obtener posición actual del cursor
                Point cursorPos = this.PointToClient(Cursor.Position);
                // Crea una nueva bala de tipo 'BalaAvion' en la posición calculada
                var bala = new Bala(TipoBala.BalaAvion, centroX, centroY);
                // Agrega la nueva bala a la lista de balas activas del jugador
                balasJugador.Add(bala);

                // Imprime en la consola de depuración cuántas balas hay en la lista actualmente
                Debug.WriteLine($"Balas del jugador: {balasJugador.Count}");
            }
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
            torres = new List<Torre>();
            try
            {
                // Torre izquierda
                var torreIzquierda = new Torre(150, 150, 0.2f)
                {
                    AjusteAngulo = 90f // Ajuste para que mire hacia la derecha inicialmente
                };
                torres.Add(torreIzquierda);

                // Torre derecha
                var torreDerecha = new Torre(this.ClientSize.Width - 250, 150, 0.2f)
                {
                    AjusteAngulo = 90f // Ajuste para que mire hacia la izquierda inicialmente
                };
                torres.Add(torreDerecha);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar torres: {ex.Message}");
            }

            // Inicializar balas de torreta
            balasTorreta = new List<Bala>();

            // Inicializar nave jugador
            try
            {
                naveJugador = new Naves(avionSeleccionado, this)
                {
                    PosX = this.ClientSize.Width / 2,
                    PosY = this.ClientSize.Height - 200, // 200px desde abajo
                    Escala = 0.5f
                };
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
            timerDisparoTorreta.Interval = 1000; // Cada 1 segundo
            timerDisparoTorreta.Tick += TimerDisparoTorreta_Tick;
            timerDisparoTorreta.Start();

            // Configurar el temporizador para actualizar las balas
            timerActualizacionBalas = new System.Windows.Forms.Timer();
            timerActualizacionBalas.Interval = 33; // ≈ 60 FPS
            timerActualizacionBalas.Tick += TimerActualizacionBalas_Tick;
            timerActualizacionBalas.Start();

            // Inicializar enemigos
            enemigos = new List<AvionEnemigo>();

            // Configurar temporizador para generación de enemigos
            timerGeneracionEnemigos = new System.Windows.Forms.Timer();
            timerGeneracionEnemigos.Interval = 5000; // Cada 5 segundos
            timerGeneracionEnemigos.Tick += TimerGeneracionEnemigos_Tick;
            timerGeneracionEnemigos.Start();
        }

        // Evento para manejar el disparo del jugador
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Tecla presionada: {e.KeyCode}");

            // Disparar con barra espaciadora
            if (e.KeyCode == Keys.Space && naveJugador != null)
            {
                if (balasJugador == null)
                    balasJugador = new List<Bala>();

                // Crear bala desde el centro de la nave jugador
                float centroX = naveJugador.PosX;
                float centroY = naveJugador.PosY - (50 * naveJugador.Escala); // Disparar desde el frente

                // Obtener posición actual del cursor
                Point cursorPos = this.PointToClient(Cursor.Position);

                // Crear bala que va hacia el cursor
                var bala = new Bala(TipoBala.BalaAvion, centroX, centroY, null, 0.15f, cursorPos);
                balasJugador.Add(bala);
            }
        }

        // Evento para manejar el disparo de las torretas
        private void TimerDisparoTorreta_Tick(object sender, EventArgs e)
        {
            if (torres == null || naveJugador == null) return;

            foreach (var torre in torres)
            {
                // Posición desde el centro de la torre
                float centroX = torre.PosX + (torre.bitmapCache?.Width ?? 0) / 2f;
                float centroY = torre.PosY + (torre.bitmapCache?.Height ?? 0) / 2f;

                var bala = new Bala(TipoBala.BalaTorreta, centroX, centroY, torre.AnguloRotacion);
                balasTorreta.Add(bala);
            }

            this.Invalidate(); // Redibujar
        }

        // Evento para actualizar las balas de torreta
        private void TimerActualizacionBalas_Tick(object sender, EventArgs e)
        {
            // Actualizar balas de torreta
            for (int i = balasTorreta.Count - 1; i >= 0; i--)
            {
                balasTorreta[i].Actualizar();
                if (balasTorreta[i].EstaFueraDePantalla(this.ClientSize))
                {
                    balasTorreta.RemoveAt(i);
                }
            }

            // Actualizar balas del jugador (si existen)
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

            // Actualizar balas de enemigos
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

            // Solo llamamos a VerificarColisiones, no a Invalidate
            VerificarColisiones();
        }

        // Evento para generar enemigos periódicamente
        private void TimerGeneracionEnemigos_Tick(object sender, EventArgs e)
        {
            if (naveJugador == null || enemigos == null) return;

            // Generar enemigo en posición aleatoria en la parte superior
            Random rnd = new Random();
            float x = rnd.Next(100, this.ClientSize.Width - 100);
            float y = rnd.Next(50, 200);

            try
            {
                var enemigo = new AvionEnemigo(x, y, naveJugador);
                enemigos.Add(enemigo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al crear enemigo: {ex.Message}");
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.Invalidate(); // Redibujar para actualizar rotación de la nave
        }

        private void DibujarElementosJuego(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);

            // Dibujar fondo del juego
            fondoJuego?.Dibujar(g);

            // Configuración óptima de renderizado
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            // Dibujar enemigos primero
            if (enemigos != null)
            {
                foreach (var enemigo in enemigos)
                {
                    enemigo.Dibujar(g);
                }
            }

            // Dibujar balas de enemigos
            foreach (var bala in balasEnemigos)
            {
                bala.Dibujar(g);
            }

            // Dibujar torres
            if (torres != null)
            {
                foreach (var torre in torres)
                {
                    // Solo calcular rotación si hay nave jugador
                    if (naveJugador != null)
                    {
                        // Calcular ángulo desde el centro de la torre al centro del avión
                        float centroTorreX = torre.PosX + (torre.bitmapCache?.Width ?? 0) / 2f;
                        float centroTorreY = torre.PosY + (torre.bitmapCache?.Height ?? 0) / 2f;
                        float centroAvionX = naveJugador.PosX;
                        float centroAvionY = naveJugador.PosY;

                        float dx = centroAvionX - centroTorreX;
                        float dy = centroAvionY - centroTorreY;

                        torre.AnguloRotacion = (float)(Math.Atan2(dy, dx) * (180 / Math.PI));
                    }

                    // Dibujar torre con rotación actual
                    torre.Dibujar(g);

                    // Dibujar punto de referencia para debug (opcional)
                    using (var brush = new SolidBrush(Color.Red))
                    {
                        g.FillEllipse(brush, torre.PosX - 3, torre.PosY - 3, 6, 6);
                    }
                }
            }

            // Dibujar balas de torreta
            if (balasTorreta != null)
            {
                foreach (var bala in balasTorreta)
                {
                    bala.Dibujar(g);
                }
            }

            // Dibujar balas del jugador
            if (balasJugador != null)
            {
                foreach (var bala in balasJugador)
                {
                    bala.Dibujar(g);
                }
            }

            // Dibujar obstáculos
            /*foreach (var obstaculo in listaObstaculos)
            {
                obstaculo.Dibujar(g);
            }*/

            // Dibujar nave
            naveJugador?.Dibujar(g, naveJugador.Escala);
        }

        // Método para manejar el movimiento del jugador
        private void VerificarColisiones()
        {
            bool necesitaRedibujar = false;

            // Verificar colisiones con enemigos
            if (enemigos != null && naveJugador != null)
            {
                for (int i = enemigos.Count - 1; i >= 0; i--)
                {
                    var enemigo = enemigos[i];

                    // Colisión entre jugador y enemigo
                    if (naveJugador.Bounds.IntersectsWith(enemigo.Bounds))
                    {
                        bool naveDestruida = naveJugador.RecibirDaño(enemigo.DañoColision);
                        enemigos.RemoveAt(i);
                        necesitaRedibujar = true;

                        if (naveDestruida)
                        {
                            MessageBox.Show("¡Nave destruida!");
                            this.Close();
                        }
                        continue;
                    }

                    // Colisión entre balas del jugador y enemigos
                    if (balasJugador != null)
                    {
                        for (int j = balasJugador.Count - 1; j >= 0; j--)
                        {
                            if (enemigo.Bounds.IntersectsWith(balasJugador[j].Bounds))
                            {
                                bool enemigoDestruido = enemigo.RecibirDaño(20);
                                balasJugador.RemoveAt(j);
                                necesitaRedibujar = true;

                                if (enemigoDestruido)
                                {
                                    enemigos.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }

                    // Colisión entre balas de enemigos y jugador
                    foreach (var bala in enemigo.Balas.ToList())
                    {
                        if (naveJugador.Bounds.IntersectsWith(bala.Bounds))
                        {
                            bool naveDestruida = naveJugador.RecibirDaño(10);
                            enemigo.Balas.Remove(bala);
                            necesitaRedibujar = true;

                            if (naveDestruida)
                            {
                                MessageBox.Show("¡Nave destruida!");
                                this.Close();
                            }
                        }
                    }
                }
            }

            // Colisiones con balas de torreta
            for (int i = balasTorreta.Count - 1; i >= 0; i--)
            {
                if (naveJugador != null && balasTorreta[i].Bounds.IntersectsWith(naveJugador.Bounds))
                {
                    bool naveDestruida = naveJugador.RecibirDaño(10);
                    balasTorreta.RemoveAt(i);

                    if (naveDestruida)
                    {
                        MessageBox.Show("¡Nave destruida!");
                        this.Close();
                    }
                }
            }

            // Colisión entre balas del jugador y torres
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
                            }
                            break;
                        }
                    }
                }
            }

            // Colisiones con balas enemigas independientes
            for (int i = balasEnemigos.Count - 1; i >= 0; i--)
            {
                if (naveJugador != null && balasEnemigos[i].Bounds.IntersectsWith(naveJugador.Bounds))
                {
                    bool naveDestruida = naveJugador.RecibirDaño(10);
                    balasEnemigos.RemoveAt(i);

                    if (naveDestruida)
                    {
                        MessageBox.Show("¡Nave destruida!");
                        this.Close();
                    }
                }
            }

            // Colisión jugador vs torres (sin daño)
            if (naveJugador != null)
            {
                foreach (var torre in torres)
                {
                    if (naveJugador.Bounds.IntersectsWith(torre.Bounds))
                    {
                        // Solo rebote o empuje
                    }
                }
            }

            // Solo redibujar si hubo cambios visibles
            if (necesitaRedibujar)
            {
                this.Invalidate();
            }
        }

        // Método para manejar el movimiento del jugador
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            // Liberar recursos
            fondoJuego?.Dispose();
            gestorMovimiento?.DetenerMovimiento();
        }
    }
}