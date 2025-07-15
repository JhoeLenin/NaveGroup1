using System;
using System.Drawing;
using System.Windows.Forms;

namespace GrupalNaves
{
    // Clase para manejar el HUD (Heads-Up Display) del juego
    public class HUD
    {
        private Panel panelPrincipal;
        private Label labelVida;
        private Label labelTiempo;
        private Label labelPuntaje;

        private System.Windows.Forms.Timer timerTiempo;
        private DateTime inicioJuego;
        private int _puntaje = 0;

        // Propiedad pública para acceder al puntaje
        public int Puntaje
        {
            get => _puntaje;
            private set
            {
                _puntaje = value;
                labelPuntaje.Text = $"Puntaje: {_puntaje}";
            }
        }

        public Control[] Controles => new Control[] { panelPrincipal, labelPuntaje };

        // Constructor que inicializa el HUD
        public HUD(Form form, int vidaInicial)
        {
            // Panel izquierdo: vida y tiempo
            panelPrincipal = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(200, 60),
                BackColor = Color.FromArgb(100, Color.Black),
                BorderStyle = BorderStyle.FixedSingle
            };

            labelVida = new Label
            {
                Text = $"Vida: {vidaInicial}",
                ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(10, 5),
                AutoSize = true
            };

            labelTiempo = new Label
            {
                Text = "Tiempo: 00:00",
                ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Location = new Point(10, 30),
                AutoSize = true
            };

            panelPrincipal.Controls.Add(labelVida);
            panelPrincipal.Controls.Add(labelTiempo);
            form.Controls.Add(panelPrincipal);

            // Label de puntaje (derecha)
            labelPuntaje = new Label
            {
                Text = "Puntaje: 0",
                ForeColor = Color.Yellow,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Location = new Point(form.ClientSize.Width - 160, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            form.Controls.Add(labelPuntaje);

            // Timer para actualizar tiempo
            inicioJuego = DateTime.Now;
            timerTiempo = new System.Windows.Forms.Timer();
            timerTiempo.Interval = 1000;
            timerTiempo.Tick += (s, e) =>
            {
                TimeSpan t = DateTime.Now - inicioJuego;
                labelTiempo.Text = $"Tiempo: {t.Minutes:D2}:{t.Seconds:D2}";
            };
            timerTiempo.Start();
        }

        public void ActualizarVida(int nuevaVida)
        {
            labelVida.Text = $"Vida: {nuevaVida}";
        }

        public void AgregarPuntaje(int puntos)
        {
            Puntaje += puntos; // Usamos la propiedad para mantener la sincronización
        }

        public void Pausar(bool pausado)
        {
            timerTiempo.Enabled = !pausado;
        }

        public void MoverPuntajeDerecha(Form form)
        {
            labelPuntaje.Location = new Point(form.ClientSize.Width - 160, 10);
        }
    }
}