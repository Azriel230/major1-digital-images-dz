using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace major1_digital_images_dz
{
    public class HoughLine
    {
        public double Theta { get; set; }          // угол в радианах
        public double Rho { get; set; }            // расстояние
        public int Votes { get; set; }             // количество голосов

        public HoughLine(double theta, double rho, int votes)
        {
            Theta = theta;
            Rho = rho;
            Votes = votes;
        }

        // Получение точек линии для отрисовки
        public void GetLinePoints(int width, int height, out Point p1, out Point p2)
        {
            double cosTheta = Math.Cos(Theta);
            double sinTheta = Math.Sin(Theta);

            // Для вертикальных линий (θ близко к 0 или 180 градусам)
            if (Math.Abs(sinTheta) < 0.001)
            {
                int x = (int)Math.Round(Rho / cosTheta);
                p1 = new Point(x, 0);
                p2 = new Point(x, height);
            }
            else
            {
                // Вычисляем точки на границах изображения
                // Левая и правая границы
                int x1 = 0;
                int y1 = (int)Math.Round((Rho - x1 * cosTheta) / sinTheta);

                int x2 = width - 1;
                int y2 = (int)Math.Round((Rho - x2 * cosTheta) / sinTheta);

                // Если точки выходят за границы, используем верхнюю и нижнюю границы
                if (y1 < 0 || y1 >= height || y2 < 0 || y2 >= height)
                {
                    y1 = 0;
                    x1 = (int)Math.Round((Rho - y1 * sinTheta) / cosTheta);

                    y2 = height - 1;
                    x2 = (int)Math.Round((Rho - y2 * sinTheta) / cosTheta);
                }

                p1 = new Point(x1, y1);
                p2 = new Point(x2, y2);
            }
        }
    }
}
