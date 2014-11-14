using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Master.UI
{
    public partial class GraphPanel : UserControl
    {
        protected LinkedList<double> data;
        protected int MAX_SIZE = 2000;

        public GraphPanel()
        {
            data = new LinkedList<double>();
            InitializeComponent();
        }

        public void clearData()
        {
            data.Clear();
        }

        /// <summary>
        /// <para>If queue is full, it will remove the first in the queue and
        /// will add the new data element to the end of the graph's data. </para>
        /// </summary>
        /// <param name="d"></param>
        public void addData(double d)
        {
            if (data.Count > MAX_SIZE)
            {
                data.RemoveFirst();
            }
            data.AddLast(d);
            this.Refresh();
        }

        private void GraphPanel_Load(object sender, EventArgs e)
        {

        }

        private void GraphPanel_Paint(object sender, PaintEventArgs e)
        {
            Font f = new Font("tahoma", 12);
            Brush b = new SolidBrush(Color.Black);
            //e.Graphics.DrawString(data.Last.Value + "", f, b, 0, 20);
        }
    }
}
