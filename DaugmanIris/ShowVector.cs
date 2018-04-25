using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DaugmanIris
{
    public partial class ShowVector : Form
    {
        public ShowVector(Bitmap img)
        {
            InitializeComponent();
            pictureBox1.Image = img;
        }
    }
}
