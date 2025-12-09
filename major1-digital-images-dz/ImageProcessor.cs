using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace major1_digital_images_dz
{
    public class ImageProcessor
    {
        public Bitmap OriginalImage;
        public Bitmap GrayImage;

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
    }
}
