using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO; // Necesario para FileNotFoundException en ActualizarPreviewNave

namespace GrupalNaves
{

    public class Menu
    {
        // Evento que se disparará cuando el usuario seleccione una nave y haga clic en "Escoger nave"
        // Form1 se suscribirá a este evento para saber qué nave se eligió.
        public event Action<TipoAvion> NaveSeleccionada;

        public void MostrarMenu()
        {
            using (var menuForm = new Form()) // Cambiado a menuForm para evitar confusión con la clase Menu
            {
                menuForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                menuForm.StartPosition = FormStartPosition.CenterScreen;
                menuForm.Text = "Selecciona tu nave";
                menuForm.Width = 400;
                menuForm.Height = 350;
                menuForm.MaximizeBox = false; // Evitar maximizar el menú
                menuForm.MinimizeBox = false; // Evitar minimizar el menú
                menuForm.ShowInTaskbar = false; // No mostrar el menú en la barra de tareas

                var label = new Label()
                {
                    Text = "Elige tu nave:",
                    Left = 50,
                    Top = 20,
                    Width = 200
                };

                var comboBox = new ComboBox()
                {
                    Left = 50,
                    Top = 50,
                    Width = 200,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                foreach (TipoAvion tipo in Enum.GetValues(typeof(TipoAvion)))
                {
                    comboBox.Items.Add(tipo.ToString());
                }
                comboBox.SelectedIndex = 0;

                var pictureBoxPreview = new PictureBox()
                {
                    Left = 100,
                    Top = 100,
                    Width = 150,
                    Height = 150,
                    BorderStyle = BorderStyle.FixedSingle,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.White
                };

                comboBox.SelectedIndexChanged += (s, e) =>
                {
                    TipoAvion tipoSeleccionadoEnCombo = (TipoAvion)comboBox.SelectedIndex;
                    ActualizarPreviewNave(pictureBoxPreview, tipoSeleccionadoEnCombo);
                };

                var botonAceptar = new Button()
                {
                    Text = "Escoger nave",
                    Left = 50,
                    Top = 270,
                    Width = 200,
                    Height = 30
                };

                botonAceptar.Click += (s, e) =>
                {
                    TipoAvion seleccionFinal = (TipoAvion)comboBox.SelectedIndex;
                    // Dispara el evento NaveSeleccionada para que Form1 lo reciba
                    NaveSeleccionada?.Invoke(seleccionFinal);
                    menuForm.Close(); // Cierra el formulario del menú
                };

                menuForm.Controls.Add(label);
                menuForm.Controls.Add(comboBox);
                menuForm.Controls.Add(pictureBoxPreview);
                menuForm.Controls.Add(botonAceptar);

                // Actualiza la previsualización inicial
                ActualizarPreviewNave(pictureBoxPreview, (TipoAvion)comboBox.SelectedIndex);

                // Muestra el menú como un diálogo modal
                menuForm.ShowDialog();
            }
        }

        // Método para actualizar la vista previa de la nave (copiado de Form1, requiere Naves.cs)
        private void ActualizarPreviewNave(PictureBox pb, TipoAvion tipo)
        {
            try
            {
                Naves navePreview = new Naves(tipo)
                {
                    PosX = 0,
                    PosY = 0,
                    Escala = 0.35f
                };

                Bitmap bmp = new Bitmap(pb.Width, pb.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(pb.BackColor);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    navePreview.Dibujar(g, navePreview.Escala);
                }
                pb.Image = bmp;
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"Error al cargar archivos para la previsualización: {ex.Message}\nAsegúrate de que los archivos 'bordes.txt' y 'coloreados.txt' para el avión '{tipo}' existen en la carpeta 'Assets\\Naves\\{tipo}'.", "Error de Archivo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Image = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error al previsualizar la nave: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                pb.Image = null;
            }
        }
    }
}