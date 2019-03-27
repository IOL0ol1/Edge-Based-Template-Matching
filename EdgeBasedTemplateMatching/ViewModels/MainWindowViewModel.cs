using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;

namespace EdgeBasedTemplateMatching.ViewModels
{
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using Microsoft.Win32;
    using Prism.Commands;
    using Prism.Mvvm;

    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            LoadCommand = new DelegateCommand<string>(LoadExecute);
            TemplateCommand = new DelegateCommand(TemplateExecute);
            MADCommand = new DelegateCommand(() => { }, () => false); // Not implemented
            SADCommand = new DelegateCommand(() => { }, () => false); // Not implemented
            SSDCommand = new DelegateCommand(() => { }, () => false); // Not implemented
            MSDCommand = new DelegateCommand(() => { }, () => false); // Not implemented
            NCCCommand = new DelegateCommand(NCCExecute);
            SSDACommand = new DelegateCommand(() => { }, () => false); // Not implemented
            SATDCommand = new DelegateCommand(() => { }, () => false); // Not implemented
        }

        private string _title = "Edge Base Template Matching";

        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        public ICommand LoadCommand { get; private set; }
        public ICommand TemplateCommand { get; private set; }
        public ICommand MADCommand { get; private set; }
        public ICommand SADCommand { get; private set; }
        public ICommand SSDCommand { get; private set; }
        public ICommand MSDCommand { get; private set; }
        public ICommand NCCCommand { get; private set; }
        public ICommand SSDACommand { get; private set; }
        public ICommand SATDCommand { get; private set; }

        private Mat template;

        /// <summary>
        /// Image used to create template
        /// </summary>
        public Mat Template
        {
            get { return template; }
            set
            {
                template = value;
                RaisePropertyChanged();
            }
        }

        private Mat destination;

        /// <summary>
        /// Image to be processed
        /// </summary>
        public Mat Destination
        {
            get { return destination; }
            set
            {
                destination = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Load template and destination image
        /// </summary>
        /// <param name="i"></param>
        private void LoadExecute(string i)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "JPG|*.jpg|PNG|*.png|BMP|*.bmp|All|*.*";
            if (dialog.ShowDialog().Value == true)
            {
                if (dialog.CheckFileExists)
                {
                    string file = dialog.FileName;
                    if (i.ToLower().Contains("t"))
                        Template = new Mat(file);
                    else
                        Destination = new Mat(file);
                }
            }
        }

        /// <summary>
        /// Edge points
        /// </summary>
        private VectorOfVectorOfPoint contoursRelative = new VectorOfVectorOfPoint();

        /// <summary>
        /// Gradient information on edge points
        /// </summary>
        private List<PointInfo[]> contoursInfo = new List<PointInfo[]>();

        private long contoursLength = 0;

        /// <summary>
        /// Create a template
        /// </summary>
        private void TemplateExecute()
        {
            try
            {
                contoursRelative = new VectorOfVectorOfPoint();
                contoursInfo = new List<PointInfo[]>();
                contoursLength = 0;

                /// clone origin image
                Mat src = template.Clone();

                /// convert to gray image
                Mat grayImage = new Mat();
                CvInvoke.CvtColor(src, grayImage, ColorConversion.Rgb2Gray);

                /// using the canny algorithm to get edges
                Mat output = new Mat();
                Mat hierarchy = new Mat();
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.Canny(grayImage, output, 100, 800);
                CvInvoke.FindContours(output, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxNone);

                /// use the sobel filter on the template image which returns the gradients in the X (Gx) and Y (Gy) direction.
                Mat gx = new Mat();
                Mat gy = new Mat();
                CvInvoke.Sobel(grayImage, gx, DepthType.Cv64F, 1, 0);
                CvInvoke.Sobel(grayImage, gy, DepthType.Cv64F, 0, 1);

                /// compute the magnitude and direction
                Mat magnitude = new Mat();
                Mat direction = new Mat();
                CvInvoke.CartToPolar(gx, gy, magnitude, direction);

                /// save edge info
                var _gx = gx.ToImage<Gray, double>();
                var _gy = gy.ToImage<Gray, double>();
                var _magnitude = magnitude.ToImage<Gray, double>();

                double magnitudeTemp = 0;
                PointInfo pointInfo = new PointInfo();
                int originx = contours[0][0].X;
                int originy = contours[0][0].Y;
                for (int i = 0, m = contours.Size; i < m; i++)
                {
                    int n = contours[i].Size;
                    contoursLength += n;
                    contoursInfo.Add(new PointInfo[n]);
                    System.Drawing.Point[] points = new System.Drawing.Point[n];

                    for (int j = 0; j < n; j++)
                    {
                        int x = contours[i][j].X;
                        int y = contours[i][j].Y;
                        points[j].X = x - originx;
                        points[j].Y = y - originy;
                        pointInfo.DerivativeX = _gx.Data[y, x, 0];
                        pointInfo.DerivativeY = _gy.Data[y, x, 0];
                        magnitudeTemp = _magnitude.Data[y, x, 0];
                        pointInfo.Magnitude = magnitudeTemp;
                        if (magnitudeTemp != 0)
                            pointInfo.MagnitudeN = 1 / magnitudeTemp;
                        contoursInfo[i][j] = pointInfo;
                    }
                    contoursRelative.Push(new VectorOfPoint(points));
                }

                /// overlay display origin image, edge(green) and origin point(red)
                /// NOTE: origin point is the first edge point
                CvInvoke.DrawContours(src, contours, -1, new Bgr(System.Drawing.Color.Green).MCvScalar, 5);
                var point = new System.Drawing.Point(contours[0][0].X, contours[0][0].Y);
                CvInvoke.Circle(src, point, 2, new Bgr(System.Drawing.Color.Red).MCvScalar, -1);
                gx.Dispose();
                _gx.Dispose();
                gy.Dispose();
                _gy.Dispose();
                magnitude.Dispose();
                _magnitude.Dispose();
                direction.Dispose();
                Template.Dispose();
                Template = src;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Trace.TraceError(ex.StackTrace);
            }
        }

        #region NCC

        private double minScore = 0.8;
        private double greediness = 0.8;

        /// <summary>
        /// NCC to find template
        /// </summary>
        private void NCCExecute()
        {
            try
            {
                Trace.TraceInformation("NCC matching start");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Mat dst = destination.Clone();

                /// convert to gray image
                Mat grayImage = new Mat();
                CvInvoke.CvtColor(dst, grayImage, ColorConversion.Rgb2Gray);

                /// use the sobel filter on the source image which returns the gradients in the X (Gx) and Y (Gy) direction.
                Mat gx = new Mat();
                Mat gy = new Mat();
                CvInvoke.Sobel(grayImage, gx, DepthType.Cv64F, 1, 0);
                CvInvoke.Sobel(grayImage, gy, DepthType.Cv64F, 0, 1);

                /// compute the magnitude and direction
                Mat magnitude = new Mat();
                Mat direction = new Mat();
                CvInvoke.CartToPolar(gx, gy, magnitude, direction);

                /// template matching
                var _gx = gx.ToImage<Gray, double>();
                var _gy = gy.ToImage<Gray, double>();
                var _magnitude = magnitude.ToImage<Gray, double>();

                long totalLength = contoursLength;
                double nMinScore = minScore / totalLength; // normalized min score
                double nGreediness = (1 - greediness * minScore) / (1 - greediness) / totalLength;

                double partialScore = 0;
                double resultScore = 0;
                int resultX = 0;
                int resultY = 0;
                for (int i = 0, h = grayImage.Height; i < h; i++)
                {
                    for (int j = 0, w = grayImage.Width; j < w; j++)
                    {
                        double sum = 0;
                        long num = 0;
                        for (int m = 0, rank = contoursRelative.Size; m < rank; m++)
                        {
                            for (int n = 0, length = contoursRelative[m].Size; n < length; n++)
                            {
                                num += 1;
                                int curX = j + contoursRelative[m][n].X;
                                int curY = i + contoursRelative[m][n].Y;
                                if (curX < 0 || curY < 0 || curX > grayImage.Width - 1 || curY > grayImage.Height - 1)
                                    continue;

                                double sdx = _gx.Data[curY, curX, 0];
                                double sdy = _gy.Data[curY, curX, 0];
                                double tdx = contoursInfo[m][n].DerivativeX;
                                double tdy = contoursInfo[m][n].DerivativeY;

                                if ((sdy != 0 || sdx != 0) && (tdx != 0 || tdy != 0))
                                {
                                    double nMagnitude = _magnitude.Data[curY, curX, 0];
                                    if (nMagnitude != 0)
                                        sum += (sdx * tdx + sdy * tdy) * contoursInfo[m][n].MagnitudeN / nMagnitude;
                                }
                                partialScore = sum / num;
#if FAST
                                if (partialScore < minScore)
#else
                                if (partialScore < Math.Min((minScore - 1) + (nGreediness * num), nMinScore * num))
#endif
                                    break;
                            }
                        }
                        if (partialScore > resultScore)
                        {
                            resultScore = partialScore;
                            resultX = j;
                            resultY = i;
                            Trace.TraceInformation($"Current Score : {resultScore}");
                        }
                    }
                }

                Trace.TraceInformation($"Score : {resultScore}");

                /// overlay display origin image, edge(green) and origin point(red)
                /// NOTE: origin point is the first edge point
                var point = new System.Drawing.Point(resultX, resultY);
                Trace.TraceInformation($"Point : {point}");
                CvInvoke.DrawContours(dst, contoursRelative, -1, new Bgr(System.Drawing.Color.Green).MCvScalar, 5, offset: point);
                CvInvoke.Circle(dst, point, 2, new Bgr(System.Drawing.Color.Red).MCvScalar, -1);
                grayImage.Dispose();
                Destination.Dispose();
                Destination = dst;

                Trace.TraceInformation($"NCC matching end. time: {(double)stopwatch.ElapsedTicks * 1000 / Stopwatch.Frequency} ms");
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                Trace.TraceError(ex.StackTrace);
            }
        }

        #endregion NCC
    }

    /// <summary>
    /// Gradient information corresponding to a point
    /// ----→  a
    /// |\ ）m
    /// | \
    /// |  \
    /// ↓   \
    /// b    c
    ///
    /// DerivativeX = a
    /// DerivativeY = b
    /// Magnitude = c = √(a²+b²)
    /// Direction = the direction of c (not currently in use)
    /// </summary>
    public struct PointInfo
    {
        public double Direction;
        public double Magnitude;

        /// <summary>
        /// normalization Magnitude = 1/Magnitude
        /// </summary>
        public double MagnitudeN;

        public double DerivativeX; // X-axis Gradient
        public double DerivativeY; // Y-axis Gradient
    }
}