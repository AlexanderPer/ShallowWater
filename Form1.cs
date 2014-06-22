using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shadertoy
{
    public partial class ShaderForm : Form
    {
        public ShaderForm()
        {
            InitializeComponent();
            //this.txtInputShader.Text = ShaderWindow.FragmentShaderExample;
            this.txtInputShader.Text = System.IO.File.ReadAllText("..\\..\\Shader\\SWater.glsl");
            /////
            RunFragmentShader();
        }

        private void btnRunShader_Click(object sender, EventArgs e)
        {
            RunFragmentShader();
        }

        private void RunFragmentShader()
        {
            ShaderWindow.FragmentShaderSource = this.txtInputShader.Lines;
            ShaderWindow.RunFragmentShader();
        }
    }
}
