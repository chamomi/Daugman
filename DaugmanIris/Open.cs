using DaugmanIris.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DaugmanIris
{
    public partial class Open : Form
    {
        Form1 parent;
        public Open(Form1 par)
        {
            InitializeComponent();
            parent = par;

            //var list = new ListView();
            list.Dock = DockStyle.Fill;
            list.GridLines = true;
            list.View = View.Details;
            list.Columns.Add("File name", -2);
            list.DoubleClick += List_DoubleClick;         
            using (var db = new ImageContext())
            {
                var query = from im in db.MyImages
                            orderby im.Name
                            select im;

                foreach (var item in query)
                {
                    ListViewItem lvi = new ListViewItem(item.Name);
                    list.Items.Add(lvi);
                }
            }
            this.Controls.Add(list);
        }

        private void List_DoubleClick(object sender, EventArgs e)
        {
            parent.DisplayFromDb(list.SelectedItems[0].Text);
            this.Close();
        }

        private void compareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (list.SelectedItems.Count != 2)
            {
                MessageBox.Show("Choose two items to compare");
                return;
            }
            using (var db = new ImageContext())
            {
                var img1 = db.MyImages.Find(list.SelectedItems[0].Text);
                var img2 = db.MyImages.Find(list.SelectedItems[1].Text);

                List<int> f1 = findVector(img1);
                List<int> f2 = findVector(img2);

                //compare two vectors
            }
        }

        private List<int> findVector(MyImage imgIn)
        {
            //normalise
            int length = 800;
            Bitmap img = new Bitmap(imgIn.GetPicture());
            Bitmap norm = new Bitmap(length + 1, imgIn.IrisR-imgIn.PupilR);

            double fi = 0;
            double step = 2 * Math.PI / length;
            int count = 0;
            while (fi < 2 * Math.PI)
            {
                int curR = imgIn.PupilR;
                int newy = norm.Height - 1;
                while(curR < imgIn.IrisR)
                {
                    int x = (int)(imgIn.IrisX + curR * Math.Cos(fi));
                    int y = (int)(imgIn.IrisY + curR * Math.Sin(fi));

                    Color c = img.GetPixel(y, x);
                    norm.SetPixel(count, newy, c);
                    curR ++;
                    newy--;
                }
                count++;
                fi += step;
            }

            //median filter
            Bitmap filt = MedianFilter(norm, 3);

            //lbp
            Bitmap lbp = LBP(filt);

            //encoding
            List<int> result = Encode(lbp);

            foreach (var elem in result)
                Trace.Write(elem + " ");

            Trace.WriteLine("");

            return result;
        }

        private List<int> Encode(Bitmap img)
        {
            List<int> features = new List<int>();

            double mgl = 0;
            for (int i = 0; i < img.Width; i++)
                for (int j = 0; j < img.Height; j++)
                    mgl += img.GetPixel(i, j).R;
            mgl /= img.Width * 24; //CHANGE HEIGHT

            double sgl = 0;
            for (int i = 0; i < img.Width; i++)
                for (int j = 0; j < img.Height; j++)
                    sgl += (img.GetPixel(i, j).R - mgl) * (img.GetPixel(i, j).R - mgl);
            sgl /= img.Width * 24; //CHANGE HEIGHT
            sgl = Math.Sqrt(sgl);

            double mprev = 0, sprev = 0;

            //blocks 8x8
            for(int a = 0; a < 17; a += 8) //TO DO: NORMALISE IMAGE HEIGHT AND CHANGE STOP CONDITION
                for(int b = 0; b < img.Width-1; b += 8)
                {
                    double m = 0;
                    for (int i = a; i < (a + 8); i++)
                        for (int j = b; j < (b + 8); j++)
                            m += img.GetPixel(j, i).R;
                    m /= 64;

                    double s = 0;
                    for (int i = a; i < (a + 8); i++)
                        for (int j = b; j < (b + 8); j++)
                            s += (img.GetPixel(j, i).R - m) * (img.GetPixel(j, i).R - m);
                    s /= 64;
                    s = Math.Sqrt(s);

                    if (m > mgl) features.Add(1);
                    else features.Add(0);
                    if (s > sgl) features.Add(1);
                    else features.Add(0);
                    if (mprev > mgl) features.Add(1);
                    else features.Add(0);
                    if (sprev > sgl) features.Add(1);
                    else features.Add(0);

                    mprev = m;
                    sprev = s;
                }

            return features;
        }

        //lbp for r=4, p=8
        private Bitmap LBP(Bitmap img)
        {
            Bitmap result = new Bitmap(img);

            for (int i = 0; i < img.Width; i++)
                for (int j = 0; j < img.Height; j++)
                {
                    string bin = "";

                    List<Coord> coords = new List<Coord>();
                    Coord cor = new Coord();
                    cor.x = j;
                    cor.y = i + 4;
                    coords.Add(cor);
                    //cor.x = j + 2;
                    //cor.y = i + 4;
                    //coords.Add(cor);
                    for (int k = 4; k > -5; k -= 4) //changed step
                    {
                        cor.x = j + 4;
                        cor.y = i + k;
                        coords.Add(cor);
                    }
                    //cor.x = j + 2;
                    //cor.y = i - 4;
                    //coords.Add(cor);
                    cor.x = j;
                    cor.y = i - 4;
                    coords.Add(cor);
                    //cor.x = j - 2;
                    //cor.y = i - 4;
                    //coords.Add(cor);
                    for (int k = -4; k < 5; k += 4) //changed step from 2
                    {
                        cor.x = j - 4;
                        cor.y = i + k;
                        coords.Add(cor);
                    }
                    //cor.x = j - 2;
                    //cor.y = i + 4;
                    //coords.Add(cor);

                    // R = 1, P = 8
                    //cor.x = j;
                    //cor.y = i + 1;
                    //coords.Add(cor);
                    //cor.x = j + 1;
                    //cor.y = i + 1;
                    //coords.Add(cor);
                    //cor.x = j + 1;
                    //cor.y = i;
                    //coords.Add(cor);
                    //cor.x = j + 1;
                    //cor.y = i - 1;
                    //coords.Add(cor);
                    //cor.x = j;
                    //cor.y = i - 1;
                    //coords.Add(cor);
                    //cor.x = j - 1;
                    //cor.y = i - 1;
                    //coords.Add(cor);
                    //cor.x = j - 1;
                    //cor.y = i;
                    //coords.Add(cor);
                    //cor.x = j - 1;
                    //cor.y = i + 1;
                    //coords.Add(cor);

                    foreach (var cord in coords)
                    {
                        int x = cord.x, y = cord.y;
                        if (x < 0) x = 0;
                        if (x > (img.Height - 1)) x = img.Height - 1;
                        if (y < 0) y = 0;
                        if (y > (img.Width - 1)) y = img.Width - 1;

                        if (img.GetPixel(i, j).R > img.GetPixel(y, x).R) bin += "0";
                        else bin += "1";
                    }
                    int outp = Convert.ToInt32(bin, 2);
                    result.SetPixel(i, j, Color.FromArgb(result.GetPixel(i, j).A, Check(outp), Check(outp), Check(outp)));
                }

            return result;
        }

        //check colors' bounds
        private int Check(double n)
        {
            if (n < 0) return 0;
            else if (n > 255) return 255;
            else return (int)n;
        }

        public static Bitmap MedianFilter(Bitmap sourceBitmap, int matrixSize)
        {
            //int bias = 0;
            bool grayscale = true;
            BitmapData sourceData =
                       sourceBitmap.LockBits(new Rectangle(0, 0,
                       sourceBitmap.Width, sourceBitmap.Height),
                       ImageLockMode.ReadOnly,
                       PixelFormat.Format32bppArgb);


            byte[] pixelBuffer = new byte[sourceData.Stride *
                                          sourceData.Height];


            byte[] resultBuffer = new byte[sourceData.Stride *
                                           sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0,
                                       pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);


            if (grayscale == true)
            {
                float rgb = 0;


                for (int k = 0; k < pixelBuffer.Length; k += 4)
                {
                    rgb = pixelBuffer[k] * 0.11f;
                    rgb += pixelBuffer[k + 1] * 0.59f;
                    rgb += pixelBuffer[k + 2] * 0.3f;


                    pixelBuffer[k] = (byte)rgb;
                    pixelBuffer[k + 1] = pixelBuffer[k];
                    pixelBuffer[k + 2] = pixelBuffer[k];
                    pixelBuffer[k + 3] = 255;
                }
            }


            int filterOffset = (matrixSize - 1) / 2;
            int calcOffset = 0;


            int byteOffset = 0;

            List<int> neighbourPixels = new List<int>();
            byte[] middlePixel;


            for (int offsetY = filterOffset; offsetY <
                sourceBitmap.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX <
                    sourceBitmap.Width - filterOffset; offsetX++)
                {
                    byteOffset = offsetY *
                                 sourceData.Stride +
                                 offsetX * 4;


                    neighbourPixels.Clear();


                    for (int filterY = -filterOffset;
                        filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset;
                            filterX <= filterOffset; filterX++)
                        {


                            calcOffset = byteOffset +
                                         (filterX * 4) +
                                (filterY * sourceData.Stride);


                            neighbourPixels.Add(BitConverter.ToInt32(
                                             pixelBuffer, calcOffset));
                        }
                    }


                    neighbourPixels.Sort();

                    middlePixel = BitConverter.GetBytes(
                                       neighbourPixels[filterOffset]);


                    resultBuffer[byteOffset] = middlePixel[0];
                    resultBuffer[byteOffset + 1] = middlePixel[1];
                    resultBuffer[byteOffset + 2] = middlePixel[2];
                    resultBuffer[byteOffset + 3] = middlePixel[3];
                }
            }


            Bitmap resultBitmap = new Bitmap(sourceBitmap.Width,
                                             sourceBitmap.Height);


            BitmapData resultData =
                       resultBitmap.LockBits(new Rectangle(0, 0,
                       resultBitmap.Width, resultBitmap.Height),
                       ImageLockMode.WriteOnly,
                       PixelFormat.Format32bppArgb);


            Marshal.Copy(resultBuffer, 0, resultData.Scan0,
                                       resultBuffer.Length);


            resultBitmap.UnlockBits(resultData);


            return resultBitmap;
        }

        private void findVectorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var db = new ImageContext())
            {
                var img1 = db.MyImages.Find(list.SelectedItems[0].Text);
                findVector(img1);
                //var show = new ShowVector(findVector(img1));
                //show.Show();
            }
        }
    }
}
