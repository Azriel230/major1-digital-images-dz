using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        Bitmap OriginalImage;
        Bitmap GrayImage;
        Bitmap filtered_Image;

        int bringht = 0;
        int p_negative = 0;
        bool is_decimal_gamma = false;
        int little_gamma = 2;
        int count_level_Quantization = 1;
        int filter_mode = 0;

        public Form1()
        {
            InitializeComponent();
            SetupLayout();
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
    }
}
