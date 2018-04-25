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

                if(img1.FVector == null)
                {
                    //calculate and save fVector
                    findVector(img1);
                }

                if(img2.FVector == null)
                {

                }

                //compare two vectors
            }
        }

        private Bitmap findVector(MyImage imgIn)
        {
            //normalise
            Bitmap img = new Bitmap(imgIn.GetPicture());
            Bitmap norm = new Bitmap(1001, imgIn.IrisR-imgIn.PupilR);

            double fi = 0;
            double step = 2 * Math.PI / 1000;
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
                
                //if ((count == 10)||(count == 250) || (count == 500) || (count == 750)) Trace.WriteLine("work");
                count++;
                fi += step;
            }

            //median filter
            Bitmap filt = MedianFilter(norm, 3);
            return filt;

            //lbp

            //encoding
            int[] result = new int[500];



            //return result;
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
                var show = new ShowVector(findVector(img1));
                show.Show();
            }
        }
    }
}
