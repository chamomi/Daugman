using DaugmanIris.Model;
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
    }
}
