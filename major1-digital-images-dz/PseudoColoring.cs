using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static major1_digital_images_dz.Form1;

namespace major1_digital_images_dz
{
    public class IntervalSettingsForm : Form
    {
        private List<ColorInterval> intervals = new List<ColorInterval>();
        private DataGridView dataGridView;
        private NumericUpDown numLower, numUpper;
        private Button btnColorPicker;
        private PictureBox colorPreview;
        private Color currentColor = Color.Red;

        public struct ColorInterval
        {
            public int LowerBound;   // Нижняя граница (включительно)
            public int UpperBound;   // Верхняя граница (исключительно)
            public Color IntervalColor; // Цвет для интервала

            public ColorInterval(int lower, int upper, Color color)
            {
                LowerBound = lower;
                UpperBound = upper;
                IntervalColor = color;
            }
        }

        public ColorInterval[] Intervals => intervals.ToArray();

        public IntervalSettingsForm()
        {
            InitializeForm();
        }

        private void InitializeForm()
        {
            this.Text = "Настройка интервалов раскрашивания";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Панель для добавления интервалов
            GroupBox groupAdd = new GroupBox();
            groupAdd.Text = "Добавить интервал";
            groupAdd.Location = new Point(15, 15);
            groupAdd.Size = new Size(560, 100);

            // Нижняя граница
            Label lblLower = new Label();
            lblLower.Text = "От:";
            lblLower.Location = new Point(20, 25);
            lblLower.Size = new Size(30, 20);

            numLower = new NumericUpDown();
            numLower.Location = new Point(55, 25);
            numLower.Size = new Size(60, 20);
            numLower.Minimum = 0;
            numLower.Maximum = 254;
            numLower.Value = 0;

            // Верхняя граница
            Label lblUpper = new Label();
            lblUpper.Text = "До:";
            lblUpper.Location = new Point(130, 25);
            lblUpper.Size = new Size(30, 20);

            numUpper = new NumericUpDown();
            numUpper.Location = new Point(165, 25);
            numUpper.Size = new Size(60, 20);
            numUpper.Minimum = 1;
            numUpper.Maximum = 255;
            numUpper.Value = 85;

            // Выбор цвета
            Label lblColor = new Label();
            lblColor.Text = "Цвет:";
            lblColor.Location = new Point(240, 25);
            lblColor.Size = new Size(40, 20);

            btnColorPicker = new Button();
            btnColorPicker.Text = "Выбрать";
            btnColorPicker.Location = new Point(285, 25);
            btnColorPicker.Size = new Size(80, 25);
            btnColorPicker.Click += (s, e) => PickColor();

            colorPreview = new PictureBox();
            colorPreview.Location = new Point(370, 25);
            colorPreview.Size = new Size(30, 25);
            colorPreview.BackColor = currentColor;
            colorPreview.BorderStyle = BorderStyle.FixedSingle;

            // Кнопка добавления
            Button btnAdd = new Button();
            btnAdd.Text = "Добавить";
            btnAdd.Location = new Point(20, 60);
            btnAdd.Size = new Size(100, 30);
            btnAdd.Click += (s, e) => AddInterval();

            // Кнопка очистки
            Button btnClear = new Button();
            btnClear.Text = "Очистить все";
            btnClear.Location = new Point(130, 60);
            btnClear.Size = new Size(100, 30);
            btnClear.Click += (s, e) => ClearAll();

            groupAdd.Controls.AddRange(new Control[]
            {
            lblLower, numLower, lblUpper, numUpper,
            lblColor, btnColorPicker, colorPreview,
            btnAdd, btnClear
            });

            // Таблица интервалов
            Label lblTable = new Label();
            lblTable.Text = "Список интервалов:";
            lblTable.Location = new Point(15, 125);
            lblTable.Size = new Size(200, 20);
            lblTable.Font = new Font(Font, FontStyle.Bold);

            dataGridView = new DataGridView();
            dataGridView.Location = new Point(15, 150);
            dataGridView.Size = new Size(560, 180);
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.RowHeadersVisible = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Колонки
            dataGridView.Columns.Add("Lower", "Нижняя граница");
            dataGridView.Columns.Add("Upper", "Верхняя граница");
            dataGridView.Columns.Add("Color", "Цвет");
            dataGridView.Columns.Add("Preview", "Просмотр");

            dataGridView.Columns[0].Width = 100;
            dataGridView.Columns[1].Width = 100;
            dataGridView.Columns[2].Width = 150;
            dataGridView.Columns[3].Width = 80;

            // Кнопка удаления
            Button btnRemove = new Button();
            btnRemove.Text = "Удалить выделенный";
            btnRemove.Location = new Point(15, 340);
            btnRemove.Size = new Size(150, 30);
            btnRemove.Click += (s, e) => RemoveSelected();

            // Кнопки OK/Cancel
            Button btnOK = new Button();
            btnOK.Text = "OK";
            btnOK.Location = new Point(400, 340);
            btnOK.Size = new Size(80, 30);
            btnOK.Click += (s, e) => ValidateAndClose();

            Button btnCancel = new Button();
            btnCancel.Text = "Отмена";
            btnCancel.Location = new Point(495, 340);
            btnCancel.Size = new Size(80, 30);
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            // Подсказка
            Label lblHint = new Label();
            lblHint.Text = "Диапазон яркости: 0-255. Все значения должны быть покрыты без пробелов.";
            lblHint.Location = new Point(15, 380);
            lblHint.Size = new Size(400, 30);
            lblHint.ForeColor = Color.DarkBlue;

            this.Controls.AddRange(new Control[]
            {
            groupAdd, lblTable, dataGridView,
            btnRemove, btnOK, btnCancel, lblHint
            });
        }

        private void PickColor()
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.Color = currentColor;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    currentColor = dialog.Color;
                    colorPreview.BackColor = currentColor;
                }
            }
        }

        private void AddInterval()
        {
            int lower = (int)numLower.Value;
            int upper = (int)numUpper.Value;

            if (lower >= upper)
            {
                MessageBox.Show("Нижняя граница должна быть меньше верхней!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Проверяем пересечения
            foreach (var interval in intervals)
            {
                if ((lower >= interval.LowerBound && lower < interval.UpperBound) ||
                    (upper > interval.LowerBound && upper <= interval.UpperBound) ||
                    (lower <= interval.LowerBound && upper >= interval.UpperBound))
                {
                    MessageBox.Show($"Пересечение с интервалом {interval}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            // Добавляем
            intervals.Add(new ColorInterval(lower, upper, currentColor));

            // Сортируем
            intervals.Sort((a, b) => a.LowerBound.CompareTo(b.LowerBound));

            // Обновляем таблицу
            UpdateTable();

            // Готовим следующий интервал
            numLower.Value = Math.Min(254, upper);
            numUpper.Value = Math.Min(255, upper + 30);

            // Меняем цвет для разнообразия
            ChangeColor();
        }

        private void ChangeColor()
        {
            // Простая смена цвета
            if (currentColor == Color.Red)
                currentColor = Color.Green;
            else if (currentColor == Color.Green)
                currentColor = Color.Blue;
            else if (currentColor == Color.Blue)
                currentColor = Color.Yellow;
            else if (currentColor == Color.Yellow)
                currentColor = Color.Magenta;
            else if (currentColor == Color.Magenta)
                currentColor = Color.Cyan;
            else
                currentColor = Color.Red;

            colorPreview.BackColor = currentColor;
        }

        private void RemoveSelected()
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                int index = dataGridView.SelectedRows[0].Index;
                if (index >= 0 && index < intervals.Count)
                {
                    intervals.RemoveAt(index);
                    UpdateTable();
                }
            }
        }

        private void ClearAll()
        {
            intervals.Clear();
            UpdateTable();
        }

        private void UpdateTable()
        {
            dataGridView.Rows.Clear();

            foreach (var interval in intervals)
            {
                int rowIdx = dataGridView.Rows.Add(
                    interval.LowerBound,
                    interval.UpperBound,
                    $"RGB({interval.IntervalColor.R}, {interval.IntervalColor.G}, {interval.IntervalColor.B})"
                );

                // Превью цвета
                DataGridViewRow row = dataGridView.Rows[rowIdx];
                DataGridViewImageCell cell = new DataGridViewImageCell();
                cell.Value = CreateColorImage(interval.IntervalColor, 70, 20);
                row.Cells[3] = cell;
            }
        }

        private Image CreateColorImage(Color color, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            using (Brush brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, 0, 0, width, height);
                g.DrawRectangle(Pens.Black, 0, 0, width - 1, height - 1);
            }
            return bmp;
        }

        private void ValidateAndClose()
        {
            if (intervals.Count == 0)
            {
                MessageBox.Show("Нет интервалов!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Сортируем
            intervals.Sort((a, b) => a.LowerBound.CompareTo(b.LowerBound));

            // Проверяем покрытие 0-255 без пробелов
            int current = 0;
            foreach (var interval in intervals)
            {
                if (interval.LowerBound != current)
                {
                    MessageBox.Show($"Пробел между {current} и {interval.LowerBound}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                current = interval.UpperBound;
            }

            if (current != 255)
            {
                MessageBox.Show($"Не покрыт диапазон до конца. Последняя граница: {current}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
