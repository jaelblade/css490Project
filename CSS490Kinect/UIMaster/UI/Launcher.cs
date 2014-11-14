using Master;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UIMaster.UI
{
    public partial class Launcher : Form
    {
        private TrueRandom trueRandom;
        public Launcher()
        {
            this.trueRandom = new TrueRandom();
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.graphPanel.addData(trueRandom.Next());
        }
    }
}
