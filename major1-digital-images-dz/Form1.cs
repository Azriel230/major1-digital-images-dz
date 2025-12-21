using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static major1_digital_images_dz.IntervalSettingsForm;
using static System.Net.Mime.MediaTypeNames;

namespace major1_digital_images_dz
{
    public partial class Form1 : Form
    {
        private TableLayoutPanel mainLayoutPanel;

        Bitmap OriginalImage = null;
        Bitmap GrayImage = null;
        Bitmap filtered_Image = null;
        Bitmap displayImage = null;  // Изображение с нарисованным регионом масштабирования (чтобы не руинить grayImage)

        int bringht = 0;
        int p_negative = 80; //threshold для хафа
        bool is_decimal_gamma = false;
        int little_gamma = 2;
        int count_level_Quantization = 1;
        int filter_mode = 0;

        int temp_x = 0;
        int temp_y = 0;
        int temp_stretch = 100; //%
        int temp_rotate = 0; //градусы

        Bitmap original_template = null;
        Bitmap conv_img = null;

        private float correlationScore = 0.0f; //

        bool is_interpolation = false;
        double scaling_coeff = 1.0;
        bool isSelecting = false;
        Point selectionStart = Point.Empty;
        Point selectionEnd = Point.Empty;
        Rectangle selectedRect = Rectangle.Empty;

        bool godPleaseFixThisShit = true;

        int thetaSteps = 180;
        int minVotes = 30;
        int maxLines = 10;

        public Form1()
        {
            InitializeComponent();
            SetupLayout();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (original_template != null)
            {
                original_template.Dispose();
                original_template = null;
            }

            if (filtered_Image != null)
            {
                filtered_Image.Dispose();
                filtered_Image = null;
            }

            if (GrayImage != null)
            {
                GrayImage.Dispose();
                GrayImage = null;
            }

            if (OriginalImage != null)
            {
                OriginalImage.Dispose();
                OriginalImage = null;
            }

            if (conv_img != null)
            {
                conv_img.Dispose();
                conv_img = null;
            }

            if (displayImage != null)
            {
                displayImage.Dispose();
                displayImage = null;
            }
        }

        private void SetupLayout()
        {
            // Удаляем элементы из текущих позиций
            this.Controls.Remove(pictureBoxOrig);
            this.Controls.Remove(pictureBoxFlex);
            this.Controls.Remove(pictureBox1);
            this.Controls.Remove(groupBox1);

            // Создаем главный TableLayoutPanel с 2 колонками
            mainLayoutPanel = new TableLayoutPanel();
            mainLayoutPanel.Dock = DockStyle.Fill;
            mainLayoutPanel.ColumnCount = 2;
            mainLayoutPanel.RowCount = 1;
            mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 90F)); // Для картинок
            mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F)); // Для groupBox
            mainLayoutPanel.Padding = new Padding(10);
            mainLayoutPanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;

            // Создаем контейнер для PictureBox'ов с использованием TableLayoutPanel
            TableLayoutPanel pictureContainer = new TableLayoutPanel();
            pictureContainer.Dock = DockStyle.Fill;
            pictureContainer.ColumnCount = 2;
            pictureContainer.RowCount = 2;
            pictureContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pictureContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pictureContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pictureContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            pictureContainer.Padding = new Padding(5);

            // Настраиваем PictureBox'ы
            pictureBoxOrig.Dock = DockStyle.Fill;
            pictureBoxFlex.Dock = DockStyle.Fill;
            pictureBox1.Dock = DockStyle.Fill;

            // Добавляем PictureBoxOrig в ячейку (0,0) - левый верхний
            pictureContainer.Controls.Add(pictureBoxOrig, 0, 0);

            // Добавляем PictureBoxFlex в ячейку (0,1) - левый нижний
            pictureContainer.Controls.Add(pictureBoxFlex, 0, 1);

            // Добавляем PictureBox1 в ячейку (1,0) - правый верхний с rowspan=2
            pictureContainer.Controls.Add(pictureBox1, 1, 0);
            pictureContainer.SetRowSpan(pictureBox1, 2);

            // Настраиваем GroupBox
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            // Добавляем контейнеры в главный TableLayoutPanel
            mainLayoutPanel.Controls.Add(pictureContainer, 0, 0);
            mainLayoutPanel.Controls.Add(groupBox1, 1, 0);

            // Добавляем главный TableLayoutPanel на форму
            this.Controls.Add(mainLayoutPanel);

            this.PerformLayout();
        }

        // Метод для загрузки изображения
        public bool LoadImage()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Изображения|*.bmp;*.jpg;*.jpeg;*.png|Все файлы|*.*";
                openFileDialog.Title = "Выберите изображение";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Загружаем изображение в OriginalImage
                        OriginalImage = new Bitmap(openFileDialog.FileName);

                        GrayImage = new Bitmap(OriginalImage);
                        Color2Black(GrayImage);
                        ResetSelection();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
            return false;
        }

        public void Color2Black(Bitmap bmp)
        {
            if (bmp == null) return;
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    byte gray = (byte)(0.3 * c.R + 0.59 * c.G + 0.11 * c.B);
                    bmp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                }
            }
        }


        //Load Picture
        private void button1_Click(object sender, EventArgs e)
        {
            if (LoadImage())
            {
                DrawImg(pictureBoxOrig, OriginalImage);
                DrawImg(pictureBox1, GrayImage);
                filtered_Image = new Bitmap(GrayImage);
            }
        }
        
        private void DrawHistogram(PictureBox targetPictureBox, int[] histogram)
        {
            if (histogram == null) return;

            int width = 256;
            int height = 200;
            Bitmap histogramBitmap = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(histogramBitmap))
            {
                g.Clear(Color.White);

                int max = 0;
                for (int i = 0; i < 256; i++)
                {
                    if (histogram[i] > max) max = histogram[i];
                }

                if (max == 0) max = 1;

                Pen pen = new Pen(Color.Blue, 1);
                for (int i = 0; i < 256; i++)
                {
                    int barHeight = (int)((float)histogram[i] / max * height);
                    g.DrawLine(pen, i, height, i, height - barHeight);
                }

                g.DrawLine(Pens.Black, 0, height - 1, width - 1, height - 1); // X
                g.DrawLine(Pens.Black, 0, 0, 0, height - 1); // Y

                pen.Dispose();
            }

            targetPictureBox.Image?.Dispose();
            targetPictureBox.Image = histogramBitmap;
        }

        private void DrawImg(PictureBox targetPictureBox, Bitmap img)
        {
            targetPictureBox.Image?.Dispose();
            targetPictureBox.Image = img;
        }

        private int[] CalcBrightnessHistogram()
        {
            if (GrayImage == null)
                return new int[0];

            int[] histogram = new int[256];

            int height = GrayImage.Height;
            int width = GrayImage.Width;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int brightness = Math.Max(0, Math.Min(255, (int)GrayImage.GetPixel(x, y).R));
                    histogram[brightness]++;
                }
            }

            return histogram;
        }

        //brightness histogram
        private void button3_Click(object sender, EventArgs e)
        {
            if (GrayImage == null) return;
            int[] histogram = CalcBrightnessHistogram();
            DrawHistogram(pictureBoxFlex, histogram);
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            bringht = (int)this.numericUpDown1.Value;
        }

        //brightness
        private void button4_Click(object sender, EventArgs e)
        {
            if (GrayImage == null) return;
            Bitmap img = new Bitmap(GrayImage);

            int height = img.Height;
            int width = img.Width;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int value = 0;
                    if ((int)c.R + bringht > 255) value = 255;
                    if ((int)c.R + bringht > 0 && (int)c.R + bringht <= 255) value = (int)c.R + bringht;
                    byte br = (byte)value;
                    img.SetPixel(x, y, Color.FromArgb(br, br, br));
                }
            }

            DrawImg(pictureBoxFlex, img);
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            p_negative = (int)this.numericUpDown2.Value;
        }

        //negative
        private void button5_Click(object sender, EventArgs e)
        {
            if (GrayImage == null) return;
            Bitmap img = new Bitmap(GrayImage);

            int height = img.Height;
            int width = img.Width;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int value = (int)c.R;
                    if ((int)c.R >= p_negative) value = 255 - (int)c.R;
                    byte neg = (byte)value;
                    img.SetPixel(x, y, Color.FromArgb(neg, neg, neg));
                }
            }

            DrawImg(pictureBoxFlex, img);
        }

        //binarization
        private void button6_Click(object sender, EventArgs e)
        {
            if (GrayImage == null) return;
            Bitmap img = new Bitmap(GrayImage);

            int height = img.Height;
            int width = img.Width;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int value = 0;
                    if ((int)c.R >= p_negative) value = 255;
                    byte neg = (byte)value;
                    img.SetPixel(x, y, Color.FromArgb(neg, neg, neg));
                }
            }

            DrawImg(pictureBoxFlex, img);
        }

        //определение границ q1 и q2 по среднему отклонению
        public (int q1, int q2) GetSigmaBounds(int[] histogram, int totalPixels)
        {
            if (histogram == null || totalPixels == 0)
                return (0, 255);

            // Вычисляем среднее
            double mean = 0;
            for (int i = 0; i < 256; i++)
            {
                mean += i * histogram[i];
            }
            mean /= totalPixels;

            // Вычисляем стандартное отклонение
            double variance = 0;
            for (int i = 0; i < 256; i++)
            {
                variance += histogram[i] * Math.Pow(i - mean, 2);
            }
            variance /= totalPixels;
            double stdDev = Math.Sqrt(variance);

            // Устанавливаем границы как mean +- k*sigma
            double k = 2.0; // Охватывает ~95% данных при нормальном распределении
            int q1 = (int)(mean - k * stdDev);
            int q2 = (int)(mean + k * stdDev);

            // Ограничиваем диапазон 0-255
            q1 = Math.Max(0, q1);
            q2 = Math.Min(255, q2);

            return (q1, q2);
        }

        //Increase contrast
        private void button7_Click(object sender, EventArgs e)
        {
            if (GrayImage == null) return;
            Bitmap img = new Bitmap(GrayImage);

            int height = img.Height;
            int width = img.Width;

            int[] histogram = CalcBrightnessHistogram();
            (int q1, int q2) = GetSigmaBounds(histogram, height * width);

            double scale = 255.0 / (q2 - q1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int brightness = (int)c.R;
                    int newBrightness;

                    // Применяем растяжение
                    if (brightness < q1)
                    {
                        newBrightness = 0;
                    }
                    else if (brightness > q2)
                    {
                        newBrightness = 255;
                    }
                    else
                    {
                        double stretchedValue = (brightness - q1) * scale;
                        newBrightness = Math.Max(0, Math.Min(255, (int)Math.Round(stretchedValue)));
                    }

                    img.SetPixel(x, y, Color.FromArgb(newBrightness, newBrightness, newBrightness));
                }
            }

            DrawImg(pictureBoxFlex, img);
        }

        //Decrease contrast
        private void button8_Click(object sender, EventArgs e)
        {
            if (GrayImage == null) return;
            Bitmap img = new Bitmap(GrayImage);

            int height = img.Height;
            int width = img.Width;

            int[] histogram = CalcBrightnessHistogram();
            (int q1, int q2) = GetSigmaBounds(histogram, height * width);

            double scale =  (q2 - q1) / 255.0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int brightness = (int)c.R;
                    double compressedValue = q1 + brightness * scale;
                    int newBrightness = Math.Max(0, Math.Min(255, (int)Math.Round(compressedValue)));

                    img.SetPixel(x, y, Color.FromArgb(newBrightness, newBrightness, newBrightness));
                }
            }

            DrawImg(pictureBoxFlex, img);
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            little_gamma = (int)numericUpDown3.Value;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            is_decimal_gamma = (bool)checkBox1.Checked;
        }
        
        //Gamma
        private void button9_Click(object sender, EventArgs e)
        {
            if (GrayImage == null) return;

            double gamma = is_decimal_gamma ? 1.0 / little_gamma : little_gamma;

            Bitmap img = new Bitmap(GrayImage);

            int height = img.Height;
            int width = img.Width;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    double compressedValue = 255.0 * Math.Pow((int)c.R / 255.0, gamma);
                    int newGammaColor = Math.Max(0, Math.Min(255, (int)Math.Round(compressedValue)));

                    img.SetPixel(x, y, Color.FromArgb(newGammaColor, newGammaColor, newGammaColor));
                }
            }

            DrawImg(pictureBoxFlex, img);
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            count_level_Quantization = (int)numericUpDown4.Value;
        }

        //Quantization
        private void button10_Click(object sender, EventArgs e)
        {
            if (GrayImage == null) 
                return;

            // Ограничиваем количество уровней
            count_level_Quantization = Math.Max(2, Math.Min(256, count_level_Quantization));

            Bitmap img = new Bitmap(GrayImage);

            // Вычисляем шаг квантования
            double step = 255.0 / (count_level_Quantization - 1);

            // Применяем квантование для черно-белого изображения
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    Color c = img.GetPixel(x, y);
                    int brightness = (c.R + c.G + c.B) / 3;
                    int level = (int)Math.Round(brightness / step);
                    byte quantizedValue = (byte)Math.Min(255, Math.Max(0, (int)(level * step)));

                    img.SetPixel(x, y, Color.FromArgb(quantizedValue, quantizedValue, quantizedValue));
                }
            }

            DrawImg(pictureBoxFlex, img);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void ApplyPseudocoloring(ColorInterval[] intervals)
        {
            if (GrayImage == null) return;

            Bitmap grayImg = GrayImage;
            Bitmap result = new Bitmap(grayImg.Width, grayImg.Height);

            // Создаем таблицу цветов для быстрого доступа
            Color[] colorTable = new Color[256];
            int intervalIndex = 0;

            // Заполняем таблицу
            for (int i = 0; i < 256; i++)
            {
                // Если вышли за текущий интервал, переходим к следующему
                if (intervalIndex < intervals.Length && i >= intervals[intervalIndex].UpperBound)
                {
                    intervalIndex++;
                }

                if (intervalIndex < intervals.Length)
                {
                    colorTable[i] = intervals[intervalIndex].IntervalColor;
                }
                else
                {
                    colorTable[i] = Color.Black; // На всякий случай
                }
            }

            // Применяем раскрашивание
            for (int y = 0; y < grayImg.Height; y++)
            {
                for (int x = 0; x < grayImg.Width; x++)
                {
                    Color grayColor = grayImg.GetPixel(x, y);
                    int brightness = grayColor.R; // Для черно-белого все каналы равны

                    result.SetPixel(x, y, colorTable[brightness]);
                }
            }

            // Обновляем изображение
            DrawImg(pictureBoxFlex, result);
        }

        //PseudoColoring
        private void button11_Click(object sender, EventArgs e)
        {
            if (GrayImage == null)
                return;

            // Открываем форму настройки интервалов
            using (IntervalSettingsForm form = new IntervalSettingsForm())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    ColorInterval[] intervals = form.Intervals;

                    if (intervals != null && intervals.Length > 0)
                    {
                        ApplyPseudocoloring(intervals);
                    }
                }
            }
        }

        //low-pass filter
        private void button2_Click(object sender, EventArgs e)
        {
            if (filtered_Image == null)
                return;

            double matrix_coeff = 0.11;
            int[] matrix_filter = new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 };

            if (filter_mode == 2)
            {
                matrix_coeff = 0.1;
                matrix_filter = new int[] { 1, 1, 1, 1, 2, 1, 1, 1, 1 };
            }
            else if (filter_mode == 3)
            {
                matrix_coeff = 0.0625;
                matrix_filter = new int[] { 1, 2, 1, 2, 4, 2, 1, 2, 1 };
            }

            filter_high_low(matrix_coeff, matrix_filter);
        }

        //high=pass filter
        private void button12_Click(object sender, EventArgs e)
        {
            if (filtered_Image == null)
                return;

            double matrix_coeff = 1;
            int[] matrix_filter = new int[] { -1, -1, -1, -1, 9, -1, -1, -1, -1 };

            if (filter_mode == 2)
            {
                matrix_filter = new int[] { 0, -1, 0, -1, 5, -1, 0, -1, 0 };
            }
            else if (filter_mode == 3)
            {
                matrix_filter = new int[] { 1, -2, 1, -2, 5, -2, 1, -2, 1 };
            }

            filter_high_low(matrix_coeff, matrix_filter);
        }

        private void filter_high_low(double matrix_coeff, int[] matrix_filter)
        {
            Bitmap result_img = new Bitmap(filtered_Image.Width, filtered_Image.Height);

            for (int y = 1; y < filtered_Image.Height - 1; y++)
            {
                for (int x = 1; x < filtered_Image.Width - 1; x++)
                {
                    int[] img_matrix = new int[9];
                    int index = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            Color pixelColor = filtered_Image.GetPixel(x + kx, y + ky);
                            img_matrix[index] = pixelColor.R;
                            index++;
                        }
                    }

                    double filter_res = 0.0;
                    for (int i = 0; i < 9; i++)
                    {
                        filter_res += (double)img_matrix[i] * matrix_coeff * (double)matrix_filter[i];
                    }

                    byte filteredValue = (byte)Math.Min(255, Math.Max(0, (int)filter_res));

                    result_img.SetPixel(x, y, Color.FromArgb(filteredValue, filteredValue, filteredValue));
                }
            }
            filtered_Image.Dispose();
            filtered_Image = new Bitmap(result_img);
            DrawImg(pictureBoxFlex, result_img);
        }

        //median filter
        private void button13_Click(object sender, EventArgs e)
        {
            int border_image = filter_mode / 2; //граница, на которую нужно отойти, чтобы сработал фильтр
            Bitmap result_img = new Bitmap(filtered_Image.Width, filtered_Image.Height);

            for (int y = border_image; y < filtered_Image.Height - border_image; y++)
            {
                for (int x = border_image; x < filtered_Image.Width - border_image; x++)
                {
                    int[] img_matrix = new int[filter_mode * filter_mode];
                    int index = 0;

                    for (int ky = -border_image; ky <= border_image; ky++)
                    {
                        for (int kx = -border_image; kx <= border_image; kx++)
                        {
                            Color pixelColor = filtered_Image.GetPixel(x + kx, y + ky);
                            img_matrix[index] = pixelColor.R;
                            index++;
                        }
                    }

                    Array.Sort(img_matrix);

                    int mid_index = filter_mode * filter_mode / 2 + 1;
                    int mid_pixel = img_matrix[border_image + 1];

                    result_img.SetPixel(x, y, Color.FromArgb(mid_pixel, mid_pixel, mid_pixel));
                }
            }
            filtered_Image.Dispose();
            filtered_Image = new Bitmap(result_img);
            DrawImg(pictureBoxFlex, result_img);
        }

        //цифиря для разрядности окна медианного фильтра,
        //а также при значениях 2, 3, или другое - выбирает 2, 3 или 1 матрицу
        //низкочастотного или высокочастного фильтра соответственно
        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            filter_mode = (int)numericUpDown5.Value;
        }

        //сброс фильтр фотокарточки
        private void button14_Click(object sender, EventArgs e)
        {
            filtered_Image.Dispose();
            filtered_Image = new Bitmap(GrayImage);
            DrawImg(pictureBoxFlex, filtered_Image);
        }

        //фильтр Собеля 3на3
        private void sobelFilter()
        {
            Bitmap result_img = new Bitmap(filtered_Image.Width, filtered_Image.Height);

            int[] h1 = new int[] { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
            int[] h2 = new int[] { 1, 2, 1, 0, 0, 0, -1, -2, -1 };

            for (int y = 1; y < filtered_Image.Height - 1; y++)
            {
                for (int x = 1; x < filtered_Image.Width - 1; x++)
                {
                    int[] img_matrix = new int[9];
                    int index = 0;

                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            Color pixelColor = filtered_Image.GetPixel(x + kx, y + ky);
                            img_matrix[index] = pixelColor.R;
                            index++;
                        }
                    }

                    double h1_res = 0.0;
                    double h2_res = 0.0;
                    for (int i = 0; i < 9; i++)
                    {
                        h1_res += (double)img_matrix[i] * (double)h1[i];
                        h2_res += (double)img_matrix[i] * (double)h2[i];
                    }
                    //double s = Math.Abs(h1_res) + Math.Abs(h2_res);
                    double s = Math.Sqrt(Math.Pow(h1_res, 2) + Math.Pow(h2_res, 2));
                    byte filteredValue = (byte)Math.Min(255, Math.Max(0, (int)s));

                    result_img.SetPixel(x, y, Color.FromArgb(filteredValue, filteredValue, filteredValue));
                }
            }
            filtered_Image.Dispose();
            filtered_Image = new Bitmap(result_img);
            if (conv_img == null)
                conv_img = new Bitmap(result_img);
            else
            {
                conv_img.Dispose();
                conv_img = new Bitmap(result_img);
            }
        }

        //show sobel filter result
        private void button15_Click(object sender, EventArgs e)
        {
            sobelFilter();
            DrawImg(pictureBoxFlex, filtered_Image);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //x
        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            temp_x = (int)numericUpDown6.Value;
            UpdateTemplateDisplay();
        }

        //y
        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            temp_y = (int)numericUpDown7.Value;
            UpdateTemplateDisplay();
        }

        //stretch
        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            temp_stretch = (int)numericUpDown8.Value;
            UpdateTemplateDisplay();
        }

        //rotate
        private void numericUpDown9_ValueChanged(object sender, EventArgs e)
        {
            temp_rotate = (int)numericUpDown9.Value;
            UpdateTemplateDisplay();
        }

        //load template
        private void button16_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                openFileDialog.Title = "Select Template Image";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        original_template = new Bitmap(openFileDialog.FileName);
                        Color2Black(original_template);
                        UpdateTemplateDisplay();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Основной метод для обновления отображения шаблона
        private void UpdateTemplateDisplay()
        {
            if (conv_img == null || original_template == null) return;

            // Создаем новое изображение для отображения
            Bitmap displayImage = null;
            displayImage = new Bitmap(conv_img);

            using (Graphics g = Graphics.FromImage(displayImage))
            {
                // Настройка качества графики
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;

                // Рассчитываем параметры трансформации
                float scale = temp_stretch / 100.0f;
                int templateWidth = (int)(original_template.Width * scale);
                int templateHeight = (int)(original_template.Height * scale);

                // Вычисляем центр для поворота
                PointF center = new PointF(
                    temp_x + templateWidth / 2.0f,
                    temp_y + templateHeight / 2.0f
                );

                // Применяем трансформации
                g.TranslateTransform(center.X, center.Y);
                g.RotateTransform(temp_rotate);
                g.TranslateTransform(-center.X, -center.Y);

                // Создаем изображение шаблона с красным оттенком
                Bitmap redTemplate = ApplyRedTint(original_template, templateWidth, templateHeight);

                // Рисуем шаблон с прозрачностью
                ColorMatrix redMatrix = new ColorMatrix(new float[][]
                {
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 0, 0, 0, 0},
                    new float[] {0, 0, 0, 0, 0},
                    new float[] {0, 0, 0, 0.7f, 0}, // Альфа-канал для прозрачности
                    new float[] {0, 0, 0, 0, 1}
                });

                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(redMatrix);
                    g.DrawImage(redTemplate,
                        new Rectangle(temp_x, temp_y, templateWidth, templateHeight),
                        0, 0, original_template.Width, original_template.Height,
                        GraphicsUnit.Pixel, attributes);
                }

                // Сбрасываем трансформации
                g.ResetTransform();
            }

            DrawImg(pictureBoxFlex, displayImage);

            CalculateCorrelation(); //вычисляем свертку
        }

        // Метод для применения красного оттенка к шаблону
        private Bitmap ApplyRedTint(Bitmap original, int newWidth, int newHeight)
        {
            Bitmap redBitmap = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(redBitmap))
            {
                // Растягиваем исходное изображение до новых размеров
                g.DrawImage(original, 0, 0, newWidth, newHeight);

                // Проходим по всем пикселям и делаем их красными
                for (int y = 0; y < redBitmap.Height; y++)
                {
                    for (int x = 0; x < redBitmap.Width; x++)
                    {
                        Color pixel = redBitmap.GetPixel(x, y);
                        // Используем яркость пикселя для определения интенсивности красного
                        int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                        redBitmap.SetPixel(x, y, Color.FromArgb(pixel.A, brightness, 0, 0));
                    }
                }
            }

            return redBitmap;
        }

        // Метод для вычисления корреляции (свертки) между шаблоном и изображением
        private void CalculateCorrelation()
        {
            if (conv_img == null || original_template == null)
            {
                correlationScore = 0.0f;
                UpdateStatusBar();
                return;
            }

            try
            {
                // Получаем текущее положение и размер шаблона
                float scale = temp_stretch / 100.0f;
                int templateWidth = (int)(original_template.Width * scale);
                int templateHeight = (int)(original_template.Height * scale);

                if (templateWidth <= 0 || templateHeight <= 0)
                {
                    correlationScore = 0.0f;
                    UpdateStatusBar();
                    return;
                }

                // Проверяем, что шаблон не выходит за границы изображения
                if (temp_x < 0 || temp_y < 0 ||
                    temp_x + templateWidth > conv_img.Width ||
                    temp_y + templateHeight > conv_img.Height)
                {
                    correlationScore = -1.0f; // Шаблон выходит за границы
                    UpdateStatusBar();
                    return;
                }

                // Создаем трансформированный шаблон
                Bitmap transformedTemplate = ApplyTransformationsToTemplate(original_template, templateWidth, templateHeight);

                if (transformedTemplate == null)
                {
                    correlationScore = 0.0f;
                    UpdateStatusBar();
                    return;
                }

                // Вычисляем нормализованную кросс-корреляцию (NCC)
                correlationScore = ComputeNormalizedCrossCorrelation(conv_img, transformedTemplate, temp_x, temp_y);

                transformedTemplate.Dispose();

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                correlationScore = 0.0f;
                UpdateStatusBar();
                Console.WriteLine($"Error calculating correlation: {ex.Message}");
            }
        }

        // Метод для применения трансформаций к шаблону (без красного оттенка)
        private Bitmap ApplyTransformationsToTemplate(Bitmap original, int width, int height)
        {
            if (original == null || width <= 0 || height <= 0) return null;

            Bitmap transformed = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(transformed))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // Вычисляем центр для поворота
                PointF center = new PointF(width / 2.0f, height / 2.0f);

                // Применяем трансформации
                g.TranslateTransform(center.X, center.Y);
                g.RotateTransform(temp_rotate);
                g.TranslateTransform(-center.X, -center.Y);

                // Рисуем шаблон с преобразованиями
                g.DrawImage(original, 0, 0, width, height);

                g.ResetTransform();
            }

            Color2Black(transformed);
            return transformed;
        }

        // Метод для вычисления простого скалярного произведения (самая простая свертка)
        private float ComputeNormalizedCrossCorrelation(Bitmap image, Bitmap template, int x, int y)
        {
            if (image == null || template == null) return 0.0f;

            int templateWidth = template.Width;
            int templateHeight = template.Height;

            // Проверяем границы
            if (x < 0 || y < 0 || x + templateWidth > image.Width || y + templateHeight > image.Height)
                return 0.0f;

            double dotProduct = 0.0;
            double imageSumSquared = 0.0;
            double templateSumSquared = 0.0;

            // Простое скалярное произведение
            for (int ty = 0; ty < templateHeight; ty++)
            {
                for (int tx = 0; tx < templateWidth; tx++)
                {
                    // Пиксель шаблона (нормализованный от 0 до 1)
                    Color templatePixel = template.GetPixel(tx, ty);
                    double templateValue = (templatePixel.R + templatePixel.G + templatePixel.B) / 3.0 / 255.0;

                    // Пиксель изображения (нормализованный от 0 до 1)
                    Color imagePixel = image.GetPixel(x + tx, y + ty);
                    double imageValue = (imagePixel.R + imagePixel.G + imagePixel.B) / 3.0 / 255.0;

                    // Скалярное произведение
                    dotProduct += imageValue * templateValue;

                    // Суммы квадратов для нормализации (чтобы было от 0 до 1)
                    imageSumSquared += imageValue * imageValue;
                    templateSumSquared += templateValue * templateValue;
                }
            }

            // Простая нормализация: делим на максимально возможное значение
            double maxPossible = Math.Sqrt(imageSumSquared * templateSumSquared);

            if (maxPossible == 0) return 0.0f;

            // Возвращаем нормализованное скалярное произведение (от 0 до 1)
            return (float)(dotProduct / maxPossible);
        }

        // Обновление статус-бара
        private void UpdateStatusBar()
        {
            string statusText;

            if (correlationScore < 0)
            {
                statusText = "Шаблон выходит за границы изображения";
            }
            else if (conv_img == null || original_template == null)
            {
                statusText = "Загрузите изображение и шаблон";
            }
            else
            {
                // Преобразуем в проценты для лучшей читаемости
                float percentage = correlationScore * 100.0f;
                statusText = $"Коэффициент попадания: {percentage:F2}% (NCC: {correlationScore:F4})";
            }

            // Обновляем статус-бар
            if (statusStrip1.Items.Count > 0)
            {
                statusStrip1.Items[0].Text = statusText;
            }
            else
            {
                ToolStripStatusLabel statusLabel = new ToolStripStatusLabel(statusText);
                statusStrip1.Items.Add(statusLabel);
                statusStrip1.Items[0].Text = statusText;
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //использовать ли интерполяцию для масштабирование региона
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            is_interpolation = (bool)checkBox2.Checked;
        }

        //значение масштабирования
        private void numericUpDown10_ValueChanged(object sender, EventArgs e)
        {
            scaling_coeff = (double)numericUpDown10.Value;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (GrayImage == null)
            {
                return;
            }

            try
            {
                // Проверяем, что изображение валидно
                int width = GrayImage.Width;
                int height = GrayImage.Height;


                // Проверяем, что клик внутри изображения (с учетом масштабирования)
                if (IsPointInsideImage(e.Location))
                {
                    isSelecting = true;
                    selectionStart = ConvertToImageCoordinates(e.Location);
                    selectionEnd = selectionStart;
                    selectedRect = new Rectangle(selectionStart, new Size(0, 0));

                    pictureBox1.Invalidate(); // Перерисовываем
                }
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Ошибка при выделении: изображение повреждено\n{ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetSelection();
            }
        }

        // Проверка, находится ли точка внутри изображения
        private bool IsPointInsideImage(Point mousePoint)
        {
            if (GrayImage == null) return false;

            Point imagePoint = ConvertToImageCoordinates(mousePoint);
            return !imagePoint.IsEmpty;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isSelecting) return;

            // Конвертируем координаты мыши в координаты изображения
            Point imagePoint = ConvertToImageCoordinates(e.Location);

            // Проверяем, что координаты не выходят за границы
            imagePoint.X = Math.Max(0, Math.Min(GrayImage.Width - 1, imagePoint.X));
            imagePoint.Y = Math.Max(0, Math.Min(GrayImage.Height - 1, imagePoint.Y));

            if (selectionEnd != imagePoint)
            {
                selectionEnd = imagePoint;

                // Обновляем прямоугольник выделения
                int x = Math.Min(selectionStart.X, selectionEnd.X);
                int y = Math.Min(selectionStart.Y, selectionEnd.Y);
                int width = Math.Abs(selectionStart.X - selectionEnd.X);
                int height = Math.Abs(selectionStart.Y - selectionEnd.Y);

                selectedRect = new Rectangle(x, y, width, height);

                pictureBox1.Invalidate(); // Перерисовываем
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (!isSelecting) return;

            isSelecting = false;

            // Конвертируем координаты мыши в координаты изображения
            Point imagePoint = ConvertToImageCoordinates(e.Location);

            // Проверяем, что координаты не выходят за границы
            imagePoint.X = Math.Max(0, Math.Min(GrayImage.Width - 1, imagePoint.X));
            imagePoint.Y = Math.Max(0, Math.Min(GrayImage.Height - 1, imagePoint.Y));

            selectionEnd = imagePoint;

            // Обновляем прямоугольник выделения
            int x = Math.Min(selectionStart.X, selectionEnd.X);
            int y = Math.Min(selectionStart.Y, selectionEnd.Y);
            int width = Math.Abs(selectionStart.X - selectionEnd.X);
            int height = Math.Abs(selectionStart.Y - selectionEnd.Y);

            // Минимальный размер выделения
            if (width < 2) width = 2;
            if (height < 2) height = 2;

            selectedRect = new Rectangle(x, y, width, height);

            if (godPleaseFixThisShit)
            {
                resetGrayImg();
                godPleaseFixThisShit = false;
            }

            // Обновляем отображение
            UpdateDisplayWithSelection();

            pictureBox1.Invalidate();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (isSelecting && selectedRect != Rectangle.Empty && GrayImage != null)
            {
                // Конвертируем координаты изображения в координаты PictureBox
                Rectangle displayRect = ConvertToDisplayCoordinates(selectedRect);

                // Рисуем прямоугольник выделения
                using (Pen pen = new Pen(Color.Purple, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    e.Graphics.DrawRectangle(pen, displayRect);
                }

                // Рисуем угловые маркеры
                DrawSelectionHandles(e.Graphics, displayRect);
            }
        }

        // Конвертация координат мыши в координаты изображения (с учетом масштабирования)
        private Point ConvertToImageCoordinates(Point mousePoint)
        {
            if (GrayImage == null || pictureBox1.Image == null)
                return Point.Empty;

            try
            {
                // Проверяем валидность изображений
                int imgWidth = GrayImage.Width;
                int imgHeight = GrayImage.Height;

                int imageX = (int)(mousePoint.X);
                int imageY = (int)(mousePoint.Y);
                return new Point(imageX, imageY);
                
            }
            catch (ArgumentException)
            {
                return Point.Empty;
            }
        }

        // Конвертация координат изображения в координаты PictureBox
        private Rectangle ConvertToDisplayCoordinates(Rectangle imageRect)
        {
            if (GrayImage == null || pictureBox1.Image == null)
                return Rectangle.Empty;

            int displayX = (int)(imageRect.X);
            int displayY = (int)(imageRect.Y);
            int displayWidth = (int)(imageRect.Width);
            int displayHeight = (int)(imageRect.Height);

            return new Rectangle(displayX, displayY, displayWidth, displayHeight);
            
        }

        // Обновление отображения с выделением
        private void UpdateDisplayWithSelection()
        {
            if (GrayImage == null)
                return;

            try
            {
                // Проверяем валидность оригинального изображения
                int width = GrayImage.Width;
                int height = GrayImage.Height;

                // Создаем новое изображение для отображения
                if (displayImage != null)
                    displayImage.Dispose();

                displayImage = new Bitmap(GrayImage);

                using (Graphics g = Graphics.FromImage(displayImage))
                {
                    // Рисуем полупрозрачное выделение
                    using (Brush selectionBrush = new SolidBrush(Color.FromArgb(50, Color.Yellow)))
                    {
                        g.FillRectangle(selectionBrush, selectedRect);
                    }

                    // Рисуем контур выделения
                    using (Pen borderPen = new Pen(Color.Purple, 2))
                    {
                        borderPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        g.DrawRectangle(borderPen, selectedRect);
                    }
                }

                DrawImg(pictureBox1, displayImage);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Ошибка при обновлении отображения: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Отрисовка угловых маркеров выделения
        private void DrawSelectionHandles(Graphics g, Rectangle rect)
        {
            int handleSize = 8;

            // Угловые маркеры
            Rectangle[] handles = new Rectangle[]
            {
                new Rectangle(rect.Left - handleSize/2, rect.Top - handleSize/2, handleSize, handleSize), // Левый верхний
                new Rectangle(rect.Right - handleSize/2, rect.Top - handleSize/2, handleSize, handleSize), // Правый верхний
                new Rectangle(rect.Left - handleSize/2, rect.Bottom - handleSize/2, handleSize, handleSize), // Левый нижний
                new Rectangle(rect.Right - handleSize/2, rect.Bottom - handleSize/2, handleSize, handleSize), // Правый нижний
            };

            using (Brush handleBrush = new SolidBrush(Color.White))
            using (Pen handleBorder = new Pen(Color.Red, 2))
            {
                foreach (Rectangle handle in handles)
                {
                    g.FillRectangle(handleBrush, handle);
                    g.DrawRectangle(handleBorder, handle);
                }
            }
        }

        // Сброс выделения
        private void ResetSelection()
        {
            isSelecting = false;
            selectionStart = Point.Empty;
            selectionEnd = Point.Empty;
            selectedRect = Rectangle.Empty;

            if (GrayImage != null)
            {
                try
                {
                    resetGrayImg();

                    // Проверяем, что изображение валидно
                    int width = GrayImage.Width; // Эта строка вызовет исключение если изображение невалидно

                    if (displayImage != null)
                    {
                        // Если displayImage уже отображается, не создаем новую копию
                        if (pictureBox1.Image != displayImage)
                        {
                            DrawImg(pictureBox1, displayImage);
                        }
                    }
                    else
                    {
                        // Создаем новую копию только если необходимо
                        displayImage = new Bitmap(GrayImage);
                        DrawImg(pictureBox1, displayImage);
                    }
                }
                catch (ArgumentException)
                {
                    // Если оригинальное изображение повреждено, создаем новое пустое
                    displayImage = new Bitmap(1, 1);
                    resetGrayImg();
                    DrawImg(pictureBox1, displayImage);
                    MessageBox.Show("Ошибка при сбросе выделения: изображение повреждено", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // Если нет оригинального изображения, создаем пустое

                displayImage = new Bitmap(1, 1);
                resetGrayImg();
                DrawImg(pictureBox1, displayImage);
            }

            pictureBox1.Invalidate();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            ResetSelection();
        }

        private void resetGrayImg()
        {
            if (GrayImage != null)
            {
                GrayImage.Dispose();
                GrayImage = new Bitmap(OriginalImage);
                Color2Black(GrayImage);
                if (displayImage != null)
                {
                    displayImage.Dispose();
                    displayImage = new Bitmap(GrayImage);
                }
            }    
        }

        public Bitmap ScaleWithoutInterpolation(Bitmap source, Rectangle region, float scaleFactor)
        {
            // Вычисляем новый размер
            int newWidth = (int)(region.Width * scaleFactor);
            int newHeight = (int)(region.Height * scaleFactor);

            Bitmap result = new Bitmap(newWidth, newHeight);

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    // Вычисляем соответствующие координаты в исходном изображении
                    int srcX = (int)(x / scaleFactor);
                    int srcY = (int)(y / scaleFactor);

                    // Корректируем границы
                    srcX = Math.Min(srcX + region.X, source.Width - 1);
                    srcY = Math.Min(srcY + region.Y, source.Height - 1);

                    // Берем ближайший пиксель
                    Color pixel = source.GetPixel(srcX, srcY);
                    result.SetPixel(x, y, pixel);
                }
            }

            return result;
        }

        public Bitmap ScaleWithInterpolation(Bitmap source, Rectangle region, float scaleFactor)
        {
            int newWidth = (int)(region.Width * scaleFactor);
            int newHeight = (int)(region.Height * scaleFactor);

            Bitmap result = new Bitmap(newWidth, newHeight);

            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    // Вычисляем точные координаты в исходном изображении
                    float srcX = x / scaleFactor;
                    float srcY = y / scaleFactor;

                    // Добавляем смещение региона
                    srcX += region.X;
                    srcY += region.Y;

                    // Получаем 4 соседних пикселя
                    int x1 = (int)Math.Floor(srcX);
                    int y1 = (int)Math.Floor(srcY);
                    int x2 = Math.Min(x1 + 1, source.Width - 1);
                    int y2 = Math.Min(y1 + 1, source.Height - 1);

                    // Доли для интерполяции
                    float xFraction = srcX - x1;
                    float yFraction = srcY - y1;

                    // Получаем цвета 4 пикселей
                    Color c11 = source.GetPixel(x1, y1);
                    Color c21 = source.GetPixel(x2, y1);
                    Color c12 = source.GetPixel(x1, y2);
                    Color c22 = source.GetPixel(x2, y2);

                    // Билинейная интерполяция для каждого канала
                    int r = (int)(
                        c11.R * (1 - xFraction) * (1 - yFraction) +
                        c21.R * xFraction * (1 - yFraction) +
                        c12.R * (1 - xFraction) * yFraction +
                        c22.R * xFraction * yFraction
                    );

                    int g = (int)(
                        c11.G * (1 - xFraction) * (1 - yFraction) +
                        c21.G * xFraction * (1 - yFraction) +
                        c12.G * (1 - xFraction) * yFraction +
                        c22.G * xFraction * yFraction
                    );

                    int b = (int)(
                        c11.B * (1 - xFraction) * (1 - yFraction) +
                        c21.B * xFraction * (1 - yFraction) +
                        c12.B * (1 - xFraction) * yFraction +
                        c22.B * xFraction * yFraction
                    );

                    // Ограничиваем значения
                    r = Math.Max(0, Math.Min(255, r));
                    g = Math.Max(0, Math.Min(255, g));
                    b = Math.Max(0, Math.Min(255, b));

                    result.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }

            return result;
        }


        //применить масштабирование выделенного региона
        private void button17_Click(object sender, EventArgs e)
        {
            if (selectedRect == Rectangle.Empty || GrayImage == null) return;

            float scaleFactor = (float)scaling_coeff;

            Bitmap scaledRegion;

            if (is_interpolation)
            {
                scaledRegion = ScaleWithInterpolation(GrayImage, selectedRect, scaleFactor);
            }
            else
            {
                scaledRegion = ScaleWithoutInterpolation(GrayImage, selectedRect, scaleFactor);
            }

            DrawImg(pictureBoxFlex, scaledRegion);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public List<HoughLine> DetectHoughLines(Bitmap edgeImage,
                                        int threshold = 80,
                                        int thetaSteps = 180,
                                        double rhoResolution = 1.0,
                                        int minVotes = 30,
                                        int neighborhoodSize = 3)
        {
            if (edgeImage == null)
                return new List<HoughLine>();

            // 1. Получаем краевые пиксели
            List<Point> edgePixels = GetEdgePixelsForHough(edgeImage, threshold);

            if (edgePixels.Count == 0)
                return new List<HoughLine>();

            // 2. Вычисляем параметры пространства Хафа
            int width = edgeImage.Width;
            int height = edgeImage.Height;

            // Максимальное возможное расстояние (диагональ изображения)
            double diagonal = Math.Sqrt(width * width + height * height);
            int rhoMax = (int)Math.Ceiling(diagonal);
            int rhoSteps = 2 * rhoMax + 1;

            // 3. Создаем аккумуляторный массив
            int[,] accumulator = new int[rhoSteps, thetaSteps];

            // 4. Процесс "голосования"
            double thetaStep = Math.PI / thetaSteps;

            foreach (Point pixel in edgePixels)
            {
                int x = pixel.X;
                int y = pixel.Y;

                for (int thetaIndex = 0; thetaIndex < thetaSteps; thetaIndex++)
                {
                    double theta = thetaIndex * thetaStep; // от 0 до π
                    double rho = x * Math.Cos(theta) + y * Math.Sin(theta);

                    // Преобразуем rho в индекс массива
                    int rhoIndex = (int)Math.Round(rho / rhoResolution) + rhoMax;

                    if (rhoIndex >= 0 && rhoIndex < rhoSteps)
                    {
                        accumulator[rhoIndex, thetaIndex]++;
                    }
                }
            }

            // 5. Поиск локальных максимумов
            List<HoughLine> lines = FindLocalMaxima(accumulator, rhoMax, rhoResolution, thetaStep, minVotes, neighborhoodSize);

            // Сортируем по количеству голосов
            lines.Sort((a, b) => b.Votes.CompareTo(a.Votes));

            return lines;
        }

        private List<Point> GetEdgePixelsForHough(Bitmap edgeImage, int threshold)
        {
            List<Point> edgePixels = new List<Point>();

            for (int y = 0; y < edgeImage.Height; y++)
            {
                for (int x = 0; x < edgeImage.Width; x++)
                {
                    Color color = edgeImage.GetPixel(x, y);
                    // Для черно-белого изображения все каналы равны
                    if (color.R >= threshold)
                    {
                        edgePixels.Add(new Point(x, y));
                    }
                }
            }

            return edgePixels;
        }

        private List<HoughLine> FindLocalMaxima(int[,] accumulator,
                                                int rhoMax,
                                                double rhoResolution,
                                                double thetaStep,
                                                int minVotes,
                                                int neighborhoodSize)
        {
            List<HoughLine> lines = new List<HoughLine>();

            int rhoSteps = accumulator.GetLength(0);
            int thetaSteps = accumulator.GetLength(1);

            for (int rhoIdx = neighborhoodSize; rhoIdx < rhoSteps - neighborhoodSize; rhoIdx++)
            {
                for (int thetaIdx = 0; thetaIdx < thetaSteps; thetaIdx++)
                {
                    int currentVotes = accumulator[rhoIdx, thetaIdx];

                    if (currentVotes < minVotes)
                        continue;

                    // Проверяем, является ли локальным максимумом
                    bool isMaxima = true;

                    for (int dr = -neighborhoodSize; dr <= neighborhoodSize && isMaxima; dr++)
                    {
                        for (int dt = -neighborhoodSize; dt <= neighborhoodSize && isMaxima; dt++)
                        {
                            if (dr == 0 && dt == 0) continue;

                            int nr = rhoIdx + dr;
                            int nt = (thetaIdx + dt + thetaSteps) % thetaSteps; // циклический по theta

                            if (nr >= 0 && nr < rhoSteps && accumulator[nr, nt] > currentVotes)
                            {
                                isMaxima = false;
                            }
                        }
                    }

                    if (isMaxima)
                    {
                        double rho = (rhoIdx - rhoMax) * rhoResolution;
                        double theta = thetaIdx * thetaStep;
                        lines.Add(new HoughLine(theta, rho, currentVotes));
                    }
                }
            }

            return lines;
        }

        // Метод для отрисовки линий на изображении
        public Bitmap DrawHoughLinesOnImage(Bitmap originalImage, List<HoughLine> lines, Color lineColor, int lineWidth = 2)
        {
            Bitmap result = new Bitmap(originalImage);

            using (Graphics g = Graphics.FromImage(result))
            using (Pen pen = new Pen(lineColor, lineWidth))
            {
                foreach (HoughLine line in lines)
                {
                    line.GetLinePoints(originalImage.Width, originalImage.Height, out Point p1, out Point p2);
                    g.DrawLine(pen, p1, p2);
                }
            }

            return result;
        }

        //Hough Transform
        private void button19_Click(object sender, EventArgs e)
        {
            if (GrayImage == null)
            {
                MessageBox.Show("Сначала загрузите изображение!", "Ошибка",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int threshold = p_negative;

                // 1. Применяем фильтр Собеля для получения границ
                sobelFilter();

                // 2. Детектируем линии
                List<HoughLine> allLines = DetectHoughLines(filtered_Image, threshold, thetaSteps, 1.0, minVotes);

                // 3. Ограничиваем количество линий
                int linesToTake = Math.Min(maxLines, allLines.Count);
                List<HoughLine> topLines = allLines.GetRange(0, linesToTake);

                // 4. Рисуем линии на оригинальном изображении
                Bitmap result = DrawHoughLinesOnImage(GrayImage, topLines, Color.Red);

                // 5. Отображаем результат
                DrawImg(pictureBoxFlex, result);

                // 6. Выводим информацию
                string info = $"Найдено линий: {allLines.Count}\n" +
                             $"Отображено: {linesToTake}\n";

                foreach (var line in topLines)
                {
                    info += $"θ={line.Theta * 180 / Math.PI:F1}°, " +
                           $"ρ={line.Rho:F1}, " +
                           $"голосов={line.Votes}\n";
                }

                MessageBox.Show(info, "Результат преобразования Хафа",
                               MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении преобразования Хафа: {ex.Message}",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void numericUpDown11_ValueChanged(object sender, EventArgs e)
        {
            thetaSteps = (int)numericUpDown11.Value;
        }

        private void numericUpDown12_ValueChanged(object sender, EventArgs e)
        {
            minVotes = (int)numericUpDown12.Value;
        }

        private void numericUpDown13_ValueChanged(object sender, EventArgs e)
        {
            maxLines = (int)numericUpDown13.Value;
        }
    }
}
