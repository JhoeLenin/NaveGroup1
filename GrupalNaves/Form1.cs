using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;


namespace GrupalNaves
{
    public partial class Form1 : Form
    {
        private Naves naveJugador;
        private TipoAvion avionSeleccionado = TipoAvion.Avion1;
        private Movimiento gestorMovimiento;
        private List<Obstaculos> listaObstaculos;
        private List<Torre> torres;
        private List<Bala> balasTorreta;
        // Temporizador para disparos de torretas
        private System.Windows.Forms.Timer timerDisparoTorreta;
        // Temporizador para actualizar las balas
        private System.Windows.Forms.Timer timerActualizacionBalas;

        private List<Bala> balasJugador;
        // Bitmap de fondo (opcional, si se desea un fondo estático)
        private FondoJuego fondoJuego;



        public Form1()
        {
            InitializeComponent();
            Debug.WriteLine($"Tamaño real del cliente: {this.ClientSize}");

            
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
            this.KeyDown += Form1_DebugKeyDown;
            this.KeyUp += Form1_DebugKeyUp;

            // Mostrar menú de selección
            Menu menuSeleccion = new Menu();
            menuSeleccion.NaveSeleccionada += OnNaveSeleccionada;
            menuSeleccion.MostrarMenu();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            fondoJuego?.CambiarTamaño(this.ClientSize);
            this.Invalidate(); // Redibujar
        }

        private void Form1_DebugKeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Tecla presionada: {e.KeyCode}");
            // Disparar con barra espaciadora
            if (e.KeyCode == Keys.Space && naveJugador != null)
            {
                if (balasJugador == null)
                {
                    balasJugador = new List<Bala>();
                    Debug.WriteLine("Lista de balas del jugador inicializada");
                }

                float centroX = naveJugador.PosX;
                float centroY = naveJugador.PosY - (50 * naveJugador.Escala);

                Debug.WriteLine($"Creando bala en ({centroX}, {centroY})");

                var bala = new Bala(TipoBala.BalaAvion, centroX, centroY);
                balasJugador.Add(bala);

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
                naveJugador = new Naves(avionSeleccionado)
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

                var bala = new Bala(TipoBala.BalaAvion, centroX, centroY);
                balasJugador.Add(bala);
            }
        }

        // Evento para manejar el disparo de torretas
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

            // Verificar colisiones
            VerificarColisiones();

            this.Invalidate();
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

            // Dibujar torres primero
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

            // Dibujar HUD (opcional)
            if (naveJugador != null)
            {
                using (var font = new Font("Arial", 16))
                using (var brush = new SolidBrush(Color.Black))
                {
                    g.DrawString($"Vida: {naveJugador.Vida}", font, brush, 20, 20);
                }
            }
        }

        // Método para manejar el movimiento del jugador
        private void VerificarColisiones()
        {
            // Verificar colisiones entre balas de torreta y jugador
            for (int i = balasTorreta.Count - 1; i >= 0; i--)
            {
                if (naveJugador != null && balasTorreta[i].Bounds.IntersectsWith(naveJugador.Bounds))
                {
                    bool naveDestruida = naveJugador.RecibirDaño(10); // 10 de daño por bala
                    balasTorreta.RemoveAt(i);

                    if (naveDestruida)
                    {
                        // Game over
                        MessageBox.Show("¡Nave destruida!");
                        this.Close();
                    }
                }
            }

            // Verificar colisiones entre balas del jugador y torres
            if (balasJugador != null)
            {
                for (int i = balasJugador.Count - 1; i >= 0; i--)
                {
                    for (int j = torres.Count - 1; j >= 0; j--)
                    {
                        if (balasJugador[i].Bounds.IntersectsWith(torres[j].Bounds))
                        {
                            bool torreDestruida = torres[j].RecibirDaño(20); // 20 de daño por bala
                            balasJugador.RemoveAt(i);

                            if (torreDestruida)
                            {
                                torres.RemoveAt(j);
                            }
                            break; // Salir del bucle de torres
                        }
                    }
                }
            }

            // Verificar colisión entre jugador y torres (sin daño)
            if (naveJugador != null)
            {
                foreach (var torre in torres)
                {
                    if (naveJugador.Bounds.IntersectsWith(torre.Bounds))
                    {
                        // Solo empujar al jugador sin causar daño
                        // Implementa lógica de empuje si lo deseas
                    }
                }
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