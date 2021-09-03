using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab5
{
    public partial class Form1 : Form
    {

        Bitmap image;
        Bitmap image2;
        Pixel[,] mImage;
        PixelComplex[,] cImage;
        Complex[] ccImageR;
        Complex[] ccImageG;
        Complex[] ccImageB;
        double maxR = 0, maxG = 0, maxB = 0;


        public Form1()
        {
            InitializeComponent();
            image = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            image = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            mImage = new Pixel[image.Width, image.Height];
            cImage = new PixelComplex[image.Width, image.Height];
            ccImageR = new Complex[image.Width * image.Height];
            ccImageG = new Complex[image.Width * image.Height];
            ccImageB = new Complex[image.Width * image.Height];
            comboBox1.Items.Add("Низкочастотный");
            comboBox1.Items.Add("Высокочастотный");
            comboBox1.Items.Add("Режекторный");
            comboBox1.Items.Add("Полосовой");

            comboBox1.SelectedIndex = 0;
            textBox1.Text = "256";
            textBox2.Text = "256";
            textBox3.Text = "20";
            textBox4.Text = "40";
            textBox4.Visible = false;
            label4.Visible = false;
            comboBox1.SelectedIndexChanged += ShowR2;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Filter = "Картинки (png, jpg, bmp, gif) |*.png;*.jpg;*.bmp;*.gif|All files (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {

                Bitmap imageT = new Bitmap(openFileDialog.FileName);
                image = new Bitmap(imageT, pictureBox1.Width, pictureBox1.Height);
                imageT.Dispose();
                pictureBox1.Image = image;
            }

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    mImage[i, j] = new Pixel();
                }
            }

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    Color c = image.GetPixel(i, j);
                    mImage[i, j].R = c.R;
                    mImage[i, j].G = c.G;
                    mImage[i, j].B = c.B;
                }
            }

            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    cImage[i, j] = new PixelComplex(mImage[i, j]);
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ToFury();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDileFialog = new SaveFileDialog();
            saveDileFialog.InitialDirectory = Directory.GetCurrentDirectory();
            saveDileFialog.Filter = "Картинки (png, jpg, bmp, gif) |*.png;*.jpg;*.bmp;*.gif|All files (*.*)|*.*";
            saveDileFialog.RestoreDirectory = true;
            image = (Bitmap)pictureBox2.Image;

            if (saveDileFialog.ShowDialog() == DialogResult.OK)
            {
                if (image != null)
                {
                    image.Save(saveDileFialog.FileName);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ToImage();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ShowFilter();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            FrequencyFilter();
            ToImage();
        }


        //Украденные из библеотеки функции для преобразования Фурье
        public static Complex[] ditfft2(Complex[] arr, int x0, int N, int s)
        {
            Complex[] X = new Complex[N];
            if (N == 1)
            {
                X[0] = arr[x0];
            }
            else
            {
                ditfft2(arr, x0, N / 2, 2 * s).CopyTo(X, 0);
                ditfft2(arr, x0 + s, N / 2, 2 * s).CopyTo(X, N / 2);

                for (int k = 0; k < N / 2; k++)
                {
                    var t = X[k];
                    double u = -2.0 * Math.PI * k / N;
                    X[k] = t + new Complex(Math.Cos(u), Math.Sin(u)) * X[k + N / 2];
                    X[k + N / 2] = t - new Complex(Math.Cos(u), Math.Sin(u)) * X[k + N / 2];
                }
            }

            return X;
        }

        public static Complex[] ditft(Complex[] arr)
        {
            Complex[] X = new Complex[arr.Length];
            for (int i = 0; i < arr.Length; ++i)
            {
                for (int j = 0; j < arr.Length; ++j)
                {
                    double u = -2.0 * Math.PI * i * j / arr.Length;
                    X[i] += (new Complex(Math.Cos(u), Math.Sin(u)) * arr[j]);
                }

            }

            return X;
        }

        public static Complex[] ditfft2d(Complex[] arr, int width, int height, bool use_FFT = true)
        {
            Complex[] X = new Complex[arr.Length];

            ParallelOptions opt = new ParallelOptions();
            if (Environment.ProcessorCount > 2)
                opt.MaxDegreeOfParallelism = Environment.ProcessorCount - 1;
            else opt.MaxDegreeOfParallelism = 1;
            Parallel.For(0, height, opt, i =>
            {
                Complex[] tmp = new Complex[width];
                Array.Copy(arr, i * width, tmp, 0, width);

                tmp = use_FFT ? ditfft2(tmp, 0, width, 1) : ditft(tmp);

                for (int k = 0; k < width; ++k)
                    X[i * width + k] = tmp[k] / width;
            }
            );
            Parallel.For(0, width, opt, j =>
            {
                Complex[] tmp = new Complex[height];
                for (int k = 0; k < height; ++k)
                    tmp[k] = X[j + k * width];

                tmp = use_FFT ? ditfft2(tmp, 0, tmp.Length, 1) : ditft(tmp);

                for (int k = 0; k < height; ++k)
                    X[j + k * width] = tmp[k] / height;
            }
            );
            return X;
        }

        public static Complex[] ditifft2d(Complex[] arr, int width, int height, bool use_FFT = true)
        {
            Complex[] X = new Complex[arr.Length];

            ParallelOptions opt = new ParallelOptions();
            if (Environment.ProcessorCount > 2)
                opt.MaxDegreeOfParallelism = Environment.ProcessorCount - 1;
            else opt.MaxDegreeOfParallelism = 1;
            //for (int i = 0; i < height; ++i)
            Parallel.For(0, height, opt, i =>
            {
                Complex[] tmp = new Complex[width];
                Array.Copy(arr, i * width, tmp, 0, width);
                for (int k = 0; k < width; ++k)
                    tmp[k] = new Complex(arr[i * width + k].Real, -arr[i * width + k].Imaginary);

                tmp = use_FFT ? ditfft2(tmp, 0, width, 1) : ditft(tmp);

                for (int k = 0; k < width; ++k)
                    X[i * width + k] = (new Complex(tmp[k].Real, -tmp[k].Imaginary));

            }
            );

            //for (int j = 0; j < width; ++j)
            Parallel.For(0, width, opt, j =>
            {
                Complex[] tmp = new Complex[height];
                for (int k = 0; k < height; ++k)
                    tmp[k] = new Complex(X[j + k * width].Real, -X[j + k * width].Imaginary);

                tmp = use_FFT ? ditfft2(tmp, 0, tmp.Length, 1) : ditft(tmp);

                for (int k = 0; k < height; ++k)
                    X[j + k * width] = (new Complex(tmp[k].Real, -tmp[k].Imaginary));
            }
            );
            return X;
        }


        //Функция из лекции
        static Complex[] DFT(Complex[] x, double n = 1)
        {
            int N = x.Length;
            Complex[] G = new Complex[N]; // 

            for (int u = 0; u < N; ++u)
            {
                double _fi = -2.0 * Math.PI * u / N;
                for (int k = 0; k < N; ++k)
                {
                    double fi = _fi * k;
                    G[u] += (new Complex(Math.Cos(fi), Math.Sin(fi)) * x[k]);
                }
                G[u] = n * G[u]; //для умножения на 1/N для прямого преобразования I
            }

            return G;
        }


        //Мои функции 
        public void ToImage()
        {
            ccImageR = ditifft2d(ccImageR, mImage.GetLength(1), mImage.GetLength(0));
            ccImageG = ditifft2d(ccImageG, mImage.GetLength(1), mImage.GetLength(0));
            ccImageB = ditifft2d(ccImageB, mImage.GetLength(1), mImage.GetLength(0));

            int r, g, b;

            Bitmap nImage = new Bitmap(pictureBox2.Width, pictureBox2.Height);

            for (int i = 0; i < pictureBox2.Width; i++)
            {
                for (int j = 0; j < pictureBox2.Height; j++)
                {
                    r = (int)ccImageR[i * pictureBox2.Height + j].Magnitude;
                    g = (int)ccImageG[i * pictureBox2.Height + j].Magnitude;
                    b = (int)ccImageB[i * pictureBox2.Height + j].Magnitude;

                    if (r > 255)
                    {
                        r = 255;
                    }
                    if (g > 255)
                    {
                        g = 255;
                    }
                    if (b > 255)
                    {
                        b = 255;
                    }

                    if (r < 0)
                    {
                        r = 0;
                    }
                    if (g < 0)
                    {
                        g = 0;
                    }
                    if (b < 0)
                    {
                        b = 0;
                    }

                    Color c = Color.FromArgb(r, g, b);
                    nImage.SetPixel(i, j, c);
                }
            }

            pictureBox2.Image = nImage;
            image2 = nImage;
        }

        public void ToFury()
        {
            maxR = 0;
            maxG = 0;
            maxB = 0;

            for (int i = 0; i < mImage.GetLength(0); i++)
            {
                for (int j = 0; j < mImage.GetLength(1); j++)
                {
                    ccImageR[i * pictureBox2.Height + j] = new Complex(Convert.ToDouble(mImage[i, j].R) * Math.Pow(-1, i + j), 0);
                    ccImageG[i * pictureBox2.Height + j] = new Complex(Convert.ToDouble(mImage[i, j].G) * Math.Pow(-1, i + j), 0);
                    ccImageB[i * pictureBox2.Height + j] = new Complex(Convert.ToDouble(mImage[i, j].B) * Math.Pow(-1, i + j), 0);
                }
            }

            ccImageR = ditfft2d(ccImageR, mImage.GetLength(1), mImage.GetLength(0));
            ccImageG = ditfft2d(ccImageG, mImage.GetLength(1), mImage.GetLength(0));
            ccImageB = ditfft2d(ccImageB, mImage.GetLength(1), mImage.GetLength(0));

            double temp = ccImageR.Max((x) => Math.Log(x.Imaginary));
            if (temp > maxR)
            {
                maxR = temp;
            }
            temp = ccImageG.Max((x) => Math.Log(x.Imaginary));
            if (temp > maxG)
            {
                maxG = temp;
            }
            temp = ccImageB.Max((x) => Math.Log(x.Imaginary));
            if (temp > maxB)
            {
                maxB = temp;
            }

            int r, g, b;

            Bitmap nImage = new Bitmap(pictureBox2.Width, pictureBox2.Height);

            for (int i = 0; i < pictureBox2.Width; i++)
            {
                for (int j = 0; j < pictureBox2.Height; j++)
                {
                    r = (int)(Math.Log((Math.Abs(ccImageR[i * pictureBox2.Height + j].Magnitude) + 1)) * (255 / maxR));
                    g = (int)(Math.Log((Math.Abs(ccImageG[i * pictureBox2.Height + j].Magnitude) + 1)) * (255 / maxG));
                    b = (int)(Math.Log((Math.Abs(ccImageB[i * pictureBox2.Height + j].Magnitude) + 1)) * (255 / maxB));

                    if (r > 255)
                    {
                        r = 255;
                    }
                    if (g > 255)
                    {
                        g = 255;
                    }
                    if (b > 255)
                    {
                        b = 255;
                    }

                    if (r < 0)
                    {
                        r = 0;
                    }
                    if (g < 0)
                    {
                        g = 0;
                    }
                    if (b < 0)
                    {
                        b = 0;
                    }

                    Color c = Color.FromArgb(r, g, b);
                    nImage.SetPixel(i, j, c);
                }
            }

            pictureBox2.Image = nImage;
            image2 = nImage;
        }

        public void FrequencyFilter()
        {
            int x, y, r, r2;
            x = Convert.ToInt32(textBox1.Text);
            y = Convert.ToInt32(textBox2.Text);
            r = Convert.ToInt32(textBox3.Text);

            for (int k = 0; k < ccImageR.Length; k++)
            {
                int i, j;
                i = k / 512;
                j = k - (i * 512);
                double distance = Math.Sqrt((i - x) * (i - x) + (j - y) * (j - y));
                switch (comboBox1.SelectedIndex)
                {
                    case 0:
                        if (!checkBox1.Checked)
                        {
                            if (distance > r)
                            {
                                ccImageR[k] = new Complex(0, 0);
                                ccImageG[k] = new Complex(0, 0);
                                ccImageB[k] = new Complex(0, 0);
                            }
                        }
                        else
                        {
                            double distance2 = Math.Sqrt((i - (pictureBox2.Width - x)) * (i - (pictureBox2.Width - x)) + (j - (pictureBox2.Height - y)) * (j - (pictureBox2.Height - y)));
                            if (distance > r && distance2 > r)
                            {
                                ccImageR[k] = new Complex(0, 0);
                                ccImageG[k] = new Complex(0, 0);
                                ccImageB[k] = new Complex(0, 0);
                            }
                        }
                        break;
                    case 1:
                        if (distance < r)
                        {
                            ccImageR[k] = new Complex(0, 0);
                            ccImageG[k] = new Complex(0, 0);
                            ccImageB[k] = new Complex(0, 0);
                            if (checkBox1.Checked)
                            {
                                ccImageR[k] = new Complex(0, 0);
                                ccImageG[k] = new Complex(0, 0);
                                ccImageB[k] = new Complex(0, 0);
                            }
                        }

                        break;
                    case 2:
                        r2 = Convert.ToInt32(textBox4.Text);
                        if (distance > r && distance < r2)
                        {
                            ccImageR[k] = new Complex(0, 0);
                            ccImageG[k] = new Complex(0, 0);
                            ccImageB[k] = new Complex(0, 0);
                            if (checkBox1.Checked)
                            {
                                ccImageR[k] = new Complex(0, 0);
                                ccImageG[k] = new Complex(0, 0);
                                ccImageB[k] = new Complex(0, 0);
                            }
                        }
                        break;
                    case 3:
                        r2 = Convert.ToInt32(textBox4.Text);
                        if (!checkBox1.Checked)
                        {
                            if (distance > r && distance < r2)
                            {
                            }
                            else
                            {
                                ccImageR[k] = new Complex(0, 0);
                                ccImageG[k] = new Complex(0, 0);
                                ccImageB[k] = new Complex(0, 0);
                            }
                        }
                        else
                        {
                            double distance2 = Math.Sqrt((i - (pictureBox2.Width - x)) * (i - (pictureBox2.Width - x)) + (j - (pictureBox2.Height - y)) * (j - (pictureBox2.Height - y)));
                            if ((distance > r && distance < r2) || (distance2 > r && distance2 < r2))
                            {
                            }
                            else
                            {
                                ccImageR[k] = new Complex(0, 0);
                                ccImageG[k] = new Complex(0, 0);
                                ccImageB[k] = new Complex(0, 0);
                            }
                        }
                        break;
                }

            }
        }

        public void ShowFilter()
        {
            Bitmap nImage = new Bitmap(image2);
            int x, y, r, r2;
            x = Convert.ToInt32(textBox1.Text);
            y = Convert.ToInt32(textBox2.Text);
            r = Convert.ToInt32(textBox3.Text);


            for (int i = 0; i < pictureBox2.Width; i++)
            {
                for (int j = 0; j < pictureBox2.Height; j++)
                {
                    double distance = Math.Sqrt((i - x) * (i - x) + (j - y) * (j - y));

                    switch (comboBox1.SelectedIndex)
                    {
                        case 0:
                            if (!checkBox1.Checked)
                            {
                                if (distance > r)
                                {
                                    Color c = Color.FromArgb(255, 0, 0);
                                    nImage.SetPixel(i, j, c);
                                }
                            }
                            else
                            {
                                double distance2 = Math.Sqrt((i - (pictureBox2.Width - x)) * (i - (pictureBox2.Width - x)) + (j - (pictureBox2.Height - y)) * (j - (pictureBox2.Height - y)));
                                if(distance>r && distance2 > r)
                                {
                                    Color c = Color.FromArgb(255, 0, 0);
                                    nImage.SetPixel(i, j, c);
                                }
                            }
                            break;
                        case 1:
                            if (distance < r)
                            {
                                Color c = Color.FromArgb(255, 0, 0);
                                nImage.SetPixel(i, j, c);
                                if (checkBox1.Checked)
                                {
                                    c = Color.FromArgb(255, 0, 0);
                                    nImage.SetPixel(pictureBox2.Width -1 - i, pictureBox2.Height -1 - j, c);
                                }
                            }

                            break;
                        case 2:
                            r2 = Convert.ToInt32(textBox4.Text);
                            if (distance > r && distance < r2)
                            {
                                Color c = Color.FromArgb(255, 0, 0);
                                nImage.SetPixel(i, j, c);
                                if (checkBox1.Checked)
                                {
                                    c = Color.FromArgb(255, 0, 0);
                                    nImage.SetPixel(pictureBox2.Width - 1 - i, pictureBox2.Height - 1 - j, c);
                                }
                            }
                            break;
                        case 3:
                            r2 = Convert.ToInt32(textBox4.Text);
                            if (!checkBox1.Checked)
                            {
                                if (distance > r && distance < r2)
                                {
                                }
                                else
                                {
                                    Color c = Color.FromArgb(255, 0, 0);
                                    nImage.SetPixel(i, j, c);
                                }
                            }
                            else
                            {
                                double distance2 = Math.Sqrt((i - (pictureBox2.Width - x)) * (i - (pictureBox2.Width - x)) + (j - (pictureBox2.Height - y)) * (j - (pictureBox2.Height - y)));
                                if ((distance > r && distance < r2) || (distance2 > r && distance2 < r2))  
                                {
                                }
                                else
                                {
                                    Color c = Color.FromArgb(255, 0, 0);
                                    nImage.SetPixel(i, j, c);
                                }
                            }
                            break;
                    }

                }
            }

            pictureBox2.Image = nImage;
        }

        public void ShowR2(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex > 1)
            {
                textBox4.Visible = true;
                label4.Visible = true;
            }
        }


        //Франкенштейн преобразования
        public void myPreobraz()
        {
            //Центрируем будущий Фурье-образ
            for (int i = 0; i < cImage.GetLength(0); i++)
            {
                for (int j = 0; j < cImage.GetLength(1); j++)
                {
                    cImage[i, j].R *= Math.Pow(-1, (i + j));
                    cImage[i, j].G *= Math.Pow(-1, (i + j));
                    cImage[i, j].B *= Math.Pow(-1, (i + j));
                }
            }

            //Создаём новый массив, в который будем помещать строки после Фурье-обработки
            PixelComplex[,] cImage2 = new PixelComplex[cImage.GetLength(0), cImage.GetLength(1)];
            for (int i = 0; i < cImage.GetLength(0); i++)
            {
                for (int j = 0; j < cImage.GetLength(1); j++)
                {
                    cImage2[i, j] = new PixelComplex();
                }
            }

            //Создаём временную строки, для перемещения в матрицу после Фурье-обработки
            Complex[] tempR = new Complex[cImage.GetLength(0)];
            Complex[] tempG = new Complex[cImage.GetLength(0)];
            Complex[] tempB = new Complex[cImage.GetLength(0)];

            double maxR = 0, maxG = 0, maxB = 0;


            for (int i = 0; i < cImage.GetLength(1); i++) // 0 - 450
            {
                for (int j = 0; j < cImage.GetLength(0); j++) // 0 - 400
                {
                    tempR[j] = new Complex(Convert.ToDouble(cImage[j, i].R.Real), 0);
                    tempG[j] = new Complex(Convert.ToDouble(cImage[j, i].G.Real), 0);
                    tempB[j] = new Complex(Convert.ToDouble(cImage[j, i].B.Real), 0);

                    double temp = Math.Log((Math.Abs(tempR[j].Magnitude) + 1));
                    if (temp > maxR)
                    {
                        maxR = temp;
                    }
                    temp = Math.Log((Math.Abs(tempG[j].Magnitude) + 1));
                    if (temp > maxG)
                    {
                        maxG = temp;
                    }
                    temp = Math.Log((Math.Abs(tempB[j].Magnitude) + 1));
                    if (temp > maxB)
                    {
                        maxB = temp;
                    }
                }

                tempR = DFT(tempR, (1.0 / tempR.Length));
                tempG = DFT(tempG, (1.0 / tempR.Length));
                tempB = DFT(tempB, (1.0 / tempR.Length));


                for (int k = 0; k < cImage.GetLength(0); k++)
                {
                    double temp = Math.Log((Math.Abs(tempR[k].Magnitude) + 1)) * (255 / maxR);
                    cImage2[k, i].R = temp;
                    temp = Math.Log((Math.Abs(tempG[k].Magnitude) + 1)) * (255 / maxG);
                    cImage2[k, i].G = temp;
                    temp = Math.Log((Math.Abs(tempB[k].Magnitude) + 1)) * (255 / maxB);
                    cImage2[k, i].B = temp;

                }
            }


            Complex[] tempR2 = new Complex[cImage2.GetLength(1)];
            Complex[] tempG2 = new Complex[cImage2.GetLength(1)];
            Complex[] tempB2 = new Complex[cImage2.GetLength(1)];

            maxR = 0;
            maxG = 0;
            maxB = 0;

            for (int i = 0; i < cImage2.GetLength(0); i++) // 0 - 400
            {
                for (int j = 0; j < cImage2.GetLength(1); j++) // 0 - 450
                {
                    tempR2[j] = new Complex(cImage2[i, j].R.Real, cImage2[i, j].R.Imaginary);
                    tempG2[j] = new Complex(cImage2[i, j].G.Real, cImage2[i, j].G.Imaginary);
                    tempB2[j] = new Complex(cImage2[i, j].B.Real, cImage2[i, j].B.Imaginary);
                }

                tempR2 = DFT(tempR2, (1.0 / tempR2.Length));
                tempG2 = DFT(tempG2, (1.0 / tempR2.Length));
                tempB2 = DFT(tempB2, (1.0 / tempR2.Length));

                for (int k = 0; k < cImage2.GetLength(1); k++)
                {
                    cImage2[i, k].R = tempR2[k];
                    cImage2[i, k].G = tempG2[k];
                    cImage2[i, k].B = tempB2[k];
                }
            }


            Bitmap nImage = new Bitmap(pictureBox2.Width, pictureBox2.Height);

            int r, g, b;

            for (int i = 0; i < cImage2.GetLength(0); i++)
            {
                for (int j = 0; j < cImage2.GetLength(1); j++)
                {
                    r = (int)(cImage2[i, j].R.Real);
                    g = (int)(cImage2[i, j].G.Real);
                    b = (int)(cImage2[i, j].B.Real);

                    if (r > 255)
                    {
                        r = 255;
                    }
                    if (g > 255)
                    {
                        g = 255;
                    }
                    if (b > 255)
                    {
                        b = 255;
                    }

                    if (r < 0)
                    {
                        r = 0;
                    }
                    if (g < 0)
                    {
                        g = 0;
                    }
                    if (b < 0)
                    {
                        b = 0;
                    }

                    Color c = Color.FromArgb(r, g, b);
                    nImage.SetPixel(i, j, c);
                }
            }

            pictureBox2.Image = nImage;





        }
    }

    //Классы
    public class Pixel
    {
        public int R;
        public int G;
        public int B;

        public double SR
        {
            get
            {
                return ((R + G + B) / 3);
            }
        }
    }

    public class PixelComplex
    {
        public Complex R;
        public Complex G;
        public Complex B;

        public double SR
        {
            get
            {
                return ((R.Real + G.Real + B.Real) / 3);
            }
        }

        public PixelComplex()
        {
            R = new Complex();
            G = new Complex();
            B = new Complex();
        }

        public PixelComplex(Pixel p)
        {
            R = new Complex(p.R, 0);
            G = new Complex(p.G, 0);
            B = new Complex(p.B, 0);
        }
    }


}
