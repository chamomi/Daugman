using DaugmanIris.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DaugmanIris
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            //using (var db = new ImageContext())
            //{
            //    foreach (var entity in db.MyImages)
            //        db.MyImages.Remove(entity);
            //    db.SaveChanges();
            //    var query = from im in db.MyImages
            //                orderby im.Name
            //                select im;

            //    //Console.WriteLine("All blogs in the database:");
            //    foreach (var item in query)
            //    {
            //        Trace.WriteLine(item.Name + " " + item.IrisR + " " + item.PupilR);
            //    }
            //}
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            if (pictureBox2.Image != null)
            {
                pictureBox2.Image.Dispose();
            }
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "JPeg Image|*.jpg|Bitmap Image|*.bmp|Gif Image|*.gif";
            if (op.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Image = Image.FromFile(op.FileName);
                label1.Text = op.FileName;
            }
        }

        //image preprocessing, starts Daugman algorithm for iris and pupil
        private void Iris_Click(object sender, EventArgs e)
        {
            try
            {
                Bitmap b = new Bitmap(pictureBox1.Image);
                Bitmap img = HistStretch(b);
                int[,] Gauss = new int[3, 3] { { 1, 2, 1 }, { 2, 4, 2 }, { 1, 2, 1 } };
                ApplyMatrix(Gauss, 16);

                int edgeV = img.Height / 3;
                int edgeH = img.Width / 3;

                Iris iris = Daugman(img, edgeV, edgeH, true);
                DrawCircle(iris, img);
                Bitmap bnew = new Bitmap(pictureBox2.Image);
                Trace.WriteLine("iris found: x= " + iris.x.ToString() + " y= " + iris.y.ToString() + " r= " + iris.r.ToString());

                edgeV = iris.y - iris.r;
                edgeH = iris.x - iris.r;
                Bitmap bnew1 = Threshold(new Bitmap(bnew), 50);
                Iris pupil = Daugman(bnew1, edgeV, edgeH, false);
                DrawCircle(pupil, bnew);
                Trace.WriteLine("pupil found: x= " + pupil.x.ToString() + " y= " + pupil.y.ToString() + " r= " + pupil.r.ToString());

                Trace.WriteLine("done");

                using (var db = new ImageContext())
                {
                    if (db.MyImages.Find(label1.Text) == null)
                    {
                        var newimg = new MyImage { IrisX = iris.x, IrisY = iris.y, IrisR = iris.r, PupilX = pupil.x, PupilY = pupil.y, PupilR = pupil.r, Name = label1.Text };
                        newimg.SavePicture(pictureBox1.Image);
                        db.MyImages.Add(newimg);
                        db.SaveChanges();
                    }
                }
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("No initial file was chosen");
            }
        }

        //Daugman algorithm
        //edgeV, edgeH - vertical and horizontal edges where we assume center point cannot be located
        //if isiris == false, we are searching for pupil
        //returns object iris which was found
        private Iris Daugman(Bitmap img, int edgeV, int edgeH, bool isiris)
        {
            int rstart, rchange, rfin;
            if (isiris)
            {
                rstart = 40;
                rchange = 5;
                rfin = img.Height / 2;
            }
            else
            {
                rstart = 7;
                rchange = 2;
                rfin = edgeH;
            }
            var iris = new Iris();
            iris.diff = 0;

            for (int i = edgeH; i < img.Width - edgeH; i++)
                for (int j = edgeV; j < img.Height - edgeV; j++)
                {
                    var list = new List<double>();
                    for (int r = rstart; r < rfin; r += rchange) 
                    {
                        double sum = GetSum(j, i, r, img);
                        if (sum == 0) break;
                        list.Add(sum);
                    }
                    int rmax = 0;
                    double summax = 0;
                    for (int r = 1; r < list.Count - 1; r++)
                    {
                        if ((list.ElementAt(r) - list.ElementAt(r - 1)) > summax)
                        {
                            summax = list.ElementAt(r) - list.ElementAt(r - 1);
                            rmax = (r - 1) * rchange + rstart;
                        }
                    }
                    if (summax > iris.diff)
                    {
                        iris.x = j; //by height
                        iris.y = i; //by width
                        iris.r = rmax;
                        iris.diff = summax;
                        Trace.WriteLine("new max sum found, params updated: x= " + j.ToString() + " y= " + i.ToString() + " r= " + rmax.ToString() + " summax=" + summax.ToString());
                    }
                    list.Clear();
                }
            return iris;
        }

        //returns sum of pixels on edge of circle given by parameters: x0, y0, r
        private double GetSum(int x0, int y0, int r, Bitmap img)
        {
            int sum = 0;
            HashSet<Coord> circle = new HashSet<Coord>();
            double fi = 0;
            double step = 2 * Math.PI / 10000;
            while(fi < 2 * Math.PI)
            {
                var coord = new Coord();
                coord.x = (int)(x0 + r * Math.Cos(fi));
                coord.y = (int)(y0 + r * Math.Sin(fi));
                if ((coord.x < 0) || (coord.y < 0) || (coord.x >= img.Height) || (coord.y >= img.Width)) return 0;
                circle.Add(coord);
                fi += step;
            }
            var circleList = circle.ToList();
            foreach(var point in circleList)
            {
                Color c = img.GetPixel(point.y, point.x);
                sum += (c.R + c.G + c.B) / 3;
            }
            return sum / (2 * Math.PI * r);
        }

        //draws circle given by iris
        private void DrawCircle(Iris iris, Bitmap img)
        {
            HashSet<Coord> circle = new HashSet<Coord>();
            double fi = 0;
            double step = 2 * Math.PI / 10000;
            while (fi < 2 * Math.PI)
            {
                var coord = new Coord();
                coord.x = (int)(iris.x + iris.r * Math.Cos(fi));
                coord.y = (int)(iris.y + iris.r * Math.Sin(fi));
                if ((coord.x < 0) || (coord.y < 0) || (coord.x >= img.Height) || (coord.y >= img.Width)) continue;
                circle.Add(coord);
                fi += step;
            }
            var circleList = circle.ToList();
            foreach (var point in circleList)
            {
                Color c = img.GetPixel(point.y, point.x);
                img.SetPixel(point.y, point.x, Color.FromArgb(c.A, 0, 0, 255));
            }
            pictureBox2.Image = img;
        }

        //thresholding
        private Bitmap Threshold(Bitmap b, int threshold)
        {
            try
            {
                Color c;
                for (int j = 0; j < b.Height; j++)
                    for (int i = 0; i < b.Width; i++)
                    {
                        c = b.GetPixel(i, j);
                        if ((c.R + c.G + c.B) < threshold * 3)
                            b.SetPixel(i, j, Color.FromArgb(c.A, 0, 0, 0));
                        else b.SetPixel(i, j, Color.FromArgb(c.A, 255, 255, 255));
                    }
                return b;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("No initial file was chosen");
                return null;
            }
        }

        //kernel application
        private Bitmap ApplyMatrix(int[,] kernel, int divisor = 1, int offset = 0)
        {
            Bitmap b, result;
            try
            {
                b = new Bitmap(pictureBox1.Image);
                result = new Bitmap(pictureBox1.Image);

                int x, y;
                int halfwidth = kernel.GetLength(1) / 2;
                int halfheigth = kernel.GetLength(0) / 2;
                int wend = halfwidth;
                if (kernel.GetLength(1) % 2 == 1) wend++;
                int hend = halfheigth;
                if (kernel.GetLength(0) % 2 == 1) hend++;
                int[] sum = new int[3];
                Color c;

                for (int i = 0; i < b.Width; i++)
                    for (int j = 0; j < b.Height; j++)
                    {
                        for (int k = 0; k < 3; k++)
                            sum[k] = 0;
                        for (int m = -halfwidth; m < wend; m++)
                            for (int n = -halfheigth; n < hend; n++)
                            {
                                if ((i + m) < 0) x = 0;
                                else if ((i + m) >= b.Width) x = b.Width - 1;
                                else x = i + m;

                                if ((j + n) < 0) y = 0;
                                else if ((j + n) >= b.Height) y = b.Height - 1;
                                else y = j + n;

                                c = b.GetPixel(x, y);
                                sum[0] += c.R * kernel[n + halfheigth, m + halfwidth];
                                sum[1] += c.G * kernel[n + halfheigth, m + halfwidth];
                                sum[2] += c.B * kernel[n + halfheigth, m + halfwidth];
                            }
                        try
                        {
                            result.SetPixel(i, j, Color.FromArgb(result.GetPixel(i, j).A, Check(sum[0] / divisor + offset), Check(sum[1] / divisor + offset), Check(sum[2] / divisor + offset)));
                        }
                        catch (InvalidOperationException)
                        {
                            MessageBox.Show("Images with indexes pixels are unfortunately unsupported");
                            break;
                        }
                    }
                return result;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("No initial file was chosen");
                return null;
            }
        }

        //check colors' bounds
        private int Check(double n)
        {
            if (n < 0) return 0;
            else if (n > 255) return 255;
            else return (int)n;
        }

        //histogram normalisation
        private Bitmap HistStretch(Bitmap img)
        {
            try
            {
                int[][] hist = new int[3][];
                for (int i = 0; i < 3; i++)
                    hist[i] = new int[256];
                Hist(hist);

                int[] Imax = new int[3];
                int[] Imin = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    int j = 0;
                    while (hist[i][j] == 0) j++;
                    Imin[i] = j;

                    j = hist[i].Length - 1;
                    while (hist[i][j] == 0) j--;
                    Imax[i] = j;
                }

                int[][] LUT = new int[3][];
                for (int i = 0; i < 3; i++)
                {
                    LUT[i] = new int[256];

                    for (int j = 0; j < 256; j++)
                        LUT[i][j] = 255 * (j - Imin[i]) / (Imax[i] - Imin[i]);
                }

                Color c;
                Bitmap b = new Bitmap(img);
                for (int j = 0; j < b.Height; j++)
                    for (int i = 0; i < b.Width; i++)
                    {
                        c = b.GetPixel(i, j);
                        b.SetPixel(i, j, Color.FromArgb(c.A, Check(LUT[0][c.R]), Check(LUT[1][c.G]), Check(LUT[2][c.B])));
                    }
                return b;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("No initial file was chosen");
                return null;
            }
        }

        //creates histograms for image
        private void Hist(int[][] hist)
        {
            hist.Initialize();

            Color c;
            Bitmap b = (Bitmap)pictureBox1.Image;

            for (int j = 0; j < b.Height; j++)
                for (int i = 0; i < b.Width; i++)
                {
                    c = b.GetPixel(i, j);
                    hist[0][c.R]++;
                    hist[1][c.G]++;
                    hist[2][c.B]++;
                }
        }

        //draw circle click
        private void goToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var iris = new Iris();
                iris.x = Int32.Parse(toolStripTextBox1.Text);
                iris.y = Int32.Parse(toolStripTextBox2.Text);
                iris.r = Int32.Parse(toolStripTextBox3.Text);
                DrawCircle(iris, new Bitmap(pictureBox1.Image));
            }
            catch (System.FormatException)
            {
                MessageBox.Show("FormatException");
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("No initial file was chosen");
            }
        }

        private void openFromDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var open = new Open(this);
            open.Show();
        }

        public void DisplayFromDb(string name)
        {
            using (var db = new ImageContext())
            {
                var img = db.MyImages.Find(name);
                pictureBox1.Image = img.GetPicture();
                label1.Text = img.Name;
                Iris iris = new Iris();
                iris.x = img.IrisX;
                iris.y = img.IrisY;
                iris.r = img.IrisR;
                DrawCircle(iris, new Bitmap(pictureBox1.Image));

                Iris pupil = new Iris();
                pupil.x = img.PupilX;
                pupil.y = img.PupilY;
                pupil.r = img.PupilR;
                DrawCircle(pupil, new Bitmap(pictureBox2.Image));
            }
        }
    }

    public struct Iris
    {
        public int x { get; set; }
        public int y { get; set; }
        public int r { get; set; }
        public double diff { get; set; }
    }

    public struct Coord
    {
        public int x { get; set; }
        public int y { get; set; }
    }
}
