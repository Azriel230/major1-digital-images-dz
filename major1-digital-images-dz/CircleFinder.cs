using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace major1_digital_images_dz
{
    public partial class CircleFinder : Form
    {
        private List<PointF> points = new List<PointF>();
        private int selectedPointIndex = -1;
        private bool isDragging = false;
        private const float pointRadius = 5f;
        private const float selectionRadius = 10f;

        // Расчетные параметры
        private double area = 0;
        private double perimeter = 0;
        private double circlePercent = 0;

        public CircleFinder()
        {
            InitializeComponent();
            this.DoubleBuffered = true; // Для плавной отрисовки
            SetupUI();
        }

        private void SetupUI()
        {
            // Настройка формы
            this.Text = "Circle Finder";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Создание панели для рисования
            panelDraw = new Panel();
            panelDraw.Location = new Point(300, 0);
            panelDraw.Size = new Size(500, 600);
            panelDraw.BackColor = Color.White;
            panelDraw.BorderStyle = BorderStyle.FixedSingle;
            panelDraw.Paint += PanelDraw_Paint;
            panelDraw.MouseDown += PanelDraw_MouseDown;
            panelDraw.MouseMove += PanelDraw_MouseMove;
            panelDraw.MouseUp += PanelDraw_MouseUp;

            // Создание кнопки сброса
            btnReset = new Button();
            btnReset.Text = "Сброс";
            btnReset.Location = new Point(20, 20);
            btnReset.Size = new Size(150, 40);
            btnReset.Click += BtnReset_Click;

            // Создание меток для отображения информации
            lblArea = new Label();
            lblArea.Location = new Point(20, 80);
            lblArea.Size = new Size(250, 30);
            lblArea.Text = "Площадь (S): 0";

            lblPerimeter = new Label();
            lblPerimeter.Location = new Point(20, 120);
            lblPerimeter.Size = new Size(250, 30);
            lblPerimeter.Text = "Периметр (P): 0";

            lblCirclePercent = new Label();
            lblCirclePercent.Location = new Point(20, 160);
            lblCirclePercent.Size = new Size(250, 60);
            lblCirclePercent.Text = "Круг на: 0%";

            // Добавление элементов на форму
            this.Controls.Add(panelDraw);
            this.Controls.Add(btnReset);
            this.Controls.Add(lblArea);
            this.Controls.Add(lblPerimeter);
            this.Controls.Add(lblCirclePercent);
        }

        // Объявление контролов
        private Panel panelDraw;
        private Button btnReset;
        private Label lblArea;
        private Label lblPerimeter;
        private Label lblCirclePercent;

        private void PanelDraw_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Отрисовка сетки (опционально)
            DrawGrid(g);

            // Отрисовка линий многоугольника
            if (points.Count >= 2)
            {
                using (Pen pen = new Pen(Color.Blue, 2))
                {
                    // Соединяем точки линиями
                    for (int i = 0; i < points.Count; i++)
                    {
                        int next = (i + 1) % points.Count;
                        g.DrawLine(pen, points[i], points[next]);
                    }
                }
            }

            // Отрисовка точек
            foreach (PointF point in points)
            {
                DrawPoint(g, point);
            }

            // Если есть выделенная точка, рисуем её другим цветом
            if (selectedPointIndex >= 0 && selectedPointIndex < points.Count)
            {
                DrawSelectedPoint(g, points[selectedPointIndex]);
            }
        }

        private void DrawGrid(Graphics g)
        {
            using (Pen gridPen = new Pen(Color.LightGray, 1))
            {
                gridPen.DashStyle = DashStyle.Dot;

                // Вертикальные линии
                for (int x = 0; x < panelDraw.Width; x += 20)
                {
                    g.DrawLine(gridPen, x, 0, x, panelDraw.Height);
                }

                // Горизонтальные линии
                for (int y = 0; y < panelDraw.Height; y += 20)
                {
                    g.DrawLine(gridPen, 0, y, panelDraw.Width, y);
                }
            }
        }

        private void DrawPoint(Graphics g, PointF point)
        {
            using (Brush brush = new SolidBrush(Color.Red))
            {
                g.FillEllipse(brush,
                    point.X - pointRadius,
                    point.Y - pointRadius,
                    pointRadius * 2,
                    pointRadius * 2);
            }
        }

        private void DrawSelectedPoint(Graphics g, PointF point)
        {
            using (Brush brush = new SolidBrush(Color.Green))
            {
                g.FillEllipse(brush,
                    point.X - pointRadius,
                    point.Y - pointRadius,
                    pointRadius * 2,
                    pointRadius * 2);
            }
        }

        private void PanelDraw_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Добавление новой точки
                points.Add(e.Location);
                CalculateParameters();
                panelDraw.Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Проверка, была ли нажата существующая точка
                selectedPointIndex = FindPointAt(e.Location);
                if (selectedPointIndex >= 0)
                {
                    isDragging = true;
                    panelDraw.Invalidate();
                }
            }
        }

        private void PanelDraw_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedPointIndex >= 0 && selectedPointIndex < points.Count)
            {
                // Перемещение точки
                points[selectedPointIndex] = e.Location;
                CalculateParameters();
                panelDraw.Invalidate();
            }
        }

        private void PanelDraw_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isDragging = false;
                selectedPointIndex = -1;
                panelDraw.Invalidate();
            }
        }

        private int FindPointAt(Point location)
        {
            for (int i = 0; i < points.Count; i++)
            {
                float dx = points[i].X - location.X;
                float dy = points[i].Y - location.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distance <= selectionRadius)
                {
                    return i;
                }
            }
            return -1;
        }

        private void CalculateParameters()
        {
            if (points.Count < 3)
            {
                area = 0;
                perimeter = 0;
                circlePercent = 0;
            }
            else
            {
                // Вычисление площади по формуле Гаусса (формула шнурования)
                area = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    int j = (i + 1) % points.Count;
                    area += points[i].X * points[j].Y;
                    area -= points[j].X * points[i].Y;
                }
                area = Math.Abs(area) / 2.0;

                // Вычисление периметра
                perimeter = 0;
                for (int i = 0; i < points.Count; i++)
                {
                    int j = (i + 1) % points.Count;
                    float dx = points[j].X - points[i].X;
                    float dy = points[j].Y - points[i].Y;
                    perimeter += Math.Sqrt(dx * dx + dy * dy);
                }

                // Вычисление процента "круглости"
                if (perimeter > 0)
                {
                    circlePercent = 100 * (4 * Math.PI * area) / (perimeter * perimeter);
                }
                else
                {
                    circlePercent = 0;
                }
            }

            // Обновление меток
            lblArea.Text = $"Площадь (S): {area:F2}";
            lblPerimeter.Text = $"Периметр (P): {perimeter:F2}";
            lblCirclePercent.Text = $"Круг на: {circlePercent:F2}%";
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            points.Clear();
            area = 0;
            perimeter = 0;
            circlePercent = 0;

            lblArea.Text = "Площадь (S): 0";
            lblPerimeter.Text = "Периметр (P): 0";
            lblCirclePercent.Text = "Круг на: 0%";

            panelDraw.Invalidate();
        }
    }
}