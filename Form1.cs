using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Win32;
using System.Collections.Concurrent;
using Emgu.CV.CvEnum;
using Emgu.CV.BgSegm;
using Emgu.CV.Ocl;
using System.Globalization;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public struct block
        {

            public double[,,] value;
            public int id;
            public double[,,] leftoverlap;
            public double[,,] rightoverlap;
            public double[,,] topoverlap;
            public double[,,] downoverlap;

        };
        List<block> texture = new List<block>();
        public int[,] result = new int[105, 105];

        List<List<Tuple<int, int, int> > >[,] horboundary = new List<List<Tuple<int, int, int>>>[30,30];
        List<List<Tuple<int, int, int>>>[,] vertboundary = new List<List<Tuple<int, int, int>>>[30,30];

        static public int siz = 11;
        
        static public int kernel = siz * 2 + 1;

        static public int overlap = kernel / 6;

        static int blocknumber = 25;

        static public double alpha =0.1;

        static int round = 2;

        public double Cmp(int block1, int block2,int dir)
        {
            List<double> vect1 = new List<double>();
            List<double> vect2 = new List<double>();
          
            if (dir == 0)
            {

                for (int i = kernel - overlap; i < kernel; i++)
                {

                    for (int j = 0; j < kernel; j++)
                    {
                        double num = texture[block1].value[i, j, 0] + texture[block1].value[i, j, 1] + texture[block1].value[i, j, 2];
                        num /= 3;
                        vect1.Add(num);
                    }

                }

                for (int i = 0; i < overlap; i++)
                {
                    for (int j = 0; j < kernel; j++)
                    {
                        double num = texture[block2].value[i, j, 0] + texture[block2].value[i, j, 1] + texture[block2].value[i, j, 2];
                        num /= 3;
                        vect2.Add(num);
                    }
                }
            }
            else if (dir == 1)
            {

                for (int i = 0; i < kernel; i++)
                {

                    for (int j = kernel - overlap; j < kernel; j++)
                    {
                        double num = texture[block1].value[i, j, 0] + texture[block1].value[i, j, 1] + texture[block1].value[i, j, 2];
                        num /= 3;
                        vect1.Add(num);
                    }

                }

                for (int i = 0; i < kernel; i++)
                {
                    for (int j = 0; j < overlap; j++)
                    {
                        double num = texture[block2].value[i, j, 0] + texture[block2].value[i, j, 1] + texture[block2].value[i, j, 2];
                        num /= 3;
                        vect2.Add(num);
                    }
                }


            }
            
            double ans = 0;
            for (int i = 0; i < vect1.Count; i++) {
                ans+= Math.Pow(vect1[i] - vect2[i],2) ;
                
            }

            return ans;

        }



        public List<List<Tuple<int, int, int>>> solvehorpath(int blocka,int blockb) {


            List<List<Tuple<int, int, int> > > ans = new List<List<Tuple<int, int, int> > > ();
            
            block a = texture[blocka];
            block b = texture[blockb];
            double[,,] num = new double[300, 300, 3];
            double[,] dis = new double[300, 300];
            for (int i = 0; i < overlap; i++)
            {
                for (int j = 0; j < kernel; j++)
                {
                    num[i+overlap, j, 0] = b.value[i, j, 0];
                    num[i+overlap, j, 1] = b.value[i, j, 1];
                    num[i+overlap, j, 2] = b.value[i, j, 2];

                    num[i, j , 0] = a.value[kernel-overlap+i, j, 0];
                    num[i, j , 1] = a.value[kernel-overlap+i, j, 1];
                    num[i, j , 2] = a.value[kernel-overlap+i, j, 2];

                    dis[i, j] = num[i, j, 0] + num[i, j, 1] + num[i, j, 2];
                    dis[i, j] /= 3;
                    dis[i+overlap, j ] = num[i+overlap, j, 0] + num[i+overlap, j , 1] + num[i+overlap, j , 2];
                    dis[i+overlap, j ] /= 3;
                    dis[i, j] = Math.Pow(dis[i, j] - dis[i + overlap, j], 2);
                    dis[i, j] = Math.Sqrt(dis[i, j]);
                }
            }

            double[,] dp = new double[300, 300];
            for (int i = 0; i < 300; i++)
            {
                for (int j = 0; j < 300; j++)
                {
                    dp[i, j] = 10000000;
                }
            }

            int[,] record = new int[300, 300];
            for (int i = 0; i < overlap; i++)
            {
                dp[i,0 ] = dis[i, 0];
            }
            for (int j = 1; j < kernel; j++) {
               
           
                for (int i = 0; i < overlap ; i++)

                {
                    if (i - 1 >= 0 )
                    {
                        double tmp = dp[i - 1, j - 1] + dis[i, j];
                        if (tmp < dp[i, j])
                        {
                            dp[i, j] = tmp;
                            record[i, j] = -1;
                        }

                    }

                    if (j - 1 >= 0)
                    {
                        double tmp = dp[i , j - 1] + dis[i, j];
                        if (tmp < dp[i, j])
                        {
                            dp[i, j] = tmp;
                            record[i, j] = 0;
                        }

                    }


                    if (i+1 < overlap)
                    {
                        double tmp = dp[i + 1, j - 1] + dis[i, j];
                        if (tmp < dp[i, j])
                        {
                            dp[i, j] = tmp;
                            record[i, j] = 1;
                        }
                    }

                }

            }

            double minr = 1000000;
            int start = 0;
            for (int i = 0; i < overlap; i++)
            {
                if (dp[i, kernel-1] < minr)
                {
                    minr = dp[i, kernel-1];
                    start = i;
                }
            }

            List<Tuple<int, int>> path = new List<Tuple<int, int>>();
            path.Add(new Tuple<int, int>(start, kernel-1 ) );
            start += record[start, kernel-1];

            for (int i = kernel - 2; i >= 0; i--)
            {
                path.Add(new Tuple<int, int>(start, i));
                start += record[start, i];
            }

            path.Reverse();

            
            for (int i = 0; i < path.Count; i++)
            {
                    ans.Add(new List<Tuple<int, int, int>>());
   
            
                int ii = path[i].Item1;
                int flag = 0;
                for (int j = 0; j <= ii; j++) {
    

                    Tuple<int, int, int> pixel = new Tuple<int, int, int>((int)num[j, i, 0],
        (int)num[j, i, 1],
        (int)num[j, i, 2]);
                    ans[i].Add(pixel);
          

                }

                for (int j = ii + 1; j < overlap; j++)
                {
                    Tuple<int, int, int> pixel = new Tuple<int, int, int>((int)num[j+overlap, i, 0],
    (int)num[j+overlap, i, 1],
    (int)num[j+overlap, i, 2]);
                    ans[i].Add(pixel);
                }



            }

            return ans;


        }

        public List<List<Tuple<int, int, int>>> solvevertpath(int blocka,int blockb)
        {

            List<List<Tuple<int, int, int>>> ans =new List<List<Tuple<int, int, int>>>();

            block a = texture[blocka];
            block b = texture[blockb];
            double[,,] num = new double[300, 300,3];
            double[,] dis = new double[300, 300];
            
            for (int i = 0; i < kernel; i++)
            {
                for (int j = 0; j < overlap; j++)
                {

                    num[i, j+overlap, 0] = b.value[i, j, 0];
                    num[i, j+overlap, 1] = b.value[i, j, 1];
                    num[i, j+overlap, 2] = b.value[i, j, 2];

                    num[i, j , 0] = a.value[i, kernel-overlap+j, 0];
                    num[i , j , 1] = a.value[i, kernel-overlap+j, 1];
                    num[i , j , 2] = a.value[i, kernel-overlap+j, 2];


                    dis[i, j] = num[i, j, 0] + num[i, j,1] + num[i, j, 2];
                    dis[i, j] /= 3;
                    dis[i , j+overlap] = num[i , j+overlap, 0] + num[i , j+overlap, 1] + num[i , j+overlap, 2];
                    dis[i , j+overlap] /= 3;

                    dis[i, j] = Math.Pow(dis[i, j] - dis[i, j+overlap], 2);
             
                }
            }
            Image<Bgr, Byte> debug = new Image<Bgr, Byte>(600, 600);


            for (int i = 0; i < kernel; i++) {
                for (int j = 0; j < overlap*2 ; j++) {
                    debug.Data[i, j, 0] = (byte)num[i, j, 0];
                    debug.Data[i, j, 1] = (byte)num[i, j, 1];
                    debug.Data[i, j, 2] = (byte)num[i, j, 2];
                }
            }

           
           

            double[,] dp = new double[300, 300];
            for (int i = 0; i < 300; i++) {
                for (int j = 0; j < 300; j++) {
                    dp[i, j] = 10000000;
                }
            }

            int[,] record = new int[300, 300];
            for (int i = 0; i < overlap; i++) {
                dp[0, i] = dis[0, i];
            }

            for (int i = 1; i < kernel; i++) {

                for (int j = 0; j < overlap; j++) {


                    if (j - 1 >= 0) {
                        double tmp = dp[i - 1,j - 1] + dis[i, j];
                        if (tmp < dp[i, j]) {
                            dp[i, j] = tmp;
                            record[i, j] = -1;
                        }

                    }

                    if (i - 1 >= 0)
                    {
                        double tmp = dp[i - 1, j ] + dis[i, j];
                        if (tmp < dp[i, j])
                        {
                            dp[i, j] = tmp;
                            record[i, j] = 0;
                        }

                    }
                    if (j + 1 < overlap) {
                        double tmp = dp[i - 1, j+1] + dis[i, j];
                        if (tmp < dp[i, j])
                        {
                            dp[i, j] = tmp;
                            record[i, j] = 1;
                        }
                    }

                }

            }

            double minr = 1000000;
            int start = 0;
            for (int i = 0; i < overlap; i++) {
                if (dp[kernel - 1, i] < minr) {
                    minr = dp[kernel - 1, i];
                    start = i;
                }
            }

            List<Tuple<int, int>> path=new List<Tuple<int, int>>();
            path.Add(new Tuple<int, int>(kernel - 1, start) );
            start += record[kernel - 1, start];
            for (int i = kernel - 2; i >= 0; i--) {
                path.Add(new Tuple<int, int>(i, start));
                start += record[i, start];
            }
            path.Reverse();

            for (int i = 0; i < path.Count; i++)
            {
                int ii = path[i].Item2;
                ans.Add(new List<Tuple<int, int, int>>());

                for (int j = 0; j <= ii; j++)
                {
                  
          
                    Tuple<int, int, int> pixel = new Tuple<int, int, int>((int)num[i, j, 0],
        (int)num[i, j, 1],
        (int)num[i, j, 2]);
                    ans[i].Add(pixel);


                }


                for (int j = ii + 1; j < overlap; j++)
                {
                    Tuple<int, int, int> pixel = new Tuple<int, int, int>((int)num[i, j+overlap, 0],
(int)num[i, j+overlap, 1],
(int)num[i, j+overlap, 2]);
                    ans[i].Add(pixel);
                }
            }

            return ans;

        }


        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int col, row;
            OpenFileDialog Openfile = new OpenFileDialog();
            Openfile.ShowDialog();
     
                Image<Bgr, Byte> My_Image = new Image<Bgr, byte>(Openfile.FileName);
                pictureBox1.Image = My_Image.ToBitmap();
                col = My_Image.Width;
                row = My_Image.Height;


            int cnt = 0;
            for (int i = 0; i < row; i++) {
                for (int j = 0; j < col; j++) {
       
                    if (j + siz < col && j - siz >= 0 && i - siz >= 0 && i + siz < row) {
                        int n = 0 ;
                        block tmp = new block();
                        tmp.value = new double[105, 105, 3];
                        for (int l = i - siz; l <= i + siz; l++) {
                            int m = 0;
                            for (int r = j - siz; r <= j + siz; r++) {
                              
                              
                                tmp.value[n, m, 0] = My_Image.Data[l, r, 0];
                                tmp.value[n, m, 1] = My_Image.Data[l, r, 1];
                                tmp.value[n, m, 2] = My_Image.Data[l, r, 2];
                                m++;
                            }
                            n++;
                        }

                        tmp.id = cnt++;
                        texture.Add(tmp);

                      
                    }
                
                }
            }



        }

        public int choose(int i,int j,int first) {

            int ans = 0;
            Random rand = new Random();//亂數種子

            if (first == 0)
            {


                if (i == 0 && j == 0)
                {
                    return rand.Next(0, texture.Count);
                }

                double minr = 9999999;
                for (int r = 0; r < texture.Count; r++)
                {
                    double num = 0;
                    if (i - 1 >= 0)
                        num += Cmp(result[i - 1, j], r, 0);
                    if (j - 1 >= 0)
                        num += Cmp(result[i, j - 1], r,1);


                    if (minr > num)
                    {
                        int change = rand.Next(0,10);
                        minr = num;
                  
                            ans = r;
                        

                    }


                }



            }



            return ans;
        }
        public double Cmp2(int block1, int block2, int dir, block curtexture)
        {
            List<double> vect1 = new List<double>();
            List<double> vect2 = new List<double>();

            if (dir == 0)
            {

                for (int i = kernel - overlap; i < kernel; i++)
                {

                    for (int j = 0; j < kernel; j++)
                    {
                        double num = texture[block1].value[i, j, 0] + texture[block1].value[i, j, 1] + texture[block1].value[i, j, 2];
                        num /= 3;
                        vect1.Add(num);
                    }

                }

                for (int i = 0; i < overlap; i++)
                {
                    for (int j = 0; j < kernel; j++)
                    {
                        double num = texture[block2].value[i, j, 0] + texture[block2].value[i, j, 1] + texture[block2].value[i, j, 2];
                        num /= 3;
                        vect2.Add(num);
                    }
                }
            }
            else if (dir == 1)
            {

                for (int i = 0; i < kernel; i++)
                {

                    for (int j = kernel - overlap; j < kernel; j++)
                    {
                        double num = texture[block1].value[i, j, 0] + texture[block1].value[i, j, 1] + texture[block1].value[i, j, 2];
                        num /= 3;
                        vect1.Add(num);
                    }

                }

                for (int i = 0; i < kernel; i++)
                {
                    for (int j = 0; j < overlap; j++)
                    {
                        double num = texture[block2].value[i, j, 0] + texture[block2].value[i, j, 1] + texture[block2].value[i, j, 2];
                        num /= 3;
                        vect2.Add(num);
                    }
                }


            }

            double ans = 0;
            for (int i = 0; i < vect1.Count; i++)
            {
                ans +=Math.Sqrt( Math.Pow(vect1[i] - vect2[i], 2) );

            }

            return ans*alpha;

        }

        public double Cmp3(int block1,  block curtexture)
        {
            List<double> vect1 = new List<double>();
            List<double> vect2 = new List<double>();


                for (int i = 0; i < kernel; i++)
                {

                    for (int j = 0; j < kernel; j++)
                    {
                        double num = texture[block1].value[i, j, 0] + texture[block1].value[i, j, 1] + texture[block1].value[i, j, 2];
                        num /= 3;
                        vect1.Add(num);
                    }

                }

                for (int i = 0; i < kernel; i++)
                {
                    for (int j = 0; j < kernel; j++)
                    {
                        double num = curtexture.value[i, j, 0] + curtexture.value[i, j, 1] + curtexture.value[i, j, 2];
                        num /= 3;
                        vect2.Add(num);
                    }
                }
            


            double ans = 0;
            for (int i = 0; i < vect1.Count; i++)
            {
                ans +=Math.Sqrt( Math.Pow(vect1[i] - vect2[i], 2) );

            }

            return ans*(1-alpha);

        }

        public double Cmp4(int ii,int jj,block curtexture)
        {
            List<double> vect1 = new List<double>();
            List<double> vect2 = new List<double>();


            Bitmap im4 = (Bitmap)pictureBox4.Image;

            Image<Bgr, Byte> My_Image = new Image<Bgr, byte>(im4);

            for (int i = ii*kernel; i < ii+kernel; i++)
            {

                for (int j = jj*kernel; j < jj+kernel; j++)
                {
                    double num = My_Image.Data[i, j, 0] + My_Image.Data[i, j, 1] + My_Image.Data[i, j, 2];
                    num /= 3;
                    vect1.Add(num);
                }

            }

            for (int i = 0; i < kernel; i++)
            {
                for (int j = 0; j < kernel; j++)
                {
                    double num = curtexture.value[i, j, 0] + curtexture.value[i, j, 1] + curtexture.value[i, j, 2];
                    num /= 3;
                    vect2.Add(num);
                }
            }



            double ans = 0;
            for (int i = 0; i < vect1.Count; i++)
            {
                ans += Math.Sqrt(Math.Pow(vect1[i] - vect2[i], 2));

            }

            return ans * (alpha);

        }

        public int choosetexture(int i,int j,block curtexture,int has) {

            int ans=0;

                double minr = 99999999;
            if (has==1) {


                for (int r = 0; r < texture.Count; r++)
                {
                    double num = 0;
                    num += Cmp4(i, j, texture[r]);


                    num += Cmp3(r, curtexture);

                    if (minr > num)
                    {
                        minr = num;

                        ans = r;


                    }


                }

                return ans;




            }


                for (int r = 0; r < texture.Count; r++)
                {
                    double num = 0;
                    if (i - 1 >= 0)
                        num += Cmp2(result[i - 1, j], r, 0,curtexture);
                    if (j - 1 >= 0)
                        num += Cmp2(result[i, j - 1], r, 1,curtexture) ;

                num += Cmp3(r, curtexture);

                    if (minr > num)
                    {
                        minr = num;

                        ans = r;


                    }


                }

            return ans;



        }

        public void showresult(int rowcnt,int colcnt,int p) {

            Image<Bgr, Byte> resultimage = new Image<Bgr, Byte>(600, 600);

            int hei = 0;
            int nextline = kernel - overlap;
            int horheight = horboundary[0,0].Count;
            
            for (int i = 0; i < rowcnt - 1; i++)
            {
                int wid = 0;
                for (int j = 0; j < colcnt - 1; j++)
                {


                    block ans = texture[result[i, j]];
                    //first one remove right others remove left and right
 
                        for (int r = overlap; r < nextline; r++)
                        {

                            for (int k = overlap; k < nextline; k++)
                            {
                                resultimage.Data[hei + r-overlap, wid + k - overlap, 0] = (byte)ans.value[r, k, 0];
                                resultimage.Data[hei + r-overlap, wid + k - overlap, 1] = (byte)ans.value[r, k, 1];
                                resultimage.Data[hei + r-overlap, wid + k - overlap, 2] = (byte)ans.value[r, k, 2];


                            }
                        }
                        wid += nextline-overlap;
                    


                    //pictureBox3.Image = resultimage.ToBitmap();
                    //CvInvoke.WaitKey(10);
                    
                   

                    for (int r = overlap; r < vertboundary[i, j].Count; r++)
                    {

                        for (int r1 = 0; r1 < vertboundary[i, j][0].Count; r1++)
                        {
                            resultimage.Data[hei+ r-overlap, wid+ r1, 0] = (byte)vertboundary[i, j][r][r1].Item1;
                            resultimage.Data[hei + r-overlap, wid + r1, 1] = (byte)vertboundary[i, j][r][r1].Item2;
                            resultimage.Data[hei + r-overlap, wid + r1, 2] = (byte)vertboundary[i, j][r][r1].Item3;
                        }
                    }
                    //pictureBox3.Image = resultimage.ToBitmap();
                    //CvInvoke.WaitKey(10);
                    wid +=overlap;


                    for (int r = overlap; r < horboundary[i, j].Count; r++)
                    {

                        for (int r1 = 0; r1 < horboundary[i, j][0].Count; r1++)
                        {
                            resultimage.Data[hei+ r1+nextline-overlap,  wid + r - nextline-overlap, 0] = (byte)horboundary[i, j][r][r1].Item1;
                            resultimage.Data[hei + r1+nextline-overlap, wid + r - nextline-overlap, 1] = (byte)horboundary[i, j][r][r1].Item2;
                            resultimage.Data[hei + r1+nextline-overlap, wid + r - nextline-overlap, 2] = (byte)horboundary[i, j][r][r1].Item3;

                        }



                    }

                    //pictureBox3.Image = resultimage.ToBitmap();
                    //CvInvoke.WaitKey(10);
                }
                hei += nextline; 
            }
            if (p == 2)
            {

                pictureBox2.Image = resultimage.ToBitmap();
                CvInvoke.WaitKey(10);
            }
            if (p == 3) {
                pictureBox3.Image = resultimage.ToBitmap();
                CvInvoke.WaitKey(10);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            
            for (int i = 0; i < blocknumber; i++) {
                for (int j = 0; j < blocknumber; j++) {

                    int ans=choose(i, j,0);
                    result[i, j] = ans;
                }
            }


            Image<Bgr, Byte> resultimage = new Image<Bgr, Byte>(600, 600);
            for (int i = 0; i < blocknumber; i++)
            {
                int k = 0;
                for (int j = 0; j < blocknumber; j++)
                {
                    block tmp = texture[result[i, j] ];


                    for (int r = 0; r < kernel; r++) {
                       
                        for (int t = 0; t < kernel; t++) {

                            resultimage.Data[i * kernel + r, k+t, 0] = (byte)tmp.value[r, t, 0];
                            resultimage.Data[i * kernel + r, k+t, 1] = (byte)tmp.value[r, t, 1];
                            resultimage.Data[i * kernel + r, k+t, 2] = (byte)tmp.value[r, t, 2];
                            



                        }
                       
                    }
                    k += kernel;
                }
            }

            pictureBox2.Image = resultimage.ToBitmap();
            CvInvoke.WaitKey(10);
         
            for (int i = 0; i < blocknumber - 1; i++) {
                for (int j = 0; j < blocknumber - 1; j++) {

                    horboundary[i, j] = solvehorpath(result[i, j], result[i+1, j ]);
                    vertboundary[i, j] = solvevertpath(result[i, j], result[i, j + 1]);

                }
            }

            showresult(blocknumber,blocknumber,2);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            int col, row;
            OpenFileDialog Openfile = new OpenFileDialog();
            Openfile.ShowDialog();

            Image<Bgr, Byte> My_Image = new Image<Bgr, byte>(Openfile.FileName);
            pictureBox4.Image = My_Image.ToBitmap();
            col = My_Image.Width;
            row = My_Image.Height;


            int cnt = 0;
            for (int i = 0; i+kernel < row; i+=kernel)
            {
                for (int j = 0; j+kernel < col; j+=kernel)
                {
                    int n = 0;
                    block tmp = new block();
                    tmp.value = new double[105, 105, 3];
                    for (int l = i ; l <i+kernel; l++)
                    {
                        int m = 0;
                        for (int r = j ; r < j+kernel; r++)
                        {


                            tmp.value[n, m, 0] = My_Image.Data[l, r, 0];
                            tmp.value[n, m, 1] = My_Image.Data[l, r, 1];
                            tmp.value[n, m, 2] = My_Image.Data[l, r, 2];
                            m++;
                        }
                        n++;
                    }
                    result[i/kernel,j/kernel]= choosetexture(i / kernel, j / kernel,tmp,0);

                }
            }
            for (int i = 0; i < row/kernel - 1; i++)
            {
                for (int j = 0; j < col/kernel- 1; j++)
                {

                    horboundary[i, j] = solvehorpath(result[i, j], result[i + 1, j]);
                    vertboundary[i, j] = solvevertpath(result[i, j], result[i, j + 1]);

                }
            }

            showresult(row/kernel,col/kernel,3);


        }

        private void button5_Click(object sender, EventArgs e)
        {
            int col, row,col1,row1;
            Bitmap im4 = (Bitmap)pictureBox4.Image;

            Bitmap im1 = (Bitmap)pictureBox1.Image;
            Image<Bgr, Byte> My_Image =new Image<Bgr, byte>(im4);
   
            pictureBox4.Image = My_Image.ToBitmap();
            col = My_Image.Width;
            row = My_Image.Height;
            alpha = 0.8 * (round - 1) / (2) + 0.1;
            siz /= 3;

            Image<Bgr, Byte> My_Image1 = new Image<Bgr, byte>(im1);
            pictureBox1.Image = My_Image.ToBitmap();
            col1 = My_Image1.Width;
            row1 = My_Image1.Height;

            texture.Clear();
            int cnt = 0;
            for (int i = 0; i < row1; i++)
            {
                for (int j = 0; j < col1; j++)
                {

                    if (j + siz < col1 && j - siz >= 0 && i - siz >= 0 && i + siz < row1)
                    {
                        int n = 0;
                        block tmp = new block();
                        tmp.value = new double[105, 105, 3];
                        for (int l = i - siz; l <=i + siz; l++)
                        {
                            int m = 0;
                            for (int r = j - siz; r <= j + siz; r++)
                            {


                                tmp.value[n, m, 0] = My_Image1.Data[l, r, 0];
                                tmp.value[n, m, 1] = My_Image1.Data[l, r, 1];
                                tmp.value[n, m, 2] = My_Image1.Data[l, r, 2];
                                m++;
                            }
                            n++;
                        }

                        tmp.id = cnt++;
                        texture.Add(tmp);


                    }

                }
            }

            cnt = 0;
            for (int i = 0; i + kernel < row; i += kernel)
            {
                for (int j = 0; j + kernel < col; j += kernel)
                {
                    int n = 0;
                    block tmp = new block();
                    tmp.value = new double[105, 105, 3];
                    for (int l = i; l < i + kernel; l++)
                    {
                        int m = 0;
                        for (int r = j; r < j + kernel; r++)
                        {


                            tmp.value[n, m, 0] = My_Image.Data[l, r, 0];
                            tmp.value[n, m, 1] = My_Image.Data[l, r, 1];
                            tmp.value[n, m, 2] = My_Image.Data[l, r, 2];
                            m++;
                        }
                        n++;
                    }
                    result[i / kernel, j / kernel] = choosetexture(i / kernel, j / kernel, tmp,1);

                }
            }
            for (int i = 0; i < row / kernel - 1; i++)
            {
                for (int j = 0; j < col / kernel - 1; j++)
                {

                    horboundary[i, j] = solvehorpath(result[i, j], result[i + 1, j]);
                    vertboundary[i, j] = solvevertpath(result[i, j], result[i, j + 1]);

                }
            }
            
            showresult(row / kernel, col / kernel,3);


        }
    }
}
