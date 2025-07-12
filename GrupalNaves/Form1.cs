using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics; // Necesario para Debug.WriteLine

namespace GrupalNaves
{
    public partial class Form1 : Form
    {
        private Naves naveJugador;
        private TipoAvion avionSeleccionado = TipoAvion.Avion1; // Valor inicial por defecto
        private Movimiento gestorMovimiento;
        private List<Obstaculos> listaObstaculos;

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true; // Habilita el doble búfer para evitar el parpadeo al dibujar
            this.ClientSize = new Size(1080, 720); // Establece el tamaño de la ventana del juego
            this.Text = "Juego de Naves Espaciales"; // Título de la ventana

            listaObstaculos = new List<Obstaculos>(); // Inicializa la lista de obstáculos

            this.Paint += DibujarElementosJuego; // Asocia el método de dibujo al evento Paint del formulario
            this.KeyPreview = true; // Esencial para que el formulario reciba eventos de teclado antes que otros controles

            // --- LÍNEAS DE DEPURACIÓN PARA EVENTOS DE TECLADO EN Form1 ---
            this.KeyDown += Form1_DebugKeyDown;
            this.KeyUp += Form1_DebugKeyUp;
            // --- FIN DE LAS LÍNEAS DE DEPURACIÓN ---

            // Instancia el menú y se suscribe a su evento de selección
            Menu menuSeleccion = new Menu();
            menuSeleccion.NaveSeleccionada += OnNaveSeleccionada; // Suscribe al evento
            menuSeleccion.MostrarMenu(); // Muestra el menú

        }

        // --- MÉTODOS DE DEPURACIÓN para la entrada de teclado de Form1 ---
        private void Form1_DebugKeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Form1 DEPURACIÓN: Tecla presionada - {e.KeyCode}");
        }

        private void Form1_DebugKeyUp(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Form1 DEPURACIÓN: Tecla soltada - {e.KeyCode}");
        }
        // --- Fin de los MÉTODOS DE DEPURACIÓN ---

        // Este método será llamado cuando el menú dispare el evento NaveSeleccionada
        private void OnNaveSeleccionada(TipoAvion tipo)
        {
            this.avionSeleccionado = tipo; // Almacena la selección del usuario
            InicializarJuego(); // Ahora podemos inicializar el juego con la nave correcta

            // Después de que el menú se cierra y antes de que Form1 se muestre completamente,
            // nos aseguramos de que tenga el foco para recibir la entrada del teclado.
            this.Focus();
            this.ActiveControl = null; // Quita el foco de cualquier otro control que pudiera tenerlo
            this.Select(); // Intenta seleccionar el propio formulario
            this.Invalidate(); // Asegura que el formulario se redibuje con la nave
        }

        // Método para inicializar la nave del jugador y los obstáculos del juego
        private void InicializarJuego()
        {
            try
            {
                naveJugador = new Naves(avionSeleccionado)
                {
                    PosX = this.ClientSize.Width / 2, // Posiciona la nave en el centro horizontal
                    PosY = this.ClientSize.Height - (int)(100 * 0.2f), // Un poco por encima del borde inferior
                    Escala = 0.5f // Escala de la nave en el juego
                };
            }
            catch (Exception ex) // Captura cualquier excepción durante la carga de la nave (FileNotFound, etc.)
            {
                MessageBox.Show($"Error al cargar la nave '{avionSeleccionado}': {ex.Message}\nVerifica los archivos en 'Assets\\Naves\\{avionSeleccionado}'.", "Error de Carga", MessageBoxButtons.OK, MessageBoxIcon.Error);
                naveJugador = null;
                this.Close(); // Cierra la aplicación si la nave no puede ser cargada
                return;
            }

            listaObstaculos.Clear(); // Limpia los obstáculos existentes antes de añadir nuevos (si el juego se reiniciara)

            // Obstáculos con dimensiones de 260px
            // Las posiciones se ajustan para que quepan razonablemente en una ventana de 1080x720
            listaObstaculos.Add(new Obstaculos(
                x: 50, y: 50,
                ancho: 260, alto: 260,
                colorRelleno: Color.DarkSlateGray,
                colorBorde: Color.Black,
                grosorBorde: 2.0f
            ));

            listaObstaculos.Add(new Obstaculos(
                x: 350, y: 300,
                ancho: 260, alto: 260,
                colorRelleno: Color.Firebrick,
                colorBorde: Color.DarkRed,
                grosorBorde: 3.0f
            ));

            listaObstaculos.Add(new Obstaculos(
                x: 700, y: 50,
                ancho: 260, alto: 260,
                colorRelleno: Color.ForestGreen,
                colorBorde: Color.DarkGreen,
                grosorBorde: 2.0f
            ));

            listaObstaculos.Add(new Obstaculos(
                x: 10, y: 400,
                ancho: 260, alto: 260,
                colorRelleno: Color.Gold,
                colorBorde: Color.DarkGoldenrod,
                grosorBorde: 1.0f
            ));

            // Asegura que el gestor de movimiento se inicialice y se inicie
            if (naveJugador != null)
            {
                if (gestorMovimiento == null)
                {
                    gestorMovimiento = new Movimiento(naveJugador, this);
                }
                gestorMovimiento.IniciarMovimiento();
            }
            else
            {
                MessageBox.Show("No se pudo iniciar el movimiento: la nave no está disponible.", "Error Interno", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        // Manejador de evento para dibujar todos los elementos del juego (nave y obstáculos)
        private void DibujarElementosJuego(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White); // Limpia el fondo del formulario
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; // Para un dibujo más suave

            naveJugador?.Dibujar(g, naveJugador.Escala); // El operador '?.' (condicional nulo) evita errores si naveJugador es null

            foreach (var obstaculo in listaObstaculos)
            {
                obstaculo.Dibujar(g);
            }
        }

        // Este método se llama cuando el formulario está a punto de cerrarse
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            gestorMovimiento?.DetenerMovimiento(); // Detiene el temporizador para liberar recursos
        }

        // El evento Form1_Load ya no es estrictamente necesario para iniciar el movimiento
        // si lo manejamos en OnNaveSeleccionada. Puede dejarse si tienes otras inicializaciones
        // que no dependen de la selección de la nave.
        // private void Form1_Load(object sender, EventArgs e)
        // {
        // }
    }
}