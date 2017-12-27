using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    public partial class CPRExplanation : Form
    {

        int numbernext=0;
        public CPRExplanation()
        {
            InitializeComponent();
        }

        private void CPRExplanation_Load(object sender, EventArgs e)
        {
            explantion();
            pictureBox1.Image = Image.FromFile(@"C:\\Users\\偉勝\\Desktop\\CPR-Beta-1.0\\CPR-Beta-1.0\\1.jpg");
            label1.Text = "步驟一，將雙手張開，讓體感偵測";
            last.ImageAlign = ContentAlignment.MiddleRight;
        //    last.Image = Image.FromFile("C:\\Users\\偉勝\\Desktop\\CPR-Beta-1.0\\CPR-Beta-1.0\\上一步.png");
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            
        }

        private void Next_Click(object sender, EventArgs e)
        {
            numbernext=numbernext+1;
            explantion();
           
            }

        private void button2_Click(object sender, EventArgs e)
        {
            numbernext = numbernext - 1;
            explantion();
        }



        private void explantion()
        {
           if(numbernext<0)
           {
               numbernext = 0;
           }
           else if (numbernext > 4)
           {
               numbernext = 4;
           }
            switch (numbernext)
            {
                case 0:
                    pictureBox1.Image = Image.FromFile(@"C:\\Users\\偉勝\\Desktop\\CPR-Beta-1.0\\CPR-Beta-1.0\\1.jpg");
                    label1.Text = "步驟一，將雙手張開，讓體感偵測";
                     break;
                case 1:
                    pictureBox1.Image = Image.FromFile(@"C:\\Users\\偉勝\\Desktop\\CPR-Beta-1.0\\CPR-Beta-1.0\\2.jpg");
                    label1.Text = "步驟二，一隻手比出1，一隻手張開表示開始";
                    break;
                case 2:
                    pictureBox1.Image = Image.FromFile(@"C:\\Users\\偉勝\\Desktop\\CPR-Beta-1.0\\CPR-Beta-1.0\\3.jpg");
                    label1.Text = "如要重來，將兩手握拳，就可以重至";
                    break;
                case 3:
                    pictureBox1.Image = Image.FromFile(@"C:\\Users\\偉勝\\Desktop\\CPR-Beta-1.0\\CPR-Beta-1.0\\4.jpg");
                    label1.Text = "步驟三，將雙手靠近並放置安妮胸口";
                    break;
                case 4:
                    pictureBox1.Image = Image.FromFile(@"C:\\Users\\偉勝\\Desktop\\CPR-Beta-1.0\\CPR-Beta-1.0\\4.jpg");
                    label1.Text = "步驟四，等待聲音訊息";
                    break;
            }
        
        }
        
        
    }
}
